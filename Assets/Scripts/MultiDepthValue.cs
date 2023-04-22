using UnityEngine;
using UnityEngine.Rendering;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.RilMsg;


[RequireComponent(typeof(Camera))]
public class MultiDepthValue : MonoBehaviour
{
    [Header("Shader Setup")]
    public Shader uberReplacementShader;

    [Header("ROS Setup")]
    public string rosServiceName;
    ROSConnection ros; 

    [Header("Multi-Camera Setup")]
    public int AgentNum;
    public static int StaticAgentNum;
    public string cameraname;

    List<Camera> camera = new List<Camera>();

    void Start()
    {
        int cnt=AgentNum;
        StaticAgentNum = cnt;
        ros = ROSConnection.GetOrCreateInstance();
        ros.ImplementService<PixelValueServiceRequest, PixelValueServiceResponse>(rosServiceName, UpdatePixel);
        // default fallbacks, if shaders are unspecified
        if (!uberReplacementShader)
            uberReplacementShader = Shader.Find("Hidden/UberReplacement");
        for (int idx=0; idx<AgentNum; idx++)
        { 
            string cameraname = string.Format("Group{0}/Agent{1}/DepthCamera", idx/100+1, idx+1);
            Debug.Log(cameraname);
            Camera cameraobject = GameObject.Find(cameraname).GetComponent<Camera>();
            camera.Add(cameraobject);
            cnt+=1;
        }
        OnCameraChange();
        OnSceneChange();
    }
    void Update()
    {
        // If you want to make faster interactive in Unity 
        // Time.timeScale = 100.0F;
        // Time.fixedDeltaTime = 0.02F * Time.timeScale; 
    }
    void LateUpdate()
    {
        OnSceneChange();
        OnCameraChange();
        
    }

    static private void SetupCameraWithReplacementShader(List<Camera> camera, Shader shader, ReplacementMode mode)
    {
        SetupCameraWithReplacementShader(camera, shader, mode, Color.black);
    }

    static private void SetupCameraWithReplacementShader(List<Camera> camera, Shader shader, ReplacementMode mode, Color clearColor)
    {
        for (int idx=0; idx<StaticAgentNum; idx++)
        {
            var newcam = camera[idx];
            var cb = new CommandBuffer();
            cb.SetGlobalFloat("_OutputMode", (int)mode);
            newcam.AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
            newcam.AddCommandBuffer(CameraEvent.BeforeFinalPass, cb);
            newcam.SetReplacementShader(shader, "");
            newcam.backgroundColor = clearColor;
            newcam.clearFlags = CameraClearFlags.Depth;
            newcam.allowHDR = false;
            newcam.allowMSAA = false;            
        }
    }

    public enum ReplacementMode
    {
        ObjectId = 0,
        CatergoryId = 1,
        DepthCompressed = 2,
        DepthMultichannel = 3,
        Normals = 4
    };

    public void OnCameraChange()
    {
        for (int idx=0; idx<AgentNum; idx++)
        {
            var newcam = camera[idx].GetComponent<Camera>();
            newcam.RemoveAllCommandBuffers();
        }   

        SetupCameraWithReplacementShader(camera, uberReplacementShader, ReplacementMode.DepthCompressed, Color.white);        
        Resources.UnloadUnusedAssets();
    }

    public void OnSceneChange()
    {
        var renderers = Object.FindObjectsOfType<Renderer>();
        var mpb = new MaterialPropertyBlock();
        for (int idx=0; idx<renderers.Length; idx++)
        {
            var objectID = renderers[idx].gameObject.GetInstanceID();
            mpb.SetColor("_ObjectColor", ColorEncoding.EncodeIDAsColor(objectID));
            renderers[idx].SetPropertyBlock(mpb);
        }        
        renderers = null;
        Resources.UnloadUnusedAssets();
    }

    private PixelValueServiceResponse UpdatePixel(PixelValueServiceRequest request)
    {
        var width = 320; //128
        var height = 180; // 72
        List<float[]> pixelLists = new List<float[]>();
        float[] pixel = new float[width*height*AgentNum];
        PixelValueServiceResponse pixelValueResponse = new PixelValueServiceResponse();
        int count = 1; 
        for (var idx=0; idx<AgentNum; idx++)
        {
            var pixels = ExtractPixel(camera[idx], width, height);
            count+=1; 
            pixelLists.Add(pixels);
            Debug.Log(idx);
        }

        int index=0;
        int NoOfRows = AgentNum;
        int NoOfColumns = width*height;
        for (int y=0; y<NoOfColumns; y++)
        {
            for (int x=0; x<NoOfRows; x++)
            {
                pixel[index] = pixelLists[x][y];
                index++;
            }
        }
        pixelValueResponse.pixel_value = pixel;
        pixel=null;
        pixelLists=null;
        Resources.UnloadUnusedAssets();

        return pixelValueResponse;
        
    }

    private float[] ExtractPixel(Camera cam, int width, int height)
    {
        var depth        = 24;
        var format       = RenderTextureFormat.Default;
        var readWrite    = RenderTextureReadWrite.Default;
        var antiAliasing = Mathf.Max(1, QualitySettings.antiAliasing);

        var finalRT      = RenderTexture.GetTemporary(width, height, depth, format, readWrite, antiAliasing);
        var renderRT     = finalRT;
        var tex          = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.hideFlags    = HideFlags.HideAndDontSave;

        var prevActiveRT = RenderTexture.active;
        var prevCameraRT = cam.targetTexture;
        float[] pixelList= new float[width*height];
        // render to offscreen texture (readonly from CPU side)
        RenderTexture.active = renderRT;
        cam.targetTexture = renderRT;
        cam.Render();

        // read offsreen texture contents into the CPU readable texture
        tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);
        tex.Apply();
        pixelList = DumpToList(tex);
        Texture2D.DestroyImmediate(tex, true);

        cam.targetTexture = prevCameraRT;
        RenderTexture.active = prevActiveRT;
        // Try reducing memory leakage
        RenderTexture.ReleaseTemporary(finalRT);
        RenderTexture.ReleaseTemporary(renderRT);

        Resources.UnloadUnusedAssets();

        return pixelList;
    }

    private static float[] DumpToList(Texture2D pixelVal)
    {
        int width  = 320; //1280 
        int height = 180; //720
        float [] pixelList = new float[width*height];
        pixelVal.hideFlags = HideFlags.HideAndDontSave;
        int count = 0; 
        for (var rowidx=0; rowidx<width; rowidx++)
        {
            for (var colidx=0; colidx<height; colidx++)
            {
                var rgbaVal = pixelVal.GetPixel(rowidx, colidx);
                pixelList[count]=rgbaVal.grayscale; 
                rgbaVal = Color.clear;
                count+=1;
            }
        } 

        Texture2D.DestroyImmediate(pixelVal, true);
        Resources.UnloadUnusedAssets();
        return pixelList;

    }

}
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
public class MultiSemanticValue : MonoBehaviour
{
    [Header("Shader Setup")]
    public Shader SegmentationShader;

    ROSConnection ros; 
    [Header("ROS Setup")]
    public string rosServiceName;

    [Header("Multi-Camera Setup")]
    public Camera[] camera;
    

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.ImplementService<PixelValueServiceRequest, PixelValueServiceResponse>(rosServiceName, UpdatePixel);
        // default fallbacks, if shaders are unspecified
        if (!SegmentationShader)
            SegmentationShader = Shader.Find("Hidden/SegmentationShader");

        OnCameraChange();
        OnSceneChange();
    }
    void Update()
    {
        Time.timeScale = 100.0F;
        Time.fixedDeltaTime = 0.02F * Time.timeScale; 
    }
    void LateUpdate()
    {
        OnSceneChange();
        OnCameraChange();
    }

    static private void SetupCameraWithReplacementShader(Camera[] camera, Shader shader)
    {
        SetupCameraWithReplacementShader(camera, shader, Color.black);
    }

    static private void SetupCameraWithReplacementShader(Camera[] camera, Shader shader, Color clearColor)
    {
        for (int idx=0; idx<camera.Length; idx++)
        {
            var newcam = camera[idx].GetComponent<Camera>();
            newcam.SetReplacementShader(shader, "");
            newcam.backgroundColor = clearColor;
            newcam.clearFlags = CameraClearFlags.SolidColor;
            newcam.allowHDR = false;
            newcam.allowMSAA = false;            
        }        
        // foreach (var cam in camera)
        // {
        //     var newcam = cam.GetComponent<Camera>();
        //     newcam.SetReplacementShader(shader, "");
        //     newcam.backgroundColor = Color.black;
        //     newcam.clearFlags = CameraClearFlags.Color;
        //     newcam.allowHDR = false;
        //     newcam.allowMSAA = false;
        // }

    }

    public void OnCameraChange()
    {
        for (int idx=0; idx<camera.Length; idx++)
        {
            var newcam = camera[idx].GetComponent<Camera>();
            newcam.RemoveAllCommandBuffers();
        }
        // foreach (var cam in camera)
        // {
        //     var newcam = cam.GetComponent<Camera>();
        //     newcam.RemoveAllCommandBuffers();
        //     newcam.targetDisplay = targetDisplay++;
        // }      
        SetupCameraWithReplacementShader(camera, SegmentationShader);
        // setup command buffers and replacement shaders
        Resources.UnloadUnusedAssets();
    }


    public void OnSceneChange()
    {
        var renderers = Object.FindObjectsOfType<Renderer>();
        var mpb = new MaterialPropertyBlock();
        for (int idx=0; idx<renderers.Length; idx++)
        {
            mpb.SetColor ("_ObjectColor", TagsManager.GetColor (renderers[idx].gameObject.tag));
            renderers[idx].SetPropertyBlock(mpb);
        }    
        // foreach (var r in renderers)
        // {
        //     var tag = r.gameObject.tag;
        //     mpb.SetColor ("_ObjectColor", TagsManager.GetColor (tag));
        //     r.SetPropertyBlock(mpb);
            
        // }
        renderers = null;
        // System.GC.Collect();
        Resources.UnloadUnusedAssets();
    }

    private PixelValueServiceResponse UpdatePixel(PixelValueServiceRequest request)
    {
        var width = 180;
        var height = 100;
        List<float[]> pixelLists = new List<float[]>();
        float[] pixel = new float[width*height*camera.Length];
        // float[] pixel = new float[3*width*height*camera.Length];
        PixelValueServiceResponse pixelValueResponse = new PixelValueServiceResponse();
        int count = 1; 
        for (var idx=0; idx<camera.Length; idx++)
        {
            var pixels = ExtractPixel(camera[idx], width, height);
            count+=1; 
            pixelLists.Add(pixels);
        }
        // foreach (var singleCamera in camera)
        // {
        //     var pixels = ExtractPixel(singleCamera, width, height);
        //     count+=1; 
        //     pixelLists.Add(pixels);
        // }
        int index=0;
        int NoOfRows = camera.Length;
        int NoOfColumns = width*height;
        // int NoOfColumns = 3*width*height;
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
        // System.GC.Collect();
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
        // float[] pixelList= new float[3*width*height];
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
        // System.GC.Collect();

        return pixelList;
    }

    private static float[] DumpToList(Texture2D pixelVal)
    {
        int width  = 180; 
        int height = 100;
        float [] pixelList = new float[width*height];
        // float [] pixelList = new float[width*height*3];
        pixelVal.hideFlags = HideFlags.HideAndDontSave;
        int count = 0; 
        for (var rowidx=0; rowidx<width; rowidx++)
        {
            for (var colidx=0; colidx<height; colidx++)
            {
                var rgbaVal = pixelVal.GetPixel(rowidx, colidx);
                pixelList[count]=rgbaVal.r;
                count+=1;
                // pixelList[count]=rgbaVal.b; 
                // pixelList[count+1]=rgbaVal.g;
                // pixelList[count+2]=rgbaVal.r;
                // count+=3;
                rgbaVal = Color.clear;
            }
        } 

        Texture2D.DestroyImmediate(pixelVal, true);
        Resources.UnloadUnusedAssets();
        return pixelList;

    }
}
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
public class GetSegmentation : MonoBehaviour
{
    [Header("Shader Setup")]
    public Shader SegmentationShader;

    ROSConnection ros; 
    [Header("ROS Setup")]
    public string rosServiceName;

    // pass configuration
    private CapturePass[] capturePasses = new CapturePass[] {
        new CapturePass() { name = "semanticCapture", supportsAntialiasing = true },
    };

    struct CapturePass
    {
        // configuration
        public string name;
        public bool supportsAntialiasing;
        public bool needsRescale;
        public CapturePass(string name_) { name = name_; supportsAntialiasing = true; needsRescale = false; camera = null; }
        public Camera camera;
    };

    // cached materials
    private Material opticalFlowMaterial;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.ImplementService<PixelValueRequest, PixelValueResponse>(rosServiceName, UpdatePixel);
        // default fallbacks, if shaders are unspecified
        if (!SegmentationShader)
            SegmentationShader = Shader.Find("Hidden/SegmentationShader");

        // use real camera to capture final image
        capturePasses[0].camera = GetComponent<Camera>();
        for (int q = 1; q < capturePasses.Length; q++)
            capturePasses[q].camera = CreateHiddenCamera(capturePasses[q].name);
        OnCameraChange();
        OnSceneChange();
    }

    void LateUpdate()
    {
        OnSceneChange();
        OnCameraChange();
    }

    private Camera CreateHiddenCamera(string name)
    {
        var go              = new GameObject(name, typeof(Camera));
        go.hideFlags        = HideFlags.HideAndDontSave;
        go.transform.parent = transform;
        var newCamera = go.GetComponent<Camera>();
        return newCamera;
    }


    static private void SetupCameraWithReplacementShader(Camera cam, Shader shader)
    {
        SetupCameraWithReplacementShader(cam, shader, Color.black);
    }

    static private void SetupCameraWithReplacementShader(Camera cam, Shader shader, Color clearColor)
    {
        cam.SetReplacementShader(shader, "");
        cam.backgroundColor = Color.black;
        cam.clearFlags = CameraClearFlags.Color;
        cam.allowHDR = false;
        cam.allowMSAA = false;
    }


    public enum ReplacementMode
    {
        CatergoryId = 1,
    };

    public void OnCameraChange()
    {
        int targetDisplay = 1;
        var mainCamera = GetComponent<Camera>();
        SetupCameraWithReplacementShader(mainCamera, SegmentationShader);
        // setup command buffers and replacement shaders
        Resources.UnloadUnusedAssets();
    }


    public void OnSceneChange()
    {
        var renderers = Object.FindObjectsOfType<Renderer>();
        var mpb = new MaterialPropertyBlock();
        foreach (var r in renderers)
        {
            var tag = r.gameObject.tag;
            mpb.SetColor ("_ObjectColor", TagsManager.GetColor (tag));
            r.SetPropertyBlock(mpb);
            
        }
        renderers = null;
        System.GC.Collect();
        Resources.UnloadUnusedAssets();
    }

    private PixelValueResponse UpdatePixel(PixelValueRequest request)
    {
        var width = 1280;
        var height = 720;
        float[] pixel = new float[3*width*height];

        PixelValueResponse pixelValueResponse = new PixelValueResponse();
        foreach (var pass in capturePasses)
        {
            if (pass.name == "semanticCapture" )
            {
                pixel = ExtractPixel(pass.camera, width, height, pass.supportsAntialiasing, pass.needsRescale);
            }
        }
        pixelValueResponse.pixel_value = pixel;
        pixel=null;
        System.GC.Collect();
        Resources.UnloadUnusedAssets();

        return pixelValueResponse;
        
    }

    private float[] ExtractPixel(Camera cam, int width, int height, bool supportsAntialiasing, bool needsRescale)
    {
        var mainCamera   = GetComponent<Camera>();
        var depth        = 24;
        var format       = RenderTextureFormat.Default;
        var readWrite    = RenderTextureReadWrite.Default;
        var antiAliasing = (supportsAntialiasing) ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;

        var finalRT      = RenderTexture.GetTemporary(width, height, depth, format, readWrite, antiAliasing);
        var renderRT     = (!needsRescale) ? finalRT :RenderTexture.GetTemporary(mainCamera.pixelWidth, mainCamera.pixelHeight, depth, format, readWrite, antiAliasing);
        var tex          = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.hideFlags    = HideFlags.HideAndDontSave;

        var prevActiveRT = RenderTexture.active;
        var prevCameraRT = cam.targetTexture;
        float[] pixelList= new float[3*width*height];
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
        System.GC.Collect();

        return pixelList;
    }

    private static float[] DumpToList(Texture2D pixelVal)
    {
        int width  = 1280; 
        int height = 720;
        float [] pixelList = new float[width*height*3];
        pixelVal.hideFlags = HideFlags.HideAndDontSave;

        int count = 0; 
        for (var rowidx=0; rowidx<width; rowidx++)
        {
            for (var colidx=0; colidx<height; colidx++)
            {
                var rgbaVal = pixelVal.GetPixel(rowidx, colidx);
                pixelList[count]=rgbaVal.b; 
                pixelList[count+1]=rgbaVal.g;
                pixelList[count+2]=rgbaVal.r;
                count+=3;
                rgbaVal = Color.clear;
            }
        } 

        Texture2D.DestroyImmediate(pixelVal, true);
        Resources.UnloadUnusedAssets();
        return pixelList;

    }
}
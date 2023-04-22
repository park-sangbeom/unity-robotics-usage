using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Windows;
using UnityEngine;
using UnityEngine.Rendering;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.RilMsg;


[RequireComponent(typeof(Camera))]
public class SegmentationScript : MonoBehaviour 
{
	[Header("ROS Service")]
    ROSConnection ros; 
    public string rosServiceName;	

	[SerializeField, HideInInspector]
	public Shader shader=null;

	[SerializeField, HideInInspector]
	private RenderTexture m_renderTexture=null;

	[SerializeField, HideInInspector]
	private bool m_enable = true;

	[SerializeField, HideInInspector]
	private KeyCode m_onoffkey = KeyCode.None;


	private GameObject m_dummy = null;
	private Camera m_maincam;
	private Camera m_dummycam = null;
	private MaterialPropertyBlock m_propertyBlock = null;
	


	private CapturePass[] capturePasses = new CapturePass[] {
		new CapturePass() { name = "segmentation" },
	};

    struct CapturePass
    {
        // configuration
        public string name;
        public bool supportsAntialiasing;
        public bool needsRescale;
        public CapturePass(string name_) { name = name_; supportsAntialiasing = true; needsRescale = false; camera = null; }

        // impl
        public Camera camera;
    };

	public RenderTexture renderTexture
	{
		get{ return m_renderTexture; }
		set{ 
			m_renderTexture = value; 
			if(m_dummycam != null)
				m_dummycam.targetTexture = m_renderTexture; 
		}
	}

	public bool enable
	{
		get{ return m_enable; }
		set{ 
			m_enable = value; 
			if(m_dummycam != null)
			{
				m_dummy.SetActive(enable);
				m_dummycam.enabled = enable;
			}
		}
	}

	public KeyCode enablekey
	{
		get {return m_onoffkey; }
		set {m_onoffkey = value; }
	}



	void CreateDummyCamera(){

		m_maincam = this.GetComponent<Camera>();

		if (m_dummy == null)
		{
			m_dummy = new GameObject ();
			m_dummy.name = "HiddenSegmentationCamera";
			m_dummy.transform.SetParent (this.transform);
			m_dummy.transform.localPosition = Vector3.zero;
			m_dummy.transform.localRotation = Quaternion.identity;
			m_dummy.transform.localScale = Vector3.one;

			m_dummy.hideFlags = HideFlags.HideInHierarchy;
		}
		
		if (m_dummycam == null)
		{
			m_dummycam = m_dummy.AddComponent<Camera> ();

			m_dummycam.cullingMask = m_maincam.cullingMask;
			m_dummycam.aspect = m_maincam.aspect;
			m_dummycam.nearClipPlane = m_maincam.nearClipPlane;
			m_dummycam.farClipPlane = m_maincam.farClipPlane;
			m_dummycam.fieldOfView = m_maincam.fieldOfView;
			m_dummycam.rect = m_maincam.rect;
			m_dummycam.depth = m_maincam.depth + 1;
			m_dummycam.clearFlags = CameraClearFlags.Color;
			m_dummycam.backgroundColor = Color.black;
			m_dummycam.targetTexture = m_renderTexture;
		}
	}

	// Use this for initialization
	void Start () {

		// setting shader
		if(shader == null)
			shader = Shader.Find ("Hidden/SegmentationShader");

		// create dummy camera
		CreateDummyCamera ();

		// set segmentation shader as replacement shader
		m_dummycam.SetReplacementShader (shader, "");

		// initialize property block
		m_propertyBlock = new MaterialPropertyBlock();

		UpdateMaterialPropertyBlock ();
		// Add for MemoryLeakage 
		Resources.UnloadUnusedAssets();
        ros = ROSConnection.GetOrCreateInstance();
        ros.ImplementService<PixelValueRequest, PixelValueResponse>(rosServiceName, UpdatePixel);
	}

	void UpdateMaterialPropertyBlock(){
		var renderers = GameObject.FindObjectsOfType<Renderer> ();

		foreach (var r in renderers) {
			var tag = r.gameObject.tag;
			m_propertyBlock.SetColor ("_ObjectColor", TagsManager.GetColor (tag));
			r.SetPropertyBlock (m_propertyBlock);
		}
		// Add for MemoryLeakage 
		renderers = null;
		m_propertyBlock.Clear();
		System.GC.Collect();
		Resources.UnloadUnusedAssets();
	}
	
	// Update is called once per frame
	void Update () {
		if(enable)
			UpdateMaterialPropertyBlock ();

		if(Input.GetKeyDown(m_onoffkey))
		{
			enable = !enable;
			if (enable == true)
			{
				Debug.Log("Enable segmentation shader");
			}
			else
			{
				Debug.Log("Disable segmentation shader");
			}
		}

	}

	void OnDisable()
	{
		enable = false;
	}

	void OnEnable()
	{
		enable = true;
	}

	public Camera GetDummyCamera(){
		return m_dummycam;
	}

	public void Render(){
		m_dummycam.Render ();
	}
	// ROS Msg Part 
    private PixelValueResponse UpdatePixel(PixelValueRequest request)
    {
        var width = 1280;
        var height = 720;
        float[] pixel = new float[3*width*height];

        PixelValueResponse pixelValueResponse = new PixelValueResponse();
        foreach (var pass in capturePasses)
        {
            if (pass.name == "segmentation")
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
	// Extract Pixel Value 
	private float[] ExtractPixel(Camera cam, int width, int height,  bool supportsAntialiasing, bool needsRescale)
	{
		var mainCamera = GetComponent<Camera>(); 
		var depth      = 24; 
		var format     = RenderTextureFormat.Default; 
		var readWrite  = RenderTextureReadWrite.Default; 
		var antiAliasing = (supportsAntialiasing) ? Mathf.Max(1, QualitySettings.antiAliasing) : 1;
		var finalRT    = RenderTexture.GetTemporary(width, height, depth, format, readWrite, antiAliasing);
		var renderRT   = (!needsRescale) ? finalRT: RenderTexture.GetTemporary(mainCamera.pixelWidth, mainCamera.pixelHeight, depth, format, readWrite, antiAliasing);
		var tex        = new Texture2D(width, height, TextureFormat.RGB24, false); 
		tex.hideFlags  = HideFlags.HideAndDontSave; 

		var prevActiveRT = RenderTexture.active; 
		var prevCameraRT = cam.targetTexture;
		float[] pixelList = new float[3*width*height]; 

		RenderTexture.active = renderRT; 
		cam.targetTexture    = renderRT; 

		cam.Render();

		tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0); 
		tex.Apply(); 
		pixelList = DumpToList(tex); 
		Texture2D.DestroyImmediate(tex,true);

		cam.targetTexture    = prevCameraRT;
		RenderTexture.active = prevActiveRT; 
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

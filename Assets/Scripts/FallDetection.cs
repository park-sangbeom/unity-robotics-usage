using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;

public class FallDetection : MonoBehaviour
{
    private Color RedColor; 
    private Color GreenColor; 

    private Texture originTexture;
    void Start()
    {
         
    }

    // Update is called once per frame
    void Update()
    {
        RedColor   = new Color(255, 0, 0);
        GreenColor = new Color(0, 255, 0);
        if (gameObject.transform.rotation.To<FLU>().w <0.66 || gameObject.transform.rotation.To<FLU>().w >0.74)
            {
                gameObject.GetComponent<Renderer>().material.color= RedColor;
                // Debug.Log("Fall down");

            }      
        else
        {
                gameObject.GetComponent<Renderer>().material.color= GreenColor;
                // Debug.Log("Not Fall down");
        }   
    }
}

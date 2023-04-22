using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetection : MonoBehaviour
{
    private Color newColor; 
    private Texture originTexture;
    void Start()
    {
         
    }

    // Update is called once per frame
    void Update()
    {
         
    }
    private void OnCollisionEnter(Collision other)
    {
        newColor = new Color(255,0, 0);
        if (other.gameObject.name!="TableTop")
        {
            // Debug.Log(other.gameObject.name);
            gameObject.GetComponent<Renderer>().material.color= newColor;
        }
        
        //Check to see if the tag on the collider is equal to Enemy
    }

    private void OnCollisionExit(Collision other) 
    {
        newColor = new Color(0,255, 0);
        gameObject.GetComponent<Renderer>().material.color= newColor;
    }
}

using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.RilMsg;
using System.Windows;

public class MultiCameraMove : MonoBehaviour
{
    [Header("ROS Setup")]
    ROSConnection ros; 
    public string rosServiceName;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance(); 
        ros.ImplementService<InitSyncRequest, InitSyncResponse>(rosServiceName, GetScenePose);
    }
    /// <returns>service response containing the object pose (or 0 if object not found)</returns>
    private InitSyncResponse GetScenePose(InitSyncRequest request)
    {
        // Debug.Log("Received Initial Scene Configuration");
        InitSyncResponse response = new InitSyncResponse();
        StartCoroutine(ObjectSync(request));
        response.init_state = 1;

        return response;        
    }
    private IEnumerator ObjectSync(InitSyncRequest request)
    {
        for (int objectIndex=0; objectIndex < request.activated_object.Length; objectIndex++)
        {
            var objectName                  = request.activated_object[objectIndex];
            GameObject targetObject         =  GameObject.Find(objectName);
            targetObject.transform.position = request.position[objectIndex].From<FLU>();
            targetObject.transform.rotation = request.orientation[objectIndex].From<FLU>();//* Quaternion.Euler(0, 90, 0);
            targetObject.SetActive(true);
        }
        // Debug.Log("Initialzied Objects");
        yield return new WaitForSeconds(0.0001f);
    }
    // Update is called once per frame
    void Update() 
    {
    }
}
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.RilMsg;

public class GetObjectState : MonoBehaviour
{
    ROSConnection ros; 
    public string rosServiceName;

    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.ImplementService<ObjectPoseServiceRequest, ObjectPoseServiceResponse>(rosServiceName, GetObjectPose);      

    }

    private ObjectPoseServiceResponse GetObjectPose(ObjectPoseServiceRequest request)
    {
        // Debug.Log("Received request for object: "+request.object_name);
        ObjectPoseServiceResponse objectPoseResponse = new ObjectPoseServiceResponse();
        var childPath = request.object_name;
        GameObject gameObject = GameObject.Find(childPath); 
        // If you want to set render off for all object in the scene.
        // var renderers = Object.FindObjectsOfType<Renderer>();
        // foreach(var obj in renderers){
        //     obj.enabled = false;
        // }  
        // If you want to set render on for a target object.
        // var gameObjectRender = gameObject.GetComponent<Renderer>();
        // gameObjectRender.enabled=true;
        if (gameObject)
        {
            objectPoseResponse.object_pose.position    = gameObject.transform.position.To<FLU>();
            objectPoseResponse.object_pose.orientation = gameObject.transform.rotation.To<FLU>();
            var objectSize = gameObject.GetComponent<MeshRenderer>().bounds.size;
            objectPoseResponse.object_size.x = objectSize[0];
            objectPoseResponse.object_size.y = objectSize[1];
            objectPoseResponse.object_size.z = objectSize[2];
        }
        return objectPoseResponse; 
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.RilMsg;
using RosMessageTypes.Geometry;

public class MultiObjectStateGet : MonoBehaviour
{
    ROSConnection ros; 
    public string rosServiceName;

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.ImplementService<MultiObjectPoseServiceRequest, MultiObjectPoseServiceResponse>(rosServiceName, GetObjectPose);      

    }


    private MultiObjectPoseServiceResponse GetObjectPose(MultiObjectPoseServiceRequest request)
    {
        MultiObjectPoseServiceResponse objectPoseResponse = new MultiObjectPoseServiceResponse();
        // Pose Message type should be "PoseMsg" not "Pose"  
        PoseMsg[] objectPose = new PoseMsg[request.object_name.Length];
        string[] objectName = new string[request.object_name.Length];
        for (int idx=0; idx<request.object_name.Length; idx++)
        {
            var childPath = request.object_name[idx];
            objectName[idx] = childPath;
            GameObject gameObject = GameObject.Find(childPath); 
            if (gameObject)
            {
                objectPose[idx] = new PoseMsg(
                    gameObject.transform.position.To<FLU>(),
                    gameObject.transform.rotation.To<FLU>()
                );
            }    
        }
        objectPoseResponse.object_pose = objectPose;
        objectPoseResponse.object_name = objectName;
        return objectPoseResponse; 
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}

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
using System.Threading; 
using System.Threading.Tasks; 

public class MultiRobotParallelMove : MonoBehaviour
{
    ROSConnection ros; 
    private string rosServiceName = "multi_action";
    private string rosTopicName   = "execution_state"; 
    // public string rosCheckerName; 

    public static readonly string[] LinkNames = 
    {"base_link","/shoulder_link", "/upper_arm_link", "/forearm_link",
    "/wrist_1_link", "/wrist_2_link", "/wrist_3_link"};    

    [SerializeField]
    GameObject[] Robots; 
    private int nRobotJoints = 7;
    // Robot Joints 
    ArticulationBody[] JointArticulationBodies;
    List<ArticulationBody[]> BodyLists = new List<ArticulationBody[]>();
    ArticulationDrive[] DriveLists;

    const float k_JointAssignmentWait = 0.001f;
    bool trajectoryDone = false; 
    // Start is called before the first frame update
    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.ImplementService<RobotPlanRequest, RobotPlanResponse>(rosServiceName, GetMotionPlan);
        ros.RegisterPublisher<ActionStateMsg>(rosTopicName);
        var nRobot = Robots.Length;
        for (var robotIdx=0; robotIdx<Robots.Length; robotIdx++)
        {
            var linkname = string.Empty; 
            JointArticulationBodies = new ArticulationBody[nRobotJoints];
            for (var i=0; i<nRobotJoints; i++)
            {
                linkname+=LinkNames[i];
                JointArticulationBodies[i] = Robots[robotIdx].transform.Find(linkname).GetComponent<ArticulationBody>();
            }
            BodyLists.Add(JointArticulationBodies);
        }
    }

    private RobotPlanResponse GetMotionPlan(RobotPlanRequest request)
    {
        // Debug.Log("Received Prepose Configuration");
        RobotPlanResponse response = new RobotPlanResponse();
        StartCoroutine(ExecuteTrajectories(request));
        StartCoroutine(CheckState());
        response.execute_state = 1;
        return response;
    }

    private IEnumerator ExecuteTrajectories(RobotPlanRequest request)
    {
        trajectoryDone = true;
        DriveLists = new ArticulationDrive[Robots.Length];      
        for (var idx=0; idx<request.joint_state.Length/7/Robots.Length; idx++)
        {
            for (var JointIdx=0; JointIdx<nRobotJoints; JointIdx++)
            {    
                for (var robotIdx=0; robotIdx<Robots.Length; robotIdx++)
                {
                    DriveLists[robotIdx] = BodyLists[robotIdx][JointIdx].xDrive;

                }
                Parallel.For(0, Robots.Length, (robotIdx)=>
                {
                    var jointXDrive = DriveLists[robotIdx];
                    Debug.Log(jointXDrive);
                    jointXDrive.target = request.joint_state[idx*7+JointIdx+(request.joint_state.Length/Robots.Length)*robotIdx]* Mathf.Rad2Deg;
                    // jointXDrive.targetVelocity = 0.05f;
                    // jointXDrive.stiffness = 8000;
                    // jointXDrive.forceLimit = 50;
                    // Debug.Log(idx*7+JointIdx+(request.joint_state.Length/Robots.Length)*robotIdx);
                    DriveLists[robotIdx] = jointXDrive; 
                });
            }
            yield return new WaitForSeconds(k_JointAssignmentWait);
        }
        trajectoryDone = false;
    }

    private IEnumerator CheckState() 
    {
        ActionStateMsg actionState = new ActionStateMsg();
        while(trajectoryDone)       
        yield return new WaitForSeconds(0.001f);
        actionState.execution_state = 1; 
        ros.Publish(rosTopicName, actionState);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

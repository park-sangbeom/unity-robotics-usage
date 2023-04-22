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

public class MultiRobotMove : MonoBehaviour
{
    ROSConnection ros; 
    private string rosServiceName = "multi_action";
    private string rosTopicName   = "execution_state"; 
    // public string rosCheckerName; 
    public int StaticAgentNum;

    public static readonly string[] LinkNames = 
    {"base_link","/shoulder_link", "/upper_arm_link", "/forearm_link",
    "/wrist_1_link", "/wrist_2_link", "/wrist_3_link"};    

    [SerializeField]
    private int nRobotJoints = 7;
    // Robot Joints 
    ArticulationBody[] JointArticulationBodies;
    List<ArticulationBody[]> BodyLists = new List<ArticulationBody[]>();

    const float k_JointAssignmentWait = 0.01f;
    bool trajectoryDone = false; 

    void Start()
    {
        ros = ROSConnection.GetOrCreateInstance();
        ros.ImplementService<RobotPlanRequest, RobotPlanResponse>(rosServiceName, GetMotionPlan);
        ros.RegisterPublisher<ActionStateMsg>(rosTopicName);
        for (int idx=0; idx<StaticAgentNum; idx++)
        {
            if (idx<100)
            {
                var linkname = string.Empty; 
                string robotname = string.Format("Group{0}/Agent{1}/RilabUR5e", "1",idx+1);
                JointArticulationBodies = new ArticulationBody[nRobotJoints];
                Debug.Log(robotname);
                ArticulationBody robot = GameObject.Find(robotname).GetComponent<ArticulationBody>();

                for (var i=0; i<nRobotJoints; i++)
                { 
                    linkname+=LinkNames[i];
                    JointArticulationBodies[i] = robot.transform.transform.Find(linkname).GetComponent<ArticulationBody>();
                }
                BodyLists.Add(JointArticulationBodies);
            }
        }
    }

    private RobotPlanResponse GetMotionPlan(RobotPlanRequest request)
    {
        // Debug.Log("Received Prepose Configuration");
        RobotPlanResponse response = new RobotPlanResponse();
        StartCoroutine(ExecuteTrajectories(request));
        // TODO: Check State 
        // StartCoroutine(CheckState());
        response.execute_state = 1;
        return response;
    }

    private IEnumerator ExecuteTrajectories(RobotPlanRequest request)
    {
        trajectoryDone = true;
        for (var idx=0; idx<request.joint_state.Length/7/StaticAgentNum; idx++)
        {
            for (var JointIdx=0; JointIdx<nRobotJoints; JointIdx++)
            {
                for (var robotIdx=0; robotIdx<StaticAgentNum; robotIdx++)
                {
                    var jointXDrive = BodyLists[robotIdx][JointIdx].xDrive;
                    jointXDrive.target = request.joint_state[idx*7+JointIdx+(request.joint_state.Length/StaticAgentNum)*robotIdx]* Mathf.Rad2Deg;
                    // jointXDrive.targetVelocity = 0.05f;
                    // jointXDrive.stiffness = 8000;
                    // jointXDrive.forceLimit = 50;
                    // Debug.Log(idx*7+JointIdx+(request.joint_state.Length/Robots.Length)*robotIdx);
                    BodyLists[robotIdx][JointIdx].xDrive = jointXDrive; 
                }
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
    // void Update()
    // {
    //     Time.timeScale = 2.0F;
    //     Time.fixedDeltaTime = 0.02F * Time.timeScale;        
    // }
}

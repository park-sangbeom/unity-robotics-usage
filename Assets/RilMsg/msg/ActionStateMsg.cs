//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.RilMsg
{
    [Serializable]
    public class ActionStateMsg : Message
    {
        public const string k_RosMessageName = "ril_msg/ActionState";
        public override string RosMessageName => k_RosMessageName;

        public long execution_state;

        public ActionStateMsg()
        {
            this.execution_state = 0;
        }

        public ActionStateMsg(long execution_state)
        {
            this.execution_state = execution_state;
        }

        public static ActionStateMsg Deserialize(MessageDeserializer deserializer) => new ActionStateMsg(deserializer);

        private ActionStateMsg(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.execution_state);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.execution_state);
        }

        public override string ToString()
        {
            return "ActionStateMsg: " +
            "\nexecution_state: " + execution_state.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize);
        }
    }
}

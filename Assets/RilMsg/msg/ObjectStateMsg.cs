//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.RilMsg
{
    [Serializable]
    public class ObjectStateMsg : Message
    {
        public const string k_RosMessageName = "ril_msg/ObjectState";
        public override string RosMessageName => k_RosMessageName;

        public long collision_state;

        public ObjectStateMsg()
        {
            this.collision_state = 0;
        }

        public ObjectStateMsg(long collision_state)
        {
            this.collision_state = collision_state;
        }

        public static ObjectStateMsg Deserialize(MessageDeserializer deserializer) => new ObjectStateMsg(deserializer);

        private ObjectStateMsg(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.collision_state);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.collision_state);
        }

        public override string ToString()
        {
            return "ObjectStateMsg: " +
            "\ncollision_state: " + collision_state.ToString();
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

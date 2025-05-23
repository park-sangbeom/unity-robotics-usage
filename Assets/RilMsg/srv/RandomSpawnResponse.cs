//Do not edit! This file was generated by Unity-ROS MessageGeneration.
using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.Robotics.ROSTCPConnector.MessageGeneration;

namespace RosMessageTypes.RilMsg
{
    [Serializable]
    public class RandomSpawnResponse : Message
    {
        public const string k_RosMessageName = "ril_msg/RandomSpawn";
        public override string RosMessageName => k_RosMessageName;

        public long spawn_state;

        public RandomSpawnResponse()
        {
            this.spawn_state = 0;
        }

        public RandomSpawnResponse(long spawn_state)
        {
            this.spawn_state = spawn_state;
        }

        public static RandomSpawnResponse Deserialize(MessageDeserializer deserializer) => new RandomSpawnResponse(deserializer);

        private RandomSpawnResponse(MessageDeserializer deserializer)
        {
            deserializer.Read(out this.spawn_state);
        }

        public override void SerializeTo(MessageSerializer serializer)
        {
            serializer.Write(this.spawn_state);
        }

        public override string ToString()
        {
            return "RandomSpawnResponse: " +
            "\nspawn_state: " + spawn_state.ToString();
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod]
#endif
        public static void Register()
        {
            MessageRegistry.Register(k_RosMessageName, Deserialize, MessageSubtopic.Response);
        }
    }
}

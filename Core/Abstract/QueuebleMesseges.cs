using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace GameWorkstore.NetworkLibrary
{
    public struct QPacket
    {
        public short Code;
        public byte Channel;
        public MsgBase Data;
    }

    public abstract class QueueVisibilityPacket<T> : NetworkMessageBase where T : QueuebleVisibilityClass, new()
    {
        protected int connectionTarget;
        protected const float maximumLostVisibilityTime = 0.25f; //250ms
        private static Dictionary<NetworkInstanceId, T> unique = new Dictionary<NetworkInstanceId, T>();
        private static Dictionary<int, NetCPUCamera> visibilityCamera = new Dictionary<int, NetCPUCamera>();

        public T[] states;

        public override void Serialize(NetWriter writer)
        {
            NetCPUCamera camera;
            if(visibilityCamera.TryGetValue(connectionTarget, out camera))
            {
                int count = 0;
                foreach(var pair in unique)
                {
                    if (GeometryUtility.TestPlanesAABB(camera.Planes, pair.Value.bounds))
                        pair.Value.markedTime = BaseConnectionService.SimulationTime();

                    pair.Value.markedToSend = (BaseConnectionService.SimulationTime() - pair.Value.markedTime) < maximumLostVisibilityTime;
                    if (pair.Value.markedToSend) count++;
                }
                writer.Write((byte)count);
                foreach (var pair in unique)
                {
                    if (pair.Value.markedToSend) pair.Value.Serialize(writer);
                }
            }
            else
            {
                writer.Write((byte)0);
            }
        }

        public override void Deserialize(NetReader reader)
        {
            states = new T[reader.ReadByte()];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new T();
                states[i].Deserialize(reader);
            }
        }

        public static void UpdateCPUCamera(int connection, NetCPUCamera camera)
        {
            visibilityCamera[connection] = camera;
        }

        public static bool HasQueue()
        {
            return unique.Count > 0;
        }

        public static void Enqueue(T data, Bounds bounds)
        {
            unique[data.instanceId] = data;
            unique[data.instanceId].bounds = bounds;
        }

        public static bool ClearPacket()
        {
            unique.Clear();
            return true;
        }
    }

    public abstract class QueueblePacket<T> : NetworkMessageBase where T : QueuebleClass, new()
    {
        //WriteOnly
        private static Dictionary<NetworkInstanceId, T> unique = new Dictionary<NetworkInstanceId, T>();
        //ReadOnly
        public T[] states;

        public override void Serialize(NetWriter writer)
        {
            writer.Write((byte)unique.Count);
            foreach (var k in unique)
            {
                k.Value.Serialize(writer);
            }
        }

        public override void Deserialize(NetReader reader)
        {
            states = new T[reader.ReadByte()];
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new T();
                states[i].Deserialize(reader);
            }
        }

        public static bool HasQueue()
        {
            return unique.Count > 0;
        }

        public static void Enqueue(T data)
        {
            unique[data.instanceId] = data;
        }

        public static bool ClearPacket()
        {
            unique.Clear();
            return true;
        }
    }

    public abstract class QueuebleVisibilityClass : QueuebleClass
    {
        public Bounds bounds;
        public bool markedToSend;
        public float markedTime;
    }

    public abstract class QueuebleClass
    {
        public NetworkInstanceId instanceId;

        public virtual void Serialize(NetWriter writer)
        {
            writer.Write(instanceId);
        }

        public virtual void Deserialize(NetReader reader)
        {
            instanceId = reader.ReadNetworkId();
        }
    }
}
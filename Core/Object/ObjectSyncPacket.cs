using UnityEngine;

namespace GameWorkstore.NetworkLibrary
{
    internal class ObjectSyncPacket : NetworkPacketBase
    {
        public override short Code { get { return (short)ReservedBySystem.ObjectSyncPacket; } }

        internal bool IsLast = false;
        internal NetworkHash128[] ObjectName = System.Array.Empty<NetworkHash128>();
        internal NetworkInstanceId[] ObjectId = System.Array.Empty<NetworkInstanceId>();
        internal NetworkInstanceId[] Authority = System.Array.Empty<NetworkInstanceId>();
        internal Vector3[] Position = System.Array.Empty<Vector3>();
        internal Quaternion[] Quaternion = System.Array.Empty<Quaternion>();
        internal byte[][] InternalParams = System.Array.Empty<byte[]>();
        internal byte[][] SharedParams = System.Array.Empty<byte[]>();

        public override void Serialize(NetWriter writer)
        {
            writer.Write(IsLast);
            writer.Write(ObjectName);
            writer.Write(ObjectId);
            writer.Write(Position, true);
            writer.Write(Quaternion);
            writer.Write(Authority);
            //Internal
            writer.Write((byte)InternalParams.Length);
            for (int i = 0; i < InternalParams.Length; i++)
            {
                writer.Write((byte)InternalParams[i].Length);
            }
            for (int i = 0; i < InternalParams.Length; i++)
            {
                for (int j = 0; j < InternalParams[i].Length; j++)
                {
                    writer.Write(InternalParams[i][j]);
                }
            }
            //Shared
            writer.Write((byte)SharedParams.Length);
            for(int i=0;i< SharedParams.Length; i++)
            {
                writer.Write((byte)SharedParams[i].Length);
            }
            for(int i=0;i< SharedParams.Length; i++)
            {
                for(int j = 0; j < SharedParams[i].Length; j++)
                {
                    writer.Write(SharedParams[i][j]);
                }
            }
        }

        public override void Deserialize(NetReader reader)
        {
            IsLast = reader.ReadBoolean();
            ObjectName = reader.ReadNetworkHash128s();
            ObjectId = reader.ReadNetworkIds();
            Position = reader.ReadVector3s(true);
            Quaternion = reader.ReadQuaternions();
            Authority = reader.ReadNetworkIds();
            //Internal
            InternalParams = new byte[reader.ReadByte()][];
            for (int i = 0; i < InternalParams.Length; i++)
            {
                InternalParams[i] = new byte[reader.ReadByte()];
            }
            for (int i = 0; i < InternalParams.Length; i++)
            {
                for (int j = 0; j < InternalParams[i].Length; j++)
                {
                    InternalParams[i][j] = reader.ReadByte();
                }
            }
            //Shared
            SharedParams = new byte[reader.ReadByte()][];
            for(int i = 0; i < SharedParams.Length; i++)
            {
                SharedParams[i] = new byte[reader.ReadByte()];
            }
            for (int i = 0; i < SharedParams.Length; i++)
            {
                for (int j = 0; j < SharedParams[i].Length; j++)
                {
                    SharedParams[i][j] = reader.ReadByte();
                }
            }
        }
    }
}

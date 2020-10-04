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
            writer.Write((byte)ObjectName.Length);
            writer.Write((byte)Authority.Length);
            for (int i = 0; i < ObjectName.Length; i++)
            {
                writer.Write(ObjectName[i]);
            }
            for (int i = 0; i < ObjectId.Length; i++)
            {
                writer.Write(ObjectId[i]);
            }
            for (int i = 0; i < ObjectId.Length; i++)
            {
                writer.Write(Position[i], true);
                writer.Write(Quaternion[i]);
            }
            for (int i = 0; i < Authority.Length; i++)
            {
                writer.Write(Authority[i]);
            }
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
            int objectCount = reader.ReadByte();
            int authorityCount = reader.ReadByte();
            ObjectName = new NetworkHash128[objectCount];
            ObjectId = new NetworkInstanceId[objectCount];
            Position = new Vector3[objectCount];
            Quaternion = new Quaternion[objectCount];
            Authority = new NetworkInstanceId[authorityCount];
            for (int i = 0; i < objectCount; i++)
            {
                ObjectName[i] = reader.ReadNetworkHash128();
            }
            for (int i = 0; i < objectCount; i++)
            {
                ObjectId[i] = reader.ReadNetworkId();
            }
            for (int i = 0; i < ObjectId.Length; i++)
            {
                Position[i] = reader.ReadVector3(true);
                Quaternion[i] = reader.ReadQuaternion();
            }
            for (int i = 0; i < Authority.Length; i++)
            {
                Authority[i] = reader.ReadNetworkId();
            }
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

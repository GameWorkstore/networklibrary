using UnityEngine.Networking;

namespace UnityEngine.NetLibrary
{
    internal class ObjectSyncPacket : MsgBase
    {
        internal const short Code = 1001;//EnumCodes.ObjectSyncPacket

        internal bool IsLast = false;
        internal NetworkHash128[] ObjectName = new NetworkHash128[0];
        internal NetworkInstanceId[] ObjectId = new NetworkInstanceId[0];
        internal NetworkInstanceId[] Authority = new NetworkInstanceId[0];
        internal Vector3[] Position = new Vector3[0];
        internal Quaternion[] Quaternion = new Quaternion[0];
        internal byte[][] InternalParams = new byte[0][];
        internal byte[][] SharedParams = new byte[0][];

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

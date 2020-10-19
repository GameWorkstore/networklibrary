using UnityEngine;

namespace GameWorkstore.NetworkLibrary
{
    internal class ObjectSyncDeltaCreatePacket : NetworkPacketBase
    {
        public override ushort Code { get { return (ushort)ReservedBySystem.ObjectSyncDeltaCreatePacket; } }

        internal NetworkInstanceId ObjectId;
        internal NetworkHash128 ObjectName;
        internal Vector3 Position;
        internal Quaternion Quaternion;
        internal byte[] InternalParams;
        internal byte[] SharedParams;
        internal bool Authority;

        public override void Serialize(NetWriter writer)
        {
            writer.Write(ObjectId);
            writer.Write(ObjectName);
            writer.Write(Position, true);
            writer.Write(Quaternion);
            writer.Write(Authority);
            writer.Write(InternalParams);
            writer.Write(SharedParams);
        }

        public override void Deserialize(NetReader reader)
        {
            ObjectId = reader.ReadNetworkId();
            ObjectName = reader.ReadNetworkHash128();
            Position = reader.ReadVector3(true);
            Quaternion = reader.ReadQuaternion();
            Authority = reader.ReadBoolean();
            InternalParams = reader.ReadBytes();
            SharedParams = reader.ReadBytes();
        }
    }
}

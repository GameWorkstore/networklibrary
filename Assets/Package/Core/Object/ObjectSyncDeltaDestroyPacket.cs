
namespace GameWorkstore.NetworkLibrary
{
    internal class ObjectSyncDeltaDestroyPacket : NetworkPacketBase
    {
        public override ushort Code { get { return (ushort)ReservedBySystem.ObjectSyncDeltaDestroyPacket; } }

        internal NetworkInstanceId ObjectId;

        public override void Serialize(NetWriter writer)
        {
            writer.Write(ObjectId);
        }

        public override void Deserialize(NetReader reader)
        {
            ObjectId = reader.ReadNetworkId();
        }
    }
}

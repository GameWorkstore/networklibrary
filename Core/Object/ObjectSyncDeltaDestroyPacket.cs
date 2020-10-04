
namespace GameWorkstore.NetworkLibrary
{
    internal class ObjectSyncDeltaDestroyPacket : NetworkPacketBase
    {
        public override short Code { get { return (short)ReservedBySystem.ObjectSyncDeltaDestroyPacket; } }

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

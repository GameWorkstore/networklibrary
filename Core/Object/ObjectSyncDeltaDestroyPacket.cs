
namespace GameWorkstore.NetworkLibrary
{
    internal class ObjectSyncDeltaDestroyPacket : NetworkPacketBase
    {
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

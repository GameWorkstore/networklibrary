namespace GameWorkstore.NetworkLibrary
{
    internal class ObjectSyncNetworkTimePacket : NetworkPacketBase
    {
        public override ushort Code { get { return (ushort)ReservedBySystem.ObjectSyncNetworkTimePacket; } }

        internal float NetworkTime;
        
        public override void Serialize(NetWriter writer)
        {
            writer.Write(NetworkTime);
        }

        public override void Deserialize(NetReader reader)
        {
            NetworkTime = reader.ReadSingle();
        }
    }
}

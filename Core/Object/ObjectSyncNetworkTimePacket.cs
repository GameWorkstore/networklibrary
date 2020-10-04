namespace GameWorkstore.NetworkLibrary
{
    internal class ObjectSyncNetworkTimePacket : NetworkPacketBase
    {
        public override short Code { get { return (short)ReservedBySystem.ObjectSyncNetworkTimePacket; } }

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

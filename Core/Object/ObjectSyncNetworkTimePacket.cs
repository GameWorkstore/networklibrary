namespace GameWorkstore.NetworkLibrary
{
    internal class ObjectSyncNetworkTimePacket : MsgBase
    {
        internal const short Code = 1000;//EnumCodes.ObjectSyncNetworkTimePacket

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

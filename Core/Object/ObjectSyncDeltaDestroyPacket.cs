using UnityEngine.Networking;

namespace GameWorkstore.NetworkLibrary
{
    internal class ObjectSyncDeltaDestroyPacket : MsgBase
    {
        internal const short Code = 1003;//EnumCodes.ObjectSyncDeltaDestroyPacket

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

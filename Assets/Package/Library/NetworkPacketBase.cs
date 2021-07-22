
using Google.Protobuf;
using UnityEngine;

namespace GameWorkstore.NetworkLibrary
{
    // This can't be an interface because users don't need to implement the
    // serialization functions, we'll code generate it for them when they omit it.
    public abstract class NetworkPacketBase
    {
        public INetConnection conn;

        // De-serialize the contents of the reader into this message
        public abstract void Deserialize(NetReader reader);

        // Serialize the contents of this message into the writer
        public abstract void Serialize(NetWriter writer);
    }

    public static class NetworkPacketExtensions
    {
        public static uint Code(this NetworkPacketBase pkg)
        {
            return (uint)Animator.StringToHash(pkg.GetType().Name);
        }

        public static uint Code<T>() where T : NetworkPacketBase, new()
        {
            return (uint)Animator.StringToHash(typeof(T).Name);
        }
    }

    public static class ProtobufPacketExtensions
    {
        public static uint Code(this IMessage pkg)
        {
            return (uint)Animator.StringToHash(pkg.GetType().Name);
        }

        public static uint Code<T>() where T : IMessage<T>, new()
        {
            return (uint)Animator.StringToHash(typeof(T).Name);
        }
    }

    public class ProtobufPacket<T> where T : IMessage
    {
        public INetConnection Conn;
        public T Proto;
    }

    public class AuthenticationRequestPacket : NetworkPacketBase
    {
        //public override ushort Code { get { return (ushort)ReservedBySystem.AuthenticationRequest; } }
        public short ServerConnectionId;
        
        public override void Deserialize(NetReader reader)
        {
            ServerConnectionId = reader.ReadShort();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(ServerConnectionId);
        }
    }

    public class AuthenticationResponsePacket : NetworkPacketBase
    {
        //public override ushort Code { get { return (ushort)ReservedBySystem.AuthenticationResponse; } }
        public string Payload;

        public override void Deserialize(NetReader reader)
        {
            Payload = reader.ReadString();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(Payload);
        }
    }

    public class NetworkAlivePacket : NetworkPacketBase
    {
        //public override ushort Code { get { return (ushort)ReservedBySystem.Alive; } }
        public override void Deserialize(NetReader reader) { }
        public override void Serialize(NetWriter writer) { }
    }
}

namespace GameWorkstore.NetworkLibrary
{
    // This can't be an interface because users don't need to implement the
    // serialization functions, we'll code generate it for them when they omit it.
    public abstract class NetworkPacketBase
    {
        public NetConnection conn;

        // De-serialize the contents of the reader into this message
        public abstract void Deserialize(NetReader reader);

        // Serialize the contents of this message into the writer
        public abstract void Serialize(NetWriter writer);

        public abstract short Code { get; }
    }

    public enum ReservedBySystem
    {
        AuthenticationRequest = 1,
        AuthenticationResponse = 2,
        Alive = 3,
        ObjectSyncNetworkTimePacket = 4,
        ObjectSyncPacket = 5,
        ObjectSyncDeltaCreatePacket = 6,
        ObjectSyncDeltaDestroyPacket = 7,
    }

    public class AuthenticationRequestPacket : NetworkPacketBase
    {
        public override short Code { get { return (short)ReservedBySystem.AuthenticationRequest; } }
        public override void Deserialize(NetReader reader) { }
        public override void Serialize(NetWriter writer) { }
    }

    public class AuthenticationResponsePacket : NetworkPacketBase
    {
        public override short Code { get { return (short)ReservedBySystem.AuthenticationResponse; } }
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
        public override short Code { get { return (short)ReservedBySystem.Alive; } }
        public override void Deserialize(NetReader reader) { }
        public override void Serialize(NetWriter writer) { }
    }
}

/*
namespace GameWorkstore.NetworkLibrary.NetworkSystem
{
    // ---------- General Typed Messages -------------------

    public class StringMessage : NetworkPacketBase
    {
        public string value;

        public StringMessage()
        {
        }

        public StringMessage(string v)
        {
            value = v;
        }

        public override void Deserialize(NetReader reader)
        {
            value = reader.ReadString();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(value);
        }
    }

    public class IntegerMessage : NetworkPacketBase
    {
        public int value;

        public IntegerMessage()
        {
        }

        public IntegerMessage(int v)
        {
            value = v;
        }

        public override void Deserialize(NetReader reader)
        {
            value = (int)reader.ReadPackedUInt32();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.WritePackedUInt32((uint)value);
        }
    }

    public class EmptyMessage : NetworkPacketBase
    {
        public override void Deserialize(NetReader reader)
        {
        }

        public override void Serialize(NetWriter writer)
        {
        }
    }

    // ---------- Public System Messages -------------------

    public class AddPlayerMessage : NetworkPacketBase
    {
        public short playerControllerId;
        public int msgSize;
        public byte[] msgData;

        public override void Deserialize(NetReader reader)
        {
            playerControllerId = (short)reader.ReadUInt16();
            msgData = reader.ReadBytesAndSize();
            if (msgData == null)
            {
                msgSize = 0;
            }
            else
            {
                msgSize = msgData.Length;
            }
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write((ushort)playerControllerId);
            writer.WriteBytesAndSize(msgData, msgSize);
        }
    }

    public class RemovePlayerMessage : NetworkPacketBase
    {
        public short playerControllerId;

        public override void Deserialize(NetReader reader)
        {
            playerControllerId = (short)reader.ReadUInt16();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write((ushort)playerControllerId);
        }
    }


    public class PeerAuthorityMessage : NetworkPacketBase
    {
        public int connectionId;
        public NetworkInstanceId netId;
        public bool authorityState;

        public override void Deserialize(NetReader reader)
        {
            connectionId = (int)reader.ReadPackedUInt32();
            netId = reader.ReadNetworkId();
            authorityState = reader.ReadBoolean();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.WritePackedUInt32((uint)connectionId);
            writer.Write(netId);
            writer.Write(authorityState);
        }
    }

    public struct PeerInfoPlayer
    {
        public NetworkInstanceId netId;
        public short playerControllerId;
    }

    public class PeerInfoMessage : NetworkPacketBase
    {
        public int connectionId;
        public string address;
        public int port;
        public bool isHost;
        public bool isYou;
        public PeerInfoPlayer[] playerIds;

        public override void Deserialize(NetReader reader)
        {
            connectionId = (int)reader.ReadPackedUInt32();
            address = reader.ReadString();
            port = (int)reader.ReadPackedUInt32();
            isHost = reader.ReadBoolean();
            isYou = reader.ReadBoolean();

            uint numPlayers = reader.ReadPackedUInt32();
            if (numPlayers > 0)
            {
                List<PeerInfoPlayer> ids = new List<PeerInfoPlayer>();
                for (uint i = 0; i < numPlayers; i++)
                {
                    PeerInfoPlayer info;
                    info.netId = reader.ReadNetworkId();
                    info.playerControllerId = (short)reader.ReadPackedUInt32();
                    ids.Add(info);
                }
                playerIds = ids.ToArray();
            }
        }

        public override void Serialize(NetWriter writer)
        {
            writer.WritePackedUInt32((uint)connectionId);
            writer.Write(address);
            writer.WritePackedUInt32((uint)port);
            writer.Write(isHost);
            writer.Write(isYou);
            if (playerIds == null)
            {
                writer.WritePackedUInt32(0);
            }
            else
            {
                writer.WritePackedUInt32((uint)playerIds.Length);
                for (int i = 0; i < playerIds.Length; i++)
                {
                    writer.Write(playerIds[i].netId);
                    writer.WritePackedUInt32((uint)playerIds[i].playerControllerId);
                }
            }
        }

        public override string ToString()
        {
            return "PeerInfo conn:" + connectionId + " addr:" + address + ":" + port + " host:" + isHost + " isYou:" + isYou;
        }
    }

    public class PeerListMessage : NetworkPacketBase
    {
        public PeerInfoMessage[] peers;
        public int oldServerConnectionId;

        public override void Deserialize(NetReader reader)
        {
            oldServerConnectionId = (int)reader.ReadPackedUInt32();
            int numPeers = reader.ReadUInt16();
            peers = new PeerInfoMessage[numPeers];
            for (int i = 0; i < peers.Length; ++i)
            {
                var peerInfo = new PeerInfoMessage();
                peerInfo.Deserialize(reader);
                peers[i] = peerInfo;
            }
        }

        public override void Serialize(NetWriter writer)
        {
            writer.WritePackedUInt32((uint)oldServerConnectionId);
            writer.Write((ushort)peers.Length);
            foreach (var a in peers)
            {
                a.Serialize(writer);
            }
        }
    }

    // ---------- Internal System Messages -------------------

    class ObjectSpawnMessage : NetworkPacketBase
    {
        public NetworkInstanceId netId;
        public NetworkHash128 assetId;
        public Vector3 position;
        public byte[] payload;

        public override void Deserialize(NetReader reader)
        {
            netId = reader.ReadNetworkId();
            assetId = reader.ReadNetworkHash128();
            position = reader.ReadVector3();
            payload = reader.ReadBytesAndSize();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(netId);
            writer.Write(assetId);
            writer.Write(position);
            writer.WriteBytesFull(payload);
        }
    }

    class ObjectSpawnSceneMessage : NetworkPacketBase
    {
        public NetworkInstanceId netId;
        public NetworkSceneId sceneId;
        public Vector3 position;
        public byte[] payload;

        public override void Deserialize(NetReader reader)
        {
            netId = reader.ReadNetworkId();
            sceneId = reader.ReadSceneId();
            position = reader.ReadVector3();
            payload = reader.ReadBytesAndSize();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(netId);
            writer.Write(sceneId);
            writer.Write(position);
            writer.WriteBytesFull(payload);
        }
    }

    class ObjectSpawnFinishedMessage : NetworkPacketBase
    {
        public uint state;

        public override void Deserialize(NetReader reader)
        {
            state = reader.ReadPackedUInt32();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.WritePackedUInt32(state);
        }
    }

    class ObjectDestroyMessage : NetworkPacketBase
    {
        public NetworkInstanceId netId;

        public override void Deserialize(NetReader reader)
        {
            netId = reader.ReadNetworkId();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(netId);
        }
    }

    class OwnerMessage : NetworkPacketBase
    {
        public NetworkInstanceId netId;
        public short playerControllerId;

        public override void Deserialize(NetReader reader)
        {
            netId = reader.ReadNetworkId();
            playerControllerId = (short)reader.ReadPackedUInt32();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(netId);
            writer.WritePackedUInt32((uint)playerControllerId);
        }
    }

    class ClientAuthorityMessage : NetworkPacketBase
    {
        public NetworkInstanceId netId;
        public bool authority;

        public override void Deserialize(NetReader reader)
        {
            netId = reader.ReadNetworkId();
            authority = reader.ReadBoolean();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(netId);
            writer.Write(authority);
        }
    }

    class OverrideTransformMessage : NetworkPacketBase
    {
        public NetworkInstanceId netId;
        public byte[] payload;
        public bool teleport;
        public int time;

        public override void Deserialize(NetReader reader)
        {
            netId = reader.ReadNetworkId();
            payload = reader.ReadBytesAndSize();
            teleport = reader.ReadBoolean();
            time = (int)reader.ReadPackedUInt32();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(netId);
            writer.WriteBytesFull(payload);
            writer.Write(teleport);
            writer.WritePackedUInt32((uint)time);
        }
    }

    class AnimationMessage : NetworkPacketBase
    {
        public NetworkInstanceId netId;
        public int      stateHash;      // if non-zero, then Play() this animation, skipping transitions
        public float    normalizedTime;
        public byte[]   parameters;

        public override void Deserialize(NetReader reader)
        {
            netId = reader.ReadNetworkId();
            stateHash = (int)reader.ReadPackedUInt32();
            normalizedTime = reader.ReadSingle();
            parameters = reader.ReadBytesAndSize();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(netId);
            writer.WritePackedUInt32((uint)stateHash);
            writer.Write(normalizedTime);

            if (parameters == null)
                writer.WriteBytesAndSize(parameters, 0);
            else
                writer.WriteBytesAndSize(parameters, parameters.Length);
        }
    }

    class AnimationParametersMessage : NetworkPacketBase
    {
        public NetworkInstanceId netId;
        public byte[]   parameters;

        public override void Deserialize(NetReader reader)
        {
            netId = reader.ReadNetworkId();
            parameters = reader.ReadBytesAndSize();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(netId);

            if (parameters == null)
                writer.WriteBytesAndSize(parameters, 0);
            else
                writer.WriteBytesAndSize(parameters, parameters.Length);
        }
    }

    class AnimationTriggerMessage : NetworkPacketBase
    {
        public NetworkInstanceId netId;
        public int      hash;

        public override void Deserialize(NetReader reader)
        {
            netId = reader.ReadNetworkId();
            hash = (int)reader.ReadPackedUInt32();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(netId);
            writer.WritePackedUInt32((uint)hash);
        }
    }

    class LobbyReadyToBeginMessage : NetworkPacketBase
    {
        public byte slotId;
        public bool readyState;

        public override void Deserialize(NetReader reader)
        {
            slotId = reader.ReadByte();
            readyState = reader.ReadBoolean();
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write(slotId);
            writer.Write(readyState);
        }
    }

    struct CRCMessageEntry
    {
        public string name;
        public byte channel;
    }

    class CRCMessage : NetworkPacketBase
    {
        public CRCMessageEntry[] scripts;

        public override void Deserialize(NetReader reader)
        {
            int numScripts = reader.ReadUInt16();
            scripts = new CRCMessageEntry[numScripts];
            for (int i = 0; i < scripts.Length; ++i)
            {
                var entry = new CRCMessageEntry();
                entry.name = reader.ReadString();
                entry.channel = reader.ReadByte();
                scripts[i] = entry;
            }
        }

        public override void Serialize(NetWriter writer)
        {
            writer.Write((ushort)scripts.Length);
            foreach (var s in scripts)
            {
                writer.Write(s.name);
                writer.Write(s.channel);
            }
        }
    }
}*/

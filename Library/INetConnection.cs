using GameWorkstore.Patterns;
using Google.Protobuf;
using UnityEngine;
using UnityEngine.Networking;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class INetConnection
    {
        protected NetworkHandlers _networkHandlers;
        protected ChannelBuffer[] _channels;
        public short HostId { get; private set; } = -1;
        public short LocalConnectionId { get; private set; } = -1;
        public short ServerConnectionId { get; set; } = -1;
        public float InitializedTime { get; private set; } = 0;
        public float LastReceivedTime { get; private set; } = 0;
        public bool Debug { get; set; } = false;

        private static readonly uint _osp = new ObjectSyncPacket().Code();
        private static readonly uint _osc = new ObjectSyncDeltaCreatePacket().Code();
        private static readonly uint _osd = new ObjectSyncDeltaDestroyPacket().Code();

        public bool SendByChannel(NetworkPacketBase packet, int channelId)
        {
            var writer = new NetWriter();
            writer.StartMessage(packet.Code());
            packet.Serialize(writer);
            writer.FinishMessage();
            return SendWriter(writer, channelId);
        }

        public bool SendByChannel(IMessage packet, int channelId)
        {
            return SendByChannel(packet.Code(), packet.ToByteArray(), channelId);
        }

        private bool SendByChannel(uint code, byte[] data, int channelId)
        {
            var writer = new NetWriter();
            writer.StartMessage(code);
            foreach (var b in data) writer.Write(b);
            writer.FinishMessage();
            return SendWriter(writer, channelId);
        }

        protected enum Direction
        {
            INCOMING,
            OUTCOMING,
        }

        private bool SendWriter(NetWriter writer, int channelId)
        {
            if (Debug) LogPacket(Direction.OUTCOMING, writer.ToArray());
            return CheckChannel(channelId) && _channels[channelId].SendWriter(writer);
        }

        protected void LogPacket(Direction direction, byte[] bytes)
        {
            var reader = new NetReader(bytes);
            reader.ReadHeader(out uint code, out ushort size);
            const int headerSize = 6;

            var builder = bytes.ToDebugString(headerSize, size);
            if (direction == Direction.OUTCOMING)
            {
                DebugMessege.Log("[ConnSend:" + LocalConnectionId + "] bytes:" + size + " code:" + code + " " + builder, DebugLevel.INFO);
            }
            else
            {
                DebugMessege.Log("[ConnRecv:" + LocalConnectionId + "] bytes:" + size + " code:" + code + " " + builder, DebugLevel.INFO);
            }
        }

        protected void LogPacketStats(uint code)
        {
            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.LLAPIMsg, "msg", 1);
            if (code > UnityTransportTypes.Highest)
            {
                if (code == _osp)
                    NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.ObjectSpawnScene, code.ToString() + ":" + code.GetType().Name, 1);
                else if (code == _osc)
                    NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.ObjectSpawn, code.ToString() + ":" + code.GetType().Name, 1);
                else if (code == _osd)
                    NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.ObjectDestroy, code.ToString() + ":" + code.GetType().Name, 1);
                else
                    NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.Command, code.ToString() + ":" + code.GetType().Name, 1);
            }
        }

        protected void HandleBytesReceived(byte[] bytes, int channelId)
        {
            if (Debug) LogPacket(Direction.INCOMING, bytes);

            var reader = new NetReader(bytes);
            reader.ReadHeader(out var code, out var size);
            var data = reader.ReadBytes(size);

            var msg = new NetMessage()
            {
                Type = code,
                Conn = this,
                Reader = new NetReader(data),
                ChannelId = channelId
            };

            if (!_networkHandlers.Invoke(code, msg))
            {
                DebugMessege.Log("Unknown message ID " + code + " connId:" + LocalConnectionId, DebugLevel.WARNING);
                return;
            }

            LastReceivedTime = Time.realtimeSinceStartup;

#if UNITY_EDITOR
            LogPacketStats(code);
#endif
        }

        public abstract void TransportReceive(byte[] bytes, int numBytes, int channelId);

        public abstract bool TransportSend(byte[] bytes, int numBytes, int channelId, out byte error);

        protected static void SetupConnection(
            INetConnection connection,
            short hostId,
            short connectionId,
            HostTopology hostTopology,
            float initializedTime)
        {
            connection.HostId = hostId;
            connection.LocalConnectionId = connectionId;
            connection.ServerConnectionId = connectionId;
            connection.InitializedTime = initializedTime;

            int numChannels = hostTopology.DefaultConfig.ChannelCount;
            int packetSize = hostTopology.DefaultConfig.PacketSize;

            connection._channels = new ChannelBuffer[numChannels];
            for (int i = 0; i < numChannels; i++)
            {
                var qos = hostTopology.DefaultConfig.Channels[i];
                int actualPacketSize = packetSize;
                if (qos.QOS == QosType.ReliableFragmented || qos.QOS == QosType.UnreliableFragmented)
                {
                    actualPacketSize = hostTopology.DefaultConfig.FragmentSize * 128;
                }
                connection._channels[i] = new ChannelBuffer(connection, actualPacketSize, (byte)i, qos.QOS.IsReliableQoS());
            }
        }

        public void FlushChannels()
        {
            foreach (ChannelBuffer channel in _channels)
            {
                channel.CheckInternalBuffer();
            }
        }

        public bool SetChannelOption(int channelId, ChannelOption option, int value)
        {
            if (_channels == null)
                return false;

            if (channelId < 0 || channelId >= _channels.Length)
                return false;

            return _channels[channelId].SetOption(option, value);
        }

        private bool CheckChannel(int channelId)
        {
            if (_channels == null)
            {
                DebugMessege.Log("Channels not initialized sending on id '" + channelId, DebugLevel.WARNING);
                return false;
            }
            if (channelId < 0 || channelId >= _channels.Length)
            {
                DebugMessege.Log("Invalid channel when sending buffered data, '" + channelId + "'. Current channel count is " + _channels.Length, DebugLevel.ERROR);
                return false;
            }
            return true;
        }

        public void GetStatsOut(out int numMsgs, out int numBufferedMsgs, out int numBytes, out int lastBufferedPerSecond)
        {
            numMsgs = 0;
            numBufferedMsgs = 0;
            numBytes = 0;
            lastBufferedPerSecond = 0;

            foreach (ChannelBuffer channel in _channels)
            {
                numMsgs += channel.NumMsgsOut;
                numBufferedMsgs += channel.NumBufferedMsgsOut;
                numBytes += channel.NumBytesOut;
                lastBufferedPerSecond += channel.LastBufferedPerSecond;
            }
        }

        public void GetStatsIn(out int numMsgs, out int numBytes)
        {
            numMsgs = 0;
            numBytes = 0;

            foreach (ChannelBuffer channel in _channels)
            {
                numMsgs += channel.NumMsgsIn;
                numBytes += channel.NumBytesIn;
            }
        }

        public void SetMaxDelay(float seconds)
        {
            if (_channels == null)
            {
                return;
            }
            foreach (ChannelBuffer channel in _channels)
            {
                channel.MaxDelay = seconds;
            }
        }

        public override string ToString()
        {
            return string.Format("hostId: {0} connectionId: {1} channel count: {2}", HostId, LocalConnectionId, (_channels != null ? _channels.Length : 0));
        }
    }

    public static class QOSExtensions
    {
        public static bool IsReliableQoS(this QosType qos)
        {
            return qos == QosType.Reliable || qos == QosType.ReliableFragmented || qos == QosType.ReliableSequenced || qos == QosType.ReliableStateUpdate;
        }
    }
}
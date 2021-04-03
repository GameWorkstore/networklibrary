using GameWorkstore.Patterns;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Google.Protobuf;

namespace GameWorkstore.NetworkLibrary
{
    /*
    * wire protocol is a list of :   size   |  msgType     | payload
    *					            (short)  (variable)   (buffer)
    */
    public class NetConnection : IDisposable
    {
        private ChannelBuffer[] _channels;

        //private readonly NetMessage _netMessage = new NetMessage();
        //private readonly NetWriter _writer = new NetWriter();
        private NetworkHandlers _networkHandlers;
        //private const int _maxPacketLogSize = 150;

        public short HostId = -1;
        public short LocalConnectionId = -1;
        public short ServerConnectionId = -1;
        public float InitializedTime;
        public float LastReceivedTime;
        public bool DebugPackets = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034")]
        public class PacketStat
        {
            public uint Code;
            public int Count;
            public int Bytes;

            public override string ToString()
            {
                return "[Code(" + Code + ")]: count=" + Count + " bytes=" + Bytes;
            }
        }

        internal Dictionary<uint, PacketStat> PacketStats { get; } = new Dictionary<uint, PacketStat>();

#if UNITY_EDITOR
        private const ushort _maxPacketStats = 255; //the same as maximum message types
#endif

        public NetConnection(short hostId, short localConnectionId, HostTopology hostTopology, float initializedTime, NetworkHandlers networkHandlers)
        {
            _networkHandlers = networkHandlers;
            HostId = hostId;
            LocalConnectionId = localConnectionId;
            ServerConnectionId = localConnectionId;
            InitializedTime = initializedTime;

            int numChannels = hostTopology.DefaultConfig.ChannelCount;
            int packetSize = hostTopology.DefaultConfig.PacketSize;

            if ((hostTopology.DefaultConfig.UsePlatformSpecificProtocols) && (Application.platform != RuntimePlatform.PS4))
            {
                throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
            }

            _channels = new ChannelBuffer[numChannels];
            for (int i = 0; i < numChannels; i++)
            {
                var qos = hostTopology.DefaultConfig.Channels[i];
                int actualPacketSize = packetSize;
                if (qos.QOS == QosType.ReliableFragmented || qos.QOS == QosType.UnreliableFragmented)
                {
                    actualPacketSize = hostTopology.DefaultConfig.FragmentSize * 128;
                }
                _channels[i] = new ChannelBuffer(this, actualPacketSize, (byte)i, IsReliableQoS(qos.QOS));
            }
        }

        // Track whether Dispose has been called.
        bool m_Disposed;

        ~NetConnection()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!m_Disposed && _channels != null)
            {
                for (int i = 0; i < _channels.Length; i++)
                {
                    _channels[i].Dispose();
                }
            }
            _channels = null;

            m_Disposed = true;
        }

        static bool IsReliableQoS(QosType qos)
        {
            return qos == QosType.Reliable || qos == QosType.ReliableFragmented || qos == QosType.ReliableSequenced || qos == QosType.ReliableStateUpdate;
        }

        public bool SetChannelOption(int channelId, ChannelOption option, int value)
        {
            if (_channels == null)
                return false;

            if (channelId < 0 || channelId >= _channels.Length)
                return false;

            return _channels[channelId].SetOption(option, value);
        }

        public void Disconnect()
        {
            if (HostId < 0) return;
            NetworkTransport.Disconnect(HostId, LocalConnectionId, out _);
        }

        public void InitializeHandlers(NetworkHandlers handlers)
        {
            _networkHandlers = handlers;
        }

        public void FlushChannels()
        {
            if (_channels == null)
            {
                return;
            }
            foreach (ChannelBuffer channel in _channels)
            {
                channel.CheckInternalBuffer();
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
                channel.maxDelay = seconds;
            }
        }

        internal bool SendByChannel(NetworkPacketBase packet, int channelId)
        {
            var writer = new NetWriter();
            writer.StartMessage(packet.Code());
            packet.Serialize(writer);
            writer.FinishMessage();
            return SendWriter(writer, channelId);
        }

        internal bool SendByChannel(IMessage packet, int channelId)
        {
            return SendByChannel(packet.Code(), packet.ToByteArray(), channelId);
        }

        internal bool SendByChannel(uint code, byte[] data, int channelId)
        {
            var writer = new NetWriter();
            writer.StartMessage(code);
            foreach(var b in data) writer.Write(b);
            writer.FinishMessage();
            return SendWriter(writer, channelId);
        }

        private bool SendWriter(NetWriter writer, int channelId)
        {
            if (DebugPackets)
            {
                LogPacket(Direction.OUTCOMING, writer.ToArray());
            }
            return CheckChannel(channelId) && _channels[channelId].SendWriter(writer);
        }

        private enum Direction
        {
            INCOMING,
            OUTCOMING,
        }

        private void LogPacket(Direction direction, byte[] bytes)
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

        bool CheckChannel(int channelId)
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

        public void ResetStats()
        {
#if UNITY_EDITOR
            for (ushort i = 0; i < _maxPacketStats; i++)
            {
                if (PacketStats.ContainsKey(i))
                {
                    var value = PacketStats[i];
                    value.Count = 0;
                    value.Bytes = 0;
                    NetworkTransport.SetPacketStat(0, i, 0, 0);
                    NetworkTransport.SetPacketStat(1, i, 0, 0);
                }
            }
#endif
        }

        private static readonly uint _osp = new ObjectSyncPacket().Code();
        private static readonly uint _osc = new ObjectSyncDeltaCreatePacket().Code();
        private static readonly uint _osd = new ObjectSyncDeltaDestroyPacket().Code();

        protected void HandleBytesReceived(byte[] bytes, int channelId)
        {
            if (DebugPackets)
            {
                LogPacket(Direction.INCOMING, bytes);
            }

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
                DebugMessege.Log("Unknown message ID " + code + " connId:" + LocalConnectionId, DebugLevel.ERROR);
                return;
            }

            LastReceivedTime = Time.realtimeSinceStartup;
#if UNITY_EDITOR
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

            if (PacketStats.TryGetValue(code, out PacketStat stat))
            {
                stat = PacketStats[code];
                stat.Count += 1;
                stat.Bytes += size;
            }
            else
            {
                PacketStats.Add(code, new PacketStat
                {
                    Code = code,
                    Count = 1,
                    Bytes = size
                });
            }
#endif
        }

        public virtual void GetStatsOut(out int numMsgs, out int numBufferedMsgs, out int numBytes, out int lastBufferedPerSecond)
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

        public virtual void GetStatsIn(out int numMsgs, out int numBytes)
        {
            numMsgs = 0;
            numBytes = 0;

            foreach (ChannelBuffer channel in _channels)
            {
                numMsgs += channel.NumMsgsIn;
                numBytes += channel.NumBytesIn;
            }
        }

        public override string ToString()
        {
            return string.Format("hostId: {0} connectionId: {1} channel count: {2}", HostId, LocalConnectionId, (_channels != null ? _channels.Length : 0));
        }

        public virtual void TransportReceive(byte[] bytes, int numBytes, int channelId)
        {
            if (numBytes >= bytes.Length)
            {
                DebugMessege.Log("Received number of bytes received [" + numBytes + "] are greater than maximum size [" + bytes.Length + "].", DebugLevel.ERROR);
                return;
            }
            HandleBytesReceived(bytes, channelId);
        }

        public virtual bool TransportSend(byte[] bytes, int numBytes, int channelId, out byte error)
        {
            return NetworkTransport.Send(HostId, LocalConnectionId, channelId, bytes, numBytes, out error);
        }
    }
}

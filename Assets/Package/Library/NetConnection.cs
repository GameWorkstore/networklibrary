using GameWorkstore.Patterns;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace GameWorkstore.NetworkLibrary
{
    /*
    * wire protocol is a list of :   size   |  msgType     | payload
    *					            (short)  (variable)   (buffer)
    */
    public class NetConnection : IDisposable
    {
        private ChannelBuffer[] _channels;

        private readonly NetMessage _netMessage = new NetMessage();
        private NetWriter _writer = new NetWriter();
        private NetworkHandlers _networkHandlers;
        //private readonly NetMessage _messageInfo = new NetMessage();
        private const int _maxPacketLogSize = 150;

        public short HostId = -1;
        public short LocalConnectionId = -1;
        public short ServerConnectionId = -1;
        public float InitializedTime = 0;
        public float LastReceivedTime = 0;
        public bool DebugPackets = false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034")]
        public class PacketStat
        {
            public ushort Code;
            public int Count;
            public int Bytes;

            public override string ToString()
            {
                return UnityTransportTypes.TypeToString(Code) + ": count=" + Count + " bytes=" + Bytes;
            }
        }

        internal Dictionary<ushort, PacketStat> PacketStats { get; } = new Dictionary<ushort, PacketStat>();

#if UNITY_EDITOR
        private const ushort _maxPacketStats = 255; //the same as maximum message types
#endif

        public NetConnection(short hostId, short localConnectionId, HostTopology hostTopology, float initializedTime, NetworkHandlers networkHandlers)
        {
            _writer = new NetWriter();
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
            return (qos == QosType.Reliable || qos == QosType.ReliableFragmented || qos == QosType.ReliableSequenced || qos == QosType.ReliableStateUpdate);
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
            _writer.StartMessage(packet.Code);
            packet.Serialize(_writer);
            _writer.FinishMessage();
            return SendWriter(_writer, channelId);
        }

        /*public virtual bool SendBytes(byte[] bytes, int numBytes, int channelId)
        {
            if (DebugPackets)
            {
                LogSend(bytes);
            }
            return CheckChannel(channelId) && _channels[channelId].SendBytes(bytes, numBytes);
        }*/

        private bool SendWriter(NetWriter writer, int channelId)
        {
            if (DebugPackets)
            {
                LogSend(writer.ToArray());
            }
            return CheckChannel(channelId) && _channels[channelId].SendWriter(writer);
        }

        private void LogSend(byte[] bytes)
        {
            NetReader reader = new NetReader(bytes);
            var size = reader.ReadUshort();
            var code = reader.ReadUshort();

            const int k_PayloadStartPosition = 4;

            var builder = new StringBuilder();
            for (int i = k_PayloadStartPosition; i < k_PayloadStartPosition + size; i++)
            {
                builder.AppendFormat("{0:X2}", bytes[i]);
                if (i > _maxPacketLogSize) break;
            }
            DebugMessege.Log("ConnectionSend con:" + LocalConnectionId + " bytes:" + size + " code:" + code + " " + builder, DebugLevel.INFO);
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

        protected void HandleBytes(
            byte[] buffer,
            int receivedSize,
            int channelId)
        {
            // build the stream form the buffer passed in
            NetReader reader = new NetReader(buffer);

            HandleReader(reader, receivedSize, channelId);
        }

        private static readonly ObjectSyncPacket _osp = new ObjectSyncPacket();
        private static readonly ObjectSyncDeltaCreatePacket _osdcp = new ObjectSyncDeltaCreatePacket();
        private static readonly ObjectSyncDeltaDestroyPacket _osddp = new ObjectSyncDeltaDestroyPacket();

        protected void HandleReader(NetReader reader, int receivedSize, int channelId)
        {
            // read until size is reached.
            // NOTE: stream.Capacity is 1300, NOT the size of the available data
            while (reader.Position < receivedSize)
            {
                // the reader passed to user code has a copy of bytes from the real stream. user code never touches the real stream.
                // this ensures it can never get out of sync if user code reads less or more than the real amount.
                var size = reader.ReadUshort();
                var code = reader.ReadUshort();

                // create a reader just for this message
                byte[] buffer = reader.ReadBytes(size);
                NetReader packetReader = new NetReader(buffer);

                if (DebugPackets)
                {
                    StringBuilder msg = new StringBuilder();
                    for (int i = 0; i < size; i++)
                    {
                        msg.AppendFormat("{0:X2}", buffer[i]);
                        if (i > _maxPacketLogSize) break;
                    }
                    DebugMessege.Log("ConnectionRecv con:" + LocalConnectionId + " bytes:" + size + " msgId:" + code + " " + msg, DebugLevel.INFO);
                }

                _netMessage.Type = code;
                _netMessage.Reader = packetReader;
                _netMessage.Conn = this;
                _netMessage.ChannelId = channelId;

                if (_networkHandlers.Invoke(code, _netMessage))
                {
                    LastReceivedTime = Time.realtimeSinceStartup;
#if UNITY_EDITOR
                    NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.LLAPIMsg, "msg", 1);

                    if (code > UnityTransportTypes.Highest)
                    {
                        if (code == _osp.Code)
                            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.ObjectSpawnScene, code.ToString() + ":" + code.GetType().Name, 1);
                        else if (code == _osdcp.Code)
                            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.ObjectSpawn, code.ToString() + ":" + code.GetType().Name, 1);
                        else if (code == _osddp.Code)
                            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.ObjectDestroy, code.ToString() + ":" + code.GetType().Name, 1);
                        //else if (channelId > 2)
                        //NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, MsgType.UpdateVars, msgType.ToString() + ":" + msgType.GetType().Name, sz);
                        //else if (channelId > 1)
                        //NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, MsgType.SyncEvent, msgType.ToString() + ":" + msgType.GetType().Name, sz);
                        else
                            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, UnityTransportTypes.Command, code.ToString() + ":" + code.GetType().Name, 1);
                    }
#endif
#if UNITY_EDITOR
                    PacketStat stat;
                    if (PacketStats.ContainsKey(code))
                    {
                        stat = PacketStats[code];
                        stat.Count += 1;
                        stat.Bytes += size;
                    }
                    else
                    {
                        stat = new PacketStat
                        {
                            Code = code,
                            Count = 1,
                            Bytes = size
                        };
                        PacketStats[code] = stat;
                    }
#endif
                }
                else
                {
                    DebugMessege.Log("Unknown message ID " + code + " connId:" + LocalConnectionId, DebugLevel.ERROR);
                    break;
                }
            }
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
            if (numBytes < bytes.Length)
            {
                HandleBytes(bytes, numBytes, channelId);
            }
        }

        public virtual bool TransportSend(byte[] bytes, int numBytes, int channelId, out byte error)
        {
            return NetworkTransport.Send(HostId, LocalConnectionId, channelId, bytes, numBytes, out error);
        }
    }
}

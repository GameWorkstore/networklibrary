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
        ChannelBuffer[] m_Channels;
        //List<PlayerController> m_PlayerControllers = new List<PlayerController>();
        NetMessage m_NetMsg = new NetMessage();
        //HashSet<NetworkIdentity> m_VisList = new HashSet<NetworkIdentity>();
        //internal HashSet<NetworkIdentity> visList { get { return m_VisList; } }
        NetWriter m_Writer;

        Dictionary<short, NetworkMessageDelegate> m_MessageHandlersDict;
        NetworkMessageHandlers m_MessageHandlers;

        //HashSet<NetworkInstanceId> m_ClientOwnedObjects;
        NetMessage m_MessageInfo = new NetMessage();

        const int k_MaxMessageLogSize = 150;

        public int hostId = -1;
        public int connectionId = -1;
        public bool isReady;
        public string address;
        public float lastMessageTime;
        //public List<PlayerController> playerControllers { get { return m_PlayerControllers; } }
        //public HashSet<NetworkInstanceId> clientOwnedObjects { get { return m_ClientOwnedObjects; } }
        public bool logNetworkMessages = false;
        public bool isConnected { get { return hostId != -1; } }


        public class PacketStat
        {
            public short msgType;
            public int count;
            public int bytes;

            public override string ToString()
            {
                return MsgType.MsgTypeToString(msgType) + ": count=" + count + " bytes=" + bytes;
            }
        }

        Dictionary<short, PacketStat> m_PacketStats = new Dictionary<short, PacketStat>();
        internal Dictionary<short, PacketStat> packetStats { get { return m_PacketStats; } }

#if UNITY_EDITOR
        static readonly int _maxPacketStats = 255;//the same as maximum message types
#endif

        public virtual void Initialize(string networkAddress, int networkHostId, int networkConnectionId, HostTopology hostTopology)
        {
            m_Writer = new NetWriter();
            address = networkAddress;
            hostId = networkHostId;
            connectionId = networkConnectionId;

            int numChannels = hostTopology.DefaultConfig.ChannelCount;
            int packetSize = hostTopology.DefaultConfig.PacketSize;

            if ((hostTopology.DefaultConfig.UsePlatformSpecificProtocols) && (Application.platform != RuntimePlatform.PS4))
            {
                throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
            }

            m_Channels = new ChannelBuffer[numChannels];
            for (int i = 0; i < numChannels; i++)
            {
                var qos = hostTopology.DefaultConfig.Channels[i];
                int actualPacketSize = packetSize;
                if (qos.QOS == QosType.ReliableFragmented || qos.QOS == QosType.UnreliableFragmented)
                {
                    actualPacketSize = hostTopology.DefaultConfig.FragmentSize * 128;
                }
                m_Channels[i] = new ChannelBuffer(this, actualPacketSize, (byte)i, IsReliableQoS(qos.QOS));
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
            if (!m_Disposed && m_Channels != null)
            {
                for (int i = 0; i < m_Channels.Length; i++)
                {
                    m_Channels[i].Dispose();
                }
            }
            m_Channels = null;

            m_Disposed = true;
        }

        static bool IsReliableQoS(QosType qos)
        {
            return (qos == QosType.Reliable || qos == QosType.ReliableFragmented || qos == QosType.ReliableSequenced || qos == QosType.ReliableStateUpdate);
        }

        public bool SetChannelOption(int channelId, ChannelOption option, int value)
        {
            if (m_Channels == null)
                return false;

            if (channelId < 0 || channelId >= m_Channels.Length)
                return false;

            return m_Channels[channelId].SetOption(option, value);
        }

        public NetConnection()
        {
            m_Writer = new NetWriter();
        }

        public void Disconnect()
        {
            address = "";
            isReady = false;
            //ClientScene.HandleClientDisconnect(this);
            if (hostId == -1)
            {
                return;
            }
            byte error;
            NetworkTransport.Disconnect(hostId, connectionId, out error);

            //RemoveObservers();
        }

        public void SetHandlers(NetworkMessageHandlers handlers)
        {
            m_MessageHandlers = handlers;
            m_MessageHandlersDict = handlers.GetHandlers();
        }

        public bool CheckHandler(short msgType)
        {
            return m_MessageHandlersDict.ContainsKey(msgType);
        }

        public bool InvokeHandlerNoData(short msgType)
        {
            return InvokeHandler(msgType, null, 0);
        }

        public bool InvokeHandler(short msgType, NetReader reader, int channelId)
        {
            if (m_MessageHandlersDict.ContainsKey(msgType))
            {
                m_MessageInfo.msgType = msgType;
                m_MessageInfo.conn = this;
                m_MessageInfo.reader = reader;
                m_MessageInfo.channelId = channelId;

                NetworkMessageDelegate msgDelegate = m_MessageHandlersDict[msgType];
                if (msgDelegate == null)
                {
                    if (LogFilter.logError) { Debug.LogError("NetworkConnection InvokeHandler no handler for " + msgType); }
                    return false;
                }
                msgDelegate(m_MessageInfo);
                return true;
            }
            return false;
        }

        public bool InvokeHandler(NetMessage netMsg)
        {
            if (m_MessageHandlersDict.ContainsKey(netMsg.msgType))
            {
                NetworkMessageDelegate msgDelegate = m_MessageHandlersDict[netMsg.msgType];
                msgDelegate(netMsg);
                return true;
            }
            return false;
        }

        public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
        {
            m_MessageHandlers.RegisterHandler(msgType, handler);
        }

        public void UnregisterHandler(short msgType)
        {
            m_MessageHandlers.UnregisterHandler(msgType);
        }

        public void FlushChannels()
        {
            if (m_Channels == null)
            {
                return;
            }
            foreach (ChannelBuffer channel in m_Channels)
            {
                channel.CheckInternalBuffer();
            }
        }

        public void SetMaxDelay(float seconds)
        {
            if (m_Channels == null)
            {
                return;
            }
            foreach (ChannelBuffer channel in m_Channels)
            {
                channel.maxDelay = seconds;
            }
        }

        public virtual bool Send(NetworkPacketBase packet)
        {
            return SendByChannel(packet, Channels.DefaultReliable);
        }

        public virtual bool SendUnreliable(NetworkPacketBase packet)
        {
            return SendByChannel(packet, Channels.DefaultUnreliable);
        }

        public virtual bool SendByChannel(NetworkPacketBase packet, int channelId)
        {
            m_Writer.StartMessage(packet.Code);
            packet.Serialize(m_Writer);
            m_Writer.FinishMessage();
            return SendWriter(m_Writer, channelId);
        }

        public virtual bool SendBytes(byte[] bytes, int numBytes, int channelId)
        {
            if (logNetworkMessages)
            {
                LogSend(bytes);
            }
            return CheckChannel(channelId) && m_Channels[channelId].SendBytes(bytes, numBytes);
        }

        public virtual bool SendWriter(NetWriter writer, int channelId)
        {
            if (logNetworkMessages)
            {
                LogSend(writer.ToArray());
            }
            return CheckChannel(channelId) && m_Channels[channelId].SendWriter(writer);
        }

        void LogSend(byte[] bytes)
        {
            NetReader reader = new NetReader(bytes);
            var msgSize = reader.ReadUInt16();
            var msgId = reader.ReadUInt16();

            const int k_PayloadStartPosition = 4;

            StringBuilder msg = new StringBuilder();
            for (int i = k_PayloadStartPosition; i < k_PayloadStartPosition + msgSize; i++)
            {
                msg.AppendFormat("{0:X2}", bytes[i]);
                if (i > k_MaxMessageLogSize) break;
            }
            Debug.Log("ConnectionSend con:" + connectionId + " bytes:" + msgSize + " msgId:" + msgId + " " + msg);
        }

        bool CheckChannel(int channelId)
        {
            if (m_Channels == null)
            {
                if (LogFilter.logWarn) { Debug.LogWarning("Channels not initialized sending on id '" + channelId); }
                return false;
            }
            if (channelId < 0 || channelId >= m_Channels.Length)
            {
                if (LogFilter.logError) { Debug.LogError("Invalid channel when sending buffered data, '" + channelId + "'. Current channel count is " + m_Channels.Length); }
                return false;
            }
            return true;
        }

        public void ResetStats()
        {
#if UNITY_EDITOR
            for (short i = 0; i < _maxPacketStats; i++)
            {
                if (m_PacketStats.ContainsKey(i))
                {
                    var value = m_PacketStats[i];
                    value.count = 0;
                    value.bytes = 0;
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

        private static ObjectSyncPacket _osp = new ObjectSyncPacket();
        private static ObjectSyncDeltaCreatePacket _osdcp = new ObjectSyncDeltaCreatePacket();
        private static ObjectSyncDeltaDestroyPacket _osddp = new ObjectSyncDeltaDestroyPacket();

        protected void HandleReader(
            NetReader reader,
            int receivedSize,
            int channelId)
        {
            // read until size is reached.
            // NOTE: stream.Capacity is 1300, NOT the size of the available data
            while (reader.Position < receivedSize)
            {
                // the reader passed to user code has a copy of bytes from the real stream. user code never touches the real stream.
                // this ensures it can never get out of sync if user code reads less or more than the real amount.
                ushort sz = reader.ReadUInt16();
                short msgType = reader.ReadInt16();

                // create a reader just for this message
                byte[] msgBuffer = reader.ReadBytes(sz);
                NetReader msgReader = new NetReader(msgBuffer);

                if (logNetworkMessages)
                {
                    StringBuilder msg = new StringBuilder();
                    for (int i = 0; i < sz; i++)
                    {
                        msg.AppendFormat("{0:X2}", msgBuffer[i]);
                        if (i > k_MaxMessageLogSize) break;
                    }
                    Debug.Log("ConnectionRecv con:" + connectionId + " bytes:" + sz + " msgId:" + msgType + " " + msg);
                }

                NetworkMessageDelegate msgDelegate = null;
                if (m_MessageHandlersDict.ContainsKey(msgType))
                {
                    msgDelegate = m_MessageHandlersDict[msgType];
                }
                if (msgDelegate != null)
                {
                    m_NetMsg.msgType = msgType;
                    m_NetMsg.reader = msgReader;
                    m_NetMsg.conn = this;
                    m_NetMsg.channelId = channelId;
                    msgDelegate(m_NetMsg);
                    lastMessageTime = Time.realtimeSinceStartup;

#if UNITY_EDITOR
                    NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, MsgType.HLAPIMsg, "msg", 1);

                    if (msgType > MsgType.Highest)
                    {
                        if (msgType == _osp.Code)
                            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, MsgType.ObjectSpawnScene, msgType.ToString() + ":" + msgType.GetType().Name, 1);
                        else if (msgType == _osdcp.Code)
                            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, MsgType.ObjectSpawn, msgType.ToString() + ":" + msgType.GetType().Name, 1);
                        else if (msgType == _osddp.Code)
                            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, MsgType.ObjectDestroy, msgType.ToString() + ":" + msgType.GetType().Name, 1);
                        //else if (channelId > 2)
                            //NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, MsgType.UpdateVars, msgType.ToString() + ":" + msgType.GetType().Name, sz);
                        //else if (channelId > 1)
                            //NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, MsgType.SyncEvent, msgType.ToString() + ":" + msgType.GetType().Name, sz);
                        else
                            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Incoming, MsgType.Command, msgType.ToString() + ":" + msgType.GetType().Name, 1);
                    }
#endif
#if UNITY_EDITOR
                    PacketStat stat;
                    if (m_PacketStats.ContainsKey(msgType))
                    {
                        stat = m_PacketStats[msgType];
                        stat.count += 1;
                        stat.bytes += sz;
                    }
                    else
                    {
                        stat = new PacketStat();
                        stat.msgType = msgType;
                        stat.count += 1;
                        stat.bytes += sz;
                        m_PacketStats[msgType] = stat;
                    }
#endif
                }
                else
                {
                    //NOTE: this throws away the rest of the buffer. Need moar error codes
                    if (LogFilter.logError) { Debug.LogError("Unknown message ID " + msgType + " connId:" + connectionId); }
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

            foreach (ChannelBuffer channel in m_Channels)
            {
                numMsgs += channel.numMsgsOut;
                numBufferedMsgs += channel.numBufferedMsgsOut;
                numBytes += channel.numBytesOut;
                lastBufferedPerSecond += channel.lastBufferedPerSecond;
            }
        }

        public virtual void GetStatsIn(out int numMsgs, out int numBytes)
        {
            numMsgs = 0;
            numBytes = 0;

            foreach (ChannelBuffer channel in m_Channels)
            {
                numMsgs += channel.numMsgsIn;
                numBytes += channel.numBytesIn;
            }
        }

        public override string ToString()
        {
            return string.Format("hostId: {0} connectionId: {1} isReady: {2} channel count: {3}", hostId, connectionId, isReady, (m_Channels != null ? m_Channels.Length : 0));
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
            return NetworkTransport.Send(hostId, connectionId, channelId, bytes, numBytes, out error);
        }
    }
}

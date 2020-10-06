using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using GameWorkstore.Patterns;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class BaseConnectionService : IService
    {
        private EventService _eventService;

        protected int SOCKETID;
        protected int PORT = 8080;
        protected int MATCHSIZE = 8;
        protected int BOTSIZE = 0;
        protected NetworkMessageHandlers _handlers = new NetworkMessageHandlers();
        protected Dictionary<int, NetConnection> _connections = new Dictionary<int, NetConnection>();

        private static int NETWORK_INITIALIZATION = 0;
        private static uint UNIQUEID = 1;

        public byte CHANNEL_STATE_COUNT = 0;
        public byte CHANNEL_RELIABLE_ORDERED { get; private set; }
        public byte CHANNEL_RELIABLE { get; private set; }
        public byte CHANNEL_ALLCOSTDELIVERY { get; private set; }
        public byte[] CHANNEL_STATECHANNEL { get; private set; }

        private int outConnectionId;
        private int outChannelId;
        private const int bufferSize = 1024;
        private int receiveSize;
        byte[] buffer = new byte[1024];
        byte error;
        NetworkEventType evnt;

        private const short _isAliveCode = 2;
        private const float _queueStayAliveTime = 1f;
        private float _lastStayAliveSolver;
        private static readonly NetworkAlivePacket _stayAlivePacket = new NetworkAlivePacket();

        private DataReceived _current;

        public override void Preprocess()
        {
            Application.runInBackground = true;
            _eventService = ServiceProvider.GetService<EventService>();

            InitTransportLayer();
            SOCKETID = -1;
            AddHandler<NetworkAlivePacket>(IsAlive);
#if UNITY_EDITOR
            NetworkDetailStats.ResetAll();
#endif
        }

        private static void IsAlive(NetMessage evt) { }

        public override void Postprocess()
        {
            DestroyTransportLayer();
        }

        protected virtual void UpdateConnection()
        {
            if (!HasSocket()) return;
            while ((evnt = NetworkTransport.ReceiveFromHost(SOCKETID, out outConnectionId, out outChannelId, buffer, bufferSize, out receiveSize, out error)) != NetworkEventType.Nothing)
            {
                switch (evnt)
                {
                    case NetworkEventType.ConnectEvent: HandleConnection(outConnectionId, error); break;
                    case NetworkEventType.DisconnectEvent: HandleDisconnection(outConnectionId, error); break;
                    case NetworkEventType.DataEvent:
                        if (PING_SIMULATION)
                            SimulateDataReceived(outConnectionId, outChannelId, ref buffer, receiveSize, error);
                        else
                            HandleDataReceived(outConnectionId, outChannelId, ref buffer, receiveSize, error);
                        break;
                    case NetworkEventType.Nothing: break;
                    default: Log("Unknown network message type received: " + evnt, DebugLevel.INFO); break;
                }
                if (!HasSocket()) return;
            }
            if (!HasSocket()) return;

            if (PING_SIMULATION)
            {
                RunSimulation();
            }
            if (SimulationTime() > _lastStayAliveSolver + _queueStayAliveTime)
            {
                foreach (var conn in _connections)
                {
                    conn.Value.SendByChannel(_isAliveCode, _stayAlivePacket, CHANNEL_RELIABLE_ORDERED);
                }
                _lastStayAliveSolver = SimulationTime();
            }
#if UNITY_EDITOR
            NetworkDetailStats.NewProfilerTick(Time.time);
#endif
        }

        protected bool HasSocket()
        {
            return SOCKETID > -1;
        }

        protected bool OpenSocket(int port = 0)
        {
            if (HasSocket())
            {
                Log("Cannot open socket because socket is already open.", DebugLevel.ERROR);
                return false;
            }

            SOCKETID = NetworkTransport.AddHost(GetHostTopology(), port);
            bool socketInitialized = HasSocket();
            if (socketInitialized)
            {
                _eventService.Update.Register(UpdateConnection);
            }

            return socketInitialized;
        }

        protected void CloseSocket()
        {
            if (!HasSocket())
            {
                Log("Cannot close socket because socket isn't open.", DebugLevel.ERROR);
                return;
            }
            _eventService.Update.Unregister(UpdateConnection);
            NetworkTransport.RemoveHost(SOCKETID);
            SOCKETID = -1;
        }

        protected static void InitTransportLayer()
        {
            if (NETWORK_INITIALIZATION == 0)
            {
                NetworkTransport.Init();
            }
            NETWORK_INITIALIZATION += 1;
        }

        protected static void DestroyTransportLayer()
        {
            NETWORK_INITIALIZATION -= 1;
            if (NETWORK_INITIALIZATION == 0)
            {
                NetworkTransport.Shutdown();
            }
        }

        protected virtual HostTopology GetHostTopology()
        {
            ConnectionConfig config = new ConnectionConfig
            {
                //AllCostTimeout = 17,
                MaxCombinedReliableMessageCount = 4,
                //PacketSize = 1200,
                AckDelay = 26
            };

            CHANNEL_RELIABLE = config.AddChannel(QosType.Reliable);
            CHANNEL_RELIABLE_ORDERED = config.AddChannel(QosType.ReliableSequenced);
            CHANNEL_ALLCOSTDELIVERY = config.AddChannel(QosType.AllCostDelivery);
            if (CHANNEL_STATECHANNEL == null)
            {
                CHANNEL_STATECHANNEL = new byte[CHANNEL_STATE_COUNT];
            }
            for (int i = 0; i < CHANNEL_STATECHANNEL.Length; i++)
            {
                CHANNEL_STATECHANNEL[i] = config.AddChannel(QosType.StateUpdate);
            }

            return new HostTopology(config, MATCHSIZE);
        }

        internal static NetworkInstanceId GetUniqueId()
        {
            UNIQUEID += 1;
            return new NetworkInstanceId(UNIQUEID);
        }

        internal int GetRTT(int connectionId)
        {
            byte err;
            int rtt = NetworkTransport.GetCurrentRTT(SOCKETID, connectionId, out err);
            return PING_SIMULATION ? (PING_SIMULATED > rtt ? PING_SIMULATED : rtt) : rtt;
        }

        protected bool IsOk(byte error)
        {
            return (NetworkError)error == NetworkError.Ok;
        }

        protected void AddConnectionToPool(NetConnection conn)
        {
            conn.SetHandlers(_handlers);
            _connections.Add(conn.connectionId, conn);
        }

        protected void RemoveConnectionFromPool(int connectionId)
        {
            _connections.Remove(connectionId);
        }

        public void AddHandler<T>(NetworkMessageDelegate function) where T : NetworkPacketBase, new()
        {
            var packet = new T();
            _handlers.RegisterHandler(packet.Code, function);
        }

        public void RemoveHandler<T>() where T : NetworkPacketBase, new()
        {
            var packet = new T();
            _handlers.UnregisterHandler(packet.Code);
        }

        protected abstract void HandleConnection(int connectionId, byte error);
        protected abstract void HandleDisconnection(int connectionId, byte error);
        protected abstract void HandleDataReceived(int connectionId, int channelId, ref byte[] buffer, int receiveSize, byte error);

        public abstract void Log(string msg, DebugLevel level);

        public bool PING_SIMULATION = false;
        public int PING_SIMULATED = 0;
        private Queue<DataReceived> _dataReceived = new Queue<DataReceived>();

        internal class DataReceived
        {
            internal float timestamp;
            internal int connectionId;
            internal int channelId;
            internal byte[] buffer;
            internal int receivedSize;
            internal byte error;
        }

        private void SimulateDataReceived(int outConnectionId, int outChannelId, ref byte[] buffer, int outReceiveSize, byte error)
        {
            if (!IsOk(error))
            {
                Log("Bad Packet received:" + (NetworkError)error, DebugLevel.ERROR);
                return;
            }
            float rt = SimulationTime();
            byte err;
            int rtt = NetworkTransport.GetCurrentRTT(SOCKETID, outConnectionId, out err);
            DataReceived rc = new DataReceived() { connectionId = outConnectionId, channelId = outChannelId, error = error, timestamp = rt + (PING_SIMULATED - rtt) / 1000f, receivedSize = outReceiveSize, buffer = ArrayPool<byte>.GetBuffer(1024) };
            if (receiveSize >= 1024)
            {
                Log("Packet exceeding limit of 1024 bytes:" + receiveSize, DebugLevel.ERROR);
                return;
            }
            Array.Copy(buffer, rc.buffer, receiveSize);
            _dataReceived.Enqueue(rc);
        }

        private void RunSimulation()
        {
            while (_dataReceived.Count > 0 || _current != null)
            {
                if (_current == null)
                {
                    _current = _dataReceived.Dequeue();
                }
                if (_current.timestamp < SimulationTime())
                {
                    HandleDataReceived(_current.connectionId, _current.channelId, ref _current.buffer, _current.receivedSize, _current.error);
                    ArrayPool<byte>.FreeBuffer(_current.buffer);
                    _current.buffer = null;
                    _current = null;
                }
                else
                {
                    return;
                }
            }
        }

        public static float SimulationTime()
        {
            return Time.realtimeSinceStartup;
        }
    }
}

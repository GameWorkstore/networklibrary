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

        protected short SOCKETID;
        protected int PORT = 8080;
        protected int PRECONNECTIONSIZE = 8;
        protected int MATCHSIZE = 8;
        protected int BOTSIZE = 0;
        protected NetworkHandlers _prehandlers = new NetworkHandlers();
        protected NetworkHandlers _handlers = new NetworkHandlers();
        protected Dictionary<short, NetConnection> _preconnections = new Dictionary<short, NetConnection>();
        protected Dictionary<short, NetConnection> _connections = new Dictionary<short, NetConnection>();

        private static int NETWORK_INITIALIZATION = 0;
        private static uint UNIQUEID = 1;

        public byte CHANNEL_STATE_COUNT = 0;
        public byte CHANNEL_RELIABLE_ORDERED { get; private set; }
        public byte CHANNEL_RELIABLE { get; private set; }
        public byte CHANNEL_ALLCOSTDELIVERY { get; private set; }
        public byte CHANNEL_UNRELIABLE { get; private set; }
        public byte[] CHANNEL_STATECHANNEL { get; private set; }

        private int _outConnectionId;
        private int _outChannelId;
        private const int bufferSize = 1024;
        private int receiveSize;
        private byte[] buffer = new byte[1024];
        private byte error;
        private NetworkEventType evnt;

        private float _lastStayAliveSolver = 0;
        private const float _queueStayAliveTime = 1f;
        private static readonly NetworkAlivePacket _stayAlivePacket = new NetworkAlivePacket();
        private DataReceived _current;

        public override void Preprocess()
        {
            Application.runInBackground = true;
            _eventService = ServiceProvider.GetService<EventService>();

            InitTransportLayer();
            SOCKETID = -1;
            AddHandler<NetworkAlivePacket>(IsAlive);
            AddHandler<NetworkAlivePacket>(IsAlive, true);
#if UNITY_EDITOR
            NetworkDetailStats.ResetAll();
#endif
        }

        private static void IsAlive(NetworkAlivePacket evt) { }

        public override void Postprocess()
        {
            RemoveHandler<NetworkAlivePacket>(IsAlive, true);
            RemoveHandler<NetworkAlivePacket>(IsAlive);
            DestroyTransportLayer();
        }

        protected virtual void UpdateConnection()
        {
            while (HasSocket())
            {
                evnt = NetworkTransport.ReceiveFromHost(SOCKETID, out _outConnectionId, out _outChannelId, buffer, bufferSize, out receiveSize, out error);
                //if nothing to process, break
                if (evnt == NetworkEventType.Nothing) break;

                switch (evnt)
                {
                    case NetworkEventType.ConnectEvent:
                        HandleConnect((short)_outConnectionId, error);
                        break;
                    case NetworkEventType.DisconnectEvent:
                        HandleDisconnect((short)_outConnectionId, error);
                        //clear simulation
                        _dataReceived.Clear();
                        //stop
                        return;
                    case NetworkEventType.DataEvent:
                        if (PING_SIMULATION)
                            SimulateDataReceived((short)_outConnectionId, _outChannelId, ref buffer, receiveSize, error);
                        else
                            HandleDataReceived((short)_outConnectionId, _outChannelId, ref buffer, receiveSize, error);
                        break;
                    case NetworkEventType.Nothing: break;
                    default: Log("Unknown network message type received: " + evnt, DebugLevel.INFO); break;
                }
            }

            // deliver simulated packets
            if (PING_SIMULATION)
            {
                RunSimulation();
            }

            // deliver stay alive packets
            if (SimulationTime() > _lastStayAliveSolver + _queueStayAliveTime)
            {
                foreach (var preconn in _preconnections)
                {
                    preconn.Value.SendByChannel(_stayAlivePacket, CHANNEL_UNRELIABLE);
                }
                foreach (var conn in _connections)
                {
                    conn.Value.SendByChannel(_stayAlivePacket, CHANNEL_UNRELIABLE);
                }
                _lastStayAliveSolver = SimulationTime();
            }

            // flush all channels
            foreach (var preconn in _preconnections)
            {
                preconn.Value.FlushChannels();
            }
            foreach (var conn in _connections)
            {
                conn.Value.FlushChannels();
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

            SOCKETID = (short)NetworkTransport.AddHost(GetHostTopology(), port);
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

        protected HostTopology GetHostTopology()
        {
            var config = new ConnectionConfig
            {
                //AllCostTimeout = 17,
                MaxCombinedReliableMessageCount = 4,
                //PacketSize = 1200,
                AckDelay = 26
            };

            CHANNEL_RELIABLE = config.AddChannel(QosType.Reliable);
            CHANNEL_RELIABLE_ORDERED = config.AddChannel(QosType.ReliableSequenced);
            CHANNEL_ALLCOSTDELIVERY = config.AddChannel(QosType.AllCostDelivery);
            CHANNEL_UNRELIABLE = config.AddChannel(QosType.Unreliable);
            if (CHANNEL_STATECHANNEL == null)
            {
                CHANNEL_STATECHANNEL = new byte[CHANNEL_STATE_COUNT];
            }
            for (int i = 0; i < CHANNEL_STATECHANNEL.Length; i++)
            {
                CHANNEL_STATECHANNEL[i] = config.AddChannel(QosType.StateUpdate);
            }

            return new HostTopology(config, MATCHSIZE + PRECONNECTIONSIZE);
        }

        internal static NetworkInstanceId GetUniqueId()
        {
            UNIQUEID += 1;
            return new NetworkInstanceId(UNIQUEID);
        }

        internal int GetRTT(int connectionId)
        {
            int rtt = NetworkTransport.GetCurrentRTT(SOCKETID, connectionId, out _);
            return PING_SIMULATION ? (PING_SIMULATED > rtt ? PING_SIMULATED : rtt) : rtt;
        }

        protected static bool IsOk(byte error)
        {
            return (NetworkError)error == NetworkError.Ok;
        }

        protected NetConnection CreatePreconnection(short connectionId)
        {
            var conn = new NetConnection(SOCKETID, connectionId, GetHostTopology(), SimulationTime(), _prehandlers);
            _preconnections.Add(connectionId, conn);
            return conn;
        }

        protected bool RemovePreconnection(short connectionId)
        {
            return _preconnections.Remove(connectionId);
        }

        protected NetConnection CreateConnection(short connectionId)
        {
            var conn = new NetConnection(SOCKETID, connectionId, GetHostTopology(), SimulationTime(), _handlers);
            _connections.Add(connectionId, conn);
            return conn;
        }

        protected bool RemoveConnection(short connectionId)
        {
            return _connections.Remove(connectionId);
        }

        public void AddHandler<T>(Action<T> function, bool prehandler = false) where T : NetworkPacketBase, new()
        {
            var packet = new T();
            if (prehandler)
            {
                _prehandlers.RegisterHandler(packet.Code, function);
            }
            else
            {
                _handlers.RegisterHandler(packet.Code, function);
            }
        }

        public bool RemoveHandler<T>(Action<T> function, bool prehandler = false) where T : NetworkPacketBase, new()
        {
            var packet = new T();
            if (prehandler)
            {
                return _prehandlers.UnregisterHandler(packet.Code, function);
            }
            return _handlers.UnregisterHandler(packet.Code, function);
        }

        protected abstract void HandleConnect(short connectionId, byte error);
        protected abstract void HandleDisconnect(short connectionId, byte error);
        protected abstract void HandleDataReceived(short connectionId, int channelId, ref byte[] buffer, int receiveSize, byte error);

        public abstract void Log(string msg, DebugLevel level);

        public bool PING_SIMULATION = false;
        public int PING_SIMULATED = 0;
        private readonly Queue<DataReceived> _dataReceived = new Queue<DataReceived>();

        internal class DataReceived
        {
            internal float timestamp;
            internal short connectionId;
            internal int channelId;
            internal byte[] buffer;
            internal int receivedSize;
            internal byte error;
        }

        private void SimulateDataReceived(short outConnectionId, int outChannelId, ref byte[] buffer, int outReceiveSize, byte error)
        {
            if (!IsOk(error))
            {
                Log("Bad Packet received:" + (NetworkError)error, DebugLevel.ERROR);
                return;
            }
            float rt = SimulationTime();
            int rtt = NetworkTransport.GetCurrentRTT(SOCKETID, outConnectionId, out _);
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

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using GameWorkstore.Patterns;
using Google.Protobuf;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class BaseConnection : IDisposable
    {
        private readonly EventService _eventService;

        protected short SocketId;
        protected int Port = 8080;
        protected int PreConnectionSize = 8;
        protected int MatchSize = 8;
        protected int BotSize = 0;
        protected readonly NetworkHandlers PreHandlers = new NetworkHandlers();
        protected readonly NetworkHandlers Handlers = new NetworkHandlers();
        protected readonly Dictionary<short, NetConnection> PreConnections = new Dictionary<short, NetConnection>();
        protected readonly Dictionary<short, NetConnection> Connections = new Dictionary<short, NetConnection>();

        private static int _networkInitialization;
        private static uint _uniqueId = 1;

        public byte ChannelStateCount = 0;
        public byte ChannelReliableOrdered { get; private set; }
        public byte ChannelReliable { get; private set; }
        public byte ChannelAllCostDelivery { get; private set; }
        public byte ChannelUnreliable { get; private set; }
        public byte[] ChannelStateStream { get; private set; }

        private int _outConnectionId;
        private int _outChannelId;
        private const int _bufferSize = 1024;
        private int _receiveSize;
        private byte[] _buffer = new byte[1024];
        private byte _error;
        private NetworkEventType _evt;

        private float _lastStayAliveSolver;
        private const float _queueStayAliveTime = 2f;
        private static readonly NetworkAlivePacket _stayAlivePacket = new NetworkAlivePacket();
        private DataReceived _current;

        protected BaseConnection()
        {
            Application.runInBackground = true;
            _eventService = ServiceProvider.GetService<EventService>();

            InitTransportLayer();
            SocketId = -1;
            AddHandler<NetworkAlivePacket>(IsAlive);
            AddHandler<NetworkAlivePacket>(IsAlive, true);
#if UNITY_EDITOR
            NetworkDetailStats.ResetAll();
#endif
        }
        
        public virtual void Dispose()
        {
            RemoveHandler<NetworkAlivePacket>(IsAlive, true);
            RemoveHandler<NetworkAlivePacket>(IsAlive);
            DestroyTransportLayer();
        }

        protected static void IsAlive(NetworkAlivePacket evt) { }

        protected virtual void UpdateConnection()
        {
            while (HasSocket())
            {
                _evt = NetworkTransport.ReceiveFromHost(SocketId, out _outConnectionId, out _outChannelId, _buffer, _bufferSize, out _receiveSize, out _error);
                //if nothing to process, break
                if (_evt == NetworkEventType.Nothing) break;

                switch (_evt)
                {
                    case NetworkEventType.ConnectEvent:
                        HandleConnect((short)_outConnectionId, _error);
                        break;
                    case NetworkEventType.DisconnectEvent:
                        HandleDisconnect((short)_outConnectionId, _error);
                        //clear simulation
                        _dataReceived.Clear();
                        //stop
                        return;
                    case NetworkEventType.DataEvent:
                        if (PING_SIMULATION)
                            SimulateDataReceived((short)_outConnectionId, _outChannelId, ref _buffer, _receiveSize, _error);
                        else
                            HandleDataReceived((short)_outConnectionId, _outChannelId, ref _buffer, _receiveSize, _error);
                        break;
                    case NetworkEventType.Nothing: break;
                    default: Log("Unknown network message type received: " + _evt, DebugLevel.INFO); break;
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
                foreach (var conn in PreConnections)
                {
                    conn.Value.SendByChannel(_stayAlivePacket, ChannelUnreliable);
                }
                foreach (var conn in Connections)
                {
                    conn.Value.SendByChannel(_stayAlivePacket, ChannelUnreliable);
                }
                _lastStayAliveSolver = SimulationTime();
            }

            // flush all channels
            foreach (var conn in PreConnections)
            {
                conn.Value.FlushChannels();
            }
            foreach (var conn in Connections)
            {
                conn.Value.FlushChannels();
            }
#if UNITY_EDITOR
            NetworkDetailStats.NewProfilerTick(Time.time);
#endif
        }

        protected bool HasSocket()
        {
            return SocketId > -1;
        }

        protected bool OpenSocket(int port = 0)
        {
            if (HasSocket())
            {
                Log("Cannot open socket because socket is already open.", DebugLevel.ERROR);
                return false;
            }

            SocketId = (short)NetworkTransport.AddHost(GetHostTopology(), port);
            var socketInitialized = HasSocket();
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
            NetworkTransport.RemoveHost(SocketId);
            SocketId = -1;
        }

        protected static void InitTransportLayer()
        {
            if (_networkInitialization == 0)
            {
                NetworkTransport.Init();
            }
            _networkInitialization += 1;
        }

        protected static void DestroyTransportLayer()
        {
            _networkInitialization -= 1;
            if (_networkInitialization == 0)
            {
                NetworkTransport.Shutdown();
            }
        }

        public static int TransportLayerInitializations()
        {
            return _networkInitialization;
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

            ChannelReliable = config.AddChannel(QosType.Reliable);
            ChannelReliableOrdered = config.AddChannel(QosType.ReliableSequenced);
            ChannelAllCostDelivery = config.AddChannel(QosType.AllCostDelivery);
            ChannelUnreliable = config.AddChannel(QosType.Unreliable);
            ChannelStateStream = new byte[ChannelStateCount];
            for (var i = 0; i < ChannelStateStream.Length; i++)
            {
                ChannelStateStream[i] = config.AddChannel(QosType.StateUpdate);
            }

            return new HostTopology(config, MatchSize + PreConnectionSize);
        }

        internal static NetworkInstanceId GetUniqueId()
        {
            _uniqueId += 1;
            return new NetworkInstanceId(_uniqueId);
        }

        internal int GetRTT(int connectionId)
        {
            var rtt = NetworkTransport.GetCurrentRTT(SocketId, connectionId, out _);
            return PING_SIMULATION ? (PING_SIMULATED > rtt ? PING_SIMULATED : rtt) : rtt;
        }

        protected static bool IsOk(byte error)
        {
            return (NetworkError)error == NetworkError.Ok;
        }

        protected NetConnection CreatePreConnection(short connectionId)
        {
            var conn = new NetConnection(SocketId, connectionId, GetHostTopology(), SimulationTime(), PreHandlers);
            PreConnections.Add(connectionId, conn);
            return conn;
        }

        protected bool RemovePreConnection(short connectionId)
        {
            return PreConnections.Remove(connectionId);
        }

        protected NetConnection CreateConnection(short connectionId)
        {
            var conn = new NetConnection(SocketId, connectionId, GetHostTopology(), SimulationTime(), Handlers);
            Connections.Add(connectionId, conn);
            return conn;
        }

        protected bool RemoveConnection(short connectionId)
        {
            return Connections.Remove(connectionId);
        }

        public void AddHandler<T>(Action<T> function, bool preHandler = false) where T : NetworkPacketBase, new()
        {
            var hash = NetworkPacketExtensions.Code<T>();
            if (preHandler)
            {
                PreHandlers.RegisterHandler(hash, function);
            }
            else
            {
                Handlers.RegisterHandler(hash, function);
            }
        }

        public bool ContainsHandler<T>(Action<T> function, bool preHandler = false) where T : NetworkPacketBase, new()
        {
            var code = NetworkPacketExtensions.Code<T>();
            return preHandler ? PreHandlers.ContainsHandler(code, function) : Handlers.ContainsHandler(code, function);
        }

        public bool RemoveHandler<T>(Action<T> function, bool preHandler = false) where T : NetworkPacketBase, new()
        {
            var code = NetworkPacketExtensions.Code<T>();
            return preHandler ? PreHandlers.UnregisterHandler(code, function) : Handlers.UnregisterHandler(code, function);
        }

        public void AddProtoHandler<T>(Action<ProtobufPacket<T>> function, bool preHandler = false) where T : IMessage<T>, new()
        {
            var code = ProtobufPacketExtensions.Code<T>();
            if(preHandler)
            {
                PreHandlers.RegisterProtoHandler(code, function);
            }
            else
            {
                Handlers.RegisterProtoHandler(code, function);
            }
        }

        public bool ContainsProtoHandler<T>(Action<ProtobufPacket<T>> function, bool preHandler = false) where T : IMessage<T>, new()
        {
            var code = ProtobufPacketExtensions.Code<T>();
            return preHandler ? PreHandlers.ContainsProtoHandler(code, function) : Handlers.ContainsProtoHandler(code, function);
        }
        
        public bool RemoveProtoHandler<T>(Action<ProtobufPacket<T>> function, bool preHandler = false) where T : IMessage<T>, new()
        {
            var code = ProtobufPacketExtensions.Code<T>();
            return preHandler ? PreHandlers.UnregisterProtoHandler(code, function) : Handlers.UnregisterProtoHandler(code, function);
        }

        protected abstract void HandleConnect(short connectionId, byte error);
        protected abstract void HandleDisconnect(short connectionId, byte error);
        protected abstract void HandleDataReceived(short connectionId, int channelId, ref byte[] buffer, int receiveSize, byte error);

        protected abstract void Log(string msg, DebugLevel level);

        public bool PING_SIMULATION = false;
        public int PING_SIMULATED = 0;
        private readonly Queue<DataReceived> _dataReceived = new Queue<DataReceived>();

        private sealed class DataReceived
        {
            internal float Timestamp;
            internal short ConnectionId;
            internal int ChannelId;
            internal byte[] Buffer;
            internal int ReceivedSize;
            internal byte Error;
        }

        private void SimulateDataReceived(short outConnectionId, int outChannelId, ref byte[] buffer, int outReceiveSize, byte error)
        {
            if (!IsOk(error))
            {
                Log("Bad Packet received:" + (NetworkError)error, DebugLevel.ERROR);
                return;
            }
            var rt = SimulationTime();
            var rtt = NetworkTransport.GetCurrentRTT(SocketId, outConnectionId, out _);
            var rc = new DataReceived
            {
                ConnectionId = outConnectionId,
                ChannelId = outChannelId,
                Error = error,
                Timestamp = rt + (PING_SIMULATED - rtt) / 1000f,
                ReceivedSize = outReceiveSize,
                Buffer = ArrayPool<byte>.GetBuffer(1024)
            };
            if (_receiveSize >= 1024)
            {
                Log("Packet exceeding limit of 1024 bytes:" + _receiveSize, DebugLevel.ERROR);
                return;
            }
            Array.Copy(buffer, rc.Buffer, _receiveSize);
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
                if (_current.Timestamp < SimulationTime())
                {
                    HandleDataReceived(_current.ConnectionId, _current.ChannelId, ref _current.Buffer, _current.ReceivedSize, _current.Error);
                    ArrayPool<byte>.FreeBuffer(_current.Buffer);
                    _current.Buffer = null;
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

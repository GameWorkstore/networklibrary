using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using GameWorkstore.Patterns;
using Google.Protobuf;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class NetworkClient : BaseConnection
    {
        private string _serverIp = "127.0.0.1";
        protected INetConnection Conn;
        private Action<bool, INetConnection> _onConnect;
        private NetworkClientState _state;
        private readonly NetworkClientObjectController _objects;

        private float _networkDifference;
        private readonly List<float> _diffs = new List<float>();

        protected NetworkClient()
        {
            _state = NetworkClientState.None;
            _objects = new NetworkClientObjectController();
            _objects.OnObjectCreated.Register(HandleObjectCreated);
            _objects.OnObjectDestroyed.Register(HandleObjectDestroyed);
            AddHandler<AuthenticationRequestPacket>(HandleAuthenticationRequest);
            AddHandler<ObjectSyncNetworkTimePacket>(SyncNetworkTime);
            AddHandler<ObjectSyncPacket>(SyncAllObjects);
            AddHandler<ObjectSyncDeltaCreatePacket>(SyncDeltaCreate);
            AddHandler<ObjectSyncDeltaDestroyPacket>(SyncDeltaDestroy);
        }

        public override void Dispose()
        {
            _objects.OnObjectCreated.Unregister(HandleObjectCreated);
            _objects.OnObjectDestroyed.Unregister(HandleObjectDestroyed);
            RemoveHandler<AuthenticationRequestPacket>(HandleAuthenticationRequest);
            RemoveHandler<ObjectSyncNetworkTimePacket>(SyncNetworkTime);
            RemoveHandler<ObjectSyncPacket>(SyncAllObjects);
            RemoveHandler<ObjectSyncDeltaCreatePacket>(SyncDeltaCreate);
            RemoveHandler<ObjectSyncDeltaDestroyPacket>(SyncDeltaDestroy);
            if (_state == NetworkClientState.Connecting || _state == NetworkClientState.Connected)
            {
                Disconnect();
            }
            base.Dispose();
        }

        public void Connect(string ip) { Connect(ip, Port); }
        public void Connect(string ip, Action<bool, INetConnection> onConnect) { Connect(ip, Port, onConnect); }
        public void Connect(string ip, int port, Action<bool, INetConnection> onConnect = null)
        {
            if (_state != NetworkClientState.None && _state != NetworkClientState.Disconnected)
            {
                return;
            }
            if (_state == NetworkClientState.Connecting)
            {
                return;
            }
            _onConnect = onConnect;
            Port = port;
            _serverIp = ip;

            //reset networkDifference
            _diffs.Clear();
            _networkDifference = 0;

            _eventService.StartCoroutine(OpenSocket(0, result =>
            {
                if (result)
                {
                    Log("Socket Open. SocketId is: " + SocketId, DebugLevel.INFO);

                    NetworkTransport.Connect(SocketId, _serverIp, Port, 0, out var error);
                    _state = NetworkClientState.Connecting;

                    switch ((NetworkError)error)
                    {
                        case NetworkError.Ok:
                            Log("Start Connection from Socket: " + SocketId, DebugLevel.INFO);
                            break;
                        default:
                            HandleConnection(false, "Start Connection from Socket failed: " + SocketId + "Error:" + (NetworkError)error);
                            break;
                    }
                }
                else
                {
                    HandleConnection(false, "Failed to Socket Open.");
                }
            }));
        }

        public void ConnectToLocalServer<T>(NetworkHostService<T> serverService, Action<bool, INetConnection> onConnect = null) where T: NetworkHost, new()
        {
            ConnectToLocalServer(serverService.Instance, onConnect);
        }

        public void ConnectToLocalServer(NetworkHost server, Action<bool, INetConnection> onConnect = null)
        {
            _onConnect = onConnect;
            _serverIp = "127.0.0.1";
            Port = DefaultPort;
            if (_state != NetworkClientState.None && _state != NetworkClientState.Disconnected)
            {
                return;
            }
            if (_state == NetworkClientState.Connecting)
            {
                return;
            }

            //reset networkDifference
            _diffs.Clear();
            _networkDifference = 0;

            _eventService.StartCoroutine(OpenSocket(0, result =>
            {
                if (result)
                {
                    Conn = server.CreateLocalConnection(Handlers);
                    Connections.Add(Conn.LocalConnectionId, Conn);
                    _state = NetworkClientState.Connected;
                }
                else
                {
                    HandleConnection(false, "Failed to Socket Open.");
                }
            }));
        }

        public void Disconnect(Action onDisconnection = null)
        {
            DisconnectInternal(false, onDisconnection);
        }

        private void DisconnectInternal(bool isLostConnection, Action onDisconnection)
        {
            if (_state != NetworkClientState.Connecting && _state != NetworkClientState.Connected)
            {
                return;
            }

            Log(isLostConnection ? "Connection Lost from Server: " + SocketId : "Disconnecting from Server: " + SocketId, DebugLevel.INFO);

            if (Conn != null)
            {
                RemoveConnection(Conn.LocalConnectionId);
                Conn = null;
            }

            CloseSocket();
            _state = NetworkClientState.Disconnected;

            //Remove All Objects
            _objects.DestroyAllObjects();

            if (isLostConnection) OnConnectionLost.Invoke();

            onDisconnection?.Invoke();
        }

        protected override void UpdateConnection()
        {
            base.UpdateConnection();
            //Queues
            foreach (var conn in Connections)
            {
                conn.Value.FlushChannels();
            }
        }

        public NetworkClientState GetCurrentState()
        {
            return _state;
        }

        public bool Send(NetworkPacketBase request, int channel)
        {
            if (!Conn.SendByChannel(request, channel))
            {
                Log("Failed to send[" + request.Code() + "] packet.", DebugLevel.WARNING);
                return false;
            }
            return true;
        }

        public bool Send(IMessage request, int channel)
        {
            if (!Conn.SendByChannel(request, channel))
            {
                Log("Failed to send[" + request.Code() + "] packet.", DebugLevel.WARNING);
                return false;
            }
            return false;
        }

        public int GetRTT()
        {
            return GetRTT(Conn.LocalConnectionId);
        }

        protected override void HandleConnect(short connectionId, byte error)
        {
            Log("PreConnection started", DebugLevel.INFO);
            _state = NetworkClientState.Connected;
            Conn = CreateConnection(connectionId); //PreConnection
            // set ServerConnectionId to uninitialized, as it isn't right yet
            Conn.ServerConnectionId = -1;
        }

        protected override void HandleDisconnect(short connectionId, byte error)
        {
            DisconnectInternal(true, null);
        }

        protected override void HandleDataReceived(short connectionId, int channelId, ref byte[] buffer, int receiveSize, byte error)
        {
            if (Connections.TryGetValue(connectionId, out var conn))
            {
                conn.TransportReceive(buffer, receiveSize, channelId);
            }
        }

        protected override void Log(string msg, DebugLevel level)
        {
            DebugMessege.Log("[Client]:" + msg, level);
        }

        public void AddTriggerHandler(NetworkHash128 hash, Action<ObjectCreationData> creationHandler, Action<NetworkBaseBehaviour> destroyHandler)
        {
            _objects.AddHandler(hash, creationHandler, destroyHandler);
        }

        public void RemoveTriggerHandler(NetworkHash128 hash)
        {
            _objects.RemoveHandler(hash);
        }

        /// <summary>
        /// When server request, send it an AuthenticationResponsePacket including the payload required to allow connection.
        /// </summary>
        /// <param name="packet"></param>
        protected virtual void HandleAuthenticationRequest(AuthenticationRequestPacket packet)
        {
            Log("Authentication request from server.", DebugLevel.INFO);
            // update server connection
            Conn.ServerConnectionId = packet.ServerConnectionId;
            var response = new AuthenticationResponsePacket
            {
                Payload = "default"
            };
            Send(response, ChannelReliable);
        }

        /// <summary>
        /// Dispatched when connected successfully or failed
        /// </summary>
        /// <param name="connected">if connection was successful.</param>
        /// <param name="message">error or acceptance.</param>
        protected virtual void HandleConnection(bool connected, string message)
        {
            _onConnect?.Invoke(connected, Conn);
            if (connected)
            {
                if (message != null) Log(message, DebugLevel.INFO);
                OnConnected.Invoke(Conn);
            }
            else
            {
                if (message != null) Log(message, DebugLevel.ERROR);
                OnConnectError.Invoke();
            }
        }

        private void SyncNetworkTime(ObjectSyncNetworkTimePacket time)
        {
            SetClientNetworkDifference(time.NetworkTime);
        }

        public float GetClientNetworkTime()
        {
            return SimulationTime() + _networkDifference + GetPhysicsRTT();
        }

        public float GetPhysicsRTT()
        {
            return GetRTT() / (2 * 1000f);
        }

        /// <summary>
        /// Check if a given connectionId is the ours
        /// </summary>
        /// <param name="serverConnectionId">NetConnection ServerConnectionId</param>
        public bool IsLocalPlayer(short serverConnectionId)
        {
            return Conn.ServerConnectionId == serverConnectionId;
        }

        private void SetClientNetworkDifference(float serverTime)
        {
            _diffs.Add(serverTime - SimulationTime());
            _networkDifference = _diffs.Sum() / _diffs.Count;
        }

        private void SyncAllObjects(ObjectSyncPacket packet)
        {
            _objects.SetSyncPacket(packet, Conn);

            //test if last packet
            if (!packet.IsLast) return;

            //client is connected!
            HandleConnection(true, "Connected:[ID:" + Conn.LocalConnectionId + "]");
        }

        private void SyncDeltaDestroy(ObjectSyncDeltaDestroyPacket packet)
        {
            _objects.SetSyncPacket(packet);
        }

        private void SyncDeltaCreate(ObjectSyncDeltaCreatePacket packet)
        {
            _objects.SetSyncPacket(packet, Conn);
        }

        public NetworkBaseBehaviour Find(NetworkInstanceId networkInstanceId)
        {
            return _objects.Find(networkInstanceId);
        }

        private void HandleObjectDestroyed(NetworkBaseBehaviour behaviour)
        {
            OnObjectDestroyed.Invoke(behaviour);
        }

        private void HandleObjectCreated(NetworkBaseBehaviour behaviour)
        {
            OnObjectCreated.Invoke(behaviour);
        }

        #region EVENTS
        public Signal<INetConnection> OnConnected;
        public Signal OnConnectError;
        public Signal OnConnectionLost;
        public Signal<NetworkBaseBehaviour> OnObjectCreated;
        public Signal<NetworkBaseBehaviour> OnObjectDestroyed;
        #endregion
    }

    public enum NetworkClientState
    {
        None,
        Connecting,
        Connected,
        Disconnected
    }
}

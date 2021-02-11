using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using GameWorkstore.Patterns;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class NetworkClientService : BaseConnectionService
    {
        private string SERVERIP = "127.0.0.1";
        protected NetConnection CONN;
        private Action<bool> OnConnect;
        private NetworkClientState STATE;
        private NetworkClientObjectController _objects;

        private float networkDifference;
        private readonly List<float> _diffs = new List<float>();

        public override void Preprocess()
        {
            base.Preprocess();
            STATE = NetworkClientState.NONE;
            _objects = new NetworkClientObjectController();
            _objects.OnObjectCreated.Register(HandleObjectCreated);
            _objects.OnObjectDestroyed.Register(HandleObjectDestroyed);
            AddHandler<AuthenticationRequestPacket>(HandleAuthenticationRequest);
            AddHandler<ObjectSyncNetworkTimePacket>(SyncNetworkTime);
            AddHandler<ObjectSyncPacket>(SyncAllObjects);
            AddHandler<ObjectSyncDeltaCreatePacket>(SyncDeltaCreate);
            AddHandler<ObjectSyncDeltaDestroyPacket>(SyncDeltaDestroy);
        }

        public override void Postprocess()
        {
            _objects.OnObjectCreated.Unregister(HandleObjectCreated);
            _objects.OnObjectDestroyed.Unregister(HandleObjectDestroyed);
            RemoveHandler<AuthenticationRequestPacket>(HandleAuthenticationRequest);
            RemoveHandler<ObjectSyncNetworkTimePacket>(SyncNetworkTime);
            RemoveHandler<ObjectSyncPacket>(SyncAllObjects);
            RemoveHandler<ObjectSyncDeltaCreatePacket>(SyncDeltaCreate);
            RemoveHandler<ObjectSyncDeltaDestroyPacket>(SyncDeltaDestroy);
            if (STATE == NetworkClientState.CONNECTING || STATE == NetworkClientState.CONNECTED)
            {
                Disconnect();
            }
            base.Postprocess();
        }

        public void Connect(string ip) { Connect(ip, PORT, null); }
        public void Connect(string ip, Action<bool> onConnect) { Connect(ip, PORT, onConnect); }
        public void Connect(string ip, int port) { Connect(ip, port, null); }
        public void Connect(string ip, int port, Action<bool> onConnect)
        {
            OnConnect = onConnect;
            PORT = port;
            SERVERIP = ip;
            if (STATE != NetworkClientState.NONE && STATE != NetworkClientState.DISCONNECTED)
            {
                return;
            }
            if (STATE == NetworkClientState.CONNECTING)
            {
                return;
            }
            
            //reset networkDifference
            _diffs.Clear();
            networkDifference = 0;

            if (OpenSocket())
            {
                Log("Socket Open. SocketId is: " + SOCKETID, DebugLevel.INFO);

                NetworkTransport.Connect(SOCKETID, SERVERIP, PORT, 0, out byte error);
                STATE = NetworkClientState.CONNECTING;

                switch ((NetworkError)error)
                {
                    case NetworkError.Ok:
                        Log("Start Connection from Socket: " + SOCKETID, DebugLevel.INFO);
                        break;
                    default:
                        HandleConnection(false, "Start Connection from Socket failed: " + SOCKETID + "Error:" + (NetworkError)error);
                        break;
                }
            }
            else
            {
                HandleConnection(false, "Failed to Socket Open.");
            }
        }

        public void Disconnect(Action onDisconnection = null)
        {
            DisconnectInternal(false,onDisconnection);
        }

        private void DisconnectInternal(bool isLostConnection, Action onDisconnection)
        {
            if (STATE != NetworkClientState.CONNECTING && STATE != NetworkClientState.CONNECTED)
            {
                return;
            }
            
            Log( isLostConnection? "Connection Lost from Server: " + SOCKETID : "Disconnecting from Server: " + SOCKETID, DebugLevel.INFO);

            if (CONN != null)
            {
                CONN.Disconnect();
                RemoveConnection(CONN.LocalConnectionId);
                CONN = null;
            }

            CloseSocket();
            STATE = NetworkClientState.DISCONNECTED;

            //Remove All Objects
            _objects.DestroyAllObjects();

            if (isLostConnection) OnConnectionLost.Invoke();
            
            onDisconnection?.Invoke();
        }

        protected override void UpdateConnection()
        {
            base.UpdateConnection();
            //Queues
            foreach (var conn in _connections)
            {
                conn.Value.FlushChannels();
            }
        }

        public NetworkClientState GetCurrentState()
        {
            return STATE;
        }

        public bool Send(NetworkPacketBase request, int channel)
        {
            if (!CONN.SendByChannel(request, channel))
            {
                Log("Failed to send[" + request.Code + "] packet.", DebugLevel.WARNING);
            }
            return false;
        }

        public int GetRTT()
        {
            return GetRTT(CONN.LocalConnectionId);
        }

        protected override void HandleConnect(short connectionId, byte error)
        {
            Log("Preconnection started", DebugLevel.INFO);
            STATE = NetworkClientState.CONNECTED;
            CONN = CreateConnection(connectionId); //PreConnection
            // set ServerConnectionId to unitialized, as it ins't right yet
            CONN.ServerConnectionId = -1;
        }

        protected override void HandleDisconnect(short connectionId, byte error)
        {
            DisconnectInternal(true, null);
        }

        protected override void HandleDataReceived(short connectionId, int channelId, ref byte[] buffer, int receiveSize, byte error)
        {
            if (_connections.TryGetValue(connectionId, out var conn))
            {
                conn.TransportReceive(buffer, receiveSize, channelId);
            }
        }

        public override void Log(string msg, DebugLevel level)
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
            CONN.ServerConnectionId = packet.ServerConnectionId;
            var response = new AuthenticationResponsePacket
            {
                Payload = "default"
            };
            Send(response, CHANNEL_RELIABLE);
        }

        /// <summary>
        /// Dispatched when connected successfully or failed
        /// </summary>
        /// <param name="connected">if connection was successful.</param>
        /// <param name="messege">error or acceptance.</param>
        protected virtual void HandleConnection(bool connected, string messege)
        {
            OnConnect?.Invoke(true);
            if (connected)
            {
                if(messege != null) Log(messege, DebugLevel.INFO);
                OnConnected.Invoke(CONN);
            }
            else
            {
                if(messege != null) Log(messege, DebugLevel.ERROR);
                OnConnectError.Invoke();
            }
        }

        private void SyncNetworkTime(ObjectSyncNetworkTimePacket time)
        {
            SetClientNetworkDifference(time.NetworkTime);
        }

        public float GetClientNetworkTime()
        {
            return SimulationTime() + networkDifference + GetPhysicsRTT();
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
            return CONN.ServerConnectionId == serverConnectionId;
        }

        public void SetClientNetworkDifference(float serverTime)
        {
            _diffs.Add(serverTime - SimulationTime());
            networkDifference = _diffs.Sum() / _diffs.Count;
        }

        private void SyncAllObjects(ObjectSyncPacket packet)
        {
            _objects.SetSyncPacket(packet, CONN);

            //test if last packet
            if (!packet.IsLast) return;

            //client is connected!
            HandleConnection(true, "Connected:[ID:" + CONN.LocalConnectionId + "]");
        }

        private void SyncDeltaDestroy(ObjectSyncDeltaDestroyPacket packet)
        {
            _objects.SetSyncPacket(packet);
        }

        private void SyncDeltaCreate(ObjectSyncDeltaCreatePacket packet)
        {
            _objects.SetSyncPacket(packet, CONN);
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
        public Signal<NetConnection> OnConnected;
        public Signal OnConnectError;
        public Signal OnConnectionLost;
        public Signal<NetworkBaseBehaviour> OnObjectCreated;
        public Signal<NetworkBaseBehaviour> OnObjectDestroyed;
        #endregion
    }

    public enum NetworkClientState
    {
        NONE,
        CONNECTING,
        CONNECTED,
        DISCONNECTED
    }
}

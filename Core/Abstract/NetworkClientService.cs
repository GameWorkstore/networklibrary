using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;
using GameWorkstore.Patterns;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class NetworkClientService : BaseConnectionService
    {
        private int CONNECTIONID;
        private string SERVERIP = "127.0.0.1";
        protected NetConnection CONN;
        private Action<bool> OnConnect;
        private NetworkClientState STATE;
        private NetworkClientObjectController _objects;

        private float networkDifference = 0;
        private readonly List<float> _diffs = new List<float>();

        public override void Preprocess()
        {
            base.Preprocess();
            STATE = NetworkClientState.NONE;
            _objects = new NetworkClientObjectController();
            _objects.OnObjectCreated.Register(HandleObjectCreated);
            _objects.OnObjectDestroyed.Register(HandleObjectDestroyed);
            AddHandler<ObjectSyncNetworkTimePacket>(SyncNetworkTime);
            AddHandler<ObjectSyncPacket>(SyncAllObjects);
            AddHandler<ObjectSyncDeltaCreatePacket>(SyncDeltaCreate);
            AddHandler<ObjectSyncDeltaDestroyPacket>(SyncDeltaDestroy);
        }

        public override void Postprocess()
        {
            _objects.OnObjectCreated.Unregister(HandleObjectCreated);
            _objects.OnObjectDestroyed.Unregister(HandleObjectDestroyed);
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

            if (OpenSocket())
            {
                Log("Socket Open. SocketId is: " + SOCKETID, DebugLevel.INFO);

                byte error;
                CONNECTIONID = NetworkTransport.Connect(SOCKETID, SERVERIP, PORT, 0, out error);

                STATE = NetworkClientState.CONNECTING;

                switch ((NetworkError)error)
                {
                    case NetworkError.Ok:
                        Log("Start Connection with Server from Socket: " + SOCKETID, DebugLevel.INFO);
                        break;
                    default:
                        Log("Start Connection with Server from Socket failed: " + SOCKETID + "Error:" + (UnityEngine.Networking.NetworkError)error, DebugLevel.ERROR);
                        OnConnect?.Invoke(false);
                        OnConnectError.Invoke();
                        break;
                }
            }
            else
            {
                Log("Failed to Socket Open.", DebugLevel.ERROR);
                OnConnect?.Invoke(false);
                OnConnectError.Invoke();
            }
        }

        public void Disconnect() { Disconnect(null); }

        public void Disconnect(Action onDisconnection)
        {
            if (STATE != NetworkClientState.CONNECTING && STATE != NetworkClientState.CONNECTED)
            {
                return;
            }

            Log("Disconnecting from Server: " + SOCKETID, DebugLevel.INFO);

            if (CONN != null)
            {
                CONN.Disconnect();
                RemoveConnectionFromPool(CONN.connectionId);
                CONN = null;
            }

            CloseSocket();
            STATE = NetworkClientState.DISCONNECTED;

            //Remove All Objects
            _objects.DestroyAllObjects();

            OnDisconnection.Invoke();
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
#if UNITY_EDITOR
            if (CONN.SendByChannel(request, channel))
            {
                return true;
            }
            else
            {
                Log("Failed to send[" + request.Code + "] packet.", DebugLevel.WARNING);
            }
            return false;
#else
            return CONN.SendByChannel(request.Code, request, channel);
#endif
        }

        public int GetRTT()
        {
            return GetRTT(CONN.connectionId);
        }

        protected override void HandleConnection(int connectionId, byte error)
        {
            Log("Connected to Server", DebugLevel.INFO);
            STATE = NetworkClientState.CONNECTED;
            CONN = new NetConnection();
            CONN.Initialize(SERVERIP, SOCKETID, CONNECTIONID, GetHostTopology());
            AddConnectionToPool(CONN);
        }

        protected override void HandleDisconnection(int connectionId, byte error)
        {
            Log("Received disconnection from Server", DebugLevel.INFO);
            Disconnect();
        }

        protected override void HandleDataReceived(int connectionId, int channelId, ref byte[] buffer, int receiveSize, byte error)
        {
            if (_connections.TryGetValue(connectionId, out NetConnection conn))
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

        private void SyncNetworkTime(NetMessage evt)
        {
            SetClientNetworkDifference(evt.ReadMessage<ObjectSyncNetworkTimePacket>().NetworkTime);
        }

        public float GetClientNetworkTime()
        {
            return SimulationTime() + networkDifference + GetPhysicsRTT();
        }

        public float GetPhysicsRTT()
        {
            return GetRTT() / (2 * 1000f);
        }

        public void SetClientNetworkDifference(float serverTime)
        {
            _diffs.Add(serverTime - SimulationTime());
            networkDifference = _diffs.Sum() / _diffs.Count;
        }

        private void SyncAllObjects(NetMessage evt)
        {
            ObjectSyncPacket packet = evt.ReadMessage<ObjectSyncPacket>();
            _objects.SetSyncPacket(packet, CONN);

            //test if last packet
            if (!packet.IsLast) return;

            //client is connected!
            OnConnected.Invoke(CONN);
            OnConnect?.Invoke(true);
        }

        private void SyncDeltaDestroy(NetMessage evt)
        {
            ObjectSyncDeltaDestroyPacket packet = evt.ReadMessage<ObjectSyncDeltaDestroyPacket>();
            _objects.SetSyncPacket(packet);
        }

        private void SyncDeltaCreate(NetMessage evt)
        {
            ObjectSyncDeltaCreatePacket packet = evt.ReadMessage<ObjectSyncDeltaCreatePacket>();
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
        public Signal<NetConnection> OnConnected = new Signal<NetConnection>();
        public Signal OnConnectError = new Signal();
        public Signal OnDisconnection = new Signal();
        public Signal<NetworkBaseBehaviour> OnObjectCreated = new Signal<NetworkBaseBehaviour>();
        public Signal<NetworkBaseBehaviour> OnObjectDestroyed = new Signal<NetworkBaseBehaviour>();
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

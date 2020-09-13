using Patterns;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Networking;

namespace UnityEngine.NetLibrary
{
    public abstract class NetworkClientService : BaseConnectionService
    {
        protected NetConnection Connection { get { return CONN ?? null; } }

        private const DebugLevel _debuglevel = DebugLevel.INFO;
        private int CONNECTIONID;
        private string SERVERIP = "127.0.0.1";
        protected NetConnection CONN;
        private NetworkClientState STATE;
        private NetworkClientObjectController _objects;

        private float networkDifference = 0;
        private List<float> diffs = new List<float>();

        public override void Preprocess()
        {
            base.Preprocess();
            STATE = NetworkClientState.NONE;
            _objects = new NetworkClientObjectController();
            _objects.OnObjectCreated.Register(HandleObjectCreated);
            _objects.OnObjectDestroyed.Register(HandleObjectDestroyed);
            AddHandler(ObjectSyncNetworkTimePacket.Code, SyncNetworkTime);
            AddHandler(ObjectSyncPacket.Code, SyncAllObjects);
            AddHandler(ObjectSyncDeltaCreatePacket.Code, SyncDeltaCreate);
            AddHandler(ObjectSyncDeltaDestroyPacket.Code, SyncDeltaDestroy);
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

        public void Connect(string ip) { Connect(ip, PORT); }

        public void Connect(string ip, int port)
        {
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

            if(OpenSocket())
            {
                Log("Socket Open. SocketId is: " + SOCKETID, DebugLevel.INFO);

                byte error;
                CONNECTIONID = NetworkTransport.Connect(SOCKETID, SERVERIP, PORT, 0, out error);

                STATE = NetworkClientState.CONNECTING;

                if ((NetworkError)error == NetworkError.Ok)
                {
                    Log("Start Connection with Server from Socket: " + SOCKETID, DebugLevel.INFO);
                }
                else
                {
                    Log("Start Connection with Server from Socket failed: " + SOCKETID + "Error:" + (UnityEngine.Networking.NetworkError)error, DebugLevel.ERROR);
                }
            }
            else
            {
                Log("Failed to Socket Open.", DebugLevel.ERROR);
                return;
            }
        }

        public void Disconnect()
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

        public bool Send(short v, MsgBase request, int channel)
        {
#if UNITY_EDITOR
            if (CONN.SendByChannel(v, request, channel))
            {
                return true;
            }
            else
            {
                Log("Failed to send[" + v + "] packet.", DebugLevel.WARNING);
            }
            return false;
#else
            return CONN.SendByChannel(v, request, channel);
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
            NetConnection conn;
            if (_connections.TryGetValue(connectionId, out conn))
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
            diffs.Add(serverTime - SimulationTime());
            networkDifference = diffs.Sum() / diffs.Count;
        }

        private void SyncAllObjects(NetMessage evt)
        {
            ObjectSyncPacket packet = evt.ReadMessage<ObjectSyncPacket>();
            _objects.SetSyncPacket(packet, CONN);
            //After Sync, Invokes Client is Connected!
            if (packet.IsLast)
            {
                OnConnection.Invoke(CONN);
            }
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
        public Signal<NetConnection> OnConnection = new Signal<NetConnection>();
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

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using GameWorkstore.Patterns;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class NetworkHostService : BaseConnectionService
    {
        private NetworkServerObjectController _objects;

        private const float _queueSolverTime = 1 / 18f; //Amount of QueueMesseges per Second
        private float _lastQueueSolver;
        private List<QPacketClass> _classes;

        protected bool USENETWORKOBJECTCONTROLLER = true;

        public override void Preprocess()
        {
            base.Preprocess();
            _classes = new List<QPacketClass>();
            _objects = new NetworkServerObjectController();
            _objects.OnObjectCreated.Register(HandleObjectCreated);
            _objects.OnObjectDestroyed.Register(HandleObjectDestroyed);
            AddHandler<AuthenticationResponsePacket>(HandleAuthentication, true);
        }

        public override void Postprocess()
        {
            _objects.OnObjectCreated.Unregister(HandleObjectCreated);
            _objects.OnObjectDestroyed.Unregister(HandleObjectDestroyed);
            RemoveHandler<AuthenticationResponsePacket>(HandleAuthentication, true);
            if (IsInitialized())
            {
                Shutdown();
            }
            base.Postprocess();
        }

        public void Init() { Init(PORT, MATCHSIZE, BOTSIZE, null); }
        public void Init(Action<bool> onInit) { Init(PORT, MATCHSIZE, BOTSIZE, onInit); }
        public void Init(int port) { Init(port, MATCHSIZE, BOTSIZE, null); }

        public void Init(int port, int matchSize, int botSize, Action<bool> OnInit)
        {
            PORT = port;
            MATCHSIZE = matchSize;
            BOTSIZE = botSize;
            if (OpenSocket(PORT))
            {
                Log("Socket Open. SocketId is: " + SOCKETID, DebugLevel.INFO);
                Preinitialize();
                OnInit?.Invoke(true);
            }
            else
            {
                Log("Failed to Socket Open.", DebugLevel.INFO);
                OnInit?.Invoke(true);
            }
        }

        public bool IsInitialized()
        {
            return HasSocket();
        }

        /// <summary>
        /// Prepare your server to receive connections here!
        /// </summary>
        protected abstract void Preinitialize();

        public void Shutdown()
        {
            DisconnectAllPlayers();
            CloseSocket();
        }

        protected override void UpdateConnection()
        {
            // queues
            if (SimulationTime() > _lastQueueSolver + _queueSolverTime)
            {
                foreach (var q in _classes)
                {
                    if (q.HasPacket())
                    {
                        foreach (var connid in _connections.Keys)
                        {
                            QPacket packet = q.GetQPacket(connid);
                            Send(connid, packet.Data, packet.Channel);
                        }
                        q.ClearPacket();
                    }
                }
                _lastQueueSolver = SimulationTime();
            }
            // preconnections has 60 seconds to communicate, or drop
            DisconnectTimeoutPreconnections();
            // default
            base.UpdateConnection();
        }

        public bool Send(short connid, NetworkPacketBase msg, byte channel)
        {
            if (_connections.ContainsKey(connid))
            {
                if (!_connections[connid].SendByChannel(msg, channel))
                {
                    Log("Error while sending packet[" + msg.Code + "] to connection[" + connid + "]", DebugLevel.WARNING);
                    return false;
                }
            }
            else
            {
                Log("Error while sending packet[" + msg.Code + "] to connection[" + connid + "] because it doesn't ", DebugLevel.WARNING);
                return false;
            }

            return true;
        }

        public bool SendToAll(NetworkPacketBase msg, byte channel, int ignoreConnectionId = -1)
        {
            bool sucess = true;
            if (ignoreConnectionId < 0)
            {
                foreach (var connid in _connections.Keys)
                {
                    sucess &= Send(connid, msg, channel);
                }
                return sucess;
            }
            foreach (var connid in _connections.Keys)
            {
                if (ignoreConnectionId == connid)
                {
                    continue;
                }
                sucess &= Send(connid, msg, channel);
            }
            return sucess;
        }

        public bool SendToConnections(NetworkPacketBase msg, byte channel, IEnumerable<short> connectionIds)
        {
            bool sucess = true;
            foreach (var connid in connectionIds)
            {
                if (_connections.ContainsKey(connid))
                {
                    sucess &= Send(connid, msg, channel);
                }
            }
            return sucess;
        }

        protected override void HandleConnect(short connectionId, byte error)
        {
            if (!IsOk(error))
            {
                Log("ConnectionError:[ID:" + connectionId + "][ERROR:" + (NetworkError)error + "]", DebugLevel.ERROR);
                return;
            }
            if (_connections.Count < MATCHSIZE)
            {
                Log("PreConnected:[ID:" + connectionId + "]", DebugLevel.INFO);
                // request authentication
                var conn = CreatePreconnection(connectionId);
                var authRequest = new AuthenticationRequestPacket()
                {
                    ServerConnectionId = connectionId
                };
                conn.SendByChannel(authRequest, CHANNEL_RELIABLE);
            }
            else
            {
                RefusePreconnection(connectionId);
            }
        }

        /// <summary>
        /// Authenticate player using payload and invokes AuthenticateConnection of RefuseConnection as result
        /// </summary>
        /// <param name="packet"></param>
        protected virtual void HandleAuthentication(AuthenticationResponsePacket authentication)
        {
            //Accepts all authenticated until fill up:
            if(_connections.Count < MATCHSIZE && authentication.Payload.Equals("default"))
            {
                AuthenticatePreconnection(authentication.conn.LocalConnectionId);
            }
            else
            {
                RefusePreconnection(authentication.conn.LocalConnectionId);
            }
        }

        protected void AuthenticatePreconnection(short connectionId)
        {
            RemovePreconnection(connectionId);
            Log("Authenticated:[ID:" + connectionId + "]", DebugLevel.INFO);
            var conn = CreateConnection(connectionId);
            ServiceProvider.GetService<EventService>().StartCoroutine(StartClient(conn));
        }

        protected void RefusePreconnection(short connectionId)
        {
            Log("Refused:[ID:" + connectionId + "]", DebugLevel.INFO);
            RemovePreconnection(connectionId);
            NetworkTransport.Disconnect(SOCKETID, connectionId, out _);
        }

        private IEnumerator StartClient(NetConnection conn)
        {
            var packets = _objects.GetSyncPacket(conn.LocalConnectionId);
            if (packets.Length > 0)
            {
                packets[packets.Length - 1].IsLast = true;
            }
            else
            {
                packets = new ObjectSyncPacket[] { new ObjectSyncPacket() { IsLast = true } };
            }

            for (int i = 0; i < 3; i++)
            {
                if (!Send(conn.LocalConnectionId, new ObjectSyncNetworkTimePacket() { NetworkTime = GetHostNetworkTime() }, CHANNEL_RELIABLE))
                {
                    Log("Failed to Send SyncNetworkPacket", DebugLevel.ERROR);
                }
            }

            yield return null;

            foreach (var packet in packets)
            {
                if (!Send(conn.LocalConnectionId, packet, CHANNEL_RELIABLE_ORDERED))
                {
                    Log("Failed to Send SyncPacket", DebugLevel.ERROR);
                }
                yield return null;
            }

            Log("Connected:[ID:" + conn.LocalConnectionId + "]", DebugLevel.INFO);
            OnSocketConnection.Invoke(conn);
        }

        protected override void HandleDisconnect(short connectionId, byte error)
        {
            // connection
            if (_connections.TryGetValue(connectionId, out NetConnection conn))
            {
                conn.Disconnect();

                var networkError = (NetworkError)error;
                if (networkError != NetworkError.Ok)
                {
                    if (networkError != NetworkError.Timeout)
                    {
                        Log("Client disconnect by Error, connectionId: " + connectionId + " error: " + networkError, DebugLevel.WARNING);
                    }
                    else
                    {
                        Log("Client disconnect by Timeout, connectionId: " + connectionId + " error: " + networkError, DebugLevel.INFO);
                    }
                }
                else
                {
                    Log("Client disconnected sucessfully:" + connectionId, DebugLevel.INFO);
                }

                RemoveConnection(connectionId);
                OnSocketDisconnection.Invoke(conn);
                return;
            }
            // preconnection
            if (_preconnections.TryGetValue(connectionId, out conn))
            {
                conn.Disconnect();
                RemovePreconnection(connectionId);
            }
        }

        protected override void HandleDataReceived(short connectionId, int channelId, ref byte[] buffer, int receiveSize, byte error)
        {
            if (_connections.TryGetValue(connectionId, out NetConnection conn))
            {
                conn.TransportReceive(buffer, receiveSize, channelId);
                return;
            }

            if(_preconnections.TryGetValue(connectionId, out conn))
            {
                conn.TransportReceive(buffer, receiveSize, channelId);
                return;
            }
        }

        public override void Log(string msg, DebugLevel level)
        {
            DebugMessege.Log("[Server]:" + msg, level);
        }

        public void AddQueueble(Func<bool> hasPacket, Func<int, QPacket> getQPacket, Func<bool> clearPacket)
        {
            _classes.Add(new QPacketClass() { HasPacket = hasPacket, GetQPacket = getQPacket, ClearPacket = clearPacket });
        }

        public bool TriggerCreate(NetworkHash128 hash, NetConnection authority)
        {
            return TriggerCreate(hash, authority, null);
        }

        public bool TriggerCreate(NetworkHash128 hash, NetConnection authority, Action<NetworkBaseBehaviour> OnExternalCompleted)
        {
            return TriggerCreate(hash, authority, Vector3.zero, Quaternion.identity, null, null, OnExternalCompleted);
        }

        public bool TriggerCreate(NetworkHash128 hash, NetConnection authority, Vector3 position, Quaternion rotation, byte[] internalParams, byte[] sharedParams, Action<NetworkBaseBehaviour> OnExternalCompleted)
        {
            var ct = new ObjectCreationStruct()
            {
                ObjectName = hash,
                ObjectId = GetUniqueId(),
                Authority = authority,
                Position = position,
                Rotation = rotation,
                InternalParams = internalParams,
                SharedParams = sharedParams,
            };
            ct.OnExternalCompleted.Register(OnExternalCompleted);
            return _objects.TriggerCreate(ct);
        }

        public void TriggerDestroy(NetworkInstanceId instance)
        {
            _objects.TriggerDestroy(instance);
        }

        public void AddTriggerHandler(NetworkHash128 hash, Action<ObjectCreationData> creationHandler, Action<NetworkBaseBehaviour> destroyHandler)
        {
            _objects.AddHandler(hash, creationHandler, destroyHandler);
        }

        public void RemoveTriggerHandler(NetworkHash128 hash)
        {
            _objects.RemoveHandler(hash);
        }

        public bool DisconnectPlayer(short connectionId)
        {
            HandleDisconnect(connectionId, 0);
            return NetworkTransport.Disconnect(SOCKETID, connectionId, out _);
        }

        private void DisconnectTimeoutPreconnections()
        {
            var preconnids = _preconnections.Where(Timeout).Select(ConnectionId).ToArray();
            foreach (var preconn in preconnids)
            {
                RefusePreconnection(preconn);
            }
        }

        public void DisconnectAllPlayers()
        {
            var preconnids = _preconnections.Select(ConnectionId).ToArray();
            foreach (var preconn in preconnids)
            {
                RefusePreconnection(preconn);
            }
            var connids = _connections.Select(ConnectionId).ToArray();
            foreach (var conn in connids)
            {
                DisconnectPlayer(conn);
            }
            _connections.Clear();
        }

        public static float GetHostNetworkTime()
        {
            return SimulationTime();
        }

        private void HandleObjectCreated(NetworkBaseBehaviour behaviour)
        {
            OnObjectCreated.Invoke(behaviour);

            bool send = true;
            foreach (var conn in _connections)
            {
                bool auth = behaviour.connectionId == conn.Key;
                send &= Send(conn.Key, new ObjectSyncDeltaCreatePacket()
                {
                    ObjectId = behaviour.networkInstanceId,
                    ObjectName = behaviour.networkHash,
                    Position = behaviour.transform.position,
                    Quaternion = behaviour.transform.rotation,
                    Authority = auth,
                    InternalParams = auth ? behaviour.internalParams : new byte[0],
                    SharedParams = behaviour.sharedParams
                },
                CHANNEL_ALLCOSTDELIVERY
                );
            }

            if (!send)
            {
                Log("Failed to Send SyncDeltaCreatePacket for Entire List of connections", DebugLevel.ERROR);
            }
        }

        private void HandleObjectDestroyed(NetworkBaseBehaviour behaviour)
        {
            OnObjectDestroyed.Invoke(behaviour);

            bool send = true;
            foreach (var conn in _connections)
            {
                send &= Send(
                    conn.Key,
                    new ObjectSyncDeltaDestroyPacket() { ObjectId = behaviour.networkInstanceId },
                    CHANNEL_ALLCOSTDELIVERY
                );
            }

            if (!send)
            {
                Log("Failed to Send SyncDeltaCreatePacket for Entire List of connections", DebugLevel.ERROR);
            }
        }

        public NetworkBaseBehaviour Find(NetworkInstanceId networkInstanceId)
        {
            return _objects.Find(networkInstanceId);
        }

        public NetConnection FindOwner(NetworkBaseBehaviour behaviour)
        {
            foreach (var conn in _connections.Values)
            {
                if (conn.LocalConnectionId == behaviour.connectionId)
                {
                    return conn;
                }
            }
            return null;
        }

        public float GetPhysicsRTT(int connectionId)
        {
            return GetRTT(connectionId) / 1000f;
        }

        public int GetPort() { return PORT; }

        /// <summary>
        /// Helper function for LINQ
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        private static bool Timeout(KeyValuePair<short, NetConnection> conn)
        {
            return SimulationTime() - conn.Value.InitializedTime > 60;
        }

        /// <summary>
        /// Helper function for LINQ
        /// </summary>
        /// <param name="conn"></param>
        /// <returns></returns>
        private static short ConnectionId(KeyValuePair<short, NetConnection> conn)
        {
            return conn.Key;
        }

        #region EVENTS
        public Signal<NetConnection> OnSocketConnection;
        public Signal<NetConnection> OnSocketDisconnection;
        public Signal<NetworkBaseBehaviour> OnObjectCreated;
        public Signal<NetworkBaseBehaviour> OnObjectDestroyed;
        #endregion
    }

    internal enum NetworkServerState
    {
        NONE,
        STARTED,
        TERMINATED
    }

    internal struct QPacketClass
    {
        internal Func<bool> HasPacket;
        internal Func<int, QPacket> GetQPacket;
        internal Func<bool> ClearPacket;
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using GameWorkstore.Patterns;
using Google.Protobuf;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class NetworkHost : BaseConnection
    {
        private readonly NetworkServerObjectController _objects;

        private const float _queueSolverTime = 1 / 18f; //Amount of QueueMesseges per Second
        private float _lastQueueSolver;
        private readonly List<QPacketClass> _classes;

        protected bool UseNetworkObjectController = true;

        protected NetworkHost()
        {
            _classes = new List<QPacketClass>();
            _objects = new NetworkServerObjectController();
            _objects.OnObjectCreated.Register(HandleObjectCreated);
            _objects.OnObjectDestroyed.Register(HandleObjectDestroyed);
            AddHandler<AuthenticationResponsePacket>(HandleAuthentication, true);
        }

        public override void Dispose()
        {
            base.Dispose();
            _objects.OnObjectCreated.Unregister(HandleObjectCreated);
            _objects.OnObjectDestroyed.Unregister(HandleObjectDestroyed);
            RemoveHandler<AuthenticationResponsePacket>(HandleAuthentication, true);
            if (IsInitialized())
            {
                Shutdown();
            }
        }

        public void Init() { Init(Port, MatchSize, BotSize, null); }
        public void Init(Action<bool> onInit) { Init(Port, MatchSize, BotSize, onInit); }
        public void Init(int port, Action<bool> onInit) { Init(port, MatchSize, BotSize, onInit); }
        public void Init(int port) { Init(port, MatchSize, BotSize, null); }
        public void Init(int port, int matchSize, int botSize, Action<bool> onInit)
        {
            Port = port;
            MatchSize = matchSize;
            BotSize = botSize;
            if (OpenSocket(Port))
            {
                Log("Socket Open. SocketId is: " + SocketId, DebugLevel.INFO);
                PreInitialize();
                onInit?.Invoke(true);
            }
            else
            {
                Log("Failed to Socket Open.", DebugLevel.INFO);
                onInit?.Invoke(true);
            }
        }

        public bool IsInitialized()
        {
            return HasSocket();
        }

        /// <summary>
        /// Prepare your server to receive connections here!
        /// </summary>
        protected virtual void PreInitialize() { }

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
                foreach (var q in _classes.Where(q => q.HasPacket()))
                {
                    foreach (var connId in Connections.Keys)
                    {
                        var packet = q.GetQPacket(connId);
                        Send(connId, packet.Data, packet.Channel);
                    }
                    q.ClearPacket();
                }
                _lastQueueSolver = SimulationTime();
            }
            // preConnections has 15 seconds to communicate, or drop
            DisconnectTimeoutPreConnections();
            // default
            base.UpdateConnection();
        }

        public bool Send(short connId, NetworkPacketBase packet, byte channel)
        {
            if (Connections.ContainsKey(connId))
            {
                if (!Connections[connId].SendByChannel(packet, channel))
                {
                    Log("Error while sending packet[" + packet + "] to connection[" + connId + "]", DebugLevel.WARNING);
                    return false;
                }
            }
            else
            {
                Log("Error while sending packet[" + packet.Code() + "] to connection[" + connId + "] because it doesn't ", DebugLevel.WARNING);
                return false;
            }

            return true;
        }

        public bool Send(short connId, IMessage packet, byte channel)
        {
            if (Connections.ContainsKey(connId))
            {
                if (!Connections[connId].SendByChannel(packet, channel))
                {
                    Log("Error while sending packet[" + packet + "] to connection[" + connId + "]", DebugLevel.WARNING);
                    return false;
                }
            }
            else
            {
                Log("Error while sending packet[" + packet.Code() + "] to connection[" + connId + "] because it doesn't ", DebugLevel.WARNING);
                return false;
            }

            return true;
        }

        public bool SendToAll(NetworkPacketBase packet, byte channel, int ignoreConnectionId = -1)
        {
            var success = true;
            if (ignoreConnectionId < 0)
            {
                foreach (var connId in Connections.Keys)
                {
                    success &= Send(connId, packet, channel);
                }
                return success;
            }
            foreach (var connId in Connections.Keys)
            {
                if (ignoreConnectionId == connId)
                {
                    continue;
                }
                success &= Send(connId, packet, channel);
            }
            return success;
        }

        public bool SendToAll(IMessage packet, byte channel, int ignoreConnectionId = -1)
        {
            var success = true;
            if (ignoreConnectionId < 0)
            {
                foreach (var connId in Connections.Keys)
                {
                    success &= Send(connId, packet, channel);
                }
                return success;
            }
            foreach (var connId in Connections.Keys)
            {
                if (ignoreConnectionId == connId)
                {
                    continue;
                }
                success &= Send(connId, packet, channel);
            }
            return success;
        }
        
        public bool SendToConnections(NetworkPacketBase msg, byte channel, IEnumerable<short> connectionIds)
        {
            var success = true;
            foreach (var connId in connectionIds)
            {
                if (Connections.ContainsKey(connId))
                {
                    success &= Send(connId, msg, channel);
                }
            }
            return success;
        }

        public bool SendToConnections(IMessage msg, byte channel, IEnumerable<short> connectionIds)
        {
            var success = true;
            foreach (var connId in connectionIds)
            {
                if (Connections.ContainsKey(connId))
                {
                    success &= Send(connId, msg, channel);
                }
            }
            return success;
        }

        protected override void HandleConnect(short connectionId, byte error)
        {
            if (!IsOk(error))
            {
                Log("ConnectionError:[ID:" + connectionId + "][ERROR:" + (NetworkError)error + "]", DebugLevel.ERROR);
                return;
            }
            if (Connections.Count < MatchSize)
            {
                Log("PreConnected:[ID:" + connectionId + "]", DebugLevel.INFO);
                // request authentication
                var conn = CreatePreConnection(connectionId);
                var authRequest = new AuthenticationRequestPacket()
                {
                    ServerConnectionId = connectionId
                };
                conn.SendByChannel(authRequest, ChannelReliable);
            }
            else
            {
                RefusePreConnection(connectionId);
            }
        }

        /// <summary>
        /// Authenticate player using payload and invokes AuthenticateConnection of RefuseConnection as result
        /// </summary>
        /// <param name="authentication"></param>
        protected virtual void HandleAuthentication(AuthenticationResponsePacket authentication)
        {
            //Accepts all authenticated until fill up:
            if(Connections.Count < MatchSize && authentication.Payload.Equals("default"))
            {
                AuthenticatePreConnection(authentication.conn.LocalConnectionId);
            }
            else
            {
                RefusePreConnection(authentication.conn.LocalConnectionId);
            }
        }

        protected void AuthenticatePreConnection(short connectionId)
        {
            RemovePreConnection(connectionId);
            Log("Authenticated:[ID:" + connectionId + "]", DebugLevel.INFO);
            var conn = CreateConnection(connectionId);
            ServiceProvider.GetService<EventService>().StartCoroutine(StartClient(conn));
        }

        protected void RefusePreConnection(short connectionId)
        {
            Log("Refused:[ID:" + connectionId + "]", DebugLevel.INFO);
            RemovePreConnection(connectionId);
            NetworkTransport.Disconnect(SocketId, connectionId, out _);
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
                packets = new[] { new ObjectSyncPacket() { IsLast = true } };
            }

            for (var i = 0; i < 3; i++)
            {
                if (!Send(conn.LocalConnectionId, new ObjectSyncNetworkTimePacket() { NetworkTime = GetHostNetworkTime() }, ChannelReliable))
                {
                    Log("Failed to Send SyncNetworkPacket", DebugLevel.ERROR);
                }
            }

            yield return null;

            foreach (var packet in packets)
            {
                if (!Send(conn.LocalConnectionId, packet, ChannelReliableOrdered))
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
            if (Connections.TryGetValue(connectionId, out var conn))
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
                    Log("Client disconnected successfully:" + connectionId, DebugLevel.INFO);
                }

                RemoveConnection(connectionId);
                OnSocketDisconnection.Invoke(conn);
                return;
            }
            // preconnection
            if (PreConnections.TryGetValue(connectionId, out conn))
            {
                conn.Disconnect();
                RemovePreConnection(connectionId);
            }
        }

        protected override void HandleDataReceived(short connectionId, int channelId, ref byte[] buffer, int receiveSize, byte error)
        {
            if (Connections.TryGetValue(connectionId, out var conn))
            {
                conn.TransportReceive(buffer, receiveSize, channelId);
                return;
            }
            if(PreConnections.TryGetValue(connectionId, out conn))
            {
                conn.TransportReceive(buffer, receiveSize, channelId);
                return;
            }
        }

        protected override void Log(string msg, DebugLevel level)
        {
            DebugMessege.Log("[Server]:" + msg, level);
        }
        
        public void AddQueueble(Func<bool> hasPacket, Func<int, QPacket> getQPacket, Func<bool> clearPacket)
        {
            _classes.Add(new QPacketClass() { HasPacket = hasPacket, GetQPacket = getQPacket, ClearPacket = clearPacket });
        }

        public bool TriggerCreate(NetworkHash128 hash, NetConnection authority, Action<NetworkBaseBehaviour> onExternalCompleted = null)
        {
            return TriggerCreate(hash, authority, Vector3.zero, Quaternion.identity, null, null, onExternalCompleted);
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
            return NetworkTransport.Disconnect(SocketId, connectionId, out _);
        }

        private void DisconnectTimeoutPreConnections()
        {
            foreach (var conn in PreConnections.Where(Timeout).Select(ConnectionId))
            {
                RefusePreConnection(conn);
            }
        }

        public void DisconnectAllPlayers()
        {
            foreach (var preConn in PreConnections.Select(ConnectionId))
            {
                RefusePreConnection(preConn);
            }
            foreach (var conn in Connections.Select(ConnectionId))
            {
                DisconnectPlayer(conn);
            }
            Connections.Clear();
        }

        public static float GetHostNetworkTime()
        {
            return SimulationTime();
        }

        private void HandleObjectCreated(NetworkBaseBehaviour behaviour)
        {
            OnObjectCreated.Invoke(behaviour);

            var send = true;
            foreach (var conn in Connections)
            {
                var auth = behaviour.connectionId == conn.Key;
                var transform = behaviour.transform;
                send &= Send(conn.Key, new ObjectSyncDeltaCreatePacket()
                {
                    ObjectId = behaviour.networkInstanceId,
                    ObjectName = behaviour.networkHash,
                    Position = transform.position,
                    Quaternion = transform.rotation,
                    Authority = auth,
                    InternalParams = auth ? behaviour.internalParams : new byte[0],
                    SharedParams = behaviour.sharedParams
                },
                ChannelAllCostDelivery
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

            var send = true;
            foreach (var conn in Connections)
            {
                send &= Send(
                    conn.Key,
                    new ObjectSyncDeltaDestroyPacket() { ObjectId = behaviour.networkInstanceId },
                    ChannelAllCostDelivery
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
            foreach (var conn in Connections.Values)
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

        public int GetPort() { return Port; }

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
        None,
        Started,
        Terminated
    }

    internal struct QPacketClass
    {
        internal Func<bool> HasPacket;
        internal Func<int, QPacket> GetQPacket;
        internal Func<bool> ClearPacket;
    }
}

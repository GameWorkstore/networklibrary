using System.Collections.Generic;
using UnityEngine;
using GameWorkstore.Patterns;

namespace GameWorkstore.NetworkLibrary
{
    internal class NetworkObjectController
    {
        protected Dictionary<NetworkHash128, ObjectHandlers> _dictionaryHandlers = new Dictionary<NetworkHash128, ObjectHandlers>();
        protected Dictionary<NetworkInstanceId, ObjectData> _dictionaryObjects = new Dictionary<NetworkInstanceId, ObjectData>();

        internal void AddHandler(
            NetworkHash128 hash,
            System.Action<ObjectCreationData> creationHandler,
            System.Action<NetworkBaseBehaviour> destroyHandler
        ){
            _dictionaryHandlers.Add(hash, new ObjectHandlers { Create = creationHandler, Destroy = destroyHandler });
        }

        internal void RemoveHandler(NetworkHash128 hash)
        {
            _dictionaryHandlers.Remove(hash);
        }
    }

    internal class NetworkServerObjectController : NetworkObjectController
    {
        internal Signal<NetworkBaseBehaviour> OnObjectCreated;
        internal Signal<NetworkBaseBehaviour> OnObjectDestroyed;

        private const int MAXIMUM_OBJECT_POOL_PER_PACKAGE = 20;
        private List<NetworkHash128>    _cookingObjectName  = new List<NetworkHash128>();
        private List<NetworkInstanceId> _cookingObjectId    = new List<NetworkInstanceId>();
        private List<Vector3>           _cookingPosition    = new List<Vector3>();
        private List<Quaternion>        _cookingQuaternion  = new List<Quaternion>();
        private List<NetworkInstanceId> _cookingAuthority   = new List<NetworkInstanceId>();
        private List<byte[]>            _cookingInternalParams = new List<byte[]>();
        private List<byte[]>            _cookingSharedParams  = new List<byte[]>();

        internal ObjectSyncPacket[] GetSyncPacket(int connectionId)
        {
            var os = new Queue<KeyValuePair<NetworkInstanceId,ObjectData>>();
            foreach(var keypair in _dictionaryObjects)
            {
                os.Enqueue(keypair);
            }
            List<ObjectSyncPacket> packets = new List<ObjectSyncPacket>();
            while (os.Count > 0)
            {
                _cookingObjectId.Clear();
                _cookingObjectName.Clear();
                _cookingPosition.Clear();
                _cookingQuaternion.Clear();
                _cookingAuthority.Clear();
                _cookingInternalParams.Clear();
                _cookingSharedParams.Clear();
                while (os.Count > 0 && _cookingObjectName.Count < MAXIMUM_OBJECT_POOL_PER_PACKAGE)
                {
                    KeyValuePair<NetworkInstanceId, ObjectData> o = os.Dequeue();
                    bool auth = o.Value.Object.connectionId == connectionId;
                    _cookingObjectId.Add(o.Key);
                    _cookingObjectName.Add(o.Value.Hash);
                    _cookingPosition.Add(o.Value.Object.transform.position);
                    _cookingQuaternion.Add(o.Value.Object.transform.rotation);
                    _cookingInternalParams.Add(auth ? o.Value.Object.internalParams : new byte[0]);
                    _cookingSharedParams.Add(o.Value.Object.sharedParams);
                    if (auth)
                    {
                        _cookingAuthority.Add(o.Key);
                    }
                }

                packets.Add(
                    new ObjectSyncPacket()
                    {
                        ObjectId = _cookingObjectId.ToArray(),
                        ObjectName = _cookingObjectName.ToArray(),
                        Position = _cookingPosition.ToArray(),
                        Quaternion = _cookingQuaternion.ToArray(),
                        InternalParams = _cookingInternalParams.ToArray(),
                        SharedParams = _cookingSharedParams.ToArray(),
                        Authority = _cookingAuthority.ToArray(),
                    });
            }
            return packets.ToArray();
        }

        ObjectHandlers handler;
        internal bool TriggerCreate(ObjectCreationStruct network)
        {
            if (_dictionaryHandlers.TryGetValue(network.ObjectName, out handler) && !_dictionaryObjects.ContainsKey(network.ObjectId))
            {
                var ct = new ObjectCreationData() { network = network };
                ct.OnInternalCompleted.Register(CompleteTriggerCreate);
                handler.Create(ct);
                return true;
            }
            Debug.LogError("Creation hash doens't exist on current objectController");
            return false;
        }

        private void CompleteTriggerCreate(ObjectCreationStruct network, NetworkBaseBehaviour behaviour)
        {
            _dictionaryObjects.Add(network.ObjectId, new ObjectData { Object = behaviour, Hash = network.ObjectName });
            behaviour.SetInstance(network.ObjectId, network.ObjectName, network.InternalParams, network.SharedParams, true, false, network.Authority == null, network.Authority == null ? -1 : network.Authority.LocalConnectionId);
            Object.DontDestroyOnLoad(behaviour.gameObject);
            OnObjectCreated.Invoke(behaviour);
            network.OnExternalCompleted.Invoke(behaviour);
        }

        ObjectData data;
        internal void TriggerDestroy(NetworkInstanceId instance)
        {
            if (!_dictionaryObjects.TryGetValue(instance, out data))
            {
                return;
            }
            _dictionaryObjects.Remove(instance);
            if (_dictionaryHandlers.TryGetValue(data.Hash, out handler))
            {
                OnObjectDestroyed.Invoke(data.Object);
                handler.Destroy(data.Object);
            }
        }

        internal NetworkBaseBehaviour Find(NetworkInstanceId networkInstanceId)
        {
            if(_dictionaryObjects.TryGetValue(networkInstanceId, out data))
            {
                return data.Object;
            }
            return null;
        }
    }

    internal class NetworkClientObjectController : NetworkObjectController
    {
        internal Signal<NetworkBaseBehaviour> OnObjectCreated;
        internal Signal<NetworkBaseBehaviour> OnObjectDestroyed;
        private List<NetworkInstanceId> _lateDestroy = new List<NetworkInstanceId>();

        ObjectHandlers handler;
        public void SetSyncPacket(ObjectSyncPacket evt, NetConnection conn)
        {
            for (int i = 0; i < evt.ObjectName.Length; i++)
            {
                if (_dictionaryHandlers.TryGetValue(evt.ObjectName[i], out handler))
                {
                    if (!_dictionaryObjects.ContainsKey(evt.ObjectId[i]))
                    {
                        bool hasAuthority = false;
                        for (int j = 0; j < evt.Authority.Length; i++)
                        {
                            if (evt.ObjectId[i].Value == evt.Authority[j].Value)
                            {
                                hasAuthority = true;
                            }
                        }
                        var ct = new ObjectCreationData()
                        {
                            network = new ObjectCreationStruct()
                            {
                                ObjectId = evt.ObjectId[i],
                                ObjectName = evt.ObjectName[i],
                                Position = evt.Position[i],
                                Rotation = evt.Quaternion[i],
                                Authority = hasAuthority ? conn : null,
                                InternalParams = evt.InternalParams[i],
                                SharedParams = evt.SharedParams[i]
                            }
                        };
                        ct.OnInternalCompleted.Register(CompleteTriggerCreate);
                        handler.Create(ct);
                    }
                    else
                    {
                        DebugMessege.Log("Object already exists " + evt.ObjectId[i].Value, DebugLevel.INFO);
                    }
                }
                else
                {
                    DebugMessege.Log("Creation hash doens't exist on current objectController for " + evt.ObjectId[i].Value, DebugLevel.ERROR);
                }
            }
        }

        internal void SetSyncPacket(ObjectSyncDeltaCreatePacket packet, NetConnection conn)
        {
            if (_dictionaryHandlers.TryGetValue(packet.ObjectName, out handler))
            {
                if (!_dictionaryObjects.ContainsKey(packet.ObjectId))
                {
                    var ct = new ObjectCreationData()
                    {
                        network = new ObjectCreationStruct()
                        {
                            ObjectId = packet.ObjectId,
                            ObjectName = packet.ObjectName,
                            Position = packet.Position,
                            Rotation = packet.Quaternion,
                            Authority = packet.Authority ? conn : null,
                            InternalParams = packet.InternalParams,
                            SharedParams = packet.SharedParams
                        }
                    };
                    ct.OnInternalCompleted.Register(CompleteTriggerCreate);
                    handler.Create(ct);
                }
                else
                {
                    DebugMessege.Log("Object already exists " + packet.ObjectId, DebugLevel.INFO);
                }
            }
            else
            {
                DebugMessege.Log("Creation hash doens't exist on current objectController for " + packet.ObjectName, DebugLevel.ERROR);
            }
        }

        private void CompleteTriggerCreate(ObjectCreationStruct network, NetworkBaseBehaviour behaviour)
        {
            //Check for LateDestroy
            if (_lateDestroy.Contains(network.ObjectId))
            {
                _lateDestroy.Remove(network.ObjectId);
                if (_dictionaryHandlers.TryGetValue(network.ObjectName, out handler))
                {
                    OnObjectDestroyed.Invoke(behaviour);
                    handler.Destroy(behaviour);
                }
                return;
            }
            //Default
            _dictionaryObjects.Add(network.ObjectId, new ObjectData { Object = behaviour, Hash = network.ObjectName });
            behaviour.SetInstance(network.ObjectId, network.ObjectName, network.InternalParams, network.SharedParams, false, true, network.Authority != null, network.Authority == null ? -1 : network.Authority.LocalConnectionId);
            Object.DontDestroyOnLoad(behaviour.gameObject);
            OnObjectCreated.Invoke(behaviour);
        }

        ObjectData data;
        internal void SetSyncPacket(ObjectSyncDeltaDestroyPacket packet)
        {
            if (!_dictionaryObjects.TryGetValue(packet.ObjectId, out data))
            {
                _lateDestroy.Add(packet.ObjectId);
                return;
            }
            _dictionaryObjects.Remove(packet.ObjectId);
            if (_dictionaryHandlers.TryGetValue(data.Hash, out handler))
            {
                OnObjectDestroyed.Invoke(data.Object);
                handler.Destroy(data.Object);
            }
        }

        internal NetworkBaseBehaviour Find(NetworkInstanceId networkInstanceId)
        {
            if (_dictionaryObjects.TryGetValue(networkInstanceId, out data))
            {
                return data.Object;
            }
            return null;
        }

        internal void DestroyAllObjects()
        {
            foreach(var entry in _dictionaryObjects)
            {
                if (_dictionaryHandlers.TryGetValue(entry.Value.Hash, out handler))
                {
                    OnObjectDestroyed.Invoke(entry.Value.Object);
                    handler.Destroy(entry.Value.Object);
                }
            }
            _dictionaryObjects.Clear();
        }
    }

    public struct ObjectHandlers
    {
        public System.Action<ObjectCreationData> Create;
        public System.Action<NetworkBaseBehaviour> Destroy;
    }

    public struct ObjectData
    {
        public NetworkBaseBehaviour Object;
        public NetworkHash128 Hash;
    }

    public class ObjectCreationStruct
    {
        public NetworkInstanceId ObjectId;
        public NetworkHash128 ObjectName;
        public NetConnection Authority;
        public Vector3 Position;
        public Quaternion Rotation;
        public byte[] InternalParams;
        public byte[] SharedParams;
        public Signal<NetworkBaseBehaviour> OnExternalCompleted;
    }

    public class ObjectCreationData
    {
        public ObjectCreationStruct network = new ObjectCreationStruct();
        public Signal<ObjectCreationStruct, NetworkBaseBehaviour> OnInternalCompleted;
    }
}
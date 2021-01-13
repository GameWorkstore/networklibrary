using UnityEngine;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class NetworkBaseBehaviour : MonoBehaviour
    {
        [HideInInspector]
        public uint networkInstanceIdV;
        [HideInInspector]
        public NetworkInstanceId networkInstanceId;
        [HideInInspector]
        public NetworkHash128 networkHash;
        [HideInInspector]
        public byte[] internalParams;
        [HideInInspector]
        public byte[] sharedParams;
        [HideInInspector]
        public bool isServer;
        [HideInInspector]
        public bool isClient;
        [HideInInspector]
        public bool hasAuthority;
        [HideInInspector]
        public int connectionId;

        public bool IsOffline { get { return !isClient && !isServer && hasAuthority; } }

        internal void SetInstance(NetworkInstanceId instance, NetworkHash128 hash, byte[] internalParams, byte[] sharedParams, bool isServer, bool isClient, bool hasAuthority, int connectionId = -1)
        {
            networkInstanceId = instance;
            networkInstanceIdV = instance.Value;
            networkHash = hash;
            this.isServer = isServer;
            this.isClient = isClient;
            this.hasAuthority = hasAuthority;
            this.connectionId = connectionId;
            this.internalParams = (internalParams == null) ? new byte[0] : internalParams;
            this.sharedParams = (sharedParams == null)? new byte[0] : sharedParams;
            NetworkStart();
        }

        protected virtual void NetworkStart()
        {
            if (isServer)
            {
                name = "[S"+connectionId+"]" + name;
            }
            if (isClient)
            {
                name = "[C"+connectionId+"]" + name;
            }
        }
    }
}
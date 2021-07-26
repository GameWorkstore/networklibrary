namespace GameWorkstore.NetworkLibrary
{
    // Handles network messages on client and server
    //public delegate void NetworkMessageDelegate(NetMessage netMsg);

    // Handles requests to spawn objects on the client
    //public delegate GameObject SpawnDelegate(Vector3 position, NetworkHash128 assetId);

    // Handles requests to unspawn objects on the client
    //public delegate void UnSpawnDelegate(GameObject spawned);

    // built-in system network messages
    public static class UnityTransportTypes
    {
        // internal system messages - cannot be replaced by user code
        public const short ObjectDestroy = 1;
        public const short Rpc = 2;
        public const short ObjectSpawn = 3;
        public const short Owner = 4;
        public const short Command = 5;
        public const short LocalPlayerTransform = 6;
        public const short SyncEvent = 7;
        public const short UpdateVars = 8;
        public const short SyncList = 9;
        public const short ObjectSpawnScene = 10;
        public const short NetworkInfo = 11;
        public const short SpawnFinished = 12;
        public const short ObjectHide = 13;
        public const short CRC = 14;
        public const short LocalClientAuthority = 15;
        public const short LocalChildTransform = 16;
        public const short PeerClientAuthority = 17;

        // used for profiling
        internal const short UserMessage = 0;
        internal const short HLAPIMsg = 28;
        internal const short LLAPIMsg = 29;
        internal const short HLAPIResend = 30;
        internal const short HLAPIPending = 31;

        public const short InternalHighest = 31;

        // public system messages - can be replaced by user code
        public const short Connect = 32;
        public const short Disconnect = 33;
        public const short Error = 34;
        public const short Ready = 35;
        public const short NotReady = 36;
        public const short AddPlayer = 37;
        public const short RemovePlayer = 38;
        public const short Scene = 39;
        public const short Animation = 40;
        public const short AnimationParameters = 41;
        public const short AnimationTrigger = 42;
        public const short LobbyReadyToBegin = 43;
        public const short LobbySceneLoaded = 44;
        public const short LobbyAddPlayerFailed = 45;
        public const short LobbyReturnToLobby = 46;

        //NOTE: update msgLabels below if this is changed.
        public const short Highest = 47;

        internal static string[] labels =
        {
            "None",
            nameof(ObjectDestroy),
            nameof(Rpc),
            nameof(ObjectSpawn),
            nameof(Owner),
            nameof(Command),
            nameof(LocalPlayerTransform),
            nameof(SyncEvent),
            nameof(UpdateVars),
            nameof(SyncList),
            nameof(ObjectSpawnScene), // 10
            nameof(NetworkInfo),
            nameof(SpawnFinished),
            nameof(ObjectHide),
            nameof(CRC),
            nameof(LocalClientAuthority),
            nameof(LocalChildTransform),
            nameof(PeerClientAuthority),
            "",
            "",
            "", // 20
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
            "", // 30
            "", // - SystemInternalHighest
            nameof(Connect), // 32,
            nameof(Disconnect),
            nameof(Error),
            nameof(Ready),
            nameof(NotReady),
            nameof(AddPlayer),
            nameof(RemovePlayer),
            nameof(Scene),
            nameof(Animation), // 40
            nameof(AnimationParameters),
            nameof(AnimationTrigger),
            nameof(LobbyReadyToBegin),
            nameof(LobbySceneLoaded),
            nameof(LobbyAddPlayerFailed), // 45
            nameof(LobbyReturnToLobby), // 46
        };

        public static string TypeToString(int value)
        {
            if (value < 0 || value > Highest)
            {
                return string.Empty;
            }
            string result = labels[value];
            if (string.IsNullOrEmpty(result))
            {
                result = "[" + value + "]";
            }
            return result;
        }
    }

    public class NetMessage
    {
        public uint Type;
        public INetConnection Conn;
        public NetReader Reader;
        public int ChannelId;

        public static string Dump(byte[] payload, int sz)
        {
            string outStr = "[";
            for (int i = 0; i < sz; i++)
            {
                outStr += (payload[i] + " ");
            }
            outStr += "]";
            return outStr;
        }

        public T ReadMessage<T>() where T : NetworkPacketBase, new()
        {
            Reader.SeekZero();
            var msg = new T { conn = Conn };
            msg.Deserialize(Reader);
            return msg;
        }
    }

    public enum ChannelOption
    {
        MaxPendingBuffers = 1
        // maybe add an InitialCapacity for Pending Buffers list if needed in the future
    }
}

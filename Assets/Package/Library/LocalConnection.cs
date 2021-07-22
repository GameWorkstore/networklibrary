using GameWorkstore.NetworkLibrary;
using GameWorkstore.Patterns;
using UnityEngine.Networking;

public class LocalConnection : INetConnection
{
    public LocalConnection OtherConnection { get; private set; }

    public LocalConnection(
            short hostId,
            short localConnectionId,
            HostTopology hostTopology,
            float initializedTime,
            NetworkHandlers serverHandlers,
            NetworkHandlers clientHandlers)
    {
        SetupConnection(this, hostId, localConnectionId, hostTopology, initializedTime);
        _networkHandlers = serverHandlers;
        //Client Connection
        OtherConnection = new LocalConnection(this, hostTopology, clientHandlers);
    }

    public LocalConnection(LocalConnection server, HostTopology hostTopology, NetworkHandlers clientHandlers)
    {
        SetupConnection(this, server.HostId, server.LocalConnectionId, hostTopology, server.InitializedTime);
        _networkHandlers = clientHandlers;
        OtherConnection = server;
    }

    public override void TransportReceive(byte[] bytes, int numBytes, int channelId)
    {
        if (numBytes >= bytes.Length)
        {
            DebugMessege.Log("Received number of bytes received [" + numBytes + "] are greater than maximum size [" + bytes.Length + "].", DebugLevel.ERROR);
            return;
        }
        HandleBytesReceived(bytes, channelId);
    }

    public override bool TransportSend(byte[] bytes, int numBytes, int channelId, out byte error)
    {
        error = (byte)NetworkError.Ok;
        OtherConnection.TransportReceive(bytes, numBytes, channelId);
        return true;
    }
}

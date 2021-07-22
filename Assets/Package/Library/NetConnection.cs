using GameWorkstore.Patterns;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace GameWorkstore.NetworkLibrary
{
    /*
    * wire protocol is a list of :   size   |  msgType     | payload
    *					            (short)  (variable)   (buffer)
    */
    public class NetConnection : INetConnection
    {
        public NetConnection(
            short hostId,
            short connectionId,
            HostTopology hostTopology,
            float initializedTime,
            NetworkHandlers networkHandlers)
        {
            if (hostTopology.DefaultConfig.UsePlatformSpecificProtocols && (Application.platform != RuntimePlatform.PS4))
            {
                throw new ArgumentOutOfRangeException("Platform specific protocols are not supported on this platform");
            }

            _networkHandlers = networkHandlers;
            SetupConnection(this, hostId, connectionId, hostTopology, initializedTime);
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
            return NetworkTransport.Send(HostId, LocalConnectionId, channelId, bytes, numBytes, out error);
        }
    }
}

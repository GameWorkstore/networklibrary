using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameWorkstore.NetworkLibrary
{
    public class ChannelBuffer
    {
        private readonly INetConnection _connection;

        private ChannelPacket _currentPacket;

        private float _lastFlushTime;
        private readonly byte _channelId;
        private readonly int _maxPacketSize;
        private readonly bool _isReliable;
        bool m_IsBroken;
        int _maxPendingPacketCount;

        const int _maxFreePacketCount = 512; //  this is for all connections. maybe make this configurable
        const int _defaultMaxPendingPacketCount = 16;  // this is per connection. each is around 1400 bytes (MTU)

        private readonly Queue<ChannelPacket> _pendingPackets = new Queue<ChannelPacket>();
        private readonly static Queue<ChannelPacket> _freePackets = new Queue<ChannelPacket>();
        internal static int pendingPacketCount; // this is across all connections. only used for profiler metrics.

        // config
        public float MaxDelay = 0.01f;

        // stats
        private float _lastBufferedMessageCountTimer = Time.realtimeSinceStartup;

        public int NumMsgsOut { get; private set; }
        public int NumBufferedMsgsOut { get; private set; }
        public int NumBytesOut { get; private set; }

        public int NumMsgsIn { get; private set; }
        public int NumBytesIn { get; private set; }

        public int NumBufferedPerSecond { get; private set; }
        public int LastBufferedPerSecond { get; private set; }

        private static readonly NetWriter _sendWriter = new NetWriter();

        // We need to reserve some space for header information, this will be taken off the total channel buffer size
        private const int _packetHeaderReserveSize = 100;

        public ChannelBuffer(INetConnection conn, int bufferSize, byte cid, bool isReliable)
        {
            _connection = conn;
            _maxPacketSize = bufferSize - _packetHeaderReserveSize;
            _currentPacket = new ChannelPacket(_maxPacketSize, isReliable);

            _channelId = cid;
            _maxPendingPacketCount = _defaultMaxPendingPacketCount;
            _isReliable = isReliable;
        }

        public bool SetOption(ChannelOption option, int value)
        {
            switch (option)
            {
                case ChannelOption.MaxPendingBuffers:
                    {
                        if (!_isReliable)
                        {
                            if (LogFilter.logError) { Debug.LogError("Cannot set MaxPendingBuffers on unreliable channel " + _channelId); }
                            return false;
                        }
                        if (value < 0 || value >= _maxFreePacketCount)
                        {
                            if (LogFilter.logError) { Debug.LogError("Invalid MaxPendingBuffers for channel " + _channelId + ". Must be greater than zero and less than " + _maxFreePacketCount); }
                            return false;
                        }
                        _maxPendingPacketCount = value;
                        return true;
                    }
            }
            return false;
        }

        public void CheckInternalBuffer()
        {
            if (Time.realtimeSinceStartup - _lastFlushTime > MaxDelay && _pendingPackets.Count > 0)
            {
                SendInternalBuffer();
                _lastFlushTime = Time.realtimeSinceStartup;
            }

            if (Time.realtimeSinceStartup - _lastBufferedMessageCountTimer > 1.0f)
            {
                LastBufferedPerSecond = NumBufferedPerSecond;
                NumBufferedPerSecond = 0;
                _lastBufferedMessageCountTimer = Time.realtimeSinceStartup;
            }
        }

        public bool SendWriter(NetWriter writer)
        {
            var segment = writer.AsArraySegment();
            return SendBytes(segment.Array, segment.Count);
        }

        internal bool SendBytes(byte[] bytes, int bytesToSend)
        {
#if UNITY_EDITOR
            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Outgoing, UnityTransportTypes.HLAPIMsg, "msg", 1);
#endif
            if (bytesToSend <= 0)
            {
                // zero length packets getting into the packet queues are bad.
                if (LogFilter.logError) { Debug.LogError("ChannelBuffer:SendBytes cannot send zero bytes"); }
                return false;
            }

            // for fragmented channels, m_MaxPacketSize is set to the max size of a fragmented packet, so anything higher than this should fail for any kind of channel.
            if (bytesToSend > _maxPacketSize)
            {
                if (LogFilter.logError) { Debug.LogError("Failed to send big message of " + bytesToSend + " bytes. The maximum is " + _maxPacketSize + " bytes on this channel."); }
                return false;
            }

            if (_pendingPackets.Count >= _maxPendingPacketCount)
            {
                if (LogFilter.logError) { Debug.LogError("ChannelBuffer buffer limit of " + _pendingPackets.Count + " packets reached."); }
                return false;
            }

            _currentPacket.Write(bytes, bytesToSend);
            if (MaxDelay <= 0.0f)
            {
                return SendInternalBuffer();
            }
            else
            {
                QueuePacket();
            }

            return true;
        }

        private void QueuePacket()
        {
            pendingPacketCount += 1;
            _pendingPackets.Enqueue(_currentPacket);
            _currentPacket = AllocPacket();
        }

        private ChannelPacket AllocPacket()
        {
#if UNITY_EDITOR
            NetworkDetailStats.SetStat(NetworkDetailStats.NetworkDirection.Outgoing, UnityTransportTypes.HLAPIPending, "msg", pendingPacketCount);
#endif
            return _freePackets.Count > 0? _freePackets.Dequeue() : new ChannelPacket(_maxPacketSize, _isReliable);
        }

        private static void FreePacket(ChannelPacket packet)
        {
#if UNITY_EDITOR
            NetworkDetailStats.SetStat(NetworkDetailStats.NetworkDirection.Outgoing, UnityTransportTypes.HLAPIPending, "msg", pendingPacketCount);
#endif
            if (_freePackets.Count >= _maxFreePacketCount)
            {
                // just discard this packet, already tracking too many free packets
                return;
            }
            _freePackets.Enqueue(packet);
        }

        public bool SendInternalBuffer()
        {
#if UNITY_EDITOR
            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Outgoing, UnityTransportTypes.LLAPIMsg, "msg", 1);
#endif
            while (_pendingPackets.Count > 0)
            {
                var packet = _pendingPackets.Dequeue();
                if (!packet.SendToTransport(_connection, _channelId) && _isReliable)
                {
                    _pendingPackets.Enqueue(packet);
                    return false;
                }
                pendingPacketCount -= 1;
                FreePacket(packet);
            }
            return true;
        }
    }
}

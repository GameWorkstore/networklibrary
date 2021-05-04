using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameWorkstore.NetworkLibrary
{
    class ChannelBuffer : IDisposable
    {
        private readonly NetConnection _connection;

        private ChannelPacket _currentPacket;

        private float _lastFlushTime;
        private readonly byte _channelId;
        private readonly int _maxPacketSize;
        private readonly bool _isReliable;
        bool m_IsBroken;
        int _maxPendingPacketCount;

        const int _maxFreePacketCount = 512; //  this is for all connections. maybe make this configurable
        const int _defaultMaxPendingPacketCount = 16;  // this is per connection. each is around 1400 bytes (MTU)

        private readonly Queue<ChannelPacket> _pendingPackets;
        private static List<ChannelPacket> _freePackets;
        internal static int pendingPacketCount; // this is across all connections. only used for profiler metrics.

        // config
        public float maxDelay = 0.01f;

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

        public ChannelBuffer(NetConnection conn, int bufferSize, byte cid, bool isReliable)
        {
            _connection = conn;
            _maxPacketSize = bufferSize - _packetHeaderReserveSize;
            _currentPacket = new ChannelPacket(_maxPacketSize, isReliable);

            _channelId = cid;
            _maxPendingPacketCount = _defaultMaxPendingPacketCount;
            _isReliable = isReliable;
            if (isReliable)
            {
                _pendingPackets = new Queue<ChannelPacket>();
                if (_freePackets == null)
                {
                    _freePackets = new List<ChannelPacket>();
                }
            }
        }

        // Track whether Dispose has been called.
        bool m_Disposed;

        public void Dispose()
        {
            Dispose(true);
            // Take yourself off the Finalization queue
            // to prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!m_Disposed)
            {
                if (disposing)
                {
                    if (_pendingPackets != null)
                    {
                        while (_pendingPackets.Count > 0)
                        {
                            pendingPacketCount -= 1;

                            var packet = _pendingPackets.Dequeue();
                            if (_freePackets.Count < _maxFreePacketCount)
                            {
                                _freePackets.Add(packet);
                            }
                        }
                        _pendingPackets.Clear();
                    }
                }
            }
            m_Disposed = true;
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
            if (Time.realtimeSinceStartup - _lastFlushTime > maxDelay && !_currentPacket.IsEmpty())
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
            return SendBytes(writer.AsArraySegment().Array, writer.AsArraySegment().Count);
        }

        public bool Send(ushort msgType, NetworkPacketBase msg)
        {
            // build the stream
            _sendWriter.StartMessage(msgType);
            msg.Serialize(_sendWriter);
            _sendWriter.FinishMessage();

            NumMsgsOut += 1;
            return SendWriter(_sendWriter);
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

            if (!_currentPacket.HasSpace(bytesToSend))
            {
                if (_isReliable)
                {
                    if (_pendingPackets.Count == 0)
                    {
                        // nothing in the pending queue yet, just flush and write
                        if (!_currentPacket.SendToTransport(_connection, _channelId))
                        {
                            QueuePacket();
                        }
                        _currentPacket.Write(bytes, bytesToSend);
                        return true;
                    }

                    if (_pendingPackets.Count >= _maxPendingPacketCount)
                    {
                        if (!m_IsBroken)
                        {
                            // only log this once, or it will spam the log constantly
                            if (LogFilter.logError) { Debug.LogError("ChannelBuffer buffer limit of " + _pendingPackets.Count + " packets reached."); }
                        }
                        m_IsBroken = true;
                        return false;
                    }

                    // calling SendToTransport here would write out-of-order data to the stream. just queue
                    QueuePacket();
                    _currentPacket.Write(bytes, bytesToSend);
                    return true;
                }

                if (!_currentPacket.SendToTransport(_connection, _channelId))
                {
                    if (LogFilter.logError) { Debug.Log("ChannelBuffer SendBytes no space on unreliable channel " + _channelId); }
                    return false;
                }

                _currentPacket.Write(bytes, bytesToSend);
                return true;
            }

            _currentPacket.Write(bytes, bytesToSend);
            if (maxDelay == 0.0f)
            {
                return SendInternalBuffer();
            }
            return true;
        }

        void QueuePacket()
        {
            pendingPacketCount += 1;
            _pendingPackets.Enqueue(_currentPacket);
            _currentPacket = AllocPacket();
        }

        ChannelPacket AllocPacket()
        {
#if UNITY_EDITOR
            NetworkDetailStats.SetStat(NetworkDetailStats.NetworkDirection.Outgoing, UnityTransportTypes.HLAPIPending, "msg", pendingPacketCount);
#endif
            if (_freePackets.Count == 0)
            {
                return new ChannelPacket(_maxPacketSize, _isReliable);
            }

            var packet = _freePackets[_freePackets.Count - 1];
            _freePackets.RemoveAt(_freePackets.Count - 1);

            packet.Reset();
            return packet;
        }

        static void FreePacket(ChannelPacket packet)
        {
#if UNITY_EDITOR
            NetworkDetailStats.SetStat(NetworkDetailStats.NetworkDirection.Outgoing, UnityTransportTypes.HLAPIPending, "msg", pendingPacketCount);
#endif
            if (_freePackets.Count >= _maxFreePacketCount)
            {
                // just discard this packet, already tracking too many free packets
                return;
            }
            _freePackets.Add(packet);
        }

        public bool SendInternalBuffer()
        {
#if UNITY_EDITOR
            NetworkDetailStats.IncrementStat(NetworkDetailStats.NetworkDirection.Outgoing, UnityTransportTypes.LLAPIMsg, "msg", 1);
#endif
            if (_isReliable && _pendingPackets.Count > 0)
            {
                // send until transport can take no more
                while (_pendingPackets.Count > 0)
                {
                    var packet = _pendingPackets.Dequeue();
                    if (!packet.SendToTransport(_connection, _channelId))
                    {
                        _pendingPackets.Enqueue(packet);
                        break;
                    }
                    pendingPacketCount -= 1;
                    FreePacket(packet);

                    if (m_IsBroken && _pendingPackets.Count < (_maxPendingPacketCount / 2))
                    {
                        if (LogFilter.logWarn) { Debug.LogWarning("ChannelBuffer recovered from overflow but data was lost."); }
                        m_IsBroken = false;
                    }
                }
                return true;
            }
            return _currentPacket.SendToTransport(_connection, _channelId);
        }
    }
}

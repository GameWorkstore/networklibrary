using GameWorkstore.Patterns;
using Google.Protobuf;
using System;
using System.Collections.Generic;

namespace GameWorkstore.NetworkLibrary
{
    public abstract class NetworkHandlerBase
    {
        public abstract void Invoke(NetMessage packet);
    }

    public class NetworkHandler<T> : NetworkHandlerBase where T : NetworkPacketBase, new()
    {
        public Action<T> Function;

        public override void Invoke(NetMessage packet)
        {
            T it = null;
            try
            {
                it = packet.ReadMessage<T>();
            }
            catch(Exception e)
            {
                DebugMessege.Log("Error while parsing " + typeof(T).Name + " packet:\n" + e.Message + "\n" + e.StackTrace, DebugLevel.ERROR);
            }
            Function(it);
        }

        public override bool Equals(object other)
        {
            return other is NetworkHandler<T> handler && Function == handler.Function;
        }

        public override int GetHashCode()
        {
            return Function.GetHashCode();
        }
    }

    public class ProtoHandler<T> : NetworkHandlerBase where T : IMessage<T>, new()
    {
        public Action<ProtobufPacket<T>> Function;
        private static readonly MessageParser<T> _parser = new MessageParser<T>(Parse);

        private static T Parse() { return new T(); }

        public override void Invoke(NetMessage packet)
        {
            T proto = default;
            try
            {
                proto = _parser.ParseFrom(packet.Reader.Buffer);
            }
            catch(Exception e)
            {
                DebugMessege.Log("Error while parsing " + typeof(T).Name + " packet:\n" + e.Message + "\n" + e.StackTrace, DebugLevel.ERROR);
            }
            Function(new ProtobufPacket<T>()
            {
                Conn = packet.Conn,
                Proto = proto,
            });
        }

        public override bool Equals(object other)
        {
            return other is ProtoHandler<T> handler && Function == handler.Function;
        }

        public override int GetHashCode()
        {
            return Function.GetHashCode();
        }
    }

    public class NetworkHandlers
    {
        private readonly Dictionary<uint, HighSpeedArray<NetworkHandlerBase>> _handlers = new Dictionary<uint, HighSpeedArray<NetworkHandlerBase>>();

        public void RegisterHandler<T>(uint code, Action<T> function) where T : NetworkPacketBase, new()
        {
            if (function == null)
            {
                Log("PacketCode[" + code + "] Error: function is null!", DebugLevel.ERROR);
                return;
            }

            var handler = new NetworkHandler<T>
            {
                Function = function
            };

            if (_handlers.TryGetValue(code, out HighSpeedArray<NetworkHandlerBase> array))
            {
                if (array.Contains(handler))
                {
                    Log("PacketCode[" + code + "] Error: function already added!", DebugLevel.ERROR);
                    return;
                }
                array.Add(handler);
            }
            else
            {
                var nArray = new HighSpeedArray<NetworkHandlerBase>(4);
                nArray.Add(handler);
                _handlers.Add(code, nArray);
            }
        }

        internal bool ContainsHandler<T>(uint code, Action<T> function) where T : NetworkPacketBase, new()
        {
            if (_handlers.TryGetValue(code, out HighSpeedArray<NetworkHandlerBase> array))
            {
                var handler = new NetworkHandler<T>
                {
                    Function = function
                };
                return array.Contains(handler);
            }
            return false;
        }

        public bool UnregisterHandler<T>(uint code, Action<T> function) where T : NetworkPacketBase, new()
        {
            if (_handlers.TryGetValue(code, out HighSpeedArray<NetworkHandlerBase> array))
            {
                var handler = new NetworkHandler<T>
                {
                    Function = function
                };
                return array.Remove(handler);
            }
            return false;
        }

        public void RegisterProtoHandler<T>(uint code, Action<ProtobufPacket<T>> function) where T : IMessage<T>, new()
        {
            if (function == null)
            {
                Log("PacketCode[" + code + "] Error: function is null!", DebugLevel.ERROR);
                return;
            }

            var handler = new ProtoHandler<T>
            {
                Function = function
            };

            if (_handlers.TryGetValue(code, out HighSpeedArray<NetworkHandlerBase> array))
            {
                if (array.Contains(handler))
                {
                    Log("PacketCode[" + code + "] Error: function already added!", DebugLevel.ERROR);
                    return;
                }
                array.Add(handler);
            }
            else
            {
                var nArray = new HighSpeedArray<NetworkHandlerBase>(4);
                nArray.Add(handler);
                _handlers.Add(code, nArray);
            }
        }

        internal bool ContainsProtoHandler<T>(uint code, Action<ProtobufPacket<T>> function) where T : IMessage<T>, new()
        {
            if (_handlers.TryGetValue(code, out HighSpeedArray<NetworkHandlerBase> array))
            {
                var handler = new ProtoHandler<T>
                {
                    Function = function
                };
                return array.Contains(handler);
            }
            return false;
        }

        internal bool UnregisterProtoHandler<T>(uint code, Action<ProtobufPacket<T>> function) where T : IMessage<T>, new()
        {
            if (_handlers.TryGetValue(code, out HighSpeedArray<NetworkHandlerBase> array))
            {
                var handler = new ProtoHandler<T>
                {
                    Function = function
                };
                return array.Remove(handler);
            }
            return false;
        }

        internal bool Invoke(uint code, NetMessage message)
        {
            if (_handlers.TryGetValue(code, out HighSpeedArray<NetworkHandlerBase> array))
            {
                for (int i = 0; i < array.Count; i++)
                {
                    array[i].Invoke(message);
                }
                return true;
            }
            return false;
        }

        internal void Log(string msg, DebugLevel level)
        {
            DebugMessege.Log("[NetworkHandlers]" + msg, level);
        }

        internal void Clear()
        {
            _handlers.Clear();
        }
    }
}

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
            var it = packet.ReadMessage<T>();
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
        public Action<T> Function;
        public static MessageParser<T> Parser = new MessageParser<T>(Parse);

        private static T Parse() { return new T(); }

        public override void Invoke(NetMessage packet)
        {
            //var it = packet.Reader.ReadBytes(packet);
            //Function(it);
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
        private readonly Dictionary<int, HighSpeedArray<NetworkHandlerBase>> _handlers = new Dictionary<int, HighSpeedArray<NetworkHandlerBase>>();

        public void RegisterHandler<T>(int code, Action<T> function) where T : NetworkPacketBase, new()
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

        internal bool ContainsHandler<T>(int code, Action<T> function) where T : NetworkPacketBase, new()
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

        public bool UnregisterHandler<T>(int code, Action<T> function) where T : NetworkPacketBase, new()
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

        public void RegisterProtoHandler<T>(int code, Action<T> function) where T : IMessage<T>, new()
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

        internal bool ContainsProtoHandler<T>(int code, Action<T> function) where T : IMessage<T>, new()
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

        internal bool UnregisterProtoHandler<T>(int code, Action<T> function) where T : IMessage<T>, new()
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

        internal bool Invoke(int code, NetMessage message)
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

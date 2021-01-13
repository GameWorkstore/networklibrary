using GameWorkstore.Patterns;
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

    public class NetworkHandlers
    {
        private readonly Dictionary<ushort, HighSpeedArray<NetworkHandlerBase>> _handlers = new Dictionary<ushort, HighSpeedArray<NetworkHandlerBase>>();

        public void RegisterHandler<T>(ushort code, Action<T> function) where T : NetworkPacketBase, new()
        {
            if (function == null)
            {
                Log("PacketCode[" + code + "] Error: function is null!", DebugLevel.ERROR);
                return;
            }

            NetworkHandler<T> handler = new NetworkHandler<T>
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
                HighSpeedArray<NetworkHandlerBase> narray = new HighSpeedArray<NetworkHandlerBase>(4);
                narray.Add(handler);
                _handlers.Add(code, narray);
            }
        }

        public bool UnregisterHandler<T>(ushort code, Action<T> function) where T : NetworkPacketBase, new()
        {
            if (_handlers.TryGetValue(code, out HighSpeedArray<NetworkHandlerBase> array))
            {
                NetworkHandler<T> handler = new NetworkHandler<T>
                {
                    Function = function
                };
                return array.Remove(handler);
            }
            return false;
        }

        internal bool Invoke(ushort code, NetMessage message)
        {
            if (_handlers.TryGetValue(code, out HighSpeedArray<NetworkHandlerBase> array))
            {
                for(int i = 0; i < array.Count; i++)
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

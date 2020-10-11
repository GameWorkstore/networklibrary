using System.Collections.Generic;

namespace GameWorkstore.NetworkLibrary
{
    public delegate void NetworkHandler(NetMessage packet);

    public class NetworkHandlers
    {
        private readonly Dictionary<short, NetworkHandler> _handlers = new Dictionary<short, NetworkHandler>();

        public void RegisterHandler(short code, NetworkHandler handler)
        {
            if (handler == null)
            {
                Log("ID: " + code + " handler is null!", DebugLevel.ERROR);
                return;
            }
            
            if (_handlers.ContainsKey(code))
            {
                Log("Replace ID: " + code + " Handler: " + handler.Method.Name, DebugLevel.INFO);
                _handlers.Remove(code);
            }
            else
            {
                Log("Added ID: " + code + " Handler: " + handler.Method.Name, DebugLevel.INFO);
            }
            _handlers.Add(code, handler);
        }

        public void UnregisterHandler(short code)
        {
            _handlers.Remove(code);
        }

        internal Dictionary<short, NetworkHandler> GetHandlers()
        {
            return _handlers;
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

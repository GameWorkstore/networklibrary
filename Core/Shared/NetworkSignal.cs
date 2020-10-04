using System;
using System.Collections.Generic;

namespace GameWorkstore.NetworkLibrary
{
    public class NetworkSignal<TMsg> where TMsg : NetworkPacketBase, new()
    {
        internal event Action<TMsg> Action = null;
        internal List<TMsg> cache = new List<TMsg>();
        /// <summary>
        /// Allow debug incoming messeges
        /// </summary>
        public bool Debug = false;

        /// <summary>
        /// called 
        /// </summary>
        /// <param name="evt"></param>
        public void Invoke(NetMessage evt)
        {
            TMsg msg = evt.ReadMessage<TMsg>();
            msg.conn = evt.conn;

            if (Debug)
            {
                DebugMessege.Log("MSG:" + evt.msgType, DebugLevel.INFO);
            }

            if (Action != null)
            {
                Action(msg);
            }
            else
            {
                cache.Add(msg);
            }
        }

        /// <summary>
        /// Register a handler. The first handler will flush cached messeges.
        /// </summary>
        /// <param name="e"></param>
        public void Register(Action<TMsg> e)
        {
            Action += e;
            if (cache.Count > 0)
            {
                foreach (var c in cache)
                {
                    e(c);
                }
                cache.Clear();
            }
        }

        /// <summary>
        /// Unregister a handler.
        /// </summary>
        /// <param name="e"></param>
        public void Unregister(Action<TMsg> e)
        {
            Action -= e;
        }
    }
}
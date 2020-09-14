using System;
using System.Collections.Generic;

namespace GameWorkstore.NetworkLibrary
{
    public class NetworkSignal<TMsg> where TMsg : NetworkMessageBase, new()
    {
        internal event Action<TMsg> Action = null;
        internal List<TMsg> cache = new List<TMsg>();
        public bool Debug = false;

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

        public void Unregister(Action<TMsg> e)
        {
            Action -= e;
        }
    }

    public class NetworkMessageBase : MsgBase
    {
        public NetConnection conn;
    }
}
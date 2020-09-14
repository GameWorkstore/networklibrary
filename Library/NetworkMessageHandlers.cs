using System.Collections.Generic;

namespace GameWorkstore.NetworkLibrary
{
    public delegate void NetworkMessageDelegate(NetMessage netMsg);

    public class NetworkMessageHandlers
    {
        Dictionary<short, NetworkMessageDelegate> m_MsgHandlers = new Dictionary<short, NetworkMessageDelegate>();

        internal void RegisterHandlerSafe(short msgType, NetworkMessageDelegate handler)
        {
            if (handler == null)
            {
                DebugMessege.Log("RegisterHandlerSafe id:" + msgType + " handler is null", DebugLevel.INFO);
                return;
            }

            DebugMessege.Log("RegisterHandlerSafe id:" + msgType + " handler:" + handler.Method.Name, DebugLevel.INFO);
            if (m_MsgHandlers.ContainsKey(msgType))
            {
                //if (LogFilter.logError) { Debug.LogError("RegisterHandlerSafe id:" + msgType + " handler:" + handler.Method.Name + " conflict"); }
                return;
            }
            m_MsgHandlers.Add(msgType, handler);
        }

        public void RegisterHandler(short msgType, NetworkMessageDelegate handler)
        {
            if (handler == null)
            {
                DebugMessege.Log("RegisterHandler id:" + msgType + " handler is null", DebugLevel.ERROR);
                return;
            }

            /*if (msgType <= MsgType.InternalHighest)
            {
                if (LogFilter.logError) { Debug.LogError("RegisterHandler: Cannot replace system message handler " + msgType); }
                return;
            }*/

            if (m_MsgHandlers.ContainsKey(msgType))
            {
                DebugMessege.Log("RegisterHandler replacing " + msgType, DebugLevel.INFO);
                m_MsgHandlers.Remove(msgType);
            }
            DebugMessege.Log("RegisterHandler id:" + msgType + " handler:" + handler.Method.Name, DebugLevel.INFO);
            m_MsgHandlers.Add(msgType, handler);
        }

        public void UnregisterHandler(short msgType)
        {
            m_MsgHandlers.Remove(msgType);
        }

        internal NetworkMessageDelegate GetHandler(short msgType)
        {
            if (m_MsgHandlers.ContainsKey(msgType))
            {
                return m_MsgHandlers[msgType];
            }
            return null;
        }

        internal Dictionary<short, NetworkMessageDelegate> GetHandlers()
        {
            return m_MsgHandlers;
        }

        internal void ClearMessageHandlers()
        {
            m_MsgHandlers.Clear();
        }
    }
}

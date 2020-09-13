using System.Collections.Generic;

namespace UnityEngine.NetLibrary
{
    public class NetPortManager
    {
        private readonly int PortStart;
        private readonly int PortEnd;
        private Stack<int> _disponiblePorts;

        public NetPortManager(int portStart, int portEnd)
        {
            _disponiblePorts = new Stack<int>();
            PortStart = portStart;
            PortEnd = portEnd;
            for (int i = PortEnd; i >= PortStart; i--)
            {
                _disponiblePorts.Push(i);
            }
        }

        public int GetNextPort()
        {
            return _disponiblePorts.Pop();
        }

        public void ReturnPort(int port)
        {
            if (!_disponiblePorts.Contains(port))
            {
                _disponiblePorts.Push(port);
            }
        }
    }
}

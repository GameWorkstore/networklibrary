namespace GameWorkstore.NetworkLibrary
{
    public static class IpAddressUtils
    {
        public static string GetIPAddress(string hostname, IpType ipType, AddressFam addFam)
        {
            //Return null if ADDRESSFAM is Ipv6 but Os does not support it
            if (addFam == AddressFam.IPv6 && !System.Net.Sockets.Socket.OSSupportsIPv6)
            {
                return null;
            }

            //////////////HANDLE LOCAL IP(IPv4 and IPv6)//////////////
            if (ipType == IpType.LocalIp)
            {

                System.Net.IPHostEntry host;
                string ipResult = "";
                host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (System.Net.IPAddress ip in host.AddressList)
                {
                    //IPv4
                    if (addFam == AddressFam.IPv4)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ipResult = ip.ToString();
                        }
                    }

                    //IPv6
                    else if (addFam == AddressFam.IPv6)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        {
                            ipResult = ip.ToString();
                        }
                    }
                }
                return ipResult;
            }

            //////////////HANDLE PUBLIC IP(IPv4 and IPv6)//////////////
            if (ipType == IpType.PublicIp)
            {
                //Return if hostName String is null
                if (string.IsNullOrEmpty(hostname))
                {
                    return null;
                }

                System.Net.IPHostEntry host = System.Net.Dns.GetHostEntry(hostname);
                string ipResult = "";
                foreach (System.Net.IPAddress ip in host.AddressList)
                {
                    //IPv4
                    if (addFam == AddressFam.IPv4)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            ipResult = ip.ToString();
                        }
                    }

                    //IPv6
                    else if (addFam == AddressFam.IPv6)
                    {
                        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                        {
                            ipResult = ip.ToString();
                        }
                    }

                }
                return ipResult;
            }
            return null;
        }

        public enum IpType
        {
            LocalIp, PublicIp
        }

        public enum AddressFam
        {
            IPv4, IPv6
        }
    }
}
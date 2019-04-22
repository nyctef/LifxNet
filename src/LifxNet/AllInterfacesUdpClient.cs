using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace LifxNet
{
    internal class AllInterfacesUdpClient : IDisposable
    {
        private readonly UdpClient _receieveClient;
        private readonly List<UdpClient> _sendClients;

        public AllInterfacesUdpClient(UdpClient receieveClient, List<UdpClient> sendClients)
        {
            _receieveClient = receieveClient;
            _sendClients = sendClients;
        }

        public static AllInterfacesUdpClient Create(int port, ILogger logger = null)
        {
            logger = logger ?? new TraceLogger();

            var sendClients = new List<UdpClient>();

            foreach (var iface in NetworkInterface.GetAllNetworkInterfaces())
            {
                logger.LogDebug($"Considering iface: {iface.Name} : {iface.Description} ");
                logger.LogDebug($"Supports IPv4: {iface.Supports(NetworkInterfaceComponent.IPv4)}");
                logger.LogDebug($"Status : {iface.OperationalStatus}");

                if (!(iface.OperationalStatus == OperationalStatus.Up && iface.Supports(NetworkInterfaceComponent.IPv4)))
                {
                    continue;
                }

                foreach (var unicastIPAddress in iface.GetIPProperties().UnicastAddresses)
                {
                    logger.LogDebug($"IP address: {unicastIPAddress.Address} ({unicastIPAddress.Address.AddressFamily})");
                    if (unicastIPAddress.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        logger.LogDebug($"Adding send client for address {unicastIPAddress.Address}");
                        var sendClient = new UdpClient(new IPEndPoint(unicastIPAddress.Address, port));

                        sendClient.Client.Blocking = false;
                        sendClient.DontFragment = true;
                        sendClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        // TODO: see if these help
                        //sendClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                        //sendClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, true);

                        sendClients.Add(sendClient);
                    }
                }
            }

            var receiveClient = new UdpClient(new IPEndPoint(IPAddress.Any, port));
            // TODO: recheck what all these options do
            receiveClient.Client.Blocking = false;
            receiveClient.DontFragment = true;
            receiveClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);

            return new AllInterfacesUdpClient(receiveClient, sendClients);
        }

        public void Dispose()
        {
            _receieveClient.Dispose();
            foreach (var sendClient in _sendClients)
            {
                sendClient.Dispose();
            }
        }

        internal Task<UdpReceiveResult> ReceiveAsync()
        {
            return _receieveClient.ReceiveAsync();
        }

        internal Task SendAsync(byte[] msg, int length, string hostName, int port)
        {
            return Task.WhenAll(_sendClients.Select(sc => sc.SendAsync(msg, length, hostName, port)));
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Collections.Concurrent;
using System.Threading;

namespace LifxNet
{
    internal class AllInterfacesUdpClient : IDisposable
    {
        private readonly AsyncQueue<UdpReceiveResult> _receiveQueue;
        private readonly List<UdpClient> _sendClients;

        public AllInterfacesUdpClient(List<UdpClient> sendClients, AsyncQueue<UdpReceiveResult> receiveQueue)
        {
            _sendClients = sendClients;
            _receiveQueue = receiveQueue;
        }

        public static AllInterfacesUdpClient Create(int port, ILogger logger = null)
        {
            logger = logger ?? new TraceLogger();

            var sendClients = new List<UdpClient>();
            var receiveQueue = new AsyncQueue<UdpReceiveResult>();

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

                        void receiveCallback(IAsyncResult result)
                        {
                            var queue = (AsyncQueue<UdpReceiveResult>)result.AsyncState;
                            IPEndPoint remoteEndpoint = null;
                            var bytes = sendClient.EndReceive(result, ref remoteEndpoint);
                            logger.LogDebug($"Received {bytes.Length} bytes from {remoteEndpoint}");

                            queue.Enqueue(new UdpReceiveResult(bytes, remoteEndpoint));

                            // and loop
                            sendClient.BeginReceive(receiveCallback, queue);

                        }
                        sendClient.BeginReceive(receiveCallback, receiveQueue);

                        sendClients.Add(sendClient);
                    }
                }
            }

            return new AllInterfacesUdpClient(sendClients, receiveQueue);
        }

        public void Dispose()
        {
            foreach (var sendClient in _sendClients)
            {
                sendClient.Dispose();
            }
        }

        internal Task<UdpReceiveResult> ReceiveAsync()
        {
            return _receiveQueue.DequeueAsync(CancellationToken.None);
        }

        internal Task SendAsync(byte[] msg, int length, string hostName, int port)
        {
            return Task.WhenAll(_sendClients.Select(async sc =>
            {
                try
                {
                    await sc.SendAsync(msg, length, hostName, port);
                }
                catch (Exception e)
                {
                    throw new Exception($"Error while trying to send {length} bytes to {hostName}:{port} : {e.Message}",  e);
                }
            }));
        }
    }
}

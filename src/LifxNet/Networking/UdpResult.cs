using System.Net.Sockets;

namespace LifxNet
{
    internal class UdpResult
    {
        public IUdpClient SendClient { get; }
        public UdpReceiveResult ReceiveResult { get; }

        public UdpResult(IUdpClient sendClient, UdpReceiveResult udpReceiveResult)
        {
            SendClient = sendClient;
            ReceiveResult = udpReceiveResult;
        }
    }
}

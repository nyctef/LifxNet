using System.Threading.Tasks;
using System.Net.Sockets;

namespace LifxNet
{
    internal class SingleUdpClient : IUdpClient
    {
        private readonly UdpClient underlying;

        public SingleUdpClient(UdpClient underlying)
        {
            this.underlying = underlying;
        }

        public void Dispose()
        {
            underlying.Dispose();
        }

        public async Task<UdpResult> ReceiveAsync()
        {
            var result = await underlying.ReceiveAsync();
            return new UdpResult(this, result);
        }

        public Task SendAsync(byte[] msg, int length, string hostName, int port)
        {
            return underlying.SendAsync(msg, length, hostName, port);
        }
    }
}

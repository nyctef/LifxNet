using System;
using System.Threading.Tasks;

namespace LifxNet
{
    internal interface IUdpClient : IDisposable
    {
        Task<UdpResult> ReceiveAsync();
        Task SendAsync(byte[] msg, int length, string hostName, int port);
    }
}

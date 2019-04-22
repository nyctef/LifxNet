using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LifxNet
{
    class ProtocolClient
    {
        public ProtocolClient()
        {

        }

        public void SendMessage(IUdpClient udpClient, LifxMessage message, IPEndPoint endpoint)
        {
            using (var memoryStream = new MemoryStream())
            {
                message.WriteToStream(memoryStream);
                byte[] packet = memoryStream.ToArray();
                udpClient.SendAsync(packet, packet.Length, endpoint.Address.ToString(), endpoint.Port);
            }
        }

        public async Task<LifxResponse> ReceiveMessage(IUdpClient udpClient)
        {
            var response = await udpClient.ReceiveAsync();

            return new LifxResponse(LifxMessage.FromPacket(response.ReceiveResult.Buffer, response.SendClient), response.ReceiveResult.RemoteEndPoint);
        }
    }
}

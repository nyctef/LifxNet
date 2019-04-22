using System.Net;

namespace LifxNet
{
    class LifxResponse
    {
        public LifxMessage Message;
        public IPEndPoint RemoteEndpoint;

        public LifxResponse(LifxMessage message, IPEndPoint remoteEndPoint)
        {
            Message = message;
            RemoteEndpoint = remoteEndPoint;
        }
    }
}
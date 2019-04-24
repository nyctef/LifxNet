using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LifxNet
{
    /// <summary>
    /// A client that can pair requests with responses
    /// </summary>
    class ConversationClient : IDisposable
    {
        private const int Port = 56700;
        private readonly ProtocolClient _protocolClient = new ProtocolClient();
        private readonly IUdpClient _broadcastClient;

        private readonly ConcurrentDictionary<(uint source, byte sequence), TaskCompletionSource<LifxResponse>> _pendingRequests
            = new ConcurrentDictionary<(uint source, byte sequence), TaskCompletionSource<LifxResponse>>();
        private bool _isRunning;

        public ConversationClient(ILogger logger)
        {
            // factory method instead of constructor?
            _broadcastClient = AllInterfacesUdpClient.Create(Port, logger);
            StartReceiveLoop();
        }

        public void BroadcastMessage(LifxMessage message)
        {
            _protocolClient.SendMessage(_broadcastClient, message, new IPEndPoint(IPAddress.Broadcast, Port));

            //var tcs = new TaskCompletionSource()
            // TODO: is there a sensible way to return results from this?
        }

        public void Dispose()
        {
            _isRunning = false;
            _broadcastClient.Dispose();
        }

        public Task<LifxResponse> SendMessage(LifxMessage message, IPEndPoint destination)
        {
            // TODO check if `message` actually requires a response? (requireAck or requireResponse needs to be set)
            // TODO: we can perhaps be smarter when message.RespondClient is not set and try to only send on the correct client for `destination`
            var client = message.RespondClient ?? _broadcastClient;

            _protocolClient.SendMessage(client, message, destination);

            var tcs = new TaskCompletionSource<LifxResponse>();

            _pendingRequests.AddOrUpdate((message.Header.Frame.SourceIdentifier, message.Header.FrameAddress.SequenceNumber), tcs, (__,_)=> tcs);

            return tcs.Task;
        }

        private void StartReceiveLoop()
        {
            _isRunning = true;
            Task.Run(async () =>
            {
                while (_isRunning)
                    try
                    {
                        var result = await _protocolClient.ReceiveMessage(_broadcastClient);
                        HandleIncomingMessages(result);
                    }
                    catch ( Exception e)
                    {
                        Console.WriteLine($"Error handling incoming message: {e.Message}");
                    }
            });
        }

        private void HandleIncomingMessages(LifxResponse result)
        {
            var header = result.Message.Header;
            var threadId = (header.Frame.SourceIdentifier, result.Message.Header.FrameAddress.SequenceNumber);

            Console.WriteLine($"Incoming message with thread id {threadId}");

            if (_pendingRequests.TryGetValue(threadId, out var pendingTcs))
            {
                Console.WriteLine("Setting TCS result");
                pendingTcs.TrySetResult(result);
            }
            else
            {
                Console.WriteLine("Treating as unhandled/unexpected");
                UnhandledMessage?.Invoke(this, result);
            }
        }

        public event EventHandler<LifxResponse> UnhandledMessage;
    }
}
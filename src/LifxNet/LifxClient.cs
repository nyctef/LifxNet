using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;

namespace LifxNet
{
    /// <summary>
    /// LIFX Client for communicating with bulbs
    /// </summary>
    public partial class LifxClient : IDisposable
	{
		
		private const int Port = 56700;
		private AllInterfacesUdpClient _socket;
        private bool _isRunning;

		private LifxClient()
		{
		}

		/// <summary>
		/// Creates a new LIFX client.
		/// </summary>
		/// <returns>client</returns>
		public static Task<LifxClient> CreateAsync()
		{
			LifxClient client = new LifxClient();
			client.Initialize();
            return Task.FromResult(client);
		}

        private void Initialize()
		{
			_socket = AllInterfacesUdpClient.Create(Port);
            _isRunning = true;
            StartReceiveLoop();
		}

        private void StartReceiveLoop()
        {
            Task.Run(async () =>
            {
                while (_isRunning)
                    try
                    {
                        var result = await _socket.ReceiveAsync();
                        if (result.ReceiveResult.Buffer.Length > 0)
                        {
                            HandleIncomingMessages(result);
                        }
                    }
                    catch { }
            });
        }

        private void HandleIncomingMessages(UdpResult result) 
		{
            var remote = result.ReceiveResult.RemoteEndPoint;
            var data = result.ReceiveResult.Buffer;
            var respondClient = result.SendClient;

			var msg = LifxMessage.FromPacket(data, respondClient);
            if (msg.Header.ProtocolHeader.MessageType == MessageType.DeviceStateService)
			{
				ProcessDeviceDiscoveryMessage(remote.Address, remote.Port, msg, respondClient);
			}
			else
			{
				if (taskCompletions.ContainsKey(msg.Header.Frame.SourceIdentifier))
				{
					var tcs = taskCompletions[msg.Header.Frame.SourceIdentifier];
					tcs(msg);
				}
				else
				{
					//TODO
				}
			}
			System.Diagnostics.Debug.WriteLine("Received from {0}:{1}", remote.ToString(),
				string.Join(",", (from a in data select a.ToString("X2")).ToArray()));

		}

		/// <summary>
		/// Disposes the client
		/// </summary>
		public void Dispose()
		{
            _isRunning = false;
			_socket.Dispose();
		}

		private async Task BroadcastMessageAsync(IUdpClient sendClient, string hostName, LifxMessage message)

		{
			if (hostName == null)
			{
				hostName = IPAddress.Broadcast.ToString();
			}
            TaskCompletionSource<LifxMessage> tcs = null;
            if (//header.AcknowledgeRequired && 
                message.Header.Frame.SourceIdentifier > 0 &&
                !(message.Payload is UnknownResponse))
            {
                tcs = new TaskCompletionSource<LifxMessage>();
                Action<LifxMessage> action = (r) =>
                {
                    tcs.TrySetResult(r);
                };
                taskCompletions[message.Header.Frame.SourceIdentifier] = action;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                await WritePacketToStreamAsync(stream, message).ConfigureAwait(false);
                var msg = stream.ToArray();

                System.Diagnostics.Debug.WriteLine("Sending {0}:{1}", hostName,
                string.Join(",", (from a in msg select a.ToString("X2")).ToArray()));

                await sendClient.SendAsync(msg, msg.Length, hostName, Port);
            }
			//{
			//	await WritePacketToStreamAsync(stream, header, (UInt16)type, payload).ConfigureAwait(false);
			//}
			T result = default(T);
			if(tcs != null)
			{
				var _ = Task.Delay(1000).ContinueWith((t) =>
				{
					if (!t.IsCompleted)
						tcs.TrySetException(new TimeoutException());
				});
				try {
					result = await tcs.Task.ConfigureAwait(false);
				}
				finally
				{
					taskCompletions.Remove(header.Identifier);
				}
			}
			return result;
		}



	}

}

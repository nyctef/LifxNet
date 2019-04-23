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
        private readonly ConversationClient _client;
        private bool _isRunning;

		private LifxClient(ConversationClient client, ILogger logger)
		{
            _client = client;
		}

		/// <summary>
		/// Creates a new LIFX client.
		/// </summary>
		/// <returns>client</returns>
		public static Task<LifxClient> CreateAsync(ILogger logger)
		{
			LifxClient client = new LifxClient(new ConversationClient(logger), logger);
			client.Initialize();
            return Task.FromResult(client);
		}

        private void Initialize()
		{
            _isRunning = true;
		}

		/// <summary>
		/// Disposes the client
		/// </summary>
		public void Dispose()
		{
            _isRunning = false;
            _client.Dispose();
		}
    }
}

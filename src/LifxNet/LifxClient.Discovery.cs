﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxNet
{
	public partial class LifxClient : IDisposable
	{
		private static Random randomizer = new Random();
		private UInt32 discoverSourceID;
		private CancellationTokenSource _DiscoverCancellationSource;
		private Dictionary<string, Device> DiscoveredBulbs = new Dictionary<string, Device>();

		/// <summary>
		/// Event fired when a LIFX bulb is discovered on the network
		/// </summary>
		public event EventHandler<DeviceDiscoveryEventArgs> DeviceDiscovered;
		/// <summary>
		/// Event fired when a LIFX bulb hasn't been seen on the network for a while (for more than 5 minutes)
		/// </summary>
		public event EventHandler<DeviceDiscoveryEventArgs> DeviceLost;

		private IList<Device> devices = new List<Device>();
		
		/// <summary>
		/// Gets a list of currently known devices
		/// </summary>
		public IEnumerable<Device> Devices { get { return devices; } }

		/// <summary>
		/// Event args for <see cref="DeviceDiscovered"/> and <see cref="DeviceLost"/> events.
		/// </summary>
		public sealed class DeviceDiscoveryEventArgs : EventArgs
		{
			/// <summary>
			/// The device the event relates to
			/// </summary>
			public Device Device { get; internal set; }
		}

		private void ProcessDeviceDiscoveryMessage(System.Net.IPAddress remoteAddress, int remotePort, LifxMessage msg, IUdpClient respondClient)
		{
            string id = msg.Header.FrameAddress.TargetMacAddressName; //remoteAddress.ToString()
            if (DiscoveredBulbs.ContainsKey(id))  //already discovered
            {
				DiscoveredBulbs[id].LastSeen = DateTime.UtcNow; //Update datestamp
                DiscoveredBulbs[id].HostName = remoteAddress.ToString(); //Update hostname in case IP changed

                return;
			}
			if (msg.Header.Frame.SourceIdentifier != discoverSourceID || //did we request the discovery?
				_DiscoverCancellationSource == null ||
				_DiscoverCancellationSource.IsCancellationRequested) //did we cancel discovery?
				return;

            if (!(msg.Payload is StateServiceResponse stateServiceResponse))
            {
                return; // not the message we were expecting
            }

			var device = new LightBulb()
			{
				HostName = remoteAddress.ToString(),
				Service = stateServiceResponse.Service,
				Port = stateServiceResponse.Port,
				LastSeen = DateTime.UtcNow,
                MacAddress = msg.Header.FrameAddress.TargetMacAddress,
                SendClient = respondClient,
			};
			DiscoveredBulbs[id] = device;
			devices.Add(device);
			if (DeviceDiscovered != null)
			{
				DeviceDiscovered(this, new DeviceDiscoveryEventArgs() { Device = device });
			}
		}

		/// <summary>
		/// Begins searching for bulbs.
		/// </summary>
		/// <seealso cref="DeviceDiscovered"/>
		/// <seealso cref="DeviceLost"/>
		/// <seealso cref="StopDeviceDiscovery"/>
		public void StartDeviceDiscovery()
		{
			if (_DiscoverCancellationSource != null && !_DiscoverCancellationSource.IsCancellationRequested)
				return;
			_DiscoverCancellationSource = new CancellationTokenSource();
			var token = _DiscoverCancellationSource.Token;
			var source = discoverSourceID = (uint)randomizer.Next(int.MaxValue);
			//Start discovery thread
            Task.Run(async () =>
			{
				System.Diagnostics.Debug.WriteLine("Sending GetServices");
                var message = LifxMessage.CreateBroadcast(new GetServiceRequest(), source, false, false, 0);
				while (!token.IsCancellationRequested)
				{
					try
					{
						await BroadcastMessageAsync(_socket, null, message);
					}
					catch { }
					await Task.Delay(5000);
					var lostDevices = devices.Where(d => (DateTime.UtcNow - d.LastSeen).TotalMinutes > 5).ToArray();
					if(lostDevices.Any())
					{
						foreach(var device in lostDevices)
						{
							devices.Remove(device);
							DiscoveredBulbs.Remove(device.MacAddressName);
							if (DeviceLost != null)
								DeviceLost(this, new DeviceDiscoveryEventArgs() { Device = device });
						}
					}
				}
			});
		}

		/// <summary>
		/// Stops device discovery
		/// </summary>
		/// <seealso cref="StartDeviceDiscovery"/>
		public void StopDeviceDiscovery()
		{
			if (_DiscoverCancellationSource == null || _DiscoverCancellationSource.IsCancellationRequested)
				return;
			_DiscoverCancellationSource.Cancel();
			_DiscoverCancellationSource = null;
		}
	}

	/// <summary>
	/// LIFX Generic Device
	/// </summary>
	public abstract class Device
	{
		internal Device() { }

		/// <summary>
		/// Service ID
		/// </summary>
		public byte Service { get; internal set; }

		internal DateTime LastSeen { get; internal set; }

        /// <summary>
        /// Gets the MAC address
        /// </summary>
        public byte[] MacAddress { get; internal set; }

        internal IUdpClient SendClient { get; internal set; }

        public IPEndPoint Endpoint { get; internal set;}

        /// <summary>
        /// Gets the MAC address
        /// </summary>
        public string MacAddressName
        {
            get
            {
                if (MacAddress == null) return null;
                return string.Join(":", MacAddress.Take(6).Select(tb => tb.ToString("X2")).ToArray());
            }
        }
    }
    /// <summary>
    /// LIFX light bulb
    /// </summary>
    public sealed class LightBulb : Device
	{
		internal LightBulb()
		{
		}

    }
}

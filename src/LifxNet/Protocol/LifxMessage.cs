using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LifxNet
{
	public abstract class LifxMessage
	{
		internal static LifxMessage Create(FrameHeader header, MessageType type, UInt32 source, byte[] payload, IUdpClient respondClient)
		{
			LifxMessage response = null;
			switch(type)
			{
				case MessageType.DeviceAcknowledgement:
					response = new AcknowledgementResponse(payload);
					break;
				case MessageType.DeviceStateLabel:
					response = new StateLabelResponse(payload);
					break;
				case MessageType.LightState:
					response = new LightStateResponse(payload);
					break;
				case MessageType.LightStatePower:
					response = new LightPowerResponse(payload);
					break;
				case MessageType.DeviceStateVersion:
					response = new StateVersionResponse(payload);
					break;
				case MessageType.DeviceStateHostFirmware:
					response = new StateHostFirmwareResponse(payload);
					break;
				case MessageType.DeviceStateService:
					response = new StateServiceResponse(payload);
					break;
				default:
					response = new UnknownResponse(payload);
					break;
			}
			response.Header = header;
			response.Type = type;
			response.Payload = payload;
			response.Source = source;
            response.RespondClient = respondClient;
			return response;
		}
		internal LifxMessage() { }
		internal FrameHeader Header { get; private set; }
		internal byte[] Payload { get; private set; }
		internal MessageType Type { get; private set; }
		internal UInt32 Source { get; private set; }
        internal IUdpClient RespondClient { get; private set; }
    }

}

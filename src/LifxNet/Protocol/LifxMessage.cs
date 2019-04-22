using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LifxNet
{
	internal sealed class LifxMessage
	{
        public LifxHeader Header { get; }

        public ILifxPayload Payload { get; }

        public IUdpClient RespondClient { get; }

        internal static LifxMessage Create(LifxHeader header, byte[] payload, IUdpClient respondClient)
		{
			ILifxPayload parsedPayload = null;

			switch(header.ProtocolHeader.MessageType)
			{
				case MessageType.DeviceAcknowledgement:
					parsedPayload = AcknowledgementResponse.FromBytes();
					break;
				case MessageType.DeviceStateLabel:
					parsedPayload = StateLabelResponse.FromBytes(payload);
					break;
				case MessageType.LightState:
					parsedPayload = LightStateResponse.FromBytes(payload);
					break;
				case MessageType.LightStatePower:
					parsedPayload = LightPowerResponse.FromBytes(payload);
					break;
				case MessageType.DeviceStateVersion:
					parsedPayload = StateVersionResponse.FromBytes(payload);
					break;
				case MessageType.DeviceStateHostFirmware:
					parsedPayload = StateHostFirmwareResponse.FromBytes(payload);
					break;
				case MessageType.DeviceStateService:
					parsedPayload = StateServiceResponse.FromBytes(payload);
					break;
				default:
					parsedPayload = UnknownResponse.FromBytes(payload);
					break;
			}

            return new LifxMessage(header, parsedPayload, respondClient);
		}

        internal LifxMessage(LifxHeader header, ILifxPayload payload, IUdpClient respondClient)
        {
            Header = header;
            Payload = payload;
            RespondClient = respondClient;
        }
    }
}

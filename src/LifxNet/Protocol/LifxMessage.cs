using System;
using System.Collections.Generic;
using System.IO;
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

        public void WriteToStream(Stream outStream)
        {
            using (var dw = new BinaryWriter(outStream))
            {
                Header.Frame.WriteToStream(dw);

                Header.FrameAddress.WriteToStream(dw);

                Header.ProtocolHeader.WriteToStream(dw);

                if (Payload != null)
                {
                    Payload.WriteToStream(dw);
                }

                dw.Flush();
            }
        }

        internal static LifxMessage FromPacket(byte[] data, IUdpClient respondClient)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                var headerBytes = new byte[36];
                Array.Copy(data, headerBytes, headerBytes.Length);

                var header = LifxHeader.FromHeaderBytes(headerBytes);

                byte[] payload = null;
                if (data.Length > 36)
                {
                    payload = new byte[data.Length - 36];
                    Array.Copy(data, payload, payload.Length);
                }

                return LifxMessage.FromPayloadBytes(header, payload, respondClient);
            }
        }

        private static LifxMessage FromPayloadBytes(LifxHeader header, byte[] payload, IUdpClient respondClient)
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

        public static LifxMessage CreateBroadcast(ILifxPayload payload, uint sourceIdentifier, bool responseRequired, bool ackRequired, byte sequenceNum)
        {
            ushort size = GetPacketSize(payload);

            return new LifxMessage(
                new LifxHeader(
                new Frame(size, 0x400, true, true, 0, sourceIdentifier),
                new FrameAddress(new byte[16], responseRequired, ackRequired, sequenceNum),
                new ProtocolHeader(payload.MessageType)),
                payload,
                null);
        }

        public static LifxMessage CreateTargeted(ILifxPayload payload, uint sourceIdentifier, bool responseRequired, bool ackRequired, byte sequenceNum, byte[] targetMac)
        {
            ushort size = GetPacketSize(payload);

            return new LifxMessage(
                new LifxHeader(
                new Frame(size, 0x400, true, false, 0, sourceIdentifier),
                new FrameAddress(targetMac, responseRequired, ackRequired, sequenceNum),
                new ProtocolHeader(payload.MessageType)),
                payload,
                null);
        }

        private static ushort GetPacketSize(ILifxPayload payload)
        {
            // TODO: is there a nicer way to get the payload size? Serializing it twice feels a bit circular
            // maybe just put it on the interface?
            const int headerSize = 36;

            using (var tempPayloadStream = new MemoryStream())
            {
                payload.WriteToStream(new BinaryWriter(tempPayloadStream));
                return (ushort)(headerSize + tempPayloadStream.ToArray().Length);
            }
        }
    }
}

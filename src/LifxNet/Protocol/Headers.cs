using System;
using System.IO;
using System.Linq;

namespace LifxNet
{
    internal class Frame
    {
        public UInt16 Size;
        public UInt16 Protocol;
        public bool Addressable;
        public bool Tagged;
        public byte Origin;
        public UInt32 SourceIdentifier;

        public Frame(ushort size, UInt16 protocol, bool addressable, bool tagged, byte origin, uint sourceIdentifier)
        {
            Size = size;
            Protocol = protocol;
            Addressable = addressable;
            Tagged = tagged;
            Origin = origin;
            SourceIdentifier = sourceIdentifier;
        }

        public static Frame FromHeaderBytes(byte[] bytes)
        {
            if (bytes.Length != 8) { throw new ArgumentException("Expecting 16 bytes for the frame address"); }

            var size = BitConverter.ToUInt16(bytes, 0);
            // protocol is everything in byte 2 and the lower half of byte 3
            var protocol = (UInt16)(bytes[2] + (bytes[3] & 0xF0));
            var addressable = (bytes[3] & (1<<4)) > 0;
            var tagged = (bytes[3] & (1<<5)) > 0;
            // origin is bits 6 and 7 of byte 3
            var origin = (byte)((bytes[3] & 0xC0) >> 6);
            var sourceIdentifier = BitConverter.ToUInt32(bytes, 4);

            return new Frame(size, protocol, addressable, tagged, origin, sourceIdentifier);
        }

        public void WriteToStream(BinaryWriter bw)
        {
            bw.Write(Size);
            bw.Write((byte)(Protocol & 0xFF)); // lower octet for protocol
            var protocol = (byte)(
                ((Protocol & 0xFF00) >> 8) + 
                (Addressable ? 1 << 4 : 0) +
                (Tagged ? 1 << 5 : 0) +
                (Origin << 6)
                );
            bw.Write((byte)protocol);
            bw.Write(SourceIdentifier);
        }
    }

    internal class FrameAddress
    {
        public byte[] TargetMacAddress;
        public bool ResponseRequired;
        public bool AckRequired;
        public byte SequenceNumber;

        public FrameAddress(byte[] target, bool responseRequired, bool ackRequired, byte sequenceNum)
        {
            TargetMacAddress = target;
            ResponseRequired = responseRequired;
            AckRequired = ackRequired;
            SequenceNumber = sequenceNum;
        }

        public static FrameAddress FromHeaderBytes(byte[] bytes)
        {
            if (bytes.Length != 16) { throw new ArgumentException("Expecting 16 bytes for the frame address"); }

            var target = new byte[8];
            Array.Copy(bytes, 0, target, 0, target.Length);
            // bytes 16-21 are marked as reserved
            var byte14 = bytes[14];
            var responseRequired = (byte14 & 1) > 0;
            var ackRequired = (byte14 & (1 << 1)) > 0;
            // rest of byte 14 is marked as reserved
            var sequenceNum = bytes[15];

            return new FrameAddress(target, responseRequired, ackRequired, sequenceNum);
        }

        internal void WriteToStream(BinaryWriter bw)
        {
            bw.Write(TargetMacAddress);
            bw.Write(new byte[] { 0, 0, 0, 0, 0, 0 }); // reserved

            if (AckRequired && ResponseRequired)
                bw.Write((byte)0x03);
            else if (AckRequired)
                bw.Write((byte)0x02);
            else if (ResponseRequired)
                bw.Write((byte)0x01);
            else
                bw.Write((byte)0x00);

            bw.Write((byte)SequenceNumber);
        }

        public string TargetMacAddressName
        {
            get
            {
                if (TargetMacAddress == null) return null;
                return string.Join(":", TargetMacAddress.Take(6).Select(tb => tb.ToString("X2")).ToArray());
            }
        }
    }

    internal class ProtocolHeader
    {
        public readonly MessageType MessageType;

        public ProtocolHeader(MessageType messageType)
        {
            MessageType = messageType;
        }

        public static ProtocolHeader FromHeaderBytes(byte[] bytes)
        {
            // TODO should probably gracefully handle these cases instead of throwing an exception - 
            // we need to handle random network traffic appropriately
            if (bytes.Length != 12) { throw new ArgumentException("Expecting 12 bytes for the protocol header"); }

            // bytes 0-7 and 10-11 are marked as reserved
            var messageType = (MessageType) BitConverter.ToUInt16(bytes, 8);

            if (!Enum.IsDefined(typeof(MessageType), messageType)) {
                // TODO log warning for unknown message type
            }

            return new ProtocolHeader(messageType);
        }

        internal void WriteToStream(BinaryWriter bw)
        {
            // TODO: double-check this - marked as reserved in docs, but makes sense
            //if (header.AtTime > DateTime.MinValue)
            //{
            //    var time = header.AtTime.ToUniversalTime();
            //    dw.Write((UInt64)(time - new DateTime(1970, 01, 01)).TotalMilliseconds * 10); //timestamp
            //}

            bw.Write((UInt64)0); // reserved (but see above)

            bw.Write((UInt16)MessageType);

            bw.Write((UInt16)0); // reserved
        }
    }

    internal class LifxHeader
    {
        public Frame Frame;
        public FrameAddress FrameAddress;
        public ProtocolHeader ProtocolHeader;

        public LifxHeader(Frame frame, FrameAddress frameAddress, ProtocolHeader protocolHeader)
        {
            Frame = frame;
            FrameAddress = frameAddress;
            ProtocolHeader = protocolHeader;
        }

        public static LifxHeader FromHeaderBytes(byte[] bytes)
        {
            // TODO should probably gracefully handle these cases instead of throwing an exception - 
            // we need to handle random network traffic appropriately
            if (bytes.Length != 36) { throw new ArgumentException("Expecting 12 bytes for the protocol header"); }

            // could look into using spans to make this more efficient, but those aren't in netstandard2.0 :<

            var frameBytes = new byte[8];
            var frameAddressBytes = new byte[16];
            var protocolHeaderBytes = new byte[12];

            Array.Copy(bytes, 0, frameBytes, 0, frameBytes.Length);
            Array.Copy(bytes, frameBytes.Length, frameAddressBytes, 0, frameAddressBytes.Length);
            Array.Copy(bytes, frameBytes.Length + frameAddressBytes.Length, protocolHeaderBytes, 0, protocolHeaderBytes.Length);

            var frame = Frame.FromHeaderBytes(frameBytes);
            var frameAddress = FrameAddress.FromHeaderBytes(frameAddressBytes);
            var protocolHeader = ProtocolHeader.FromHeaderBytes(protocolHeaderBytes);

            return new LifxHeader(frame, frameAddress, protocolHeader);
        }
    }

}

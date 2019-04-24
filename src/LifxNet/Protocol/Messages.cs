using System;
using System.IO;
using System.Text;

namespace LifxNet
{
    internal class GetServiceRequest : ILifxPayload
    {
        public MessageType MessageType => MessageType.DeviceGetService;

        public void WriteToStream(BinaryWriter dw)
        {
        }
    }

    /// <summary>
    /// Response to any message sent with ack_required set to 1. 
    /// </summary>
    internal class AcknowledgementResponse : ILifxPayload
    {
        public MessageType MessageType => MessageType.DeviceAcknowledgement;

        internal static AcknowledgementResponse FromBytes()
        {
            return new AcknowledgementResponse();
        }

        public void WriteToStream(BinaryWriter dw)
        {
        }
    }

    /// <summary>
    /// Response to GetService message.
    /// Provides the device Service and port.
    /// If the Service is temporarily unavailable, then the port value will be 0.
    /// </summary>
    internal class StateServiceResponse : ILifxPayload
    {
        public StateServiceResponse(byte service, uint port)
        {
            Service = service;
            Port = port;
        }

        public Byte Service { get; }

        public UInt32 Port { get; }

        public MessageType MessageType => MessageType.DeviceStateService;

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            var service = payload[0];
            var port = BitConverter.ToUInt32(payload, 1);

            return new StateServiceResponse(service, port);
        }

        public void WriteToStream(BinaryWriter dw)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Response to GetLabel message. Provides device label.
    /// </summary>
    internal class StateLabelResponse : ILifxPayload
    {
        internal StateLabelResponse(string label)
        {
            Label = label;
        }

        public string Label { get; }

        public MessageType MessageType => MessageType.DeviceStateLabel;

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            return new StateLabelResponse(Utilities.DecodeString(payload));
        }

        public void WriteToStream(BinaryWriter dw)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Sent by a device to provide the current light state
    /// </summary>
    internal class LightStateResponse : ILifxPayload
    {
        public LightStateResponse(ushort hue, ushort saturation, ushort brightness, ushort kelvin, bool isOn, string label)
        {
            Hue = hue;
            Saturation = saturation;
            Brightness = brightness;
            Kelvin = kelvin;
            IsOn = isOn;
            Label = label;
        }

        /// <summary>
        /// Hue
        /// </summary>
        public UInt16 Hue { get; }
        /// <summary>
        /// Saturation (0=desaturated, 65535 = fully saturated)
        /// </summary>
        public UInt16 Saturation { get; }
        /// <summary>
        /// Brightness (0=off, 65535=full brightness)
        /// </summary>
        public UInt16 Brightness { get; }
        /// <summary>
        /// Bulb color temperature
        /// </summary>
        public UInt16 Kelvin { get; }
        /// <summary>
        /// Power state
        /// </summary>
        public bool IsOn { get; }
        /// <summary>
        /// Light label
        /// </summary>
        public string Label { get; }

        public MessageType MessageType => MessageType.LightState;

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            var hue = BitConverter.ToUInt16(payload, 0);
            var saturation = BitConverter.ToUInt16(payload, 2);
            var brightness = BitConverter.ToUInt16(payload, 4);
            var kelvin = BitConverter.ToUInt16(payload, 6);
            var isOn = BitConverter.ToUInt16(payload, 10) > 0;
            var label = Encoding.UTF8.GetString(payload, 12, 32).Replace("\0", "");

            return new LightStateResponse(hue, saturation, brightness, kelvin, isOn, label);
        }

        public void WriteToStream(BinaryWriter dw)
        {
            throw new NotImplementedException();
        }
    }

    internal class LightPowerResponse : ILifxPayload
    {
        public LightPowerResponse(bool isOn)
        {
            IsOn = isOn;
        }

        public bool IsOn { get; }

        public MessageType MessageType => MessageType.LightStatePower;

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            return new LightPowerResponse(BitConverter.ToUInt16(payload, 0) > 0);
        }

        public void WriteToStream(BinaryWriter dw)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Response to GetVersion message.	Provides the hardware version of the device.
    /// </summary>
    internal class StateVersionResponse : ILifxPayload
    {
        public StateVersionResponse(uint vendor, uint product, uint version)
        {
            Vendor = vendor;
            Product = product;
            Version = version;
        }

        /// <summary>
        /// Vendor ID
        /// </summary>
        public UInt32 Vendor { get; }
        /// <summary>
        /// Product ID
        /// </summary>
        public UInt32 Product { get; }
        /// <summary>
        /// Hardware version
        /// </summary>
        public UInt32 Version { get; }

        public MessageType MessageType => MessageType.DeviceStateVersion;

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            var vendor = BitConverter.ToUInt32(payload, 0);
            var product = BitConverter.ToUInt32(payload, 4);
            var version = BitConverter.ToUInt32(payload, 8);

            return new StateVersionResponse(vendor, product, version);
        }

        public void WriteToStream(BinaryWriter dw)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Response to GetHostFirmware message. Provides host firmware information.
    /// </summary>
    internal class StateHostFirmwareResponse : ILifxPayload
    {
        public StateHostFirmwareResponse(DateTime build, uint version)
        {
            Build = build;
            Version = version;
        }

        /// <summary>
        /// Firmware build time
        /// </summary>
        public DateTime Build { get; }

        /// <summary>
        /// Firmware version
        /// </summary>
        public UInt32 Version { get; }

        public MessageType MessageType => MessageType.DeviceStateHostFirmware;

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            var nanoseconds = BitConverter.ToUInt64(payload, 0);
            var build = Utilities.Epoch.AddMilliseconds(nanoseconds * 0.000001);
            //8..15 UInt64 is reserved
            var version = BitConverter.ToUInt32(payload, 16);

            return new StateHostFirmwareResponse(build, version);
        }

        public void WriteToStream(BinaryWriter dw)
        {
            throw new NotImplementedException();
        }
    }

    internal class LightSetPowerRequest : ILifxPayload
    {
        public readonly bool IsOn;
        public readonly UInt32 TransitionDuration;

        public LightSetPowerRequest(bool isOn, UInt32 totalMilliseconds)
        {
            IsOn = isOn;
            TransitionDuration = totalMilliseconds;
        }

        public MessageType MessageType => MessageType.LightSetPower;

        public void WriteToStream(BinaryWriter dw)
        {
            dw.Write((UInt16)(IsOn ? UInt16.MaxValue : 0));
            dw.Write(TransitionDuration);
        }
    }

    internal class DeviceSetPowerRequest : ILifxPayload
    {
        public readonly bool IsOn;

        public DeviceSetPowerRequest(bool isOn)
        {
            IsOn = isOn;
        }

        public MessageType MessageType => MessageType.DeviceSetPower;

        public void WriteToStream(BinaryWriter dw)
        {
            dw.Write((UInt16)(IsOn ? UInt16.MaxValue : 0));
        }
    }

    internal class LightGetPowerRequest : ILifxPayload
    {
        public MessageType MessageType => MessageType.LightGetPower;

        public void WriteToStream(BinaryWriter dw) { }
    }

    internal class DeviceGetLabelRequest : ILifxPayload
    {
        public MessageType MessageType => MessageType.DeviceGetLabel;

        public void WriteToStream(BinaryWriter dw) { }
    }

    internal class DeviceSetLabelRequest : ILifxPayload
    {
        public readonly string Label;

        public DeviceSetLabelRequest(string label)
        {
            Label = label;
        }

        public MessageType MessageType => MessageType.DeviceSetLabel;

        public void WriteToStream(BinaryWriter dw)
        {
            dw.Write(Utilities.EncodeString(Label));
        }
    }

    internal class DeviceGetVersionRequest : ILifxPayload
    {
        public MessageType MessageType => MessageType.DeviceGetVersion;

        public void WriteToStream(BinaryWriter dw) { }
    }

    internal class DeviceGetHostFirmware : ILifxPayload
    {
        public MessageType MessageType => MessageType.DeviceGetHostFirmware;

        public void WriteToStream(BinaryWriter dw) { }
    }

    internal class LightSetColorRequest : ILifxPayload
    {
        private UInt16 hue;
        private UInt16 saturation;
        private UInt16 brightness;
        private UInt16 kelvin;
        private UInt32 duration;

        public LightSetColorRequest(UInt16 hue, UInt16 saturation, UInt16 brightness, UInt16 kelvin, UInt32 duration)
        {
            this.hue = hue;
            this.saturation = saturation;
            this.brightness = brightness;
            this.kelvin = kelvin;
            this.duration = duration;
        }

        public MessageType MessageType => MessageType.LightSetColor;

        public void WriteToStream(BinaryWriter dw)
        {
            dw.Write((byte)0x00); //reserved
            dw.Write(hue);
            dw.Write(saturation);
            dw.Write(brightness);
            dw.Write(kelvin);
            dw.Write(duration);
        }
    }

    internal class LightGetStateRequest : ILifxPayload
    {
        public MessageType MessageType => MessageType.LightGet;

        public void WriteToStream(BinaryWriter dw) { }
    }

    internal class UnknownResponse : ILifxPayload
    {
        internal UnknownResponse(byte[] payload)
        {
            Payload = payload;
        }

        public byte[] Payload { get; }

        public MessageType MessageType => unchecked((MessageType)(-1));

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            return new UnknownResponse(payload);
        }

        public void WriteToStream(BinaryWriter dw)
        {
            dw.Write(Payload);
        }
    }

}

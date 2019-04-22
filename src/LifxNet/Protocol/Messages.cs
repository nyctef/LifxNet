using System;
using System.Text;

namespace LifxNet
{
    /// <summary>
    /// Response to any message sent with ack_required set to 1. 
    /// </summary>
    internal class AcknowledgementResponse : ILifxPayload
    {
        internal static AcknowledgementResponse FromBytes()
        {
            return new AcknowledgementResponse();
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

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            var service = payload[0];
            var port = BitConverter.ToUInt32(payload, 1);

            return new StateServiceResponse(service, port);
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

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            return new StateLabelResponse(Encoding.UTF8.GetString(payload, 0, payload.Length).Replace("\0", ""));
        }
    }

    /// <summary>
    /// Sent by a device to provide the current light state
    /// </summary>
    public class LightStateResponse : ILifxPayload
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
    }
    internal class LightPowerResponse : ILifxPayload
    {
        public LightPowerResponse(bool isOn)
        {
            IsOn = isOn;
        }

        public bool IsOn { get;  }

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            return new LightPowerResponse(BitConverter.ToUInt16(payload, 0) > 0);
        }
    }

    /// <summary>
    /// Response to GetVersion message.	Provides the hardware version of the device.
    /// </summary>
    public class StateVersionResponse : ILifxPayload
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

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            var vendor = BitConverter.ToUInt32(payload, 0);
            var product = BitConverter.ToUInt32(payload, 4);
            var version = BitConverter.ToUInt32(payload, 8);

            return new StateVersionResponse(vendor, product, version);
        }
    }

    /// <summary>
    /// Response to GetHostFirmware message. Provides host firmware information.
    /// </summary>
    public class StateHostFirmwareResponse : ILifxPayload
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

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            var nanoseconds = BitConverter.ToUInt64(payload, 0);
            var build = Utilities.Epoch.AddMilliseconds(nanoseconds * 0.000001);
            //8..15 UInt64 is reserved
            var version = BitConverter.ToUInt32(payload, 16);
            
            return new StateHostFirmwareResponse(build, version);
        }
    }

    internal class UnknownResponse : ILifxPayload
    {
        internal UnknownResponse(byte[] payload)
        {
            Payload = payload;
        }

        public byte[] Payload { get; }

        internal static ILifxPayload FromBytes(byte[] payload)
        {
            return new UnknownResponse(payload);
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxNet
{
    public partial class LifxClient : IDisposable
    {
        /// <summary>
        /// Turns the device on
        /// </summary>
        public Task TurnDeviceOnAsync(Device device)
        {
            System.Diagnostics.Debug.WriteLine("Sending TurnDeviceOn to {0}", device.Endpoint);
            return SetDevicePowerStateAsync(device, true);
        }

        /// <summary>
        /// Turns the device off
        /// </summary>
        public Task TurnDeviceOffAsync(Device device)
        {
            System.Diagnostics.Debug.WriteLine("Sending TurnDeviceOff to {0}", device.Endpoint);
            return SetDevicePowerStateAsync(device, false);
        }

        /// <summary>
        /// Sets the device power state
        /// </summary>
        public async Task SetDevicePowerStateAsync(Device device, bool isOn)
        {
            System.Diagnostics.Debug.WriteLine("Sending TurnDeviceOff to {0}", device.Endpoint);

            var payload = new DeviceSetPowerRequest(isOn);
            var message = LifxMessage.CreateTargeted(payload, (uint)randomizer.Next(), false, true, 0, device.MacAddress, device.SendClient);
            await _client.SendMessage(message, device.Endpoint);
        }

        /// <summary>
        /// Gets the label for the device
        /// </summary>
        public async Task<string> GetDeviceLabelAsync(Device device)
        {
            System.Diagnostics.Debug.WriteLine("Sending GetDeviceLabel to {0}", device.Endpoint);

            var payload = new DeviceGetLabelRequest();
            var message = LifxMessage.CreateTargeted(payload, (uint)randomizer.Next(), true, false, 0, device.MacAddress, device.SendClient);

            var response = await _client.SendMessage(message, device.Endpoint);
            return (response.Message.Payload is StateLabelResponse label) ? label.Label : throw new InvalidOperationException("wrong response");
        }

        /// <summary>
        /// Sets the label on the device
        /// </summary>
        public async Task SetDeviceLabelAsync(Device device, string label)
        {
            System.Diagnostics.Debug.WriteLine("Sending SetDeviceLabelAsync to {0}", device.Endpoint);

            var payload = new DeviceSetLabelRequest(label);

            var message = LifxMessage.CreateTargeted(payload, (uint)randomizer.Next(), false, true, 0, device.MacAddress, device.SendClient);
            await _client.SendMessage(message, device.Endpoint);
        }

        /// <summary>
        /// Gets the device version
        /// </summary>
        public async Task<DeviceVersion> GetDeviceVersionAsync(Device device)
        {
            System.Diagnostics.Debug.WriteLine("Sending GetDeviceVersion to {0}", device.Endpoint);

            var payload = new DeviceGetVersionRequest();
            var message = LifxMessage.CreateTargeted(payload, (uint)randomizer.Next(), true, false, 0, device.MacAddress, device.SendClient);

            var response = await _client.SendMessage(message, device.Endpoint);
            return (response.Message.Payload is StateVersionResponse versionResponse) ? new DeviceVersion(versionResponse) : throw new InvalidOperationException("wrong response");
        }

        /// <summary>
        /// Gets the device's host firmware
        /// </summary>
        public async Task<FirmwareVersion> GetDeviceHostFirmwareAsync(Device device)
        {
            System.Diagnostics.Debug.WriteLine("Sending GetDeviceHostFirmware to {0}", device.Endpoint);

            var payload = new DeviceGetHostFirmware();
            var message = LifxMessage.CreateTargeted(payload, (uint)randomizer.Next(), true, false, 0, device.MacAddress, device.SendClient);

            var response = await _client.SendMessage(message, device.Endpoint);
            return (response.Message.Payload is StateHostFirmwareResponse versionResponse) ? new FirmwareVersion(versionResponse) : throw new InvalidOperationException("wrong response");
        }
    }
}

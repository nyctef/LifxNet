using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LifxNet
{
	public partial class LifxClient : IDisposable
	{
		private Dictionary<UInt32, Action<LifxMessage>> taskCompletions = new Dictionary<uint, Action<LifxMessage>>();

		/// <summary>
		/// Turns a bulb on using the provided transition time
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="transitionDuration"></param>
		/// <returns></returns>
		public Task TurnBulbOnAsync(LightBulb bulb, TimeSpan transitionDuration)
		{
			System.Diagnostics.Debug.WriteLine("Sending TurnBulbOn to {0}", bulb.Endpoint);
			return SetLightPowerAsync(bulb, transitionDuration, true);
		}

		/// <summary>
		/// Turns a bulb off using the provided transition time
		/// </summary>
		public Task TurnBulbOffAsync(LightBulb bulb, TimeSpan transitionDuration)
		{
			System.Diagnostics.Debug.WriteLine("Sending TurnBulbOff to {0}", bulb.Endpoint);
			return SetLightPowerAsync(bulb, transitionDuration, false);
		}

		private async Task SetLightPowerAsync(LightBulb bulb, TimeSpan transitionDuration, bool isOn)
		{
			if (bulb == null)
				throw new ArgumentNullException("bulb");
			if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
				transitionDuration.Ticks < 0)
				throw new ArgumentOutOfRangeException("transitionDuration");

			var payload = new LightSetPowerRequest(isOn, (UInt32)transitionDuration.TotalMilliseconds);
            var message = LifxMessage.CreateTargeted(payload, (uint)randomizer.Next(), false, true, 0, bulb.MacAddress, bulb.SendClient);

            var response = await _client.SendMessage(message, bulb.Endpoint);
            //return response.Message.Payload is AcknowledgementResponse ack;
		}
		/// <summary>
		/// Gets the current power state for a light bulb
		/// </summary>
		/// <param name="bulb"></param>
		/// <returns></returns>
		public async Task<bool> GetLightPowerAsync(LightBulb bulb)
		{
            var payload = new LightGetPowerRequest();
            var message = LifxMessage.CreateTargeted(payload, (uint)randomizer.Next(), true, false, 0, bulb.MacAddress, bulb.SendClient);

            var response = await _client.SendMessage(message, bulb.Endpoint);
            return (response.Message.Payload is LightPowerResponse power) ? power.IsOn : throw new InvalidOperationException("wrong response");
        }

		/// <summary>
		/// Sets color and temperature for a bulb and uses a transition time to the provided state
		/// </summary>
		/// <param name="bulb"></param>
		/// <param name="color"></param>
		/// <param name="kelvin"></param>
		/// <param name="transitionDuration"></param>
		/// <returns></returns>
		public Task SetColorAsync(LightBulb bulb, Color color, UInt16 kelvin, TimeSpan transitionDuration = default)
		{
			var hsl = Utilities.RgbToHsl(color);
			return SetColorAsync(bulb, hsl[0], hsl[1], hsl[2], kelvin, transitionDuration);
		}

		/// <summary>
		/// Sets color and temperature for a bulb and uses a transition time to the provided state
		/// </summary>
		/// <param name="bulb">Light bulb</param>
		/// <param name="hue">0..65535</param>
		/// <param name="saturation">0..65535</param>
		/// <param name="brightness">0..65535</param>
		/// <param name="kelvin">2700..9000</param>
		/// <param name="transitionDuration"></param>
		/// <returns></returns>
		public async Task SetColorAsync(LightBulb bulb,
			UInt16 hue,
			UInt16 saturation,
			UInt16 brightness,
			UInt16 kelvin,
			TimeSpan transitionDuration)
		{
            if (transitionDuration.TotalMilliseconds > UInt32.MaxValue ||
                transitionDuration.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException("transitionDuration");
            }

            if (kelvin < 2500 || kelvin > 9000)
            {
                throw new ArgumentOutOfRangeException("kelvin", "Kelvin must be between 2500 and 9000");
            }

            System.Diagnostics.Debug.WriteLine("Setting color to {0}", bulb.Endpoint);

            UInt32 duration = (UInt32)transitionDuration.TotalMilliseconds;

            var payload = new LightSetColorRequest(hue, saturation,  brightness, kelvin, duration);
            var message = LifxMessage.CreateTargeted(payload, (uint)randomizer.Next(), false, true, 0, bulb.MacAddress, bulb.SendClient);
            var response = await _client.SendMessage(message, bulb.Endpoint);
		}

        /// <summary>
        /// Gets the current state of the bulb
        /// </summary>
        /// <param name="bulb"></param>
        /// <returns></returns>
        public async Task<LightState> GetLightStateAsync(LightBulb bulb)
		{
            var payload = new LightGetStateRequest();
            var message = LifxMessage.CreateTargeted(payload, (uint)randomizer.Next(), true, false, 0, bulb.MacAddress, bulb.SendClient);

            var response = await _client.SendMessage(message, bulb.Endpoint);
            return (response.Message.Payload is LightStateResponse state) ? new LightState(state) : throw new InvalidOperationException("wrong response");
        }
    }
}

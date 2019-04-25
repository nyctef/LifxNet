using LifxNet;
using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace lifxctl
{
    // TODOs:
    // sort out logging (remove console/trace usages and pass logger everywhere)
    // expand command list to cover other lifx messages
    // clean up code a bit more


    class Program
    {
        static async Task<int> Main(string[] args)
        {
            var rootCommand = new RootCommand();

            var discoverCommand = new Command("discover")
            {
                Handler = CommandHandler.Create(RunDiscover)
            };
            rootCommand.AddCommand(discoverCommand);

            var lightCommand = new Command("light");
            Option deviceSpec = new Option("--device-spec", argument: new Argument<string>() { Arity = ArgumentArity.ExactlyOne });
            ValidateSymbol checkDeviceSpec = r =>
               {
                   // apparently we have to do this validation manually at the moment (https://github.com/dotnet/command-line-api/issues/484)
                   if (r.Children["device-spec"] is null)
                   {
                       return "device-spec is required";
                   }
                   else
                   {
                       return null;
                   }
               };

            Command lightOnCommand = new Command("on") { Handler = CommandHandler.Create<string>(RunLightOn), };
            lightOnCommand.AddOption(deviceSpec);
            lightOnCommand.Argument.AddValidator(checkDeviceSpec);
            Command lightOffCommand = new Command("off") { Handler = CommandHandler.Create<string>(RunLightOff), };
            lightOffCommand.AddOption(deviceSpec);
            lightOffCommand.Argument.AddValidator(checkDeviceSpec);

            lightCommand.AddCommand(lightOnCommand);
            lightCommand.AddCommand(lightOffCommand);
            rootCommand.AddCommand(lightCommand);

            var deviceCommand = new Command("device");

            Command getDeviceLabelCommand = new Command("get-label") { Handler = CommandHandler.Create<string>(RunGetDeviceLabel), };
            getDeviceLabelCommand.AddOption(deviceSpec);
            getDeviceLabelCommand.Argument.AddValidator(checkDeviceSpec);

            Command getDeviceVersionCommand = new Command("get-version") { Handler = CommandHandler.Create<string>(RunGetDeviceVersion), };
            getDeviceVersionCommand.AddOption(deviceSpec);
            getDeviceVersionCommand.Argument.AddValidator(checkDeviceSpec);

            deviceCommand.AddCommand(getDeviceLabelCommand);
            deviceCommand.AddCommand(getDeviceVersionCommand);
            rootCommand.AddCommand(deviceCommand);


            var parser = new CommandLineBuilder(rootCommand)
                .UseDefaults()
                .UseParseDirective()
                .EnablePositionalOptions()
                .Build();

            return await parser.InvokeAsync(parser.Parse(args));
        }

        private static async Task<int> RunDiscover()
        {
            var client = await LifxClient.CreateAsync(new TraceLogger());

            client.DeviceDiscovered += (o, e) =>
            {
                Console.WriteLine($"{e.Device.MacAddressName}@{e.Device.Endpoint.Address}:{e.Device.Endpoint.Port}");
                //Console.WriteLine($"Device {e.Device.MacAddressName} found @ {e.Device.Endpoint}");
                //var version = await client.GetDeviceVersionAsync(e.Device);
                //var label = await client.GetDeviceLabelAsync(e.Device);
                //var state = await client.GetLightStateAsync(e.Device as LightBulb);
                //Console.WriteLine($"{state.Label}\n\tIs on: {state.IsOn}\n\tHue: {state.Hue}\n\tSaturation: {state.Saturation}\n\tBrightness: {state.Brightness}\n\tTemperature: {state.Kelvin}");

            };

            client.StartDeviceDiscovery();

            // give some time for discovery to complete
            await Task.Delay(1000);

            return 0;
        }

        private static async Task<int> RunLightOn(string deviceSpec)
        {
            var parsedSpec = DeviceSpec.Parse(deviceSpec);
            var bulb = LightBulb.Create(parsedSpec.IPEndPoint, parsedSpec.MacAddress, service: 1);

            var client = await LifxClient.CreateAsync(new TraceLogger());

            await client.TurnBulbOnAsync(bulb, TimeSpan.FromMilliseconds(0));

            return 0;
        }

        private static async Task<int> RunLightOff(string deviceSpec)
        {
            var parsedSpec = DeviceSpec.Parse(deviceSpec);
            var bulb = LightBulb.Create(parsedSpec.IPEndPoint, parsedSpec.MacAddress, service: 1);

            var client = await LifxClient.CreateAsync(new TraceLogger());

            await client.TurnBulbOffAsync(bulb, TimeSpan.FromMilliseconds(0));

            return 0;
        }

        private static async Task<int> RunGetDeviceLabel(string deviceSpec)
        {
            var parsedSpec = DeviceSpec.Parse(deviceSpec);
            // TODO: don't assume it's a bulb
            var device = LightBulb.Create(parsedSpec.IPEndPoint, parsedSpec.MacAddress, service: 1);

            var client = await LifxClient.CreateAsync(new TraceLogger());

            var result = await client.GetDeviceLabelAsync(device);

            Console.WriteLine(result);
            return 0;
        }

        private static async Task<int> RunGetDeviceVersion(string deviceSpec)
        {
            var parsedSpec = DeviceSpec.Parse(deviceSpec);
            // TODO: don't assume it's a bulb
            var device = LightBulb.Create(parsedSpec.IPEndPoint, parsedSpec.MacAddress, service: 1);

            var client = await LifxClient.CreateAsync(new TraceLogger());

            var versionTask = client.GetDeviceVersionAsync(device);
            var firmwareVersionTask = client.GetDeviceHostFirmwareAsync(device);

            var version = await versionTask;
            var firmwareVersion = await firmwareVersionTask;

            // TODO: look up values to something meaningful
            Console.WriteLine($"Vendor ID: {version.Vendor} Product ID: {version.Product} Version: {version.Version}");
            Console.WriteLine($"Firmware version {firmwareVersion.Version} (built at {firmwareVersion.Build})");

            return 0;
        }

    }

    internal class DeviceSpec
    {
        public readonly IPEndPoint IPEndPoint;
        public readonly PhysicalAddress MacAddress;

        public DeviceSpec(IPEndPoint iPEndPoint, PhysicalAddress physicalAddress)
        {
            IPEndPoint = iPEndPoint;
            MacAddress = physicalAddress;
        }

        internal static DeviceSpec Parse(string deviceSpec)
        {
            var regex = new Regex(@"
^
(?<mac>(?:\w\w\:?)+)
\@
(?<ip>(?:\d+\.?)+)
\:
(?<port>\d+)
$

", RegexOptions.IgnorePatternWhitespace);

            var match = regex.Match(deviceSpec);

            if (!match.Success)
            {
                throw new ArgumentException($"Failed to parse spec {deviceSpec}");
            }

            string ip = match.Groups["ip"].Value;
            string port = match.Groups["port"].Value;
            string mac = match.Groups["mac"].Value;

            Console.WriteLine($"{ip} / {port} / {mac}");

            return new DeviceSpec(new IPEndPoint(IPAddress.Parse(ip), int.Parse(port)), PhysicalAddress.Parse(mac.ToUpper().Replace(":", "")));
        }
    }
}

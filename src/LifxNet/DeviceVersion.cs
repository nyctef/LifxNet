namespace LifxNet
{
    public class DeviceVersion
    {
        internal DeviceVersion(StateVersionResponse versionResponse)
        {
            // TODO: parse these values into something useful
            Vendor = versionResponse.Vendor;
            Product = versionResponse.Product;
            Version = versionResponse.Version;
        }

        public uint Vendor { get; }
        public uint Product { get; }
        public uint Version { get; }
    }
}
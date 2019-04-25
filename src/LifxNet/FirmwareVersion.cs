using System;

namespace LifxNet
{
    public class FirmwareVersion
    {
        internal FirmwareVersion(StateHostFirmwareResponse versionResponse)
        {
            Build = versionResponse.Build;
            // TODO: parse version to make it useful
            Version = versionResponse.Version;
        }

        public DateTime Build { get; }
        public uint Version { get; }
    }
}
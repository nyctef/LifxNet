using System;
using System.Linq;

namespace LifxNet
{
    internal class FrameHeader
	{
		public UInt32 Identifier;
		public byte Sequence;
		public bool AcknowledgeRequired;
		public bool ResponseRequired;
		public byte[] TargetMacAddress;
		public DateTime AtTime;
		public FrameHeader()
		{
			Identifier = 0;
			Sequence = 0;
			AcknowledgeRequired = false;
			ResponseRequired = false;
			TargetMacAddress = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
			AtTime = DateTime.MinValue;
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

}

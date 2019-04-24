using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LifxNet.Tests.Protocol
{
    public class MessageSerializationTests
    {
        [Test]
        public void GetService()
        {
            var expected = "24 00 00 34 4c e5 c1 06 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 00 02 00 00 00";

            var message = LifxMessage.CreateBroadcast(new GetServiceRequest(), 113370444, false, false, 0);
            var actual = FormatBytes(GetBytes(message));

            Assert.AreEqual(expected, actual);
        }

        [Test]
        public void LightSetPower()
        {
            var expected = "2a 00 00 14 c0 65 74 0c d0 73 d5 28 b3 34 00 00 00 00 00 00 00 00 00 01 00 00 00 00 00 00 00 00 75 00 00 00 ff ff c8 00 00 00";

            var message = LifxMessage.CreateTargeted(new LightSetPowerRequest(true, 200), 208954816, false, false, 1, new byte[] { 0xd0, 0x73, 0xd5, 0x28, 0xb3, 0x34, 0, 0 }, null);
            var actual = FormatBytes(GetBytes(message));

            Assert.AreEqual(expected, actual);
        }

        private byte[] GetBytes(LifxMessage message)
        {
            using (var memoryStream = new MemoryStream())
            {
                message.WriteToStream(memoryStream);

                return memoryStream.ToArray();
            }
        }

        private string FormatBytes(byte[] bytes)
        {
            return string.Join(" ", bytes.Select(b => b.ToString("x2")).ToArray());
        }
    }
}

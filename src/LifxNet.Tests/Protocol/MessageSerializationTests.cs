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

            var message = LifxMessage.CreateBroadcast(new GetServiceRequest(), 113370444, true, false, 0);
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
            return string.Join(" ", bytes.Select(b => b.ToString("X2")).ToArray()).ToLower();
        }
    }
}

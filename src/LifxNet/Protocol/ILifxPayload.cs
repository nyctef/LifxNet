using System.IO;

namespace LifxNet
{
    internal interface ILifxPayload
    {
        // marker interface for possible message payloads
        void WriteToStream(BinaryWriter dw);
    }
}
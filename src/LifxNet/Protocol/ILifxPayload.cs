using System.IO;

namespace LifxNet
{
    internal interface ILifxPayload
    {
        MessageType MessageType { get; }

        // marker interface for possible message payloads
        void WriteToStream(BinaryWriter dw);
    }
}
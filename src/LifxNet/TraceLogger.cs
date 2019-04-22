namespace LifxNet
{
    internal class TraceLogger : ILogger
    {
        public void LogDebug(string message)
        {
            System.Diagnostics.Trace.WriteLine($"DEBUG: {message}");
        }
        public void LogInfo(string message)
        {
            System.Diagnostics.Trace.WriteLine($"INFO: {message}");
        }
    }
}
using Yarp.ReverseProxy.Transforms;

namespace Red55.Uma.Authz.Proxy.Yarp
{
    internal static partial class Logging
    {
        [LoggerMessage (Message = "Request Header: {Header}={Value}")]
        internal static partial void LogReqHeaders(ILogger log, LogLevel level, string header, string value);
    }

    internal class LogReqHeadersTransform(ILogger<LogReqHeadersTransform> log) : RequestTransform
    {
        ILogger Log => log;
        public IReadOnlyCollection<string> Headers { get; set; } = [];
        public LogLevel LogLevel { get; set; } = LogLevel.Debug;
#pragma warning disable CA1873 // Avoid potentially expensive logging
        public override ValueTask ApplyAsync(RequestTransformContext context)
        {
            if (Log.IsEnabled(LogLevel) == false)
            {
                return ValueTask.CompletedTask;
            }
            
            if (Headers is null || Headers.Count == 0)
            {
                context.ProxyRequest.Headers.ToList().ForEach(h =>
                {

                    Logging.LogReqHeaders (Log, LogLevel, h.Key, 
                        string.Join(",", h.Value.ToArray()));
                });
            } 
            else
            {
                foreach (var item in Headers)
                {
                    Logging.LogReqHeaders (Log, LogLevel,
                        item,
                        string.Join(",", context.ProxyRequest.Headers.TryGetValues (item, out var values) ?  values.ToArray() : ["N/A"]));
                }
            }

            return ValueTask.CompletedTask;
        }
#pragma warning restore CA1873 // Avoid potentially expensive logging
    }
}

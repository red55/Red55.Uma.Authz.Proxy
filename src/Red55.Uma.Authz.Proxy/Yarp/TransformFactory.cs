using System.Collections.Frozen;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Red55.Uma.Authz.Proxy.Models;
using Red55.Uma.Authz.Proxy.Uma.Api;

using Yarp.ReverseProxy.Transforms.Builder;

namespace Red55.Uma.Authz.Proxy.Yarp;

public class TransformFactory(IOptions<AppConfig> appConfig, 
    ILoggerFactory loggerFactory, IServiceProvider serviceProvider,
    ILogger<TransformFactory> logger) : ITransformFactory
{
    static readonly string UmaAuthzResponseTransform_Key =
        nameof (UmaAuthzTokenEndpointTransform).Replace ("Transform", "");
    static readonly string LogReqHeadersTransform_Key =
        nameof (LogReqHeadersTransform).Replace ("Transform", "");

    ILoggerFactory LoggerFactory => loggerFactory;
    ILogger Log => logger;

    AppConfig Config => appConfig.Value;

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue (LogReqHeadersTransform_Key, out var logHeadersValue))
        {
            var logLevel = LogLevel.Debug;

            if (!string.IsNullOrEmpty (logHeadersValue))
            {
                _ = Enum.TryParse (logHeadersValue, out logLevel);                
            }

            Log.LogWarning ("Using LogLevel: '{LogLevel}' for LogReqHeadersTransform.",
                logHeadersValue);

            string [] headerNames = [];
            if (transformValues.TryGetValue("Headers", out var names))
            {
                headerNames = names.Split (',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }

            var logTransform = serviceProvider.GetRequiredService<LogReqHeadersTransform> ();
            logTransform.LogLevel = logLevel;
            logTransform.Headers = headerNames;

            context.RequestTransforms.Add (logTransform);
            return true;

        }
        else if (transformValues.TryGetValue (UmaAuthzResponseTransform_Key, out var umaEndpoint))
        {
            if (string.IsNullOrEmpty (umaEndpoint))
            {
                throw new Exceptions.InvalidTransformConfigException ("UMA endpoint must be provided for UmaAuthzResponseTransform.");
            }

            string[] azps = [];

            if (transformValues.TryGetValue ("azp", out var azp))
            {
                azps = azp.Split (',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            }

            var endpointUri = new Uri (umaEndpoint, UriKind.RelativeOrAbsolute);
            if (!endpointUri.IsAbsoluteUri
                || string.IsNullOrEmpty (endpointUri.Host))
            {
                endpointUri = new Uri (Config.UmaServerBaseUrl, endpointUri);
            }

            var ep = serviceProvider.GetRequiredService<UmaAuthzTokenEndpointTransform> ();
            ep.ClientId = azps.ToFrozenSet();
            ep.EndPoint = endpointUri;

            context.ResponseTransforms.Add (ep);
            return true;
        }
        return false;
    }

    public bool Validate(TransformRouteValidationContext context,
        IReadOnlyDictionary<string, string> transformValues)
    {
        return true;
    }
}

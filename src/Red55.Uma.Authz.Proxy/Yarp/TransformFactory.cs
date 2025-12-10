using Microsoft.Extensions.Options;

using Red55.Uma.Authz.Proxy.Models;

using Yarp.ReverseProxy.Transforms.Builder;

namespace Red55.Uma.Authz.Proxy.Yarp;

public class TransformFactory(IOptions<AppConfig> appConfig, ILoggerFactory logger) : ITransformFactory
{
    static readonly string UmaAuthzResponseTransform_Key =
        nameof (UmaAuthzResponseTransform).Replace ("Transform", "");
    ILoggerFactory LoggerFactory => logger;

    AppConfig Config => appConfig.Value;

    public bool Build(TransformBuilderContext context, IReadOnlyDictionary<string, string> transformValues)
    {
        if (transformValues.TryGetValue (UmaAuthzResponseTransform_Key, out var umaEndpoint))
        {
            if (string.IsNullOrEmpty (umaEndpoint))
            {
                throw new Exceptions.InvalidTransformConfigException ("UMA endpoint must be provided for UmaAuthzResponseTransform.");
            }

            string azp = string.Empty;
            _ = transformValues.TryGetValue ("azp", out azp);

            var endpointUri = new Uri (umaEndpoint, UriKind.RelativeOrAbsolute);
            if (!endpointUri.IsAbsoluteUri
                || string.IsNullOrEmpty (endpointUri.Host))
            {
                endpointUri = new Uri (new Uri (Config.UmaServerBaseUrl), endpointUri);
            }

            context.ResponseTransforms.Add (new UmaAuthzResponseTransform (endpointUri,
                azp ?? string.Empty,
                LoggerFactory.CreateLogger<UmaAuthzResponseTransform> ()));
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

using System.Buffers;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text.Json;

using EnsureThat;

using Red55.Uma.Authz.Proxy.Extensions;
using Red55.Uma.Authz.Proxy.Helpers;
using Red55.Uma.Authz.Proxy.Uma.Api;

using Yarp.ReverseProxy.Transforms;
namespace Red55.Uma.Authz.Proxy.Yarp;

internal class UmaAuthzResponseTransform(UmaTokenEndpoint umaEndpoint,
    ILogger<UmaAuthzResponseTransform> logger) : ResponseTransform
{
    private ILogger Log { get; } = logger;
    public string ClientId {get; set;}
    public Uri EndPoint { get; set; }

    public override async ValueTask ApplyAsync(ResponseTransformContext context)
    {
        EnsureArg.IsNotNull (context, nameof (context));
        EnsureArg.IsNotNull (context.HttpContext, nameof (context.HttpContext));
        EnsureArg.IsNotNull (context.ProxyResponse, nameof (context.ProxyResponse));
        EnsureArg.IsNotNull (context.HttpContext.Response, nameof (context.HttpContext.Response));
        EnsureArg.IsNotNull (context.HttpContext.Response.ContentType,
            nameof (context.HttpContext.Response.ContentType));

        var hostHeader = context.HttpContext.Request.Headers.Host.ToString ();
        var forwardedProto = context.HttpContext.Request.Headers["X-Forwarded-Proto"].FirstOrDefault ();

        Log.LogDebug ("Request was sent with host: {Host}, proto: {Proto}", hostHeader, forwardedProto);
        if (!context.ProxyResponse.IsSuccessStatusCode)
        {
            return;
        }

        if (!context.HttpContext.Response.ContentType.Equals ("application/json",
            StringComparison.CurrentCultureIgnoreCase))
        {
            return;
        }
        var bw = new ArrayBufferWriter<byte> ();
        using var w = new Utf8JsonWriter (bw);
        try
        {

            Stream bodyStream = await context.ProxyResponse!.Content.ReadAsStreamAsync (context.CancellationToken);

            var r = await JsonSerialization.DeserializeAsync<Models.TokenEndpointResponse> (bodyStream,
                context.CancellationToken);
            if (r is null || string.IsNullOrWhiteSpace (r.AccessToken))
            {
                Log.LogWarning ("Skip Token Endpoint response as no access token present");
                return;
            }

            var h = new JwtSecurityTokenHandler ();
            var t = h.ReadJwtToken (r.AccessToken);
            var azp = t.Claims.FirstOrDefault (c => c.Type.Equals ("azp", StringComparison.OrdinalIgnoreCase));
            if (!azp?.Value.Equals (ClientId) ?? true)
            {
                Log.LogWarning ("Skip token as azp not ours. {TokenAzp}:{ClientId}", azp, ClientId);
                return;
            }                        

            var authzResult = await umaEndpoint.AuthorizeAsync (EndPoint,
                forwardedProto ?? EndPoint.Scheme,
                hostHeader,
                t, ClientId, context.CancellationToken);

            if (authzResult)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                JsonSerialization.Serialize (r, w);
            }
            else
            {
                Log.LogWarning ("UMA authorization failed for token with azp {TokenAzp}", azp?.Value);
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                JsonSerialization.Serialize (new Models.OAuthErrorResponse
                {
                    Error = "access_denied",
                    ErrorDescription = "UMA authorization failed"
                },
                w);


            }
            await w.FlushAsync ();
            context.HttpContext.Response.ContentLength = bw.WrittenCount;


        }
        catch (Exception e)
        {
            Log.LogError (e, "Error processing UMA authorization response");
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally
        {
            context.HttpContext.Response.BodyWriter.Write (bw.WrittenSpan);
        }

    }
}

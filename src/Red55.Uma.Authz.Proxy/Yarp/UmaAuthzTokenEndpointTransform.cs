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

internal class UmaAuthzTokenEndpointTransform(UmaTokenEndpoint umaEndpoint,
    ILogger<UmaAuthzTokenEndpointTransform> logger) : ResponseTransform
{
    private ILogger Log { get; } = logger;
    public IReadOnlySet<string> ClientId {get; set;}
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
        using MemoryStream bodyStream = new ();
        await (await context.ProxyResponse!.Content.ReadAsStreamAsync (context.CancellationToken)).CopyToAsync(bodyStream, 
            context.CancellationToken);
        bodyStream.Position = 0;
        var r = await JsonSerialization.DeserializeAsync<Models.TokenEndpointResponse> (bodyStream,
        context.CancellationToken);
        
        try
        {
            if (r is null || string.IsNullOrEmpty(r.AccessToken))
            {
                Log.LogError ("Failed to decode token endpoint response");
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                JsonSerialization.Serialize (new Models.OAuthErrorResponse
                {
                    Error = "internal_error",
                    ErrorDescription = "Failed to decode Token endpoint response"
                },
                w);
                return;
            }

            var h = new JwtSecurityTokenHandler ();
            var t = h.ReadJwtToken (r.AccessToken);
            var azp = t.Claims.FirstOrDefault (c => c.Type.Equals ("azp", StringComparison.OrdinalIgnoreCase))?.Value;
            if (string.IsNullOrEmpty(azp) || !ClientId.Contains(azp))
            {
                Log.LogWarning ("Skip token as azp not ours. {TokenAzp}:{ClientId}", azp, ClientId);
                return;
            }

            var authzResult = await umaEndpoint.AuthorizeAsync (EndPoint,
                forwardedProto ?? EndPoint.Scheme,
                hostHeader,
                t, azp,
                context.CancellationToken);

            if (authzResult)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.OK;
                return;
            }
            else
            {
                Log.LogWarning ("UMA authorization failed for token with azp {TokenAzp}", azp);
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                JsonSerialization.Serialize (new Models.OAuthErrorResponse
                {
                    Error = "access_denied",
                    ErrorDescription = "UMA authorization failed"
                },
                w);

                return;
            }
        }
        catch (Exception e)
        {
            Log.LogError (e, "Error processing UMA authorization response");
            context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        }
        finally
        {

            await w.FlushAsync (context.CancellationToken);
            
            if (bw.WrittenCount > 0)
            {
                context.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.HttpContext.Response.ContentLength = bw.WrittenCount;
                context.HttpContext.Response.ContentType = "application/json";
                context.HttpContext.Response.BodyWriter.Write (bw.WrittenSpan);
            }
            else
            {
                bodyStream.Position = 0;
                await bodyStream.CopyToAsync (context.HttpContext.Response.Body, context.CancellationToken);
            }
            
        }

    }
}

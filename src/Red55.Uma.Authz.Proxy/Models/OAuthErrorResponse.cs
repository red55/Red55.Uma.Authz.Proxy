using System.Text.Json.Serialization;

namespace Red55.Uma.Authz.Proxy.Models;

[JsonSerializable (typeof (OAuthErrorResponse))]
internal partial class OAuthErrorResponseJsonSerializerContext : JsonSerializerContext
{
}

internal class OAuthErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? ErrorDescription { get; set; }
}


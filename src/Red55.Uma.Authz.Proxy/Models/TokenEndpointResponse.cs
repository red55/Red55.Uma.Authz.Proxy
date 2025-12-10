using System.Text.Json.Serialization;

namespace Red55.Uma.Authz.Proxy.Models;

[JsonSerializable (typeof (TokenEndpointResponse))]
internal partial class TokenEndpointResponseSerializerContext : JsonSerializerContext
{
}

internal class TokenEndpointResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public uint ExpiresIn { get; init; }
    public uint RefreshExpiresIn { get; init; }
    public string RefreshToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = string.Empty;
    public string NotBeforePolicy { get; init; } = string.Empty;
    public string SessionState { get; init; } = string.Empty;
    public string Scope { get; init; } = string.Empty;
}

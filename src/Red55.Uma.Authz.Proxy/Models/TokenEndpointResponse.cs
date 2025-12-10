using System.Text.Json.Serialization;

namespace Red55.Uma.Authz.Proxy.Models;

[JsonSerializable (typeof (TokenEndpointResponse))]
internal partial class TokenEndpointResponseSerializerContext : JsonSerializerContext
{
}

internal class TokenEndpointResponse
{
    public string AccessToken { get; init; }
    public uint ExpiresIn { get; init; }
    public uint RefreshExpiresIn { get; init; }
    public string RefreshToken { get; init; }
    public string TokenType { get; init; }
    public string NotBeforePolicy { get; init; }
    public string SessionState { get; init; }
    public string Scope { get; init; }
}

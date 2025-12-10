using System.Text.Json.Serialization;

using Refit;

namespace Red55.Uma.Authz.Proxy.Models.Uma;

[JsonSerializable (typeof (UmaAuthorizationRequest))]
internal partial class UmaAuthorizationRequestJsonSerializerContext : JsonSerializerContext
{

}
internal class UmaAuthorizationRequest
{
    [AliasAs ("grant_type")]
    public string? GrantType { get; } = "urn:ietf:params:oauth:grant-type:uma-ticket";

    [AliasAs ("response_mode")]
    public string? ResponseMode { get; set; } = "decision";

    [AliasAs ("audience")]
    public string? Audience { get; set; }
}

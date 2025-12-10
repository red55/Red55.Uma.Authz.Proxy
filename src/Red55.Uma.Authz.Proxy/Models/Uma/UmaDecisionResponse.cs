using System.Text.Json.Serialization;

namespace Red55.Uma.Authz.Proxy.Models;

[JsonSerializable (typeof (UmaDecisionResponse))]
internal partial class UmaDecisionResponseSerializerContext : JsonSerializerContext
{
}

internal class UmaDecisionResponse
{
    public bool Result { get; init; } = false;
}

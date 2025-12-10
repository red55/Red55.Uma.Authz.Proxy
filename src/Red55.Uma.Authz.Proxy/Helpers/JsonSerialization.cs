using System.Text.Json;
namespace Red55.Uma.Authz.Proxy.Helpers;

internal static class JsonSerialization
{


    static readonly internal JsonSerializerOptions _options = new ()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    static internal string Serialize<T>(T obj)
    {
        return JsonSerializer.Serialize<T> (obj, _options);
    }

    static internal void Serialize<T>(T obj, Utf8JsonWriter writer)
    {
        JsonSerializer.Serialize<T> (writer, obj, _options);
    }
    static internal T? Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T> (json, _options);
    }

    static internal async Task SerializeAsync<T>(T obj, Stream stream, CancellationToken cancellationToken)
    {
        await JsonSerializer.SerializeAsync<T> (stream, obj, _options, cancellationToken);
    }

    static internal async Task<T?> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken)
    {
        return await JsonSerializer.DeserializeAsync<T> (stream, _options, cancellationToken);
    }
}

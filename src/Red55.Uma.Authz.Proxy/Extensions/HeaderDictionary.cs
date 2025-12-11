namespace Red55.Uma.Authz.Proxy.Extensions;

public static class HeaderDictionaryExtensions
{    
    public static string GetHeaderValue(this IHeaderDictionary headers, string key)
    {
        if (headers.TryGetValue(key, out var value))
        {
            return value.ToString();
        }
        return string.Empty;
    }
    
}

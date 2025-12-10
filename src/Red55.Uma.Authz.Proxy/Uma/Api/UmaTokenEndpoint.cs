using System.IdentityModel.Tokens.Jwt;

using Red55.Uma.Authz.Proxy.Models;
using Red55.Uma.Authz.Proxy.Models.Uma;

using Refit;

namespace Red55.Uma.Authz.Proxy.Uma.Api;


internal interface IUmaTokenEndpoint
{
    [Post ("/{relativeUrl}")]
    [QueryUriFormat (UriFormat.Unescaped)]
    [Headers ("Content-Type: application/x-www-form-urlencoded", "Host: authz.house")]
    Task<ApiResponse<UmaDecisionResponse>> GetRptTokenAsync(string relativeUrl,
        [Authorize ("Bearer")] string accessToken,
        [Body] string req,
        CancellationToken cancellationToken);
}

internal static class UmaTokenEndpoint
{
    public static async Task<bool> AuthorizeAsync(Uri endpoint,
        JwtSecurityToken accessToken, string clientId,
        CancellationToken cancellationToken)
    {
        var port = endpoint.IsDefaultPort ? string.Empty : ":" + endpoint.Port;

        var api = RestService.For<IUmaTokenEndpoint> ($"{endpoint.Scheme}://{endpoint.Host}{port}");
        var rq = new UmaAuthorizationRequest ()
        {
            Audience = clientId
        };

        var b = $"grant_type={rq.GrantType}&response_mode={rq.ResponseMode}&audience={rq.Audience}";
        var r = await api.GetRptTokenAsync (endpoint.PathAndQuery[1..], accessToken.RawData, b, cancellationToken);

        return r.IsSuccessful switch
        {
            true => r.Content?.Result ?? false,
            _ => false
        };
    }
}

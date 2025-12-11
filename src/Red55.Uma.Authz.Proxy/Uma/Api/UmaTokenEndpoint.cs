using System.IdentityModel.Tokens.Jwt;

using Red55.Uma.Authz.Proxy.Models;
using Red55.Uma.Authz.Proxy.Models.Uma;

using Refit;

namespace Red55.Uma.Authz.Proxy.Uma.Api;


internal partial interface IUmaTokenEndpoint
{
    [Post ("/{relativeUrl}")]
    [QueryUriFormat (UriFormat.Unescaped)]
    [Headers ("Content-Type: application/x-www-form-urlencoded")] //, "X-Forwarded-Proto: http"
    Task<ApiResponse<UmaDecisionResponse>> GetUmaTokenAsync(
        [Header ("X-Forwarded-Proto")] string scheme,
        [Header ("Host")] string host,
        string relativeUrl,        
        [Authorize ("Bearer")] string accessToken,
        [Body] string req,
        CancellationToken cancellationToken);
}

internal class UmaTokenEndpoint(IUmaTokenEndpoint api)
{
    public async Task<bool> AuthorizeAsync(Uri endPoint, 
        string schemeHeader,
        string hostHeader,
        JwtSecurityToken accessToken, string clientId,
        CancellationToken cancellationToken)
    {
        // api.BaseAddress = new Uri($"{endPoint.Scheme}://{endPoint.Host}");
        // var port = endpoint.IsDefaultPort ? string.Empty : ":" + endpoint.Port;        
        // var api = RestService.For<IUmaTokenEndpoint> ($"{endpoint.Scheme}://{endpoint.Host}{port}");

        var rq = new UmaAuthorizationRequest ()
        {
            Audience = clientId
        };

        var b = $"grant_type={rq.GrantType}&response_mode={rq.ResponseMode}&audience={rq.Audience}";
        var r = await api.GetUmaTokenAsync (
            schemeHeader,
            hostHeader,
            endPoint.PathAndQuery[1..], accessToken.RawData, b, cancellationToken);

        return r.IsSuccessful switch
        {
            true => r.Content?.Result ?? false,
            _ => false
        };
    }
}

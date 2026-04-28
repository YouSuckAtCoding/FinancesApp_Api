using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Api.Jwt;

public class JwtService
{
    private static readonly Uri TokenEndpoint = new("http://localhost:5002/token");

    public async Task<string> GeneratePartialToken(UserCredentials credentials,
                                                   CancellationToken token = default)
    {
        var request = new
        {
            UserId = credentials.UserId,
            Login = credentials.Email,
            TokenType = (int)TokenType.Partial,
            AccountIds = Array.Empty<Guid>(),
            CustomClaims = new Dictionary<string, object>
            {
                { "role", "user" }
            }
        };

        return await PostTokenRequest(request, token);
    }

    public async Task<string> GenerateFullToken(GenerateFullJwtRequest fullRequest,
                                                CancellationToken token = default)
    {
        var request = new
        {
            UserId = fullRequest.UserId,
            Login = fullRequest.Login,
            TokenType = (int)TokenType.Full,
            AccountIds = fullRequest.AccountIds,
            CustomClaims = fullRequest.CustomClaims.Count > 0
                ? fullRequest.CustomClaims
                : new Dictionary<string, object> { { "role", "user" } }
        };

        return await PostTokenRequest(request, token);
    }

    private static async Task<string> PostTokenRequest(object request, CancellationToken token)
    {
        using HttpClient client = new();
        var response = await client.PostAsJsonAsync(TokenEndpoint, request, token);
        response.EnsureSuccessStatusCode(); 
        return await response.Content.ReadAsStringAsync(token);
    }
}

using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Api.Jwt;

public class JwtService
{
    public async Task<string> GenerateJwtToken(UserCredentials credentials, 
                                               CancellationToken token = default)
    {
        try
        {
            using HttpClient client = new();

            var tokenRequest = new GenerateJwtRequest
            {
                UserId = credentials.UserId,
                Login = credentials.Email,
                CustomClaims = new Dictionary<string, object>
            {
                { "role", "user" }
            }

            };

            var response = await client.PostAsJsonAsync(new Uri("http://localhost:5002/token"), tokenRequest, token);

            return await response.Content.ReadAsStringAsync(token);
        }
        catch (Exception ex)
        {
            // Log the exception (not implemented here)
            throw new ApplicationException("An error occurred while generating the JWT token.", ex);
        }

    }
}

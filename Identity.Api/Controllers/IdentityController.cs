using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Identity.Api.Controllers;

[ApiController]
public class IdentityController : ControllerBase
{
    private static readonly TimeSpan TokenLifetime = TimeSpan.FromMinutes(10);
    private readonly IConfiguration _configuration;

    public IdentityController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("token")]
    public IActionResult GenerateToken([FromBody]TokenGenerationRequest request)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Sub, request.Login),
            new("userid", request.UserId.ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64)
        };
        
        foreach (var claimPair in request.CustomClaims)
        {
            var jsonElement = (JsonElement)claimPair.Value;

            var valueType = jsonElement.ValueKind switch
            {
                JsonValueKind.True => ClaimValueTypes.Boolean,
                JsonValueKind.False => ClaimValueTypes.Boolean,
                JsonValueKind.Number => ClaimValueTypes.Double,
                _ => ClaimValueTypes.String
            };
            
            var claim = new Claim(claimPair.Key, claimPair.Value.ToString()!, valueType);
            claims.Add(claim);
        }

        var rsa = new KeyUtils(_configuration).LoadPrivateKey();
        var rsaKey = new RsaSecurityKey(rsa);

        var _signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(TokenLifetime),
            Issuer = "https://FinancesApp.com",
            Audience = "https://FinancesAppCustomers.com",
            SigningCredentials = _signingCredentials
        };
        
        var token = tokenHandler.CreateToken(tokenDescriptor);

        var jwt = tokenHandler.WriteToken(token);
        
        return Ok(jwt);
    }
}

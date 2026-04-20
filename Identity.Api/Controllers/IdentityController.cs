using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;

namespace Identity.Api.Controllers;

[ApiController]
public class IdentityController : ControllerBase
{
    private static readonly TimeSpan PartialTokenLifetime = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan FullTokenLifetime = TimeSpan.FromMinutes(30);
    private readonly IConfiguration _configuration;

    public IdentityController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("token")]
    public IActionResult GenerateToken([FromBody] TokenGenerationRequest request)
    {
        var encryptionKey = _configuration["ClaimEncryptionKey"]
            ?? throw new InvalidOperationException("ClaimEncryptionKey not found in configuration.");

        var isPartial = request.TokenType == TokenType.Partial;
        var lifetime = isPartial ? PartialTokenLifetime : FullTokenLifetime;

        var claims = BuildClaims(request, encryptionKey, isPartial);

        var rsa = new KeyUtils(_configuration).LoadPrivateKey();
        var rsaKey = new RsaSecurityKey(rsa);
        var signingCredentials = new SigningCredentials(rsaKey, SecurityAlgorithms.RsaSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.Add(lifetime),
            Issuer = "https://FinancesApp.com",
            Audience = "https://FinancesAppCustomers.com",
            SigningCredentials = signingCredentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        var jwt = tokenHandler.WriteToken(token);

        return Ok(jwt);
    }

    private static List<Claim> BuildClaims(TokenGenerationRequest request, string encryptionKey, bool isPartial)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(),
                ClaimValueTypes.Integer64),
            new("token_type", isPartial ? "partial" : "full"),
            new("sub_enc", ClaimEncryption.Encrypt(request.Login, encryptionKey)),
            new("userid_enc", ClaimEncryption.Encrypt(request.UserId.ToString(), encryptionKey))
        };

        if (!isPartial && request.AccountIds.Count > 0)
        {
            var accountIdsJson = JsonSerializer.Serialize(request.AccountIds);
            claims.Add(new Claim("account_ids_enc", ClaimEncryption.Encrypt(accountIdsJson, encryptionKey)));
        }

        if (isPartial)        
            claims.Add(new Claim("2fa_pending", "true", ClaimValueTypes.Boolean));
        
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

        return claims;
    }
}

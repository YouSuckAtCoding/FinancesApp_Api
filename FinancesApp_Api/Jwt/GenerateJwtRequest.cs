namespace FinancesApp_Api.Jwt;

public class GenerateJwtRequest
{
    public Guid UserId { get; set; }

    public string Login { get; set; } = "";

    public Dictionary<string, object> CustomClaims { get; set; } = [];
}

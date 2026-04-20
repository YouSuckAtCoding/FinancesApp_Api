namespace FinancesApp_Api.Jwt;

public enum TokenType { Partial, Full }

public class GeneratePartialJwtRequest
{
    public Guid UserId { get; set; }
    public string Login { get; set; } = "";
}

public class GenerateFullJwtRequest
{
    public Guid UserId { get; set; }
    public string Login { get; set; } = "";
    public List<Guid> AccountIds { get; set; } = [];
    public Dictionary<string, object> CustomClaims { get; set; } = [];
}

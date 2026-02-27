namespace FinancesApp_Api.Endpoints;
public static class CredentialsEndpoints
{
    private const string Base = "api/credentials";

    public const string GetByUserId = $"{Base}/user/{{userId}}";
    public const string GetByLogin = $"{Base}/login/{{login}}";
    public const string CreateCredentials = $"{Base}";
    public const string UpdateCredentials = $"{Base}/{{userId}}";
    public const string DeleteCredentials = $"{Base}/{{userId}}";
}

namespace FinancesApp_Api.Endpoints;

public static class AccountEndpoints
{
    public const string Base = "api/accounts";

    public const string GetAccounts = Base;

    public const string GetAccountById = $"{Base}/{{id}}";

    public const string GetActiveAccounts = $"{Base}/active";

    public const string CreateAccount = Base;
}

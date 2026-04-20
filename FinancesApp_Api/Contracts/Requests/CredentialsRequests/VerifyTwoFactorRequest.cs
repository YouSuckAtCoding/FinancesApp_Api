namespace FinancesApp_Api.Contracts.Requests.CredentialsRequests;

public class VerifyTwoFactorRequest
{
    public string TotpCode { get; set; } = "";
}

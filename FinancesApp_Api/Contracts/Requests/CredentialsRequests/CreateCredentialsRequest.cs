namespace FinancesApp_Api.Contracts.Requests.CredentialsRequests;

public class CreateCredentialsRequest
{
    public string Login { get; set; } = "";
    public string PlainPassword { get; set; } = "";
}
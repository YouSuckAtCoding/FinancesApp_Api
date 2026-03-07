namespace FinancesApp_Api.Contracts.Requests.CredentialsRequests;

public class CreateCredentialsRequest
{
    public string Email { get; set; } = "";
    public string PlainPassword { get; set; } = "";
}
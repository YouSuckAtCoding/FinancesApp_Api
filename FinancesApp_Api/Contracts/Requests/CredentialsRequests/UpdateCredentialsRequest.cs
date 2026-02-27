namespace FinancesApp_Api.Contracts.Requests.CredentialsRequests;

public class UpdateCredentialsRequest
{
    public string UserId { get; set; } = string.Empty;
    public string NewPlainPassword { get; set; } = string.Empty;
}

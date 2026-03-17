namespace FinancesApp_Api.Contracts.Requests.AccountRequests;

public class ApplyDeltaRequest
{
    public string UserId { get; set; } = "";
    public string AccountId { get; set; } = "";
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "";
    public int OperationType { get; set; }
    public string RequestedAt { get; set; } = "";
}
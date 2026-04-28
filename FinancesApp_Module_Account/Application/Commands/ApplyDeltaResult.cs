namespace FinancesApp_Module_Account.Application.Commands;

public record ApplyDeltaResult(bool Success, string? ErrorMessage = null)
{
    public static ApplyDeltaResult Ok() => new(true);
    public static ApplyDeltaResult Failed(string message) => new(false, message);
}

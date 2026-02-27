namespace FinancesApp_Module_Account.Domain;
public static class DomainResult<T> where T : class
{
    public record Result<T>(T Value, bool IsSuccess, string? ErrorMessage = null);
    public static Result<T> Success<T>(T value) => new(value, true);
    public static Result<T> Failure<T>(string errorMessage) => new(default!, false, errorMessage);
    public static Result<T> Failure<T>(string errorMessage, T value) => new(value, false, errorMessage);
}

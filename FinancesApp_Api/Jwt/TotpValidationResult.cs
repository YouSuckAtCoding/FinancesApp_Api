namespace FinancesApp_Api.Jwt;

public enum TotpValidationStatus
{
    Valid,
    InvalidCodeFormat,
    InvalidTokenType,
    MissingUserIdentity,
    InvalidUserId,
    NoActiveTotp,
    TotpExpired,
    InvalidCode
}

public record TotpValidationResult(TotpValidationStatus Status, Guid UserId = default, Guid TotpId = default)
{
    public bool IsValid => Status == TotpValidationStatus.Valid;
}

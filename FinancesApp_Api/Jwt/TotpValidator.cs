using System.Security.Claims;
using FinancesApp_Api.Contracts.Requests.CredentialsRequests;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Application.Commands;
using FinancesApp_Module_Credentials.Application.Queries;
using FinancesApp_Module_Credentials.Domain;

namespace FinancesApp_Api.Jwt;

public class TotpValidator(TotpService totpService,
                           IQueryHandler<GetActiveUserTotp, UserCredentialsTotp?> getActiveTotpHandler,
                           ICommandHandler<InvalidateTotpCredential, bool> invalidateTotpHandler,
                           IConfiguration configuration)
{
    public async Task<TotpValidationResult> Validate(VerifyTwoFactorRequest request,
                                                      ClaimsPrincipal user,
                                                      CancellationToken token = default)
    {
        if (string.IsNullOrWhiteSpace(request.TotpCode) || request.TotpCode.Length != 6)
            return new TotpValidationResult(TotpValidationStatus.InvalidCodeFormat);

        var tokenType = user.FindFirst("token_type")?.Value;
        if (tokenType != "partial")
            return new TotpValidationResult(TotpValidationStatus.InvalidTokenType);

        var encryptedUserId = user.FindFirst("userid_enc")?.Value;
        if (string.IsNullOrEmpty(encryptedUserId))
            return new TotpValidationResult(TotpValidationStatus.MissingUserIdentity);

        var encryptionKey = configuration["ClaimEncryptionKey"]
            ?? throw new InvalidOperationException("ClaimEncryptionKey not found in configuration.");

        var decryptedUserId = ClaimEncryption.Decrypt(encryptedUserId, encryptionKey);
        if (!Guid.TryParse(decryptedUserId, out var userGuid))
            return new TotpValidationResult(TotpValidationStatus.InvalidUserId);

        var activeTotp = await getActiveTotpHandler.Handle(
            new GetActiveUserTotp { UserId = userGuid }, token);

        if (activeTotp is null)
            return new TotpValidationResult(TotpValidationStatus.NoActiveTotp);

        if (DateTimeOffset.UtcNow > activeTotp.InvalidAt)
        {
            await invalidateTotpHandler.Handle(new InvalidateTotpCredential(activeTotp.Id), token);
            return new TotpValidationResult(TotpValidationStatus.TotpExpired);
        }

        if (!totpService.VerifyCode(activeTotp.SecurityCode, request.TotpCode))
            return new TotpValidationResult(TotpValidationStatus.InvalidCode);

        return new TotpValidationResult(TotpValidationStatus.Valid, userGuid, activeTotp.Id);
    }
}

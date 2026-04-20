namespace FinancesApp_Api.Contracts.Responses.CredentialsResponses;

public record LoginResponse(string Token,
                            string QrCodeImage);

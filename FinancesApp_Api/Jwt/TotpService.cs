using OtpNet;
using QRCoder;

namespace FinancesApp_Api.Jwt;

public class TotpService
{
    public TotpGenerationResult GenerateSecret(string userEmail)
    {
        byte[] secretKey = KeyGeneration.GenerateRandomKey(20);
        string base32Secret = Base32Encoding.ToString(secretKey);

        var otpAuthUri = $"otpauth://totp/FinancesApp:{userEmail}?secret={base32Secret}&issuer=FinancesApp&digits=6&period=30";

        using var qrGenerator = new QRCodeGenerator();
        var qrCodeData = qrGenerator.CreateQrCode(otpAuthUri, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrCodeData);
        var qrCodeBytes = qrCode.GetGraphic(5);
        var qrCodeBase64 = Convert.ToBase64String(qrCodeBytes);

        return new TotpGenerationResult(base32Secret, qrCodeBase64);
    }

    public bool VerifyCode(string base32Secret, string totpCode)
    {
        var secretBytes = Base32Encoding.ToBytes(base32Secret);
        var totp = new Totp(secretBytes, step: 30, totpSize: 6);
        return totp.VerifyTotp(totpCode, out _, new VerificationWindow(previous: 1, future: 1));
    }
}

public record TotpGenerationResult(string Base32Secret, string QrCodeBase64);

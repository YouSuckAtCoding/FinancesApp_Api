using System.Security.Cryptography;

namespace Identity.Api;

public class KeyUtils
{
    private readonly IConfiguration _configuration;
    private static string PublicKey { get; set; } = "";
    private static string PrivateKey { get; set; } = "";
    public KeyUtils(IConfiguration configuration)
    {
        _configuration = configuration;
        PublicKey = _configuration["PublicKeyPath"] ?? throw new InvalidOperationException("Public key not found in configuration.");
        PrivateKey = _configuration["PrivateKeyPath"] ?? throw new InvalidOperationException("Private key not found in configuration.");
    }
    public RSA LoadPrivateKey()
    {
        var rsa = RSA.Create();
        string pem = File.ReadAllText(PrivateKey);
        rsa.ImportFromPem(pem);
        return rsa;
    }

    public RSA LoadPublicKey()
    {
        var rsa = RSA.Create();
        string pem = File.ReadAllText(PublicKey);
        rsa.ImportFromPem(pem);
        return rsa;
    }
}

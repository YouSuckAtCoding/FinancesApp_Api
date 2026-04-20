using System.Security.Cryptography;
using System.Text;

namespace Identity.Api;

public static class ClaimEncryption
{
    public static string Encrypt(string plainText, string base64Key)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(base64Key);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        aes.IV.CopyTo(result, 0);
        encryptedBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText, string base64Key)
    {
        var fullBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(base64Key);

        var iv = new byte[aes.BlockSize / 8];
        var encryptedBytes = new byte[fullBytes.Length - iv.Length];
        Array.Copy(fullBytes, 0, iv, 0, iv.Length);
        Array.Copy(fullBytes, iv.Length, encryptedBytes, 0, encryptedBytes.Length);

        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);

        return Encoding.UTF8.GetString(decryptedBytes);
    }
}

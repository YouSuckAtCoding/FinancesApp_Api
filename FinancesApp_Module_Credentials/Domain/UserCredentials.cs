using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace FinancesApp_Module_Credentials.Domain;
public class UserCredentials
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Login { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;

    public UserCredentials()
    {
        
    }
    public UserCredentials(Guid id, Guid userId, string login, string passwordHash)
    {
        Id = id;
        UserId = userId;
        Login = login;
        Password = passwordHash;
    }

    public UserCredentials(string login, string passwordHash)
    {
        Login = login;
        ValidateLogin();
        SetPassword(passwordHash);
        
    }

    public void SetPassword(string plainPassword)
    {
        ValidatePassword(plainPassword);
        Password = BCrypt.Net.BCrypt.HashPassword(plainPassword);
    }

    public bool ValidatePassword(string plainPassword)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
            throw new ArgumentException("Password cannot be empty.");
        if (plainPassword.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        return true;
    }

    public bool VerifyPassword(string plainPassword)
    {
        return BCrypt.Net.BCrypt.Verify(plainPassword, Password);
    }

    public void ValidateLogin()
    {
        if (string.IsNullOrWhiteSpace(Login))
            throw new ArgumentException("Login cannot be empty.");
        if (Login.Length < 3)
            throw new ArgumentException("Login must be at least 3 characters.");
        if (!Regex.IsMatch(Login, @"^[a-zA-Z0-9._@]+$"))
            throw new ArgumentException("Login contains invalid characters.");
    }

    public static string Encrypt(string plainText, string key)
    {
        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);
        aes.GenerateIV();

        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

        var result = new byte[aes.IV.Length + encryptedBytes.Length];
        aes.IV.CopyTo(result, 0);
        encryptedBytes.CopyTo(result, aes.IV.Length);

        return Convert.ToBase64String(result);
    }

    public static string Decrypt(string cipherText, string key)
    {
        var fullBytes = Convert.FromBase64String(cipherText);

        using var aes = Aes.Create();
        aes.Key = Convert.FromBase64String(key);

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


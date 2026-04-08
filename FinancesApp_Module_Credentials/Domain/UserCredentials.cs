using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using FinancesApp_CQRS.Interfaces;
using FinancesApp_Module_Credentials.Domain.Events;

namespace FinancesApp_Module_Credentials.Domain;
public class UserCredentials : AggregateRoot
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string Password { get; private set; } = string.Empty;
    public bool IsDeleted { get; private set; }

    public UserCredentials()
    {

    }

    public UserCredentials(Guid id, Guid userId, string login, string passwordHash)
    {
        Id = id;
        UserId = userId;
        Email = login;
        Password = passwordHash;
    }

    public UserCredentials(Guid userId, string login, string plainPassword)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Email = login;
        ValidateLogin();
        ValidatePassword(plainPassword);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        Raise(new CredentialsRegisteredEvent(Guid.NewGuid(),
                                             DateTimeOffset.UtcNow,
                                             Id,
                                             UserId,
                                             Email,
                                             passwordHash));
    }

    public UserCredentials(string login, string plainPassword)
    {
        Email = login;
        ValidateLogin();
        ValidatePassword(plainPassword);
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(plainPassword);

        Raise(new CredentialsRegisteredEvent(Guid.NewGuid(),
                                             DateTimeOffset.UtcNow,
                                             Id,
                                             UserId,
                                             Email,
                                             passwordHash));
    }

    public void ChangePassword(string newPlainPassword)
    {
        ValidatePassword(newPlainPassword);
        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPlainPassword);

        Raise(new CredentialsPasswordChangedEvent(Guid.NewGuid(),
                                                   DateTimeOffset.UtcNow,
                                                   Id,
                                                   UserId,
                                                   newPasswordHash));
    }

    public void Delete()
    {
        Raise(new CredentialsDeletedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Id, UserId));
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
        if (string.IsNullOrWhiteSpace(Email))
            throw new ArgumentException("Login cannot be empty.");
        if (Email.Length < 3)
            throw new ArgumentException("Login must be at least 3 characters.");
        if (!Regex.IsMatch(Email, @"^[a-zA-Z0-9._@]+$"))
            throw new ArgumentException("Login contains invalid characters.");
    }

    public override void RebuildFromEvents(List<IDomainEvent> events)
    {
        ClearUncommittedEvents();
        foreach (var evt in events)
            Apply(evt);
        SetAggregateVersions(events.Count);
    }

    protected override void Apply(IDomainEvent evt)
    {
        switch (evt)
        {
            case CredentialsRegisteredEvent e:
                Id = e.Id;
                UserId = e.UserId;
                Email = e.Email;
                Password = e.PasswordHash;
                break;

            case CredentialsPasswordChangedEvent e:
                Password = e.NewPasswordHash;
                break;

            case CredentialsDeletedEvent:
                IsDeleted = true;
                break;

            default:
                throw new NotImplementedException(
                    string.Format("No event handler for selected operation {0}", evt.GetType().Name));
        }
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

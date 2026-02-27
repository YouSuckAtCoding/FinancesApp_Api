using FinancesApp_Module_Credentials.Domain;
using FluentAssertions;

namespace FinancesApp_Tests.UserCredentialsTests.Domain;

public class UserCredentialsDomainTests
{
    [Fact]
    public void Should_Create_UserCredentials_When_Login_And_Password_Are_Valid()
    {
        var action = () => new UserCredentials("valid_user", "SecurePass1!");

        action.Should().NotThrow();
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Login_Is_Empty()
    {
        var credentials = new UserCredentials();
      
        var action = () => credentials.ValidateLogin();

        action.Should().Throw<ArgumentException>()
            .WithMessage("Login cannot be empty.");
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Login_Is_Too_Short()
    {
        var credentials = new UserCredentials(
            Guid.NewGuid(), Guid.NewGuid(), "ab", "hashedpassword"
        );

        var action = () => credentials.ValidateLogin();

        action.Should().Throw<ArgumentException>()
            .WithMessage("Login must be at least 3 characters.");
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Login_Contains_Invalid_Characters()
    {
        var credentials = new UserCredentials(
            Guid.NewGuid(), Guid.NewGuid(), "invalid login!", "hashedpassword"
        );

        var action = () => credentials.ValidateLogin();

        action.Should().Throw<ArgumentException>()
            .WithMessage("Login contains invalid characters.");
    }

    [Fact]
    public void Should_Not_Throw_When_Login_Contains_Valid_Special_Characters()
    {
        var credentials = new UserCredentials(
            Guid.NewGuid(), Guid.NewGuid(), "user.name_01@domain", "hashedpassword"
        );

        var action = () => credentials.ValidateLogin();

        action.Should().NotThrow();
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Password_Is_Empty()
    {
        var credentials = new UserCredentials(
            Guid.NewGuid(), Guid.NewGuid(), "validuser", "hashedpassword"
        );

        var action = () => credentials.ValidatePassword(string.Empty);

        action.Should().Throw<ArgumentException>()
            .WithMessage("Password cannot be empty.");
    }

    [Fact]
    public void Should_Throw_ArgumentException_When_Password_Is_Too_Short()
    {
        var credentials = new UserCredentials(
            Guid.NewGuid(), Guid.NewGuid(), "validuser", "hashedpassword"
        );

        var action = () => credentials.ValidatePassword("short");

        action.Should().Throw<ArgumentException>()
            .WithMessage("Password must be at least 8 characters.");
    }

    [Fact]
    public void Should_Hash_Password_When_SetPassword_Is_Called()
    {
        var credentials = new UserCredentials(
            Guid.NewGuid(), Guid.NewGuid(), "validuser", "hashedpassword"
        );
        var plainPassword = "SecurePass1!";

        credentials.SetPassword(plainPassword);

        credentials.Password.Should().NotBe(plainPassword);
        credentials.Password.Should().NotBeNullOrWhiteSpace();
    }
    [Fact]
    public void Should_Return_True_When_Password_Matches()
    {
        var plainPassword = "SecurePass1!";
        var credentials = new UserCredentials("validuser", plainPassword);

        var result = credentials.VerifyPassword(plainPassword);

        result.Should().BeTrue();
    }

    [Fact]
    public void Should_Return_False_When_Password_Does_Not_Match()
    {
        var credentials = new UserCredentials("validuser", "SecurePass1!");

        var result = credentials.VerifyPassword("WrongPassword!");

        result.Should().BeFalse();
    }

    [Fact]
    public void Should_Encrypt_And_Decrypt_Text_Successfully()
    {
        var plainText = "sensitive data";
        var key = Convert.ToBase64String(new byte[32]); // 256-bit key

        var encrypted = UserCredentials.Encrypt(plainText, key);
        var decrypted = UserCredentials.Decrypt(encrypted, key);

        encrypted.Should().NotBe(plainText);
        decrypted.Should().Be(plainText);
    }

    [Fact]
    public void Should_Produce_Different_CipherText_For_Same_Input()
    {
        var plainText = "sensitive data";
        var key = Convert.ToBase64String(new byte[32]);

        var encrypted1 = UserCredentials.Encrypt(plainText, key);
        var encrypted2 = UserCredentials.Encrypt(plainText, key);

        // AES with random IV should produce different ciphertext each time
        encrypted1.Should().NotBe(encrypted2);
    }

    [Fact]
    public void Should_Throw_When_Decrypting_With_Wrong_Key()
    {
        var plainText = "sensitive data";
        var key = Convert.ToBase64String(new byte[32]);
        var wrongKey = Convert.ToBase64String(new byte[32].Select((b, i) => (byte)(i + 1)).ToArray());

        var encrypted = UserCredentials.Encrypt(plainText, key);
        var action = () => UserCredentials.Decrypt(encrypted, wrongKey);

        action.Should().Throw<Exception>();
    }
}
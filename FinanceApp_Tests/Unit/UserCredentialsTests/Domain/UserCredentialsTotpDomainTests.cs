using FinancesApp_Module_Credentials.Domain;
using FinancesApp_Module_Credentials.Domain.Events;
using FluentAssertions;

namespace FinancesApp_Tests.Unit.UserCredentialsTests.Domain;

public class UserCredentialsTotpDomainTests
{
    [Fact]
    public void Constructor_Should_Raise_TotpCredentialCreatedEvent()
    {
        var userId = Guid.NewGuid();
        var secret = "JBSWY3DPEHPK3PXP";

        var totp = new UserCredentialsTotp(userId, secret);

        var events = totp.GetUncommittedEvents();
        events.Should().ContainSingle();
        events.Single().Should().BeOfType<TotpCredentialCreatedEvent>();

        var evt = (TotpCredentialCreatedEvent)events.Single();
        evt.UserId.Should().Be(userId);
        evt.SecurityCode.Should().Be(secret);
    }

    [Fact]
    public void Constructor_Should_Set_Properties_Via_Apply()
    {
        var userId = Guid.NewGuid();
        var secret = "JBSWY3DPEHPK3PXP";

        var totp = new UserCredentialsTotp(userId, secret);

        totp.UserId.Should().Be(userId);
        totp.SecurityCode.Should().Be(secret);
        totp.Active.Should().BeTrue();
        totp.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(2));
        totp.InvalidAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(5), TimeSpan.FromSeconds(2));
    }

    [Fact]
    public void Constructor_Should_Set_NextVersion_To_1()
    {
        var totp = new UserCredentialsTotp(Guid.NewGuid(), "JBSWY3DPEHPK3PXP");

        totp.NextVersion.Should().Be(1);
        totp.CurrentVersion.Should().Be(0);
    }

    [Fact]
    public void Invalidate_Should_Raise_TotpCredentialInvalidatedEvent()
    {
        var totp = new UserCredentialsTotp(Guid.NewGuid(), "JBSWY3DPEHPK3PXP");

        totp.Invalidate();

        var events = totp.GetUncommittedEvents();
        events.Should().HaveCount(2);
        events.Last().Should().BeOfType<TotpCredentialInvalidatedEvent>();
    }

    [Fact]
    public void Invalidate_Should_Set_Active_To_False()
    {
        var totp = new UserCredentialsTotp(Guid.NewGuid(), "JBSWY3DPEHPK3PXP");

        totp.Invalidate();

        totp.Active.Should().BeFalse();
    }

    [Fact]
    public void Invalidate_Should_Throw_When_Already_Invalidated()
    {
        var totp = new UserCredentialsTotp(Guid.NewGuid(), "JBSWY3DPEHPK3PXP");
        totp.Invalidate();

        var act = () => totp.Invalidate();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*already invalidated*");
    }

    [Fact]
    public void RebuildFromEvents_Should_Restore_Created_State()
    {
        var userId = Guid.NewGuid();
        var secret = "JBSWY3DPEHPK3PXP";
        var timestamp = DateTimeOffset.UtcNow;
        var createdEvent = new TotpCredentialCreatedEvent(Guid.NewGuid(), timestamp, Guid.NewGuid(), userId, secret);

        var totp = new UserCredentialsTotp();
        totp.RebuildFromEvents([createdEvent]);

        totp.UserId.Should().Be(userId);
        totp.SecurityCode.Should().Be(secret);
        totp.Active.Should().BeTrue();
        totp.CreatedAt.Should().Be(timestamp);
        totp.InvalidAt.Should().Be(timestamp.AddMinutes(5));
        totp.CurrentVersion.Should().Be(1);
    }

    [Fact]
    public void RebuildFromEvents_Should_Restore_Invalidated_State()
    {
        var userId = Guid.NewGuid();
        var createdEvent = new TotpCredentialCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), userId, "JBSWY3DPEHPK3PXP");
        var invalidatedEvent = new TotpCredentialInvalidatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), userId);

        var totp = new UserCredentialsTotp();
        totp.RebuildFromEvents([createdEvent, invalidatedEvent]);

        totp.Active.Should().BeFalse();
        totp.CurrentVersion.Should().Be(2);
    }

    [Fact]
    public void RebuildFromEvents_Should_Clear_Uncommitted_Events()
    {
        var userId = Guid.NewGuid();
        var createdEvent = new TotpCredentialCreatedEvent(Guid.NewGuid(), DateTimeOffset.UtcNow, Guid.NewGuid(), userId, "JBSWY3DPEHPK3PXP");

        var totp = new UserCredentialsTotp();
        totp.RebuildFromEvents([createdEvent]);

        totp.GetUncommittedEvents().Should().BeEmpty();
    }

    [Fact]
    public void ReadModel_Constructor_Should_Set_All_Properties()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var invalidAt = now.AddMinutes(5);

        var totp = new UserCredentialsTotp(Guid.NewGuid(), userId, "SECRET", now, invalidAt, true);

        totp.UserId.Should().Be(userId);
        totp.SecurityCode.Should().Be("SECRET");
        totp.CreatedAt.Should().Be(now);
        totp.InvalidAt.Should().Be(invalidAt);
        totp.Active.Should().BeTrue();
    }
}

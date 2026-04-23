using FinancesApp_Module_Credentials.Application.Queries;
using FinancesApp_Module_Credentials.Application.Queries.Handlers;
using FinancesApp_Module_Credentials.Application.Repositories;
using FinancesApp_Module_Credentials.Domain;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace FinancesApp_Tests.Unit.UserCredentialsTests.Handlers;

public class GetActiveUserTotpHandlerTests
{
    private readonly IUserCredentialsTotpReadRepository _mockReadRepo;
    private readonly ILogger<GetActiveUserTotpHandler> _mockLogger;
    private readonly GetActiveUserTotpHandler _handler;

    public GetActiveUserTotpHandlerTests()
    {
        _mockReadRepo = Substitute.For<IUserCredentialsTotpReadRepository>();
        _mockLogger = Substitute.For<ILogger<GetActiveUserTotpHandler>>();
        _handler = new GetActiveUserTotpHandler(_mockReadRepo, _mockLogger);
    }

    [Fact]
    public async Task Should_Return_Active_Totp_When_Found()
    {
        var userId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var expectedTotp = new UserCredentialsTotp(Guid.NewGuid(), userId, "JBSWY3DPEHPK3PXP", now, now.AddMinutes(5), true);

        _mockReadRepo.GetActiveTotpByUserIdAsync(userId, null, Arg.Any<CancellationToken>())
            .Returns(expectedTotp);

        var query = new GetActiveUserTotp { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.SecurityCode.Should().Be("JBSWY3DPEHPK3PXP");
        result.Active.Should().BeTrue();
    }

    [Fact]
    public async Task Should_Return_Null_When_No_Active_Totp()
    {
        var userId = Guid.NewGuid();

        _mockReadRepo.GetActiveTotpByUserIdAsync(userId, null, Arg.Any<CancellationToken>())
            .Returns((UserCredentialsTotp?)null);

        var query = new GetActiveUserTotp { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_Return_Null_When_Exception_Occurs()
    {
        var userId = Guid.NewGuid();

        _mockReadRepo.GetActiveTotpByUserIdAsync(userId, null, Arg.Any<CancellationToken>())
            .Throws(new Exception("Database failure"));

        var query = new GetActiveUserTotp { UserId = userId };

        var result = await _handler.Handle(query);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Repository()
    {
        var userId = Guid.NewGuid();
        var cts = new CancellationToken();

        _mockReadRepo.GetActiveTotpByUserIdAsync(userId, null, Arg.Any<CancellationToken>())
            .Returns((UserCredentialsTotp?)null);

        var query = new GetActiveUserTotp { UserId = userId };

        await _handler.Handle(query, cts);

        await _mockReadRepo.Received(1).GetActiveTotpByUserIdAsync(userId, null, cts);
    }
}

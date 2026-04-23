using FinancesApp_CQRS.Dispatchers;
using FinancesApp_CQRS.Interfaces;
using FluentAssertions;

namespace FinancesApp_Tests.Unit.OutboxTests;

public class EventDispatcherTests
{
    private record TestEvent(Guid EventId, DateTimeOffset Timestamp) : IDomainEvent;

    private sealed class FakeEventHandler<TEvent> : IEventHandler<TEvent> where TEvent : IDomainEvent
    {
        public List<TEvent> Handled { get; } = [];
        public List<CancellationToken> ReceivedTokens { get; } = [];

        public Task HandleAsync(TEvent evt, CancellationToken token = default)
        {
            Handled.Add(evt);
            ReceivedTokens.Add(token);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task Should_Dispatch_Single_Event_To_Registered_Handler()
    {
        var dispatcher = new EventDispatcher();
        var handler = new FakeEventHandler<TestEvent>();
        dispatcher.Register(handler);

        var evt = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await dispatcher.Dispatch(evt);

        handler.Handled.Should().ContainSingle().Which.Should().Be(evt);
    }

    [Fact]
    public async Task Should_Dispatch_Enumerable_Calling_Handler_For_Each_Event()
    {
        var dispatcher = new EventDispatcher();
        var handler = new FakeEventHandler<TestEvent>();
        dispatcher.Register(handler);

        var events = new[]
        {
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow),
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow),
            new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow)
        };

        await dispatcher.Dispatch(events);

        handler.Handled.Should().HaveCount(3).And.BeEquivalentTo(events);
    }

    [Fact]
    public async Task Should_Not_Throw_When_No_Handler_Is_Registered_For_Event_Type()
    {
        var dispatcher = new EventDispatcher();
        var evt = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);

        var act = async () => await dispatcher.Dispatch(evt);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Should_Dispatch_To_All_Handlers_Registered_For_Same_Event_Type()
    {
        var dispatcher = new EventDispatcher();
        var handlerA = new FakeEventHandler<TestEvent>();
        var handlerB = new FakeEventHandler<TestEvent>();
        dispatcher.Register(handlerA);
        dispatcher.Register(handlerB);

        var evt = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await dispatcher.Dispatch(evt);

        handlerA.Handled.Should().ContainSingle().Which.Should().Be(evt);
        handlerB.Handled.Should().ContainSingle().Which.Should().Be(evt);
    }

    [Fact]
    public async Task Should_Pass_CancellationToken_To_Handler()
    {
        var dispatcher = new EventDispatcher();
        var handler = new FakeEventHandler<TestEvent>();
        dispatcher.Register(handler);

        using var cts = new CancellationTokenSource();
        var evt = new TestEvent(Guid.NewGuid(), DateTimeOffset.UtcNow);
        await dispatcher.Dispatch(evt, cts.Token);

        handler.ReceivedTokens.Should().ContainSingle().Which.Should().Be(cts.Token);
    }
}

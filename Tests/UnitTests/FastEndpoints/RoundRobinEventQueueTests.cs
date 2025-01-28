using FakeItEasy;
using FastEndpoints;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventQueue;

public class RoundRobinEventQueueTests
{
    [Test]
    public async Task multiple_subscribers()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(A.Fake<IHostApplicationLifetime>());
        var provider = services.BuildServiceProvider();
        EventHub<RRTestEventMulti, InMemoryEventStorageRecord, InMemoryEventHubStorage>.Mode = HubMode.RoundRobin | HubMode.EventBroker;
        var hub = new EventHub<RRTestEventMulti, InMemoryEventStorageRecord, InMemoryEventHubStorage>(provider);

        var writerA = new TestServerStreamWriter<RRTestEventMulti>();
        var writerB = new TestServerStreamWriter<RRTestEventMulti>();

        var ctx = A.Fake<ServerCallContext>();
        A.CallTo(ctx).WithReturnType<CancellationToken>().Returns(default);

        _ = hub.OnSubscriberConnected(hub, Guid.NewGuid().ToString(), writerA, ctx);
        _ = hub.OnSubscriberConnected(hub, Guid.NewGuid().ToString(), writerB, ctx);

        var e1 = new RRTestEventMulti { EventID = 111 };
        await EventHubBase.AddToSubscriberQueues(e1, default);

        var e2 = new RRTestEventMulti { EventID = 222 };
        await EventHubBase.AddToSubscriberQueues(e2, default);

        var e3 = new RRTestEventMulti { EventID = 333 };
        await EventHubBase.AddToSubscriberQueues(e3, default);

        while (writerA.Responses.Count + writerB.Responses.Count < 3)
            await Task.Delay(100);

        if (writerA.Responses.Count == 2)
        {
            await Assert.That(writerB.Responses.Count).IsEqualTo(1);
            await Assert.That(writerB.Responses[0].EventID).IsEqualTo(222);
            
            await Assert.That(writerA.Responses[0].EventID).IsEqualTo(111);
            await Assert.That(writerA.Responses[1].EventID).IsEqualTo(333);
        }
        else if (writerB.Responses.Count == 2)
        {
            await Assert.That(writerA.Responses.Count).IsEqualTo(1);
            await Assert.That(writerA.Responses[0].EventID).IsEqualTo(222);
            
            await Assert.That(writerB.Responses[0].EventID).IsEqualTo(111);
            await Assert.That(writerB.Responses[1].EventID).IsEqualTo(333);
        }
        else
            throw new();
    }

    [Test]
    public async Task multiple_subscribers_but_one_goes_offline()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(A.Fake<IHostApplicationLifetime>());
        var provider = services.BuildServiceProvider();
        EventHub<RRTestEventOneConnected, InMemoryEventStorageRecord, InMemoryEventHubStorage>.Mode = HubMode.RoundRobin;
        var hub = new EventHub<RRTestEventOneConnected, InMemoryEventStorageRecord, InMemoryEventHubStorage>(provider);

        var writerA = new TestServerStreamWriter<RRTestEventOneConnected>();
        var writerB = new TestServerStreamWriter<RRTestEventOneConnected>();

        var ctxA = A.Fake<ServerCallContext>();
        A.CallTo(ctxA).WithReturnType<CancellationToken>().Returns(default);
        _ = hub.OnSubscriberConnected(hub, Guid.NewGuid().ToString(), writerA, ctxA);

        var ctxB = A.Fake<ServerCallContext>();
        var cts = new CancellationTokenSource(100);
        A.CallTo(ctxB).WithReturnType<CancellationToken>().Returns(cts.Token);
        _ = hub.OnSubscriberConnected(hub, Guid.NewGuid().ToString(), writerB, ctxB);

        await Task.Delay(200); //subscriber B is cancelled by now

        var e1 = new RRTestEventOneConnected { EventID = 111 };
        await EventHubBase.AddToSubscriberQueues(e1, default);

        var e2 = new RRTestEventOneConnected { EventID = 222 };
        await EventHubBase.AddToSubscriberQueues(e2, default);

        while (writerA.Responses.Count + writerB.Responses.Count < 2)
            await Task.Delay(100);

        if (writerA.Responses.Count == 2)
        {
            await Assert.That(writerA.Responses[0].EventID).IsEqualTo(111);
            await Assert.That(writerA.Responses[1].EventID).IsEqualTo(222);
            await Assert.That(writerB.Responses.Count).IsEqualTo(0);
        }
        else if (writerB.Responses.Count == 2)
        {
            await Assert.That(writerB.Responses[0].EventID).IsEqualTo(111);
            await Assert.That(writerB.Responses[1].EventID).IsEqualTo(222);
            await Assert.That(writerA.Responses.Count).IsEqualTo(0);
        }
        else
            throw new();
    }

    [Test]
    public async Task only_one_subscriber()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(A.Fake<IHostApplicationLifetime>());
        var provider = services.BuildServiceProvider();
        EventHub<RRTestEventOnlyOne, InMemoryEventStorageRecord, InMemoryEventHubStorage>.Mode = HubMode.RoundRobin;
        var hub = new EventHub<RRTestEventOnlyOne, InMemoryEventStorageRecord, InMemoryEventHubStorage>(provider);

        var writer = new TestServerStreamWriter<RRTestEventOnlyOne>();

        var ctx = A.Fake<ServerCallContext>();
        A.CallTo(ctx).WithReturnType<CancellationToken>().Returns(default);

        _ = hub.OnSubscriberConnected(hub, Guid.NewGuid().ToString(), writer, ctx);

        var e1 = new RRTestEventOnlyOne { EventID = 111 };
        await EventHubBase.AddToSubscriberQueues(e1, default);

        var e2 = new RRTestEventOnlyOne { EventID = 222 };
        await EventHubBase.AddToSubscriberQueues(e2, default);

        var e3 = new RRTestEventOnlyOne { EventID = 333 };
        await EventHubBase.AddToSubscriberQueues(e3, default);

        while (writer.Responses.Count < 1)
            await Task.Delay(100);

        await Assert.That(writer.Responses).HasCount().EqualTo(3);
        await Assert.That(writer.Responses[0].EventID).IsEqualTo(111);
        await Assert.That(writer.Responses[1].EventID).IsEqualTo(222);
        await Assert.That(writer.Responses[2].EventID).IsEqualTo(333);
    }

    class RRTestEventOnlyOne : IEvent
    {
        public int EventID { get; set; }
    }

    class RRTestEventMulti : IEvent
    {
        public int EventID { get; set; }
    }

    class RRTestEventOneConnected : IEvent
    {
        public int EventID { get; set; }
    }

    class TestServerStreamWriter<T> : IServerStreamWriter<T>
    {
        public WriteOptions? WriteOptions { get; set; }
        public List<T> Responses { get; } = new();

        public async Task WriteAsync(T message)
            => Responses.Add(message);

        public Task WriteAsync(T message, CancellationToken ct)
            => WriteAsync(message);
    }
}
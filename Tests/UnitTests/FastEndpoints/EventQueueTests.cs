using FakeItEasy;
using FastEndpoints;
using Grpc.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EventQueue;

public class EventQueueTests
{
    [Test]
    public async Task subscriber_storage_queue_and_dequeue()
    {
        var sut = new InMemoryEventSubscriberStorage();

        var record1 = new InMemoryEventStorageRecord
        {
            Event = "test1",
            EventType = "System.String",
            ExpireOn = DateTime.UtcNow.AddMinutes(1),
            SubscriberID = "sub1"
        };

        var record2 = new InMemoryEventStorageRecord
        {
            Event = "test2",
            EventType = "System.String",
            ExpireOn = DateTime.UtcNow.AddMinutes(1),
            SubscriberID = "sub1"
        };

        var record3 = new InMemoryEventStorageRecord
        {
            Event = "test3",
            EventType = "System.String",
            ExpireOn = DateTime.UtcNow.AddMinutes(1),
            SubscriberID = "sub2"
        };

        await sut.StoreEventAsync(record1, default);
        await sut.StoreEventAsync(record2, default);

        var r1x = await sut.GetNextBatchAsync(new() { SubscriberID = record1.SubscriberID });
        await Assert.That(r1x).HasSingleItem();
        await Assert.That(r1x.Single().Event).IsEqualTo(record1.Event);

        var r2x = await sut.GetNextBatchAsync(new() { SubscriberID = record1.SubscriberID });
        await Assert.That(r2x).HasSingleItem();
        await Assert.That(r2x.Single().Event).IsEqualTo(record2.Event);

        await sut.StoreEventAsync(record3, default);
        await Task.Delay(100);

        var r3 = await sut.GetNextBatchAsync(new() { SubscriberID = record3.SubscriberID });
        await Assert.That(r3).HasSingleItem();
        await Assert.That(r3.Single().Event).IsEqualTo(record3.Event);

        var r3x = await sut.GetNextBatchAsync(new() { SubscriberID = record2.SubscriberID });
        await Assert.That(r3x).IsEmpty();

        var r4x = await sut.GetNextBatchAsync(new() { SubscriberID = record3.SubscriberID });
        await Assert.That(r4x).IsEmpty();
    }

    [Test]
    public async Task subscriber_storage_queue_overflow()
    {
        var sut = new InMemoryEventSubscriberStorage();

        InMemoryEventQueue.MaxLimit = 10000;

        for (var i = 0; i <= InMemoryEventQueue.MaxLimit; i++)
        {
            var r = new InMemoryEventStorageRecord
            {
                Event = i,
                EventType = "System.String",
                ExpireOn = DateTime.UtcNow.AddMinutes(1),
                SubscriberID = "subId"
            };

            if (i < InMemoryEventQueue.MaxLimit)
                await sut.StoreEventAsync(r, default);
            else
            {
                await Assert.ThrowsAsync<OverflowException>(async () => await sut.StoreEventAsync(r, default));
            }
        }
    }

    [Test, NotInParallel] 
    public async Task event_hub_publisher_mode()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(A.Fake<IHostApplicationLifetime>());
        var provider = services.BuildServiceProvider();
        var hub = new EventHub<TestEvent, InMemoryEventStorageRecord, InMemoryEventHubStorage>(provider);
        EventHub<TestEvent, InMemoryEventStorageRecord, InMemoryEventHubStorage>.Mode = HubMode.EventPublisher;

        var writer = new TestServerStreamWriter<TestEvent>();

        var ctx = A.Fake<ServerCallContext>();
        A.CallTo(ctx).WithReturnType<CancellationToken>().Returns(default);

        _ = hub.OnSubscriberConnected(hub, Guid.NewGuid().ToString(), writer, ctx);
        _ = hub.OnSubscriberConnected(hub, Guid.NewGuid().ToString(), writer, ctx);

        var e1 = new TestEvent { EventID = 123 };
        await EventHubBase.AddToSubscriberQueues(e1, default);

        while (writer.Responses.Count < 1)
            await Task.Delay(100);

        await Assert.That(writer.Responses[0].EventID).IsEqualTo(123);
    }

    [Test]
    public async Task event_hub_broker_mode()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, LoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        services.AddSingleton(A.Fake<IHostApplicationLifetime>());
        var provider = services.BuildServiceProvider();
        var hub = new EventHub<TestEvent, InMemoryEventStorageRecord, InMemoryEventHubStorage>(provider);
        EventHub<TestEvent, InMemoryEventStorageRecord, InMemoryEventHubStorage>.Mode = HubMode.EventBroker;

        var writer = new TestServerStreamWriter<TestEvent>();

        var ctx = A.Fake<ServerCallContext>();
        A.CallTo(ctx).WithReturnType<CancellationToken>().Returns(default);

        _ = hub.OnSubscriberConnected(hub, Guid.NewGuid().ToString(), writer, ctx);

        var e1 = new TestEvent { EventID = 321 };
        _ = hub.OnEventReceived(hub, e1, ctx);

        while (writer.Responses.Count < 1)
            await Task.Delay(100);

        await Assert.That(writer.Responses[0].EventID).IsEqualTo(321);
    }

    class TestEvent : IEvent
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
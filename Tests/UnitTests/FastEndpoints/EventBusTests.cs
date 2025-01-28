using FakeItEasy;
using FastEndpoints;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCases.EventHandlingTest;

namespace EventBus;

public class EventBusTests
{
    [Test]
    public async Task AbilityToFakeAnEventHandler()
    {
        var fakeHandler = A.Fake<IEventHandler<NewItemAddedToStock>>();

        A.CallTo(() => fakeHandler.HandleAsync(A<NewItemAddedToStock>.Ignored, A<CancellationToken>.Ignored))
         .Returns(Task.CompletedTask)
         .Once();

        // await evnt.PublishAsync(cancellation: TestContext.Current.CancellationToken);
        var evnt = Factory.CreateEvent([fakeHandler]);
        await evnt.PublishAsync();
    }

    [Test]
    public async Task EventHandlersExecuteSuccessfully()
    {
        var logger = A.Fake<ILogger<NotifyCustomers>>();

        var event1 = new NewItemAddedToStock { ID = 1, Name = "one", Quantity = 10 };
        var event2 = new NewItemAddedToStock { ID = 2, Name = "two", Quantity = 20 };

        var handlers = new IEventHandler<NewItemAddedToStock>[]
        {
            new NotifyCustomers(logger),
            new UpdateInventoryLevel()
        };

        // await new EventBus<NewItemAddedToStock>(handlers).PublishAsync(event1, Mode.WaitForNone, TestContext.Current.CancellationToken);
        await new EventBus<NewItemAddedToStock>(handlers).PublishAsync(event1, Mode.WaitForNone);
        // await new EventBus<NewItemAddedToStock>(handlers).PublishAsync(event2, Mode.WaitForAny, TestContext.Current.CancellationToken);
        await new EventBus<NewItemAddedToStock>(handlers).PublishAsync(event2, Mode.WaitForAny);

        // await Task.Delay(100, TestContext.Current.CancellationToken);
        await Task.Delay(100);
        
        await Assert.That(event2.ID).IsEqualTo(0);
        await Assert.That(event2.Name).IsEqualTo("pass");
        
        await Assert.That(event1.ID).IsEqualTo(0);
        await Assert.That(event1.Name).IsEqualTo("pass");
    }

    [Test]
    public async Task HandlerLogicThrowsException()
    {
        var logger = A.Fake<ILogger<NotifyCustomers>>();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => new EventBus<NewItemAddedToStock>([new NotifyCustomers(logger)]).PublishAsync(
                new()
                // cancellation: TestContext.Current.CancellationToken
                ));
    }

    [Test]
    public async Task RegisterFakeEventHandlerAndPublish()
    {
        var fakeHandler = new FakeEventHandler();

        Factory.RegisterTestServices(
            s =>
            {
                s.AddSingleton<IEventHandler<NewItemAddedToStock>>(fakeHandler);
            });

        // await new NewItemAddedToStock { Name = "xyz" }.PublishAsync(cancellation: TestContext.Current.CancellationToken);
        await new NewItemAddedToStock { Name = "xyz" }.PublishAsync();

        await Assert.That(fakeHandler.Name).IsEqualTo("xyz");
    }
}

file class FakeEventHandler : IEventHandler<NewItemAddedToStock>
{
    public string? Name { get; private set; }

    public Task HandleAsync(NewItemAddedToStock eventModel, CancellationToken ct)
    {
        Name = eventModel.Name;

        return Task.CompletedTask;
    }
}
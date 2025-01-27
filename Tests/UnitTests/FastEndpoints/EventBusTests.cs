using FakeItEasy;
using FastEndpoints;
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

        var evnt = Testory.CreateEvent([fakeHandler]);
        await evnt.PublishAsync(cancellation: TestContext.CancellationToken);
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

        await new EventBus<NewItemAddedToStock>(handlers).PublishAsync(event1, Mode.WaitForNone, TestContext.Current.CancellationToken);
        await new EventBus<NewItemAddedToStock>(handlers).PublishAsync(event2, Mode.WaitForAny, TestContext.Current.CancellationToken);

        await Task.Delay(100, TestContext.Current.CancellationToken);

        event2.ID.ShouldBe(0);
        event2.Name.ShouldBe("pass");

        event1.ID.ShouldBe(0);
        event1.Name.ShouldBe("pass");
    }

    [Test]
    public async Task HandlerLogicThrowsException()
    {
        var logger = A.Fake<ILogger<NotifyCustomers>>();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => new EventBus<NewItemAddedToStock>([new NotifyCustomers(logger)]).PublishAsync(
                new(),
                cancellation: TestContext.Current.CancellationToken));
    }

    [Test]
    public async Task RegisterFakeEventHandlerAndPublish()
    {
        var fakeHandler = new FakeEventHandler();

        Testory.RegisterTestServices(
            s =>
            {
                s.AddSingleton<IEventHandler<NewItemAddedToStock>>(fakeHandler);
            });

        await new NewItemAddedToStock { Name = "xyz" }.PublishAsync(cancellation: TestContext.Current.CancellationToken);

        fakeHandler.Name.ShouldBe("xyz");
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
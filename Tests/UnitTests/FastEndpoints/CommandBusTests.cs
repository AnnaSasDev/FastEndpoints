using FakeItEasy;
using FastEndpoints;
using Microsoft.Extensions.Logging;
using TestCases.CommandBusTest;
using TestCases.CommandHandlerTest;
using Web.Services;

namespace CommandBus;

public class CommandBusTests
{
    [Test]
    public async Task AbilityToFakeTheCommandHandler()
    {
        Factory.RegisterTestServices(_ => { });

        var command = new SomeCommand { FirstName = "a", LastName = "b" };

        var fakeHandler = A.Fake<ICommandHandler<SomeCommand, string>>();
        A.CallTo(() => fakeHandler.ExecuteAsync(A<SomeCommand>.Ignored, A<CancellationToken>.Ignored))
         .Returns(Task.FromResult("Fake Result"));

        fakeHandler.RegisterForTesting();

        var result = await command.ExecuteAsync();

        await Assert.That(result).IsEqualTo("Fake Result");
    }

    [Test]
    public async Task CommandExecutionWorks()
    {
        Factory.RegisterTestServices(_ => { });

        var command = new SomeCommand { FirstName = "a", LastName = "b" };
        var handler = new SomeCommandHandler(A.Fake<ILogger<SomeCommandHandler>>(), A.Fake<IEmailService>());

        var res = await handler.ExecuteAsync(command, default);

        await Assert.That(res).IsEqualTo("a b");
    }

    [Test]
    public async Task CommandHandlerAddsErrors()
    {
        Factory.RegisterTestServices(_ => { });

        var command = new GetFullName { FirstName = "yoda", LastName = "minch" };
        var handler = new MakeFullName(A.Fake<ILogger<MakeFullName>>());

        try
        {
            await handler.ExecuteAsync(command);
        }
        catch (ValidationFailureException x)
        {
            await Assert.That(x.Failures).HasCount().EqualTo(2);
            await Assert.That(x.Failures!.First().PropertyName).IsEqualTo("FirstName");
            await Assert.That(x.Failures!.Last().PropertyName).IsEqualTo("GeneralErrors");
        }

        await Assert.That(handler.ValidationFailures).HasCount().EqualTo(2);
    }

    [Test]
    public async Task CommandHandlerExecsWithoutErrors()
    {
        Factory.RegisterTestServices(_ => { });

        var command = new GetFullName { FirstName = "bobbaa", LastName = "fett" };
        var handler = new MakeFullName(A.Fake<ILogger<MakeFullName>>());

        await handler.ExecuteAsync(command);

        await Assert.That(handler.ValidationFailures).HasCount().EqualTo(0);
    }
}
﻿using TestCases.CommandBusTest;

namespace Messaging;

public class CommandBusTests(Sut App) : TestBase<Sut>
{
    [Test]
    public async Task Generic_Command_With_Result()
    {
        var (rsp, res) = await App.GuestClient.GETAsync<TestCases.CommandHandlerTest.GenericCmdEndpoint, IEnumerable<Guid>>();
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.Count().ShouldBe(3);
        res.First().ShouldBe(Guid.Empty);
    }

    [Test]
    public async Task Generic_Command_Without_Result()
    {
        var (rsp, res) = await App.GuestClient.GETAsync<TestCases.CommandHandlerTest.GenericCmdWithoutResultEndpoint, Guid>();
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.ShouldBe(Guid.Empty);
    }

    [Test]
    public async Task Command_Handler_Sends_Error_Response()
    {
        var res = await App.Client.GETAsync<TestCases.CommandHandlerTest.ConcreteCmdEndpoint, ErrorResponse>();
        res.Response.IsSuccessStatusCode.ShouldBeFalse();
        res.Result.StatusCode.ShouldBe(400);
        res.Result.Errors.Count.ShouldBe(2);
        res.Result.Errors["generalErrors"].Count.ShouldBe(2);
    }

    [Test]
    public async Task Non_Concrete_Void_Command()
    {
        ICommand cmd = new VoidCommand
        {
            FirstName = "johnny",
            LastName = "lawrence"
        };

        var act = async () => cmd.ExecuteAsync(Cancellation);

        await act.ShouldNotThrowAsync();
    }

    [Fact, Trait("ExcludeInCiCd", "Yes")]
    public async Task Command_That_Returns_A_Result_With_TestHandler()
    {
        var (rsp, _) = await App.Client.GETAsync<Endpoint, string>();
        rsp.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        TestCommandHandler.FullName.ShouldBe("x y zseeeee!");
    }

    [Test]
    public async Task Void_Command_With_Test_Handler()
    {
        var (rsp, _) = await App.Client.GETAsync<Endpoint, string>();

        rsp.StatusCode.ShouldBe(System.Net.HttpStatusCode.OK);
        TestVoidCommandHandler.FullName.ShouldBe("x y z");
    }
}

[DontRegister]
public class TestVoidCommandHandler : ICommandHandler<VoidCommand>
{
    public static string FullName = default!;

    public Task ExecuteAsync(VoidCommand command, CancellationToken ct)
    {
        FullName = command.FirstName + " " + command.LastName + " z";

        return Task.CompletedTask;
    }
}

[DontRegister]
public class TestCommandHandler : ICommandHandler<SomeCommand, string>
{
    public static string FullName = default!;

    public Task<string> ExecuteAsync(SomeCommand command, CancellationToken c)
    {
        FullName = command.FirstName + " " + command.LastName + " zseeeee!";

        return Task.FromResult(FullName);
    }
}
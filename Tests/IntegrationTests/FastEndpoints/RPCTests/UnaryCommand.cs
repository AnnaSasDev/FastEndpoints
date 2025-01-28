﻿using TestCases.CommandBusTest;

namespace RemoteProcedureCalls;

public class UnaryCommand(Sut f) : RpcTestBase(f)
{
    [Test]
    public async Task Unary()
    {
        var command = new SomeCommand
        {
            FirstName = "johnny",
            LastName = "lawrence"
        };

        var res = await Remote.ExecuteUnary(command, command.GetType(), default);

        res.ShouldBe("johnny lawrence");
    }

    [Test]
    public async Task Unary_Echo()
    {
        var command = new EchoCommand
        {
            FirstName = "johnny",
            LastName = "lawrence"
        };

        var res = await Remote.ExecuteUnary(command, command.GetType(), default);

        res.ShouldBeEquivalentTo(command);
    }
}
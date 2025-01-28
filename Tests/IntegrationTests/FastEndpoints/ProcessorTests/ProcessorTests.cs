﻿using System.Net;

namespace Processors;

public class ProcessorTests(Sut App) : TestBase<Sut>
{
    [Test]
    public async Task PreProcessorShortCircuitingWhileValidatorFails()
    {
        var x = await App.Client.GETAsync<
                    TestCases.PrecessorShortWhileValidatorFails.Endpoint,
                    TestCases.PrecessorShortWhileValidatorFails.Request,
                    object>(
                    new()
                    {
                        Id = 0
                    });

        x.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        x.Result.ToString().ShouldBe("hello from pre-processor!");
    }

    [Test]
    public async Task PreProcessorsAreRunIfValidationFailuresOccur()
    {
        var (rsp, res) = await App.AdminClient.POSTAsync<
                             TestCases.PreProcessorIsRunOnValidationFailure.Endpoint,
                             TestCases.PreProcessorIsRunOnValidationFailure.Request,
                             ErrorResponse>(
                             new()
                             {
                                 FailureCount = 0,
                                 FirstName = ""
                             });

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        res.Errors.ShouldNotBeNull();
        res.Errors.Count.ShouldBe(2);
        res.Errors["x"].First().ShouldBe("blah");
    }

    [Test]
    public async Task ProcessorAttributes()
    {
        var (rsp, res) =
            await App.Client.POSTAsync<
                TestCases.ProcessorAttributesTest.Endpoint,
                TestCases.ProcessorAttributesTest.Request,
                List<string>>(
                new()
                {
                    Values = ["zero"]
                });
        rsp.IsSuccessStatusCode.ShouldBeTrue();
        res.Count.ShouldBe(5);
        res.ShouldBe(["zero", "one", "two", "three", "four"]);
    }

    [Test]
    public async Task PreProcessorShortCircuitMissingHeader()
    {
        var (rsp, res) = await App.Client.GETAsync<
                             Sales.Orders.Retrieve.Endpoint,
                             Sales.Orders.Retrieve.Request,
                             ErrorResponse>(new() { OrderID = "order1" });

        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        res.Errors.ShouldNotBeNull();
        res.Errors.Count.ShouldBe(1);
        res.Errors.ShouldContainKey("missingHeaders");
    }

    [Test]
    public async Task PreProcessorShortCircuitWrongHeaderValue()
    {
        var (rsp, _) = await App.AdminClient.POSTAsync<
                           Sales.Orders.Retrieve.Endpoint,
                           Sales.Orders.Retrieve.Request,
                           object>(
                           new()
                           {
                               OrderID = "order1"
                           });

        rsp.StatusCode.ShouldBe(HttpStatusCode.MethodNotAllowed);
    }

    [Test]
    public async Task PreProcessorShortCircuitHandlerExecuted()
    {
        var (rsp, res) = await App.CustomerClient.GETAsync<
                             Sales.Orders.Retrieve.Endpoint,
                             Sales.Orders.Retrieve.Request,
                             ErrorResponse>(new() { OrderID = "order1" });

        rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
        res.Message.ShouldBe("ok!");
    }

    [Test]
    public async Task ProcessorStateWorks()
    {
        var x = await App.Client.GETAsync<
                    TestCases.ProcessorStateTest.Endpoint,
                    TestCases.ProcessorStateTest.Request,
                    string>(new() { Id = 10101 });

        x.Response.StatusCode.ShouldBe(HttpStatusCode.OK);
        x.Result.ShouldBe("10101 jane doe True");
    }

    [Test]
    public async Task PostProcessorCanHandleExceptions()
    {
        var x = await App.Client.GETAsync<
                    TestCases.PostProcessorTest.Endpoint,
                    TestCases.PostProcessorTest.Request,
                    TestCases.PostProcessorTest.ExceptionDetailsResponse>(new() { Id = 10101 });

        x.Response.StatusCode.ShouldBe(HttpStatusCode.PreconditionFailed);
        x.Result.Type.ShouldBe(nameof(NotImplementedException));
    }

    [Test]
    public async Task ExceptionIsThrownWhenAPostProcDoesntHandleExceptions()
    {
        var (rsp, res) = await App.Client.GETAsync<TestCases.PostProcessorTest.EpNoPostProcessor, InternalErrorResponse>();
        rsp.IsSuccessStatusCode.ShouldBeFalse();
        res.Code.ShouldBe(500);
        res.Reason.ShouldBe("The method or operation is not implemented.");
    }
}
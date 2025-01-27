using System.Text.Json;
using FakeItEasy;
using FastEndpoints;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TestCases.TypedResultTest;
using Web.Services;

namespace Web;

public class WebTests
{
    [Test]
    public async Task mapper_endpoint_setting_mapper_manually()
    {
        //arrange
        var logger = A.Fake<ILogger<TestCases.MapperTest.Endpoint>>();
        var ep = Factory.Create<TestCases.MapperTest.Endpoint>(logger);
        ep.Map = new();
        var req = new TestCases.MapperTest.Request
        {
            FirstName = "john",
            LastName = "doe",
            Age = 22
        };

        //act
        await ep.HandleAsync(req, CancellationToken.None);

        //assert
        await Assert.That(ep.Response).IsNotNull();
        await Assert.That(ep.Response.Name).IsEqualTo("john doe");
        await Assert.That(ep.Response.Age).IsEqualTo(22);
    }

    [Test]
    public async Task mapper_endpoint_resolves_mapper_automatically()
    {
        //arrange
        var logger = A.Fake<ILogger<TestCases.MapperTest.Endpoint>>();
        var ep = Factory.Create<TestCases.MapperTest.Endpoint>(logger);
        var req = new TestCases.MapperTest.Request
        {
            FirstName = "john",
            LastName = "doe",
            Age = 22
        };

        //act
        await ep.HandleAsync(req, CancellationToken.None);

        //assert
        await Assert.That(ep.Response).IsNotNull();
        await Assert.That(ep.Response.Name).IsEqualTo("john doe");
        await Assert.That(ep.Response.Age).IsEqualTo(22);
    }

    [Test]
    public async Task endpoint_with_mapper_throws_mapper_not_set()
    {
        var logger = A.Fake<ILogger<TestCases.MapperTest.Endpoint>>();
        var ep = Factory.Create<TestCases.MapperTest.Endpoint>(logger);

        ep.Map = null!;
        ep.Definition.MapperType = null;
        
        var req = new TestCases.MapperTest.Request
        {
            FirstName = "john",
            LastName = "doe",
            Age = 22
        };
        
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ep.HandleAsync(req, CancellationToken.None));
        await Assert.That(ex.Message).IsEqualTo("Endpoint mapper is not set!");
    }

    [Test]
    public async Task handle_with_correct_input_without_context_should_set_create_customer_response_correctly()
    {
        var emailer = A.Fake<IEmailService>();
        A.CallTo(() => emailer.SendEmail()).Returns("test email");

        var ep = Factory.Create<Customers.Create.Endpoint>(emailer);

        var req = new Customers.Create.Request
        {
            CreatedBy = "by harry potter"
        };

        await ep.HandleAsync(req, CancellationToken.None);
        
        await Assert.That(ep.Response).IsEqualTo("test email by harry potter");
    }

    [Test]
    public async Task handle_with_correct_input_with_property_di_without_context_should_set_create_customer_response_correctly()
    {
        var emailer = A.Fake<IEmailService>();
        A.CallTo(() => emailer.SendEmail()).Returns("test email");

        var ep = Factory.Create<Customers.CreateWithPropertiesDI.Endpoint>(
            ctx =>
            {
                ctx.AddTestServices(s => s.AddSingleton(emailer));
            });

        var req = new Customers.CreateWithPropertiesDI.Request
        {
            CreatedBy = "by harry potter"
        };

        await ep.HandleAsync(req, CancellationToken.None);
        await Assert.That(ep.Response).IsEqualTo("test email by harry potter");
    }

    [Test]
    public async Task handle_with_correct_input_with_context_should_set_login_admin_response_correctly()
    {
        //arrange
        var fakeConfig = A.Fake<IConfiguration>();
        A.CallTo(() => fakeConfig["TokenKey"]).Returns("00000000000000000000000000000000");

        var ep = Factory.Create<Admin.Login.Endpoint>(
            A.Fake<ILogger<Admin.Login.Endpoint>>(),
            A.Fake<IEmailService>(),
            fakeConfig);

        var req = new Admin.Login.Request
        {
            UserName = "admin",
            Password = "pass"
        };

        //act
        await ep.HandleAsync(req, CancellationToken.None);
        var rsp = ep.Response;

        //assert
        await Assert.That(rsp).IsNotNull();
        await Assert.That(rsp.Permissions).Contains("Inventory_Delete_Item");
        await Assert.That(ep.ValidationFailed).IsFalse();
    }

    [Test]
    public async Task handle_with_bad_input_should_set_admin_login_validation_failed()
    {
        //arrange
        var ep = Factory.Create<Admin.Login.Endpoint>(
            A.Fake<ILogger<Admin.Login.Endpoint>>(),
            A.Fake<IEmailService>(),
            A.Fake<IConfiguration>());

        var req = new Admin.Login.Request
        {
            UserName = "x",
            Password = "y"
        };

        //act
        await ep.HandleAsync(req, CancellationToken.None);

        //assert
        await Assert.That(ep.ValidationFailed).IsTrue();
        await Assert.That(ep.ValidationFailures.Any(f => f.ErrorMessage == "Authentication Failed!")).IsTrue();
    }

    [Test]
    public async Task execute_customer_recent_list_should_return_correct_data()
    {
        var endpoint = Factory.Create<Customers.List.Recent.Endpoint>();
        var res = await endpoint.ExecuteAsync(CancellationToken.None) as Customers.List.Recent.Response;

        await Assert.That(res?.Customers).IsNotNull().And.HasCount().EqualTo(3);
        await Assert.That(res?.Customers?.First().Key).IsEqualTo("ryan gunner");
        await Assert.That(res?.Customers?.Last().Key).IsEqualTo(res?.Customers?.Last().Key);
    }

    [Test]
    public async Task union_type_result_returning_endpoint()
    {
        var ep = Factory.Create<MultiResultEndpoint>();

        var res0 = await ep.ExecuteAsync(new() { Id = 0 }, CancellationToken.None);
        await Assert.That(res0.Result).IsTypeOf<NotFound>();

        var res1 = await ep.ExecuteAsync(new() { Id = 1 }, CancellationToken.None);
        var errors = (res1.Result as ProblemDetails)!.Errors;
        await Assert.That(errors).HasCount().EqualTo(1);
        await Assert.That(errors.First().Name).IsEqualTo(nameof(Request.Id)).Because("value has to be greater than 1");

        var res2 = await ep.ExecuteAsync(new() { Id = 2 }, CancellationToken.None);
        var response = res2.Result as Ok<Response>;
        await Assert.That(response).IsNotNull();
        await Assert.That(response?.Value?.RequestId).IsNotNull().And.IsEqualTo(2);
        await Assert.That(response?.StatusCode).IsNotNull().And.IsEqualTo(200);
    }

    [Test]
    public async Task created_at_success()
    {
        var linkgen = A.Fake<LinkGenerator>();

        var ep = Factory.Create<Inventory.Manage.Create.Endpoint>(
            ctx =>
            {
                ctx.AddTestServices(s => s.AddSingleton(linkgen));
            });

        await ep.HandleAsync(
            new()
            {
                Name = "Grape Juice",
                Description = "description",
                ModifiedBy = "me",
                Price = 100,
                GenerateFullUrl = false
            },
            CancellationToken.None);

        await Assert.That(ep.HttpContext.Response.Headers.ContainsKey("Location")).IsTrue();
        await Assert.That(ep.HttpContext.Response.StatusCode).IsEqualTo(201);
    }

    [Test]
    public async Task processor_state_access_from_unit_test()
    {
        //arrange
        var ep = Factory.Create<TestCases.ProcessorStateTest.Endpoint>();

        var state = ep.ProcessorState<TestCases.ProcessorStateTest.Thingy>();
        state.Id = 101;
        state.Name = "blah";

        //act
        await ep.HandleAsync(new() { Id = 0 }, CancellationToken.None);

        //assert
        // False represents the lack of global state addition from endpoint without global preprocessor
        await Assert.That(ep.Response).IsEqualTo("101 blah False");
        await Assert.That(state.Duration).IsGreaterThan(95);
    }

    [Test]
    public async Task unit_test_concurrency_and_httpContext_isolation()
    {
        await Parallel.ForEachAsync(
            Enumerable.Range(1, 100),
            async (id, _) =>
            {
                var ep = Factory.Create<TestCases.UnitTestConcurrencyTest.Endpoint>(
                    ctx =>
                    {
                        ctx.AddTestServices(s => s.AddSingleton(new TestCases.UnitTestConcurrencyTest.SingltonSVC(id)));
                    });

                var result = await ep.ExecuteAsync(new() { Id = id }, CancellationToken.None);
                await Assert.That(result).IsEqualTo(id);
            });
    }

    [Test]
    public async Task list_element_validation_error()
    {
        var ep = Factory.Create<TestCases.ValidationErrorTest.ListValidationErrorTestEndpoint>();
        await ep.HandleAsync(
            new()
            {
                NumbersList = new()
                {
                    1, 2, 3
                }
            },
            CancellationToken.None);

        await Assert.That(ep.ValidationFailed).IsTrue();
        await Assert.That(ep.ValidationFailures.Count).IsEqualTo(3);
        await Assert.That(ep.ValidationFailures[0].PropertyName).IsEqualTo("NumbersList[0]");
        await Assert.That(ep.ValidationFailures[1].PropertyName).IsEqualTo("NumbersList[1]");
        await Assert.That(ep.ValidationFailures[2].PropertyName).IsEqualTo("NumbersList[2]");
    }

    [Test]
    public async Task dict_element_validation_error()
    {
        var ep = Factory.Create<TestCases.ValidationErrorTest.DictionaryValidationErrorTestEndpoint>();
        await ep.HandleAsync(
            new()
            {
                StringDictionary = new()
                {
                    { "a", "1" },
                    { "b", "2" }
                }
            },
            CancellationToken.None);

        await Assert.That(ep.ValidationFailed).IsTrue();
        await Assert.That(ep.ValidationFailures.Count).IsEqualTo(2);
        await Assert.That(ep.ValidationFailures[0].PropertyName).IsEqualTo("StringDictionary[\"a\"]");
        await Assert.That(ep.ValidationFailures[1].PropertyName).IsEqualTo("StringDictionary[\"b\"]");
    }

    [Test]
    public async Task array_element_validation_error()
    {
        var ep = Factory.Create<TestCases.ValidationErrorTest.ArrayValidationErrorTestEndpoint>();
        await ep.HandleAsync(
            new()
            {
                StringArray = new[]
                {
                    "a",
                    "b"
                }
            },
            CancellationToken.None);

        await Assert.That(ep.ValidationFailed).IsTrue();
        await Assert.That(ep.ValidationFailures.Count).IsEqualTo(2);
        await Assert.That(ep.ValidationFailures[0].PropertyName).IsEqualTo("StringArray[0]");
        await Assert.That(ep.ValidationFailures[1].PropertyName).IsEqualTo("StringArray[1]");
    }

    [Test]
    public async Task array_element_object_property_validation_error()
    {
        var ep = Factory.Create<TestCases.ValidationErrorTest.ObjectArrayValidationErrorTestEndpoint>();
        await ep.HandleAsync(
            new()
            {
                ObjectArray = new[]
                {
                    new TestCases.ValidationErrorTest.TObject { Test = "a" },
                    new TestCases.ValidationErrorTest.TObject { Test = "b" }
                }
            },
            CancellationToken.None);

        await Assert.That(ep.ValidationFailed).IsTrue();
        await Assert.That(ep.ValidationFailures.Count).IsEqualTo(2);
        await Assert.That(ep.ValidationFailures[0].PropertyName).IsEqualTo("ObjectArray[0].Test");
        await Assert.That(ep.ValidationFailures[1].PropertyName).IsEqualTo("ObjectArray[1].Test");
    }

    [Test]
    public async Task list_in_list_validation_error()
    {
        var ep = Factory.Create<TestCases.ValidationErrorTest.ListInListValidationErrorTestEndpoint>();
        await ep.HandleAsync(
            new()
            {
                NumbersList = new()
                {
                    new() { 1, 2 },
                    new() { 3, 4 }
                }
            },
            CancellationToken.None);

        await Assert.That(ep.ValidationFailed).IsTrue();
        await Assert.That(ep.ValidationFailures.Count).IsEqualTo(4);
        await Assert.That(ep.ValidationFailures[0].PropertyName).IsEqualTo("NumbersList[0][0]");
        await Assert.That(ep.ValidationFailures[1].PropertyName).IsEqualTo("NumbersList[0][1]");
        await Assert.That(ep.ValidationFailures[2].PropertyName).IsEqualTo("NumbersList[1][0]");
        await Assert.That(ep.ValidationFailures[3].PropertyName).IsEqualTo("NumbersList[1][1]");
    }

    [Test]
    public async Task problem_details_serialization_test()
    {
        var problemDetails = new ProblemDetails(
            new List<ValidationFailure>
            {
                new("p1", "v1"),
                new("p2", "v2")
            },
            "instance",
            "trace",
            400);

        var json = JsonSerializer.Serialize(problemDetails);
        var res = JsonSerializer.Deserialize<ProblemDetails>(json)!;
        res.Errors = new HashSet<ProblemDetails.Error>(res.Errors);
        await Assert.That(res).IsEquivalentTo(problemDetails);
    }
}
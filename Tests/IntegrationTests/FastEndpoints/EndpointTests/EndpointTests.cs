using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using TestCases.EmptyRequestTest;
using TestCases.Routing;

namespace EndpointTests;

[ClassDataSource<Sut>]
public class EndpointTests(Sut App) : TestBase
{
    [Test]
    public async Task EmptyRequest()
    {
        var endpointUrl = IEndpoint.TestURLFor<EmptyRequestEndpoint>();

        var requestUri = new Uri(
            App.AdminClient.BaseAddress!.ToString().TrimEnd('/') +
            (endpointUrl.StartsWith('/') ? endpointUrl : "/" + endpointUrl));

        var message = new HttpRequestMessage
        {
            Content = new StringContent(string.Empty, Encoding.UTF8, "application/json"),
            Method = HttpMethod.Get,
            RequestUri = requestUri
        };

        var response = await App.AdminClient.SendAsync(message, Cancellation);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task OnBeforeOnAfterValidation()
    {
        var (rsp, res) = await App.AdminClient.POSTAsync<
                             TestCases.OnBeforeAfterValidationTest.Endpoint,
                             TestCases.OnBeforeAfterValidationTest.Request,
                             TestCases.OnBeforeAfterValidationTest.Response>(
                             new()
                             {
                                 Host = "blah",
                                 Verb = Http.DELETE
                             });

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(res.Host).IsEqualTo("localhost");
    }

    [Test]
    public async Task GlobalRoutePrefixOverride()
    {
        using var stringContent = new StringContent("this is the body content");
        stringContent.Headers.ContentType = MediaTypeHeaderValue.Parse("text/plain");

        var rsp = await App.AdminClient.PostAsync("/mobile/api/test-cases/global-prefix-override/12345", stringContent, Cancellation);

        var res = await rsp.Content.ReadFromJsonAsync<TestCases.PlainTextRequestTest.Response>(Cancellation);

        await Assert.That(res).IsNotNull();
        await Assert.That(res!.BodyContent).IsEqualTo("this is the body content");
        await Assert.That(res.Id).IsEqualTo(12345);
    }

    [Test]
    public async Task HydratedTestUrlGeneratorWorksForSupportedVerbs()
    {
        // Arrange
        TestCases.HydratedTestUrlGeneratorTest.Request req = new()
        {
            Id = 123,
            Guid = Guid.Empty,
            String = "string",
            NullableString = "null",
            FromClaim = "fromClaim",
            FromHeader = "fromHeader",
            HasPermission = true
        };

        // Act
        var getResp = await App.AdminClient
                               .GETAsync<TestCases.HydratedTestUrlGeneratorTest.Endpoint, TestCases.HydratedTestUrlGeneratorTest.Request, string>(req);

        var postResp = await App.AdminClient
                                .POSTAsync<TestCases.HydratedTestUrlGeneratorTest.Endpoint, TestCases.HydratedTestUrlGeneratorTest.Request, string>(req);

        var putResp = await App.AdminClient
                               .PUTAsync<TestCases.HydratedTestUrlGeneratorTest.Endpoint, TestCases.HydratedTestUrlGeneratorTest.Request, string>(req);

        var patchResp = await App.AdminClient
                                 .PATCHAsync<TestCases.HydratedTestUrlGeneratorTest.Endpoint, TestCases.HydratedTestUrlGeneratorTest.Request, string>(req);

        var deleteResp = await App.AdminClient
                                  .DELETEAsync<TestCases.HydratedTestUrlGeneratorTest.Endpoint, TestCases.HydratedTestUrlGeneratorTest.Request, string>(req);

        // Assert
        var expectedPath = "/api/test/hydrated-test-url-generator-test/123/00000000-0000-0000-0000-000000000000/string/null/{fromClaim}/{fromHeader}/True";
        await Assert.That(getResp.Result).IsEqualTo(expectedPath);
        await Assert.That(postResp.Result).IsEqualTo(expectedPath);
        await Assert.That(putResp.Result).IsEqualTo(expectedPath);
        await Assert.That(patchResp.Result).IsEqualTo(expectedPath);
        await Assert.That(deleteResp.Result).IsEqualTo(expectedPath);
        }

    [Test]
    public async Task NonOptionalRouteParamThrowsExceptionIfParamIsNull()
    {
        var request = new NonOptionalRouteParamTest.Request(null!);

        var act = async () => await App.Client.POSTAsync<NonOptionalRouteParamTest, NonOptionalRouteParamTest.Request>(request);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(act);
        await Assert.That(ex.Message).IsEqualTo("Route param value missing for required param [{UserId}].");
    }

    [Test]
    public async Task OptionalRouteParamWithNullValueReturnsDefaultValue()
    {
        var request = new OptionalRouteParamTest.Request(null);

        var (rsp, res) = await App.Client.POSTAsync<OptionalRouteParamTest, OptionalRouteParamTest.Request, string>(request);

        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();
        await Assert.That(res).IsEqualTo("default offer");
    }

    [Test]
    public async Task OptionalRouteParamWithValueReturnsSentValue()
    {
        var request = new OptionalRouteParamTest.Request("blah blah!");

        var (rsp, res) = await App.Client.POSTAsync<OptionalRouteParamTest, OptionalRouteParamTest.Request, string>(request);

        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();
        await Assert.That(res).IsEqualTo("blah blah!");
    }
}
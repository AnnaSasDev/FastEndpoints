using System.Net;
using System.Net.Http.Json;
using TestClass = TestCases.AntiforgeryTest;

namespace Int.FastEndpoints.WebTests;

[ClassDataSource<Sut>]
public class AntiforgeryTest(Sut App) : TestBase
{
    [Test]
    public async Task Html_Form_Renders_With_Af_Token()
    {
        var content = await App.GuestClient.GetStringAsync($"{App.GuestClient.BaseAddress}api/{TestClass.Routes.GetFormHtml}", Cancellation);
        await Assert.That(content).Contains("__RequestVerificationToken");
    }

    [Test]
    public async Task Af_Middleware_Blocks_Request_With_Bad_Token()
    {
        var form = new MultipartFormDataContent
        {
            { new StringContent("qweryuiopasdfghjklzxcvbnm"), "__RequestVerificationToken" }
        };

        var rsp = await App.GuestClient.SendAsync(
                      new()
                      {
                          Content = form,
                          RequestUri = new($"{App.GuestClient.BaseAddress}api/{TestClass.Routes.Validate}"),
                          Method = HttpMethod.Post
                      },
                      Cancellation);

        await Assert.That(rsp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        var errResponse = await rsp.Content.ReadFromJsonAsync<ErrorResponse>(Cancellation);
        await Assert.That(errResponse).IsNotNull();
        await Assert.That(errResponse!.Errors)
                    .ContainsKey("generalErrors")
                    .And.HasCount().EqualTo(1);
        await Assert.That(errResponse.Errors["generalErrors"][0]).IsEqualTo("Anti-forgery token is invalid!");
    }

    [Test]
    public async Task Af_Token_Verification_Succeeds()
    {
        var (_, tokenRsp) = await App.GuestClient.GETAsync<TestClass.GetAfTokenEndpoint, TestClass.TokenResponse>();

        var form = new MultipartFormDataContent
        {
            { new StringContent(tokenRsp.Value!), "__RequestVerificationToken" }
        };

        var rsp = await App.GuestClient.SendAsync(
                      new()
                      {
                          Content = form,
                          RequestUri = new($"{App.GuestClient.BaseAddress}api/{TestClass.Routes.Validate}"),
                          Method = HttpMethod.Post
                      },
                      Cancellation);

        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();
        var content = await rsp.Content.ReadAsStringAsync(Cancellation);
        await Assert.That(content).IsNotEmpty().And.Contains("antiforgery success");
    }
}
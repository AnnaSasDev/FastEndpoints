using System.Net;
using System.Net.Http.Json;
using Login = Admin.Login;

namespace Web;

[ClassDataSource<Sut>]
public class AdminTests(Sut App) : TestBase
{
    [Test]
    public async Task AdminLoginWithBadInput()
    {
        var (resp, result) = await App.GuestClient.POSTAsync<Login.Endpoint, Login.Request, ErrorResponse>(
                                 new()
                                 {
                                     UserName = "x",
                                     Password = "y"
                                 });

        resp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        result.Errors.Count.ShouldBe(2);
    }

    [Test]
    public async Task AdminLoginSuccess()
    {
        var (resp, result) = await App.GuestClient.POSTAsync<Login.Endpoint, Login.Request, Login.Response>(
                                 new()
                                 {
                                     UserName = "admin",
                                     Password = "pass"
                                 });

        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.Permissions.Count().ShouldBe(8);
        result.JWTToken.ShouldNotBeNull();
    }

    [Test]
    public async Task AdminLoginInvalidCreds()
    {
        var (rsp, _) = await App.GuestClient.POSTAsync<Login.Endpoint, Login.Request, Login.Response>(
                           new()
                           {
                               UserName = "admin",
                               Password = "xxxxx"
                           });
        rsp.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        rsp.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");

        // read(deserialize) the 400 response to see what's actually wrong
        // or change the response DTO type above to ErrorResponse
        var errRsp = await rsp.Content.ReadFromJsonAsync<ErrorResponse>(Cancellation);
        errRsp!.Errors["generalErrors"][0].ShouldBe("Authentication Failed!");
    }

    [Test]
    public async Task AdminLoginThrottling()
    {
        var client = App.CreateClient();
        client.DefaultRequestHeaders.Add("X-Custom-Throttle-Header", "TEST");

        var successCount = 0;

        for (var i = 1; i <= 6; i++)
        {
            var (rsp, res) = await client.POSTAsync<Login.Endpoint_V1, Login.Request, Login.Response>(
                                 new()
                                 {
                                     UserName = "admin",
                                     Password = "pass"
                                 });

            if (i <= 5)
            {
                rsp.StatusCode.ShouldBe(HttpStatusCode.OK);
                res.JWTToken.ShouldNotBeNullOrEmpty();
                successCount++;
            }
            else
            {
                i.ShouldBe(6);
                rsp.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
            }
        }

        successCount.ShouldBe(5);
    }

    [Test]
    public async Task AdminLoginV2()
    {
        var (resp, result) = await App.GuestClient.GETAsync<Login.Endpoint_V2, EmptyRequest, int>(new());
        resp.StatusCode.ShouldBe(HttpStatusCode.OK);
        result.ShouldBe(2);
    }
}
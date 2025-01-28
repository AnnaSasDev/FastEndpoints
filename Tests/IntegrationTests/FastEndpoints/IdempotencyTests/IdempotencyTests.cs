using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TestCases.Idempotency;

namespace IdempotencyTests;

public class IdempotencyTests(Sut App) : TestBase<Sut>
{
    [Test]
    public async Task Header_Not_Present()
    {
        var url = $"{Endpoint.BaseRoute}/123";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        var res = await App.Client.SendAsync(req, Cancellation);
        await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Multiple_Headers()
    {
        var url = $"{Endpoint.BaseRoute}/123";
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Add("Idempotency-Key", ["1", "2"]);
        var res = await App.Client.SendAsync(req, Cancellation);
        await Assert.That(res.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task MultiPart_Form_Request()
    {
        var idmpKey = Guid.NewGuid().ToString();
        var url = $"{Endpoint.BaseRoute}/321";

        using var fileContent = new ByteArrayContent(
            await new StreamContent(File.OpenRead("test.png"))
                .ReadAsByteArrayAsync(Cancellation));

        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("image/png");

        using var form = new MultipartFormDataContent();
        form.Add(fileContent, "File", "test.png");
        form.Add(new StringContent("500"), "Width");

        var req1 = new HttpRequestMessage(HttpMethod.Get, url);
        req1.Content = form;
        req1.Headers.Add("Idempotency-Key", idmpKey);

        //initial request - uncached response
        var res1 = await App.Client.SendAsync(req1, Cancellation);
        await Assert.That(res1.IsSuccessStatusCode).IsTrue();
        await Assert.That(res1.Headers.Any(h => h.Key == "Idempotency-Key" && h.Value.First() == idmpKey)).IsTrue();

        var rsp1 = await res1.Content.ReadFromJsonAsync<Response>(Cancellation);
        await Assert.That(rsp1).IsNotNull();
        await Assert.That(rsp1!.Id).IsEqualTo("321");

        var ticks = rsp1.Ticks;
        await Assert.That(ticks).IsGreaterThan(0);

        //duplicate request - cached response
        var req2 = new HttpRequestMessage(HttpMethod.Get, url);
        req2.Content = form;
        req2.Headers.Add("Idempotency-Key", idmpKey);

        var res2 = await App.Client.SendAsync(req2, Cancellation);
        await Assert.That(res2.IsSuccessStatusCode).IsTrue();
        var rsp2 = await res2.Content.ReadFromJsonAsync<Response>(Cancellation);
        await Assert.That(rsp2).IsNotNull();
        await Assert.That(rsp2!.Id).IsEqualTo("321");
        await Assert.That(rsp2.Ticks).IsEqualTo(ticks);

        //changed request - uncached response
        var req3 = new HttpRequestMessage(HttpMethod.Get, url);
        form.Add(new StringContent("500"), "Height"); // the change
        req3.Content = form;
        req3.Headers.Add("Idempotency-Key", idmpKey);

        var res3 = await App.Client.SendAsync(req3, Cancellation);
        await Assert.That(res3.IsSuccessStatusCode).IsTrue();

        var rsp3 = await res3.Content.ReadFromJsonAsync<Response>(Cancellation);
        await Assert.That(rsp3).IsNotNull();
        await Assert.That(rsp3!.Id).IsEqualTo("321");
        await Assert.That(rsp3.Ticks).IsGreaterThan(ticks);
    }

    [Test]
    public async Task Json_Body_Request()
    {
        var idmpKey = Guid.NewGuid().ToString();
        var client = App.CreateClient(c => c.DefaultRequestHeaders.Add("Idempotency-Key", idmpKey));
        var req = new Request { Content = "hello" };

        //initial request - uncached response
        var (res1, rsp1) = await client.GETAsync<Endpoint, Request, Response>(req);
        await Assert.That(res1.IsSuccessStatusCode).IsTrue();

        var ticks = rsp1.Ticks;
        await Assert.That(ticks).IsGreaterThan(0);

        //duplicate request - cached response
        var (res2, rsp2) = await client.GETAsync<Endpoint, Request, Response>(req);
        await Assert.That(res2.IsSuccessStatusCode).IsTrue();

        await Assert.That(rsp2.Ticks).IsEqualTo(ticks); 

        //changed request - uncached response
        req.Content = "bye"; //the change
        var (res3, rsp3) = await client.GETAsync<Endpoint, Request, Response>(req);
        await Assert.That(res3.IsSuccessStatusCode).IsTrue();

        await Assert.That(rsp3.Ticks).IsEqualTo(ticks);
    }
}
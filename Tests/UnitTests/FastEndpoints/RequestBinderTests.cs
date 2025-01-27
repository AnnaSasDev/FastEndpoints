using FastEndpoints;
using Microsoft.AspNetCore.Http;

namespace RequestBinder;

public class RequestBinderTests
{
    [Test]
    public async Task CanBindClassDto()
    {
        var hCtx = new DefaultHttpContext();
        var intgr = 123;
        hCtx.Request.RouteValues["Int"] = intgr;
        var guid = new Guid();
        hCtx.Request.RouteValues["Guid"] = guid;
        var ctx = new BinderContext(hCtx, [], null, false);
        var binder = new RequestBinder<RequestClass>();

        var res = await binder.BindAsync(ctx, default);

        await Assert.That(res.Int).IsEqualTo(intgr);
        await Assert.That(res.Guid).IsEqualTo(guid);
        await Assert.That(res.DefaultValueProp).IsEqualTo(345);
        await Assert.That(res.OptionalProp).IsEqualTo(678);
    }

    sealed class RequestClass
    {
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public int Int { get; set; }
        public Guid Guid { get; set; }
        public int DefaultValueProp { get; } = 345;
        public int OptionalProp { get; }

        public RequestClass(int intgr, int optionalProp = 678)
        {
            Int = intgr;
            OptionalProp = optionalProp;
        }
    }
}
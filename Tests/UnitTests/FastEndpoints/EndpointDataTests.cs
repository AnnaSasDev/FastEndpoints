using FastEndpoints;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EPData;

public class EndpointDataTests
{
    [Test]
    public async Task ConfigureIsExecuted()
    {
        var ep = Factory.Create<ConfigureEndpoint>();
        await Assert.That(ep.Definition.Routes).HasSingleItem().And.Contains("configure/test");
    }

    [Test]
    public async Task ItCanFilterTypes()
    {
        const string typename = "Foo";
        var options = new EndpointDiscoveryOptions
        {
            Filter = t => t.Name.Contains(typename, StringComparison.OrdinalIgnoreCase),
            Assemblies = new[] { GetType().Assembly }
        };

        var sut = new EndpointData(options, new());
        var ep = new Foo
        {
            Definition = sut.Found[0]
        };
        sut.Found[0].Initialize(ep, null);

        await Assert.That(sut.Found.Length).IsEqualTo(1);
        await Assert.That(sut.Found[0].Routes).HasSingleItem();
        await Assert.That(sut.Found[0].Routes[0]).IsEqualTo(typename);
    }

    static EndpointDefinition WireUpProcessorEndpoint()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IServiceResolver, ServiceResolver>();
        services.TryAddSingleton<IEndpointFactory, EndpointFactory>();
        var sp = services.BuildServiceProvider();
        Config.ServiceResolver = sp.GetRequiredService<IServiceResolver>();
        var epFactory = sp.GetRequiredService<IEndpointFactory>();
        using var scope = sp.CreateScope();
        var httpCtx = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        var epDef = new EndpointDefinition(typeof(PreProcessorRegistration), typeof(EmptyRequest), typeof(EmptyResponse));
        var baseEp = epFactory.Create(epDef, httpCtx);
        epDef.ImplementsConfigure = true; // Override as there's no EndpointData resolver
        epDef.Initialize(baseEp, httpCtx);

        return epDef;
    }

    [Test]
    public async Task BaselineProcessorOrder()
    {
        var epDef = WireUpProcessorEndpoint();

        // Simulate global definition
        epDef.PreProcessors(Order.Before, new ProcOne(), new ProcTwo());
        epDef.PreProcessors(Order.After, new ProcThree(), new ProcFour());
        epDef.PostProcessors(Order.Before, new PostProcOne(), new PostProcTwo());
        epDef.PostProcessors(Order.After, new PostProcThree(), new PostProcFour());

        await Assert.That(epDef.PreProcessorList).HasCount().EqualTo(5);
        await Assert.That(epDef.PreProcessorList[0]).IsTypeOf<ProcOne>();
        await Assert.That(epDef.PreProcessorList[1]).IsTypeOf<ProcTwo>();
        await Assert.That(epDef.PreProcessorList[2]).IsTypeOf<ProcRequest>();
        await Assert.That(epDef.PreProcessorList[3]).IsTypeOf<ProcThree>();
        await Assert.That(epDef.PreProcessorList[4]).IsTypeOf<ProcFour>();
        
        await Assert.That(epDef.PostProcessorList).HasCount().EqualTo(5);
        await Assert.That(epDef.PostProcessorList[0]).IsTypeOf<PostProcOne>();
        await Assert.That(epDef.PostProcessorList[1]).IsTypeOf<PostProcTwo>();
        await Assert.That(epDef.PostProcessorList[2]).IsTypeOf<PostProcRequest>();
        await Assert.That(epDef.PostProcessorList[3]).IsTypeOf<PostProcThree>();
        await Assert.That(epDef.PostProcessorList[4]).IsTypeOf<PostProcFour>();
    }

    [Test]
    public async Task MultiCallProcessorOrder()
    {
        var epDef = WireUpProcessorEndpoint();

        // Simulate global definition
        epDef.PreProcessors(Order.Before, new ProcOne());
        epDef.PreProcessors(Order.Before, new ProcTwo());
        epDef.PreProcessors(Order.After, new ProcThree());
        epDef.PreProcessors(Order.After, new ProcFour());
        epDef.PostProcessors(Order.Before, new PostProcOne());
        epDef.PostProcessors(Order.Before, new PostProcTwo());
        epDef.PostProcessors(Order.After, new PostProcThree());
        epDef.PostProcessors(Order.After, new PostProcFour());

        await Assert.That(epDef.PreProcessorList).HasCount().EqualTo(5);
        await Assert.That(epDef.PreProcessorList[0]).IsTypeOf<ProcOne>();
        await Assert.That(epDef.PreProcessorList[1]).IsTypeOf<ProcTwo>();
        await Assert.That(epDef.PreProcessorList[2]).IsTypeOf<ProcRequest>();
        await Assert.That(epDef.PreProcessorList[3]).IsTypeOf<ProcThree>();
        await Assert.That(epDef.PreProcessorList[4]).IsTypeOf<ProcFour>();
        
        await Assert.That(epDef.PostProcessorList).HasCount().EqualTo(5);
        await Assert.That(epDef.PostProcessorList[0]).IsTypeOf<PostProcOne>();
        await Assert.That(epDef.PostProcessorList[1]).IsTypeOf<PostProcTwo>();
        await Assert.That(epDef.PostProcessorList[2]).IsTypeOf<PostProcRequest>();
        await Assert.That(epDef.PostProcessorList[3]).IsTypeOf<PostProcThree>();
        await Assert.That(epDef.PostProcessorList[4]).IsTypeOf<PostProcFour>();
    }

    [Test]
    public async Task ServiceResolvedProcessorOrder()
    {
        var epDef = WireUpProcessorEndpoint();

        // Simulate global definition
        epDef.PreProcessor<ProcOne>(Order.Before);
        epDef.PreProcessor<ProcTwo>(Order.Before);
        epDef.PreProcessor<ProcThree>(Order.After);
        epDef.PreProcessor<ProcFour>(Order.After);
        epDef.PostProcessor<PostProcOne>(Order.Before);
        epDef.PostProcessor<PostProcTwo>(Order.Before);
        epDef.PostProcessor<PostProcThree>(Order.After);
        epDef.PostProcessor<PostProcFour>(Order.After);

        await Assert.That(epDef.PreProcessorList).HasCount().EqualTo(5);
        await Assert.That(epDef.PreProcessorList[0]).IsTypeOf<ProcOne>();
        await Assert.That(epDef.PreProcessorList[1]).IsTypeOf<ProcTwo>();
        await Assert.That(epDef.PreProcessorList[2]).IsTypeOf<ProcRequest>();
        await Assert.That(epDef.PreProcessorList[3]).IsTypeOf<ProcThree>();
        await Assert.That(epDef.PreProcessorList[4]).IsTypeOf<ProcFour>();
        
        await Assert.That(epDef.PostProcessorList).HasCount().EqualTo(5);
        await Assert.That(epDef.PostProcessorList[0]).IsTypeOf<PostProcOne>();
        await Assert.That(epDef.PostProcessorList[1]).IsTypeOf<PostProcTwo>();
        await Assert.That(epDef.PostProcessorList[2]).IsTypeOf<PostProcRequest>();
        await Assert.That(epDef.PostProcessorList[3]).IsTypeOf<PostProcThree>();
        await Assert.That(epDef.PostProcessorList[4]).IsTypeOf<PostProcFour>();
    }
}

public class Foo : EndpointWithoutRequest
{
    public override void Configure()
        => Get(nameof(Foo));

    public override async Task HandleAsync(CancellationToken ct)
        => await SendOkAsync(ct);
}

public class Boo : EndpointWithoutRequest
{
    public override void Configure()
        => Get(nameof(Boo));

    public override async Task HandleAsync(CancellationToken ct)
        => await SendOkAsync(ct);
}

public class PreProcessorRegistration : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get(nameof(PreProcessorRegistration));
        PreProcessors(new ProcRequest());
        PostProcessors(new PostProcRequest());
    }
}

public class ProcOne : IGlobalPreProcessor
{
    public async Task PreProcessAsync(IPreProcessorContext context, CancellationToken ct)
        => throw new NotImplementedException();
}

public class PostProcOne : IGlobalPostProcessor
{
    public async Task PostProcessAsync(IPostProcessorContext context, CancellationToken ct)
        => throw new NotImplementedException();
}

public class ProcTwo : ProcOne { }

public class PostProcTwo : PostProcOne { }

public class ProcThree : ProcOne { }

public class PostProcThree : PostProcOne { }

public class ProcFour : ProcOne { }

public class PostProcFour : PostProcOne { }

public class ProcRequest : IPreProcessor<EmptyRequest>
{
    public async Task PreProcessAsync(IPreProcessorContext<EmptyRequest> context, CancellationToken ct)
        => throw new NotImplementedException();
}

public class PostProcRequest : IPostProcessor<EmptyRequest, object?>
{
    public Task PostProcessAsync(IPostProcessorContext<EmptyRequest, object?> context, CancellationToken ct)
        => throw new NotImplementedException();
}

public class ConfigureEndpoint : EndpointWithoutRequest
{
    public override void Configure()
    {
        Get("configure/test");
    }

    public override Task HandleAsync(CancellationToken ct)
        => SendOkAsync(ct);
}
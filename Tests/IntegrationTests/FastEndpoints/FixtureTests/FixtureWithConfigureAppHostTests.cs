namespace FixtureTests;

public class FixtureWithConfigureAppHostTests(FixtureWithConfigureAppHost sut) : TestBase<FixtureWithConfigureAppHost>
{
    [Test]
    public void Captures_Generic_App_Host()
    {
        sut.Host.ShouldNotBeNull();
    }

    [Test]
    public void Propagates_Registered_Dependencies()
    {
        var idFromContainer = sut.Services.GetRequiredService<FixtureId>();
        idFromContainer.Id.ShouldBe(FixtureWithConfigureAppHost.Id);
    }
}
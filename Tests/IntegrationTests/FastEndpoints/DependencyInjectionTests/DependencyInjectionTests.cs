namespace DependencyInjection;

public class DiTests(Sut App) : TestBase<Sut>
{
    [Test]
    public async Task Service_Registration_Generator()
    {
        var (rsp, res) = await App.GuestClient.GETAsync<TestCases.ServiceRegistrationGeneratorTest.Endpoint, string[]>();

        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();
        await Assert.That(res).IsEquivalentTo(["Scoped", "Transient", "Singleton"]);
    }

    [Test]
    public async Task Keyed_Service_Property_Injection()
    {
        var (rsp, res) = await App.GuestClient.GETAsync<TestCases.KeyedServicesTests.Endpoint, string>();

        await Assert.That(rsp.IsSuccessStatusCode).IsTrue();
        await Assert.That(res).IsEquivalentTo("AAA");
    }
}
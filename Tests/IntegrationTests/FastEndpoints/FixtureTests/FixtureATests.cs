using Web.Services;

namespace FixtureTests;

public class FixtureATests(FixtureA sut) : TestBase<FixtureA>
{
    [Test]
    public async Task Mock_Email_Service_Is_Resolved()
    {
        var svc = sut.Services.GetRequiredService<IEmailService>();
        svc.SendEmail().ShouldBe("Email was not sent during testing!");
    }
}
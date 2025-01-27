// ReSharper disable ArrangeAttributes

using FastEndpoints.Swagger;

namespace ParamCreationContext;

public class ParamCreationContextTests
{
    const string ParamName = "param";
    const string OtherParam = "otherParam";

    [Test]
    [Arguments("int", typeof(int))]
    [Arguments("bool", typeof(bool))]
    [Arguments("datetime", typeof(DateTime))]
    [Arguments("decimal", typeof(decimal))]
    [Arguments("double", typeof(double))]
    [Arguments("float", typeof(float))]
    [Arguments("guid", typeof(Guid))]
    [Arguments("long", typeof(long))]
    [Arguments("min", typeof(long))]
    [Arguments("max", typeof(long))]
    [Arguments("range", typeof(long))]
    public async Task ShouldBuildParamMapCorrectly_WhenKnownTypeIsSpecified(string paramType, Type expectedType)
    {
        var operationPath = $"/route/{{{ParamName}:{paramType}}}/";
        var sut = new OperationProcessor.ParamCreationContext(null!, null!, null!, null, operationPath);

        await Assert.That(sut.TypeForRouteParam(ParamName)).IsEqualTo(expectedType);
    }

    [Test]
    public async Task ShouldBuildParamMapCorrectly_WhenNoTypeSpecified()
    {
        const string operationPath = $"/route/{{{ParamName}}}/";
        var sut = new OperationProcessor.ParamCreationContext(null!, null!, null!, null, operationPath);

        await Assert.That(sut.TypeForRouteParam(ParamName)).IsEqualTo(typeof(string));
    }

    [Test]
    public async Task ShouldBuildParamMapCorrectly_WhenUnknownTypeIsSpecified()
    {
        const string operationPath = $"/route/{{{ParamName}:unknownType}}/";
        var sut = new OperationProcessor.ParamCreationContext(null!, null!, null!, null, operationPath);

        await Assert.That(sut.TypeForRouteParam(ParamName)).IsEqualTo(typeof(string));
    }

    [Test]
    [Arguments("int", typeof(int))]
    [Arguments("bool", typeof(bool))]
    [Arguments("datetime", typeof(DateTime))]
    [Arguments("decimal", typeof(decimal))]
    [Arguments("double", typeof(double))]
    [Arguments("float", typeof(float))]
    [Arguments("guid", typeof(Guid))]
    [Arguments("long", typeof(long))]
    [Arguments("min", typeof(long))]
    [Arguments("max", typeof(long))]
    [Arguments("range", typeof(long))]
    public async Task ShouldBuildParamMapCorrectly_WhenMixedParamTypesSpecified(string paramType, Type expectedType)
    {
        var operationPath = $"/route/{{{ParamName}:{paramType}}}/{{{OtherParam}:unknownType}}/";
        var sut = new OperationProcessor.ParamCreationContext(null!, null!, null!, null, operationPath);

        await Assert.That(sut.TypeForRouteParam(ParamName)).IsEqualTo(expectedType);
        await Assert.That(sut.TypeForRouteParam(OtherParam)).IsEqualTo(typeof(string));
    }

    [Test]
    [Arguments("int", typeof(int))]
    [Arguments("bool", typeof(bool))]
    [Arguments("datetime", typeof(DateTime))]
    [Arguments("decimal", typeof(decimal))]
    [Arguments("double", typeof(double))]
    [Arguments("float", typeof(float))]
    [Arguments("guid", typeof(Guid))]
    [Arguments("long", typeof(long))]
    [Arguments("min", typeof(long))]
    [Arguments("max", typeof(long))]
    [Arguments("range", typeof(long))]
    [Arguments("unknownType", typeof(string))]
    public async Task ShouldBuildParamMapCorrectly_WhenTypeIsSpecified_AndGoogleRestApiGuidelineRouteStyle(string paramType, Type expectedType)
    {
        var operationPath = $"/route/{{{ParamName}:{paramType}:min(10)}}:deactivate";
        var sut = new OperationProcessor.ParamCreationContext(null!, null!, null!, null, operationPath);

        await Assert.That(sut.TypeForRouteParam(ParamName)).IsEqualTo(expectedType);
    }

    [Test]
    public async Task ShouldBuildParamMapCorrectly_WhenTypeNotSpecified_AndGoogleRestApiGuidelineRouteStyle()
    {
        const string operationPath = $"/route/{{{ParamName}}}:deactivate";
        var sut = new OperationProcessor.ParamCreationContext(null!, null!, null!, null, operationPath);

        await Assert.That(sut.TypeForRouteParam(ParamName)).IsEqualTo(typeof(string));
    }

    [Test]
    public async Task ShouldBuildParamMapCorrectly_WhenTypeSpecified_AndHasMultipleConstraints_AndGoogleRestApiGuidelineRouteStyle()
    {
        const string operationPath = $"/route/{{{ParamName}:min(5):max(10)}}:deactivate";
        var sut = new OperationProcessor.ParamCreationContext(null!, null!, null!, null, operationPath);

        await Assert.That(sut.TypeForRouteParam(ParamName)).IsEqualTo(typeof(long));
    }

    [Test]
    public async Task Multi_Semi_Colon_Route_Segments()
    {
        const string operationPath = $"api/a:{{{ParamName}}}:{{id2}}/{{{OtherParam}:long}}/";
        var sut = new OperationProcessor.ParamCreationContext(null!, null!, null!, null, operationPath);

        await Assert.That(sut.TypeForRouteParam(ParamName)).IsEqualTo(typeof(string));
        await Assert.That(sut.TypeForRouteParam(OtherParam)).IsEqualTo(typeof(long));
    }
}
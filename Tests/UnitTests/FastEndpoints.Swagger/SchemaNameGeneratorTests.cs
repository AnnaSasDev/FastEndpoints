using FastEndpoints.Swagger;

namespace SchemaNameGen;

public class SchemaNameGeneratorTests
{
    static readonly SchemaNameGenerator _shortNameGenerator = new(shortSchemaNames: true);
    static readonly SchemaNameGenerator _longNameGenerator = new(shortSchemaNames: false);

    [Test]
    public async Task ShortNameNonGeneric()
    {
        var res = _shortNameGenerator.Generate(typeof(Model));
        await Assert.That(res).IsEqualTo("Model");
    }

    [Test]
    public async Task ShortNameGeneric()
    {
        var res = _shortNameGenerator.Generate(typeof(GenericModel<string>));
        await Assert.That(res).IsEqualTo("GenericModelOfString");
    }

    [Test]
    public async Task ShortNameGenericDeep()
    {
        var res = _shortNameGenerator.Generate(typeof(GenericModel<GenericModel<List<Model>>>));
        await Assert.That(res).IsEqualTo("GenericModelOfGenericModelOfListOfModel");
    }

    [Test]
    public async Task ShortNameGenericMulti()
    {
        var res = _shortNameGenerator.Generate(typeof(GenericMultiModel<List<Model>, GenericModel<int>>));
        await Assert.That(res).IsEqualTo("GenericMultiModelOfListOfModelAndGenericModelOfInt32");
    }

    [Test]
    public async Task LongNameNonGeneric()
    {
        var res = _longNameGenerator.Generate(typeof(Model));
        await Assert.That(res).IsEqualTo("SchemaNameGenModel");
    }

    [Test]
    public async Task LongNameGeneric()
    {
        var res = _longNameGenerator.Generate(typeof(GenericModel<string>));
        await Assert.That(res).IsEqualTo("SchemaNameGenGenericModelOfString");
    }

    [Test]
    public async Task LongNameGenericDeep()
    {
        var res = _longNameGenerator.Generate(typeof(GenericModel<List<GenericModel<string>>>));
        await Assert.That(res).IsEqualTo("SchemaNameGenGenericModelOfListOfGenericModelOfString");
    }

    [Test]
    public async Task LongNameGenericDeepMulti()
    {
        var res = _longNameGenerator.Generate(typeof(GenericMultiModel<List<GenericModel<string>>, GenericMultiModel<int, string>>));
        await Assert.That(res).IsEqualTo("SchemaNameGenGenericMultiModelOfListOfGenericModelOfStringAndGenericMultiModelOfInt32AndString");
    }
}

public class Model { }

public class GenericModel<T> { }

public class GenericMultiModel<T1, T2> { }
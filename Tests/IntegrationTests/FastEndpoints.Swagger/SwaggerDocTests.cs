namespace Swagger;

[ClassDataSource<Fixture>]
public class SwaggerDocTests(Fixture App)
{
    //NOTE: the Verify snapshot testing doesn't seem to work in gh workflow for some reason
    //      so we're doing manual json file comparison. matching against verified json files in the project root vs latest generated json.
    //      to update the golden master (verified json files), just set '_updateSnapshots = true' and run the tests.
    //      don't forget to 'false' afterward. because if you don't you're always comparing against newly generated output.

    // ReSharper disable once ConvertToConstant.Local
    private static readonly bool _updateSnapshots = false;
    static readonly CancellationToken _cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10)).Token;

    [Test]
    public async Task release_0_doc()
    {
        var doc = await App.DocGenerator.GenerateAsync("Initial Release");
        var json = doc.ToJson();
        var currentDoc = JToken.Parse(json);

        await UpdateSnapshotIfEnabled("release-0.json", json);

        var snapshot = await File.ReadAllTextAsync("release-0.json", _cancellation);
        var snapshotDoc = JToken.Parse(snapshot);

        await Assert.That(currentDoc).IsEquivalentTo(snapshotDoc);
    }

    [Test]
    public async Task release_1_doc()
    {
        var doc = await App.DocGenerator.GenerateAsync("Release 1.0");
        var json = doc.ToJson();

        var currentDoc = JToken.Parse(json);

        await UpdateSnapshotIfEnabled("release-1.json", json);

        var snapshot = await File.ReadAllTextAsync("release-1.json", _cancellation);
        var snapshotDoc = JToken.Parse(snapshot);

        await Assert.That(currentDoc).IsEquivalentTo(snapshotDoc);
    }

    [Test]
    public async Task release_2_doc()
    {
        var doc = await App.DocGenerator.GenerateAsync("Release 2.0");
        var json = doc.ToJson();

        var currentDoc = JToken.Parse(json);

        await UpdateSnapshotIfEnabled("release-2.json", json);

        var snapshot = await File.ReadAllTextAsync("release-2.json", _cancellation);
        var snapshotDoc = JToken.Parse(snapshot);

        await Assert.That(currentDoc).IsEquivalentTo(snapshotDoc);
    }

    // ReSharper disable once UnusedMember.Local
    static async Task UpdateSnapshotIfEnabled(string jsonFileName, string jsonContent)
    {
        if (_updateSnapshots is false)
            return;

        var destination = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", jsonFileName));

        await File.WriteAllTextAsync(destination, jsonContent);

        throw new OperationCanceledException($"Snapshots updated! Go ahead and comment out the {nameof(UpdateSnapshotIfEnabled)}() methods and re-run the tests!");
    }

    [Test]
    public async Task release_versioning_v0()
    {
        var doc = await App.DocGenerator.GenerateAsync("ReleaseVersioning - v0");
        var json = doc.ToJson();
        var currentDoc = JToken.Parse(json);

        await UpdateSnapshotIfEnabled("release-versioning-v0.json", json);

        var snapshot = await File.ReadAllTextAsync("release-versioning-v0.json", _cancellation);
        var snapshotDoc = JToken.Parse(snapshot);

        await Assert.That(currentDoc).IsEquivalentTo(snapshotDoc);
    }

    [Test]
    public async Task release_versioning_v1()
    {
        var doc = await App.DocGenerator.GenerateAsync("ReleaseVersioning - v1");
        var json = doc.ToJson();
        var currentDoc = JToken.Parse(json);

        await UpdateSnapshotIfEnabled("release-versioning-v1.json", json);

        var snapshot = await File.ReadAllTextAsync("release-versioning-v1.json", _cancellation);
        var snapshotDoc = JToken.Parse(snapshot);

        await Assert.That(currentDoc).IsEquivalentTo(snapshotDoc);
    }

    [Test]
    public async Task release_versioning_v2()
    {
        var doc = await App.DocGenerator.GenerateAsync("ReleaseVersioning - v2");
        var json = doc.ToJson();
        var currentDoc = JToken.Parse(json);

        await UpdateSnapshotIfEnabled("release-versioning-v2.json", json);

        var snapshot = await File.ReadAllTextAsync("release-versioning-v2.json", _cancellation);
        var snapshotDoc = JToken.Parse(snapshot);

        await Assert.That(currentDoc).IsEquivalentTo(snapshotDoc);
    }

    [Test]
    public async Task release_versioning_v3()
    {
        var doc = await App.DocGenerator.GenerateAsync("ReleaseVersioning - v3");
        var json = doc.ToJson();
        var currentDoc = JToken.Parse(json);

        await UpdateSnapshotIfEnabled("release-versioning-v3.json", json);

        var snapshot = await File.ReadAllTextAsync("release-versioning-v3.json", _cancellation);
        var snapshotDoc = JToken.Parse(snapshot);

        await Assert.That(currentDoc).IsEquivalentTo(snapshotDoc);
    }
}
// using Bogus;
// using Xunit;

namespace FastEndpoints.Testing;

/// <summary>
/// abstract class for implementing a test-class, which is a collection of integration tests that may be related to each other.
/// test methods can be run in a given order by decorating the methods with <see cref="PriorityAttribute" />
/// </summary>
// [TestCaseOrderer(typeof(TestCaseOrderer))]
// public abstract class TestBase : IFaker
// {
//     static readonly Faker _faker = new();
//
//     public Faker Fake => _faker;

// #pragma warning disable CA1822
//     public ITestContext Context => TestContext.Current;
//     public CancellationToken Cancellation => TestContext.Current.CancellationToken;
//     public ITestOutputHelper Output
//         => TestContext.Current.TestOutputHelper ?? throw new InvalidOperationException("Test output helper is not available in the current context!");
// #pragma warning restore CA1822

    // ReSharper disable VirtualMemberNeverOverridden.Global

    // /// <summary>
    // /// override this method if you'd like to do some one-time setup for the test-class.
    // /// it is run before any of the test-methods of the class is executed.
    // /// </summary>
    // protected virtual ValueTask SetupAsync()
    //     => ValueTask.CompletedTask;
    //
    // /// <summary>
    // /// override this method if you'd like to do some one-time teardown for the test-class.
    // /// it is run after all test-methods have executed.
    // /// </summary>
    // protected virtual ValueTask TearDownAsync()
    //     => ValueTask.CompletedTask;
    //
    // ValueTask IAsyncLifetime.InitializeAsync()
    //     => SetupAsync();
    //
    // ValueTask IAsyncDisposable.DisposeAsync()
    //     => TearDownAsync();
}
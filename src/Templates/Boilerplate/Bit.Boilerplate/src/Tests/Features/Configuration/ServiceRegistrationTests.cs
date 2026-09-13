using Microsoft.Extensions.Logging.EventSource;

namespace Boilerplate.Tests.Features.Configuration;

[TestClass, TestCategory("IntegrationTest")]
public partial class ServiceRegistrationTests
{
    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// A singleton handed to the container as a ready-made instance (<c>services.AddSingleton(new Xyz())</c>) is never
    /// disposed by it - the container only disposes what it creates itself - so anything that instance owns (sockets,
    /// connection pools, file handles, timers) outlives the host that registered it. A single long-running app hides
    /// that: the leak ends with the process. This test suite does not, because it builds a host per test, and the
    /// abandoned resources of every earlier host are still around while the next ones run. That is exactly how the
    /// <c>NpgsqlDataSource</c> registration used to bring a PostgreSQL run down with "sorry, too many clients already"
    /// halfway through the suite.
    /// So: register such a service through a factory (<c>services.AddSingleton(_ => new Xyz())</c>) instead, which
    /// makes the container the owner and disposal automatic.
    /// </summary>
    [TestMethod]
    public async Task DisposableSingletons_Should_BeCreatedByTheContainer()
    {
        await using var server = new AppTestServer();

        server.Build();

        // The one legitimate exception, registered by AddLogging: LoggingEventSource.Instance is a process-wide static
        // that is meant to outlive every host, so handing it over as an instance is correct - and letting the container
        // dispose it along with one host would break every other host in the process.
        Type[] allowedProcessWideInstances = [typeof(LoggingEventSource)];

        var preCreatedDisposables = server.Services
            .Where(descriptor => descriptor.ImplementationInstance is IDisposable or IAsyncDisposable
                                 && allowedProcessWideInstances.Contains(descriptor.ImplementationInstance.GetType()) is false)
            .Select(descriptor => $"{descriptor.ServiceType.Name} => {descriptor.ImplementationInstance!.GetType().Name}")
            .ToArray();

        Assert.IsEmpty(preCreatedDisposables,
            $"These disposable services are registered as pre-created instances, so nothing ever disposes them: {string.Join(", ", preCreatedDisposables)}.");
    }
}

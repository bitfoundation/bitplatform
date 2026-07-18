using System.Reflection;

namespace Boilerplate.Tests.Features.Configuration;

/// <summary>
/// Unit tests for <c>IConfigurationBuilderExtensions.AddClientConfigurations</c>. They run against the non-browser,
/// non-Blazor-Hybrid ("server/test host") branch of the method - the one that merges the embedded client appsettings
/// beneath whatever sources the builder already carries.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class AppConfigurationBuilderTests
{
    /// <summary>
    /// <c>AddClientConfigurations</c> reflects over the already-loaded <c>Boilerplate.Client.Core</c> assembly via
    /// <c>AppDomain.CurrentDomain.GetAssemblies().Single(...)</c>. In a bare unit test nothing may have forced those
    /// client assemblies to load yet, so eagerly load them here to keep that <c>Single(...)</c> lookup deterministic.
    /// </summary>
    [ClassInitialize]
    public static void EnsureClientAssembliesLoaded(TestContext _)
    {
        Assembly.Load("Boilerplate.Client.Core");
        Assembly.Load("Boilerplate.Client.Web");
    }

    [TestMethod]
    public void AddClientConfigurations_Should_MergeSharedAndClientCoreAppSettings()
    {
        var configuration = new ConfigurationBuilder()
            .AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web")
            .Build();

        // Provided by Boilerplate.Client.Core/appsettings.json.
        Assert.AreEqual("http://localhost:5030/", configuration["ServerAddress"]);
        // Provided by Boilerplate.Shared/appsettings.json - the lowest-priority layer - proving it is merged in too.
        Assert.AreEqual("100000", configuration["MemoryCache:SizeLimit"]);
    }

    [TestMethod]
    public void AddClientConfigurations_Should_ReturnTheSameBuilderForChaining()
    {
        var builder = new ConfigurationBuilder();

        var returned = builder.AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");

        Assert.AreSame(builder, returned);
    }

    [TestMethod]
    public void AddClientConfigurations_Should_KeepPreExistingSourcesAsHighestPriority()
    {
        // A source already present on the builder (e.g. the host's own configuration or environment variables the
        // caller added) must keep winning over the embedded client appsettings the extension appends beneath it.
        var builder = new ConfigurationBuilder();
        builder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ServerAddress"] = "https://overridden.example/"
        });

        var configuration = builder
            .AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web")
            .Build();

        Assert.AreEqual("https://overridden.example/", configuration["ServerAddress"]);
    }
}

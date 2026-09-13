using System.Reflection;

namespace Boilerplate.Tests.Features.Diagnostics;

/// <summary>
/// A logging category in <c>appsettings.json</c> is matched against a record's category with
/// <c>category.StartsWith(rule.CategoryName)</c>, so a key naming a type or namespace that no longer exists matches
/// nothing at all and the provider silently falls back to its section's <c>Default</c>. Nothing reports it: the
/// configuration binds, the app starts, and the only symptom is that the records the entry was written to keep are
/// missing from production telemetry.
/// <para>
/// This has already happened once. The move to the feature-based <c>Infrastructure/</c> layout left eight provider
/// sections pointing at <c>Boilerplate.Client.Core.Services.AuthManager</c>, so the token-refresh log the entries
/// exist for was dropped from Sentry, Application Insights, OpenTelemetry, Console, EventLog and EventSource in
/// Production.
/// </para>
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class LoggingConfigurationTests
{
    [ClassInitialize]
    public static void EnsureAppAssembliesLoaded(TestContext _)
    {
        Assembly.Load("Boilerplate.Shared");
        Assembly.Load("Boilerplate.Client.Core");
        Assembly.Load("Boilerplate.Client.Web");
    }

    [TestMethod]
    public void EveryConfiguredLogCategory_Should_NameATypeOrNamespaceThatStillExists()
    {
        var configuration = new ConfigurationBuilder()
            .AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web")
            .Build();

        var appTypeNames = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith("Boilerplate", StringComparison.Ordinal) is true)
            .SelectMany(assembly => assembly.GetTypes())
            .Select(type => type.FullName)
            .OfType<string>()
            .ToArray();

        // Only the app's own categories are checkable: a framework category such as Microsoft.EntityFrameworkCore
        // names types this assembly does not necessarily load.
        var appCategories = CollectLogCategories(configuration.GetSection("Logging"))
            .Where(category => category.StartsWith("Boilerplate", StringComparison.Ordinal))
            .Distinct()
            .ToArray();

        Assert.IsNotEmpty(appCategories, "No app-owned logging categories were found, so this guard is checking nothing - has the configuration layout moved?");

        var danglingCategories = appCategories
            .Where(category => appTypeNames.Any(typeName => typeName == category || typeName.StartsWith($"{category}.", StringComparison.Ordinal)) is false)
            .ToArray();

        Assert.IsEmpty(danglingCategories,
            $"These logging categories match no type or namespace in the app, so their level never applies and the provider falls back to its Default: {string.Join(", ", danglingCategories)}");
    }

    /// <summary>
    /// Walks the whole <c>Logging</c> section, because a category can sit under the root <c>LogLevel</c> or under any
    /// provider alias's own <c>LogLevel</c> - and the same stale key is typically copied into every one of them.
    /// </summary>
    private static IEnumerable<string> CollectLogCategories(IConfigurationSection loggingSection)
    {
        foreach (var section in loggingSection.GetChildren())
        {
            if (section.Key is "LogLevel")
            {
                foreach (var category in section.GetChildren())
                {
                    yield return category.Key;
                }
            }
            else
            {
                foreach (var category in CollectLogCategories(section))
                {
                    yield return category;
                }
            }
        }
    }
}

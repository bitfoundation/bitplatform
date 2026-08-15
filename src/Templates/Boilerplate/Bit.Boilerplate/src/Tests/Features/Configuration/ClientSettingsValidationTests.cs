using System.Reflection;
using Boilerplate.Client.Core;
using Microsoft.Extensions.Options;

//+:cnd:noEmit
namespace Boilerplate.Tests.Features.Configuration;

/// <summary>
/// <c>AddOptions&lt;T&gt;().ValidateDataAnnotations().ValidateOnStart()</c> is registered for every settings type, but
/// <c>ValidateOnStart</c> is only ever executed by <c>Microsoft.Extensions.Hosting</c>'s <c>IHost</c> - and three of the
/// four client hosts (MAUI, Windows Forms and standalone WebAssembly) build their service provider directly. On those,
/// nothing ran the guards unless the injected instance itself came from the options pipeline. These tests pin both
/// halves of that fix: the injected instance is the validated one, and the validator is present to be called.
/// </summary>
[TestClass, TestCategory("UnitTest")]
public class ClientSettingsValidationTests
{
    /// <summary>
    /// <c>AddClientConfigurations</c> reflects over the already-loaded client assemblies, so force them to load.
    /// </summary>
    [ClassInitialize]
    public static void EnsureClientAssembliesLoaded(TestContext _)
    {
        Assembly.Load("Boilerplate.Client.Core");
        Assembly.Load("Boilerplate.Client.Web");
    }

    [TestMethod]
    public void TheInjectedSettings_Should_BeTheValidatedOptionsInstance()
    {
        using var serviceProvider = BuildClientServiceProvider();

        Assert.AreSame(serviceProvider.GetRequiredService<IOptions<ClientCoreSettings>>().Value,
                       serviceProvider.GetRequiredService<ClientCoreSettings>(),
                       "ClientCoreSettings is injected as a second, hand-bound copy, so every [Required] and every Validate() override is bypassed for the instance the app actually uses.");

        Assert.AreSame(serviceProvider.GetRequiredService<IOptions<SharedSettings>>().Value,
                       serviceProvider.GetRequiredService<SharedSettings>(),
                       "SharedSettings is injected as a second, hand-bound copy, so its validation is bypassed for the instance the app actually uses.");
    }

    /// <summary>
    /// A whitespace <c>ServerAddress</c> is deliberate: it survives <c>IConfigurationExtensions.GetServerAddress</c>'s
    /// <c>Uri.TryCreate(..., RelativeOrAbsolute)</c> check at registration time, so the failure this asserts on can only
    /// come from the settings validation itself.
    /// </summary>
    [TestMethod]
    public void AnInvalidSetting_Should_FailTheHostsThatRunNoStartupValidator()
    {
        using var serviceProvider = BuildClientServiceProvider(new() { ["ServerAddress"] = "   " });

        var startupValidator = serviceProvider.GetService<IStartupValidator>();

        Assert.IsNotNull(startupValidator,
            "No IStartupValidator is registered, so the explicit Validate() call the non-IHost client hosts make would silently do nothing.");

        Assert.ThrowsExactly<OptionsValidationException>(() => startupValidator.Validate(),
            "Settings validation passed for a ClientCoreSettings whose [Required] ServerAddress is whitespace.");

        Assert.ThrowsExactly<OptionsValidationException>(() => serviceProvider.GetRequiredService<ClientCoreSettings>(),
            "Resolving the settings returned an invalid instance instead of failing, which is what happens when the injected copy bypasses the options pipeline.");
    }

    private static ServiceProvider BuildClientServiceProvider(Dictionary<string, string?>? overrides = null)
    {
        var configurationBuilder = new ConfigurationBuilder()
            .AddClientConfigurations(clientEntryAssemblyName: "Boilerplate.Client.Web");

        if (overrides is not null)
        {
            configurationBuilder.AddInMemoryCollection(overrides);
        }

        var services = new ServiceCollection();

        services.AddClientCoreProjectServices(configurationBuilder.Build());

        return services.BuildServiceProvider();
    }
}

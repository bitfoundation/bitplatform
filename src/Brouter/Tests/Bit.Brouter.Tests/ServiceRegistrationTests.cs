using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

[TestClass]
public class ServiceRegistrationTests
{
    [TestMethod]
    public void AddBitBrouterServices_is_idempotent()
    {
        var services = new ServiceCollection();

        services.AddBitBrouterServices();
        services.AddBitBrouterServices();

        var brouter = services.Single(d => d.ServiceType == typeof(IBrouter));
        var service = services.Single(d => d.ServiceType == typeof(BrouterService));

        Assert.AreEqual(ServiceLifetime.Scoped, brouter.Lifetime);
        Assert.AreEqual(ServiceLifetime.Scoped, service.Lifetime);
    }

    [TestMethod]
    public void AddBitBrouterServices_applies_every_configure_callback_in_call_order()
    {
        var services = new ServiceCollection();

        services.AddBitBrouterServices(o => { o.CaseSensitive = true; o.MaxLoaderCacheEntries = 111; });
        services.AddBitBrouterServices(o => o.MaxLoaderCacheEntries = 222);

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<BrouterOptions>>().Value;

        Assert.IsTrue(options.CaseSensitive);            // set only by the first call, not discarded
        Assert.AreEqual(222, options.MaxLoaderCacheEntries); // the later call wins
    }

    [TestMethod]
    public void AddBitBrouterServices_keeps_a_previously_registered_IBrouter()
    {
        var services = new ServiceCollection();
        var custom = new CustomBrouterProbe();
        services.AddScoped<IBrouter>(_ => custom);

        services.AddBitBrouterServices();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.AreSame(custom, scope.ServiceProvider.GetRequiredService<IBrouter>());
    }

    private sealed class CustomBrouterProbe : IBrouter
    {
        public BrouterLocation Location => throw new NotSupportedException();
        public void Navigate(string url, bool replace = false, bool forceLoad = false, string? historyState = null) => throw new NotSupportedException();
        public void Back() => throw new NotSupportedException();
        public void NavigateToName(string name, IReadOnlyDictionary<string, object?>? parameters = null,
                                   string? query = null, bool replace = false, string? historyState = null) => throw new NotSupportedException();
        public string ResolveUrl(string name, IReadOnlyDictionary<string, object?>? parameters = null, string? query = null) => throw new NotSupportedException();
        public event Func<BrouterNavigationContext, ValueTask>? OnNavigating { add { } remove { } }
        public event Func<BrouterNavigationContext, ValueTask>? OnNavigated { add { } remove { } }
        public event Func<BrouterNavigationContext, Exception?, ValueTask>? OnError { add { } remove { } }
    }
}

using Bit.Bdnd.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Bdnd.Tests;

[TestClass]
public class BitBdndTests
{
    [TestMethod]
    public void AddBitBdndServicesRegistersTheInteropAsScoped()
    {
        var services = new ServiceCollection();

        var result = services.AddBitBdndServices();

        Assert.AreSame(services, result);
        var descriptor = Assert.ContainsSingle(services);
        Assert.AreEqual(typeof(BdndInterop), descriptor.ServiceType);
        Assert.AreEqual(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [TestMethod]
    public void AddBitBdndServicesDoesNotRegisterTheManager()
    {
        // every BdndProvider creates its own manager
        var services = new ServiceCollection().AddBitBdndServices();

        Assert.IsFalse(services.Any(d => d.ServiceType == typeof(BdndDragDropManager)));
    }

    [TestMethod]
    public void CallingAddBitBdndServicesTwiceIsHarmless()
    {
        var services = new ServiceCollection();

        services.AddBitBdndServices();
        services.AddBitBdndServices();

        Assert.ContainsSingle(services);
    }

    [TestMethod]
    public void AnInteropRegisteredByTheAppIsKept()
    {
        var services = new ServiceCollection();
        services.AddSingleton(sp => new BdndInterop(sp.GetRequiredService<IJSRuntime>()));

        services.AddBitBdndServices();

        var descriptor = Assert.ContainsSingle(services);
        Assert.AreEqual(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [TestMethod]
    public void AddBitBdndServicesRejectsNull()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() => BitBdnd.AddBitBdndServices(null!));
    }

    [TestMethod]
    public async Task TheInteropResolvesOncePerScope()
    {
        var services = new ServiceCollection().AddSingleton<IJSRuntime>(new FakeJSRuntime()).AddBitBdndServices();
        // BdndInterop is IAsyncDisposable only, so the scopes are disposed asynchronously
        await using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using var scope1 = provider.CreateAsyncScope();
        await using var scope2 = provider.CreateAsyncScope();

        var a = scope1.ServiceProvider.GetRequiredService<BdndInterop>();

        Assert.AreSame(a, scope1.ServiceProvider.GetRequiredService<BdndInterop>());
        Assert.AreNotSame(a, scope2.ServiceProvider.GetRequiredService<BdndInterop>());
    }
}

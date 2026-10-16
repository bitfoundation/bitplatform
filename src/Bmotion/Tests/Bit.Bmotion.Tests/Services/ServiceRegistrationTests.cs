using Bit.Bmotion.Tests.TestInfra;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bmotion.Tests.Services;

/// <summary>
/// Pins what <see cref="BitBmotion.AddBitBmotionServices(IServiceCollection)"/> registers and with
/// which lifetime. The lifetimes are load-bearing: the engine, bridge and registries are per-scope
/// singletons every component shares, while <see cref="BmotionScrollTracker"/> must be transient -
/// it is owned and disposed by its consumer, so a shared instance would be torn down by the first
/// component to unmount.
/// </summary>
[TestClass]
public class ServiceRegistrationTests
{
    private static ServiceLifetime LifetimeOf<T>(IServiceCollection services)
        => services.Single(d => d.ServiceType == typeof(T)).Lifetime;

    [TestMethod]
    public void AddBitBmotionServices_RegistersEveryServiceWithItsLifetime()
    {
        var services = new ServiceCollection();
        services.AddBitBmotionServices();

        Assert.AreEqual(ServiceLifetime.Singleton, LifetimeOf<BitBmotionOptions>(services));
        Assert.AreEqual(ServiceLifetime.Scoped, LifetimeOf<IBmotionInterop>(services));
        Assert.AreEqual(ServiceLifetime.Scoped, LifetimeOf<BmotionAnimationEngine>(services));
        Assert.AreEqual(ServiceLifetime.Scoped, LifetimeOf<BmotionCapabilities>(services));
        Assert.AreEqual(ServiceLifetime.Scoped, LifetimeOf<BmotionLayoutRegistry>(services));
        Assert.AreEqual(ServiceLifetime.Scoped, LifetimeOf<BmotionPrerenderState>(services));
        Assert.AreEqual(ServiceLifetime.Scoped, LifetimeOf<BmotionAnimateService>(services));
        Assert.AreEqual(ServiceLifetime.Scoped, LifetimeOf<BmotionViewTransition>(services));
        Assert.AreEqual(ServiceLifetime.Transient, LifetimeOf<BmotionScrollTracker>(services));
    }

    [TestMethod]
    public void AddBitBmotionServices_UsesTheRealBridge()
    {
        var services = new ServiceCollection();
        services.AddBitBmotionServices();

        Assert.AreEqual(typeof(BmotionInterop), services.Single(d => d.ServiceType == typeof(IBmotionInterop)).ImplementationType);
    }

    [TestMethod]
    public void AddBitBmotionServices_AppliesTheConfigureCallback()
    {
        var services = new ServiceCollection();
        services.AddBitBmotionServices(o =>
        {
            o.ReducedMotion = BmReducedMotionMode.User;
            o.CssSafeMode = BmCssSafeMode.Warn;
        });

        var options = (BitBmotionOptions)services.Single(d => d.ServiceType == typeof(BitBmotionOptions)).ImplementationInstance!;
        Assert.AreEqual(BmReducedMotionMode.User, options.ReducedMotion);
        Assert.AreEqual(BmCssSafeMode.Warn, options.CssSafeMode);
    }

    [TestMethod]
    public void TestContext_MatchesProductionDi_ExceptForTheBridge()
    {
        using var ctx = new BmotionTestContext();

        // Same registrations as an app, the bridge swapped for the fake...
        Assert.AreSame(ctx.Interop, ctx.Services.GetRequiredService<IBmotionInterop>());
        Assert.AreSame(ctx.Options, ctx.Services.GetRequiredService<BitBmotionOptions>());
        // ...and every service resolves (and shares one engine) through it.
        var engine = ctx.Services.GetRequiredService<BmotionAnimationEngine>();
        Assert.AreSame(engine, ctx.Engine);
        Assert.IsNotNull(ctx.Services.GetRequiredService<BmotionAnimateService>());
        Assert.IsNotNull(ctx.Services.GetRequiredService<BmotionViewTransition>());
        Assert.IsNotNull(ctx.Services.GetRequiredService<BmotionLayoutRegistry>());
        Assert.IsNotNull(ctx.Services.GetRequiredService<BmotionPrerenderState>());
        Assert.IsTrue(ctx.Services.GetRequiredService<BmotionCapabilities>().SupportsFrameLoop, "capabilities read the fake bridge");
    }
}

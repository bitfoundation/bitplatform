using System.Collections.Generic;
using Bit.Blex;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// Middleware runs in the order it was registered. Order decides which veto is consulted first and
/// what a logger sees, so silently reordering DI-resolved middleware ahead of instances made the
/// pipeline disagree with the code that configured it.
/// </summary>
[TestClass]
public class MiddlewareOrderTests
{
    private class Named(List<string> log, string name) : IBlexMiddleware
    {
        public void OnAction(BlexActionContext context) => log.Add(name);
    }

    private sealed class First(List<string> log) : Named(log, "first");

    private sealed class Second(List<string> log) : Named(log, "second");

    [TestMethod]
    public void InstanceRegisteredBeforeAType_StillRunsFirst()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddBitBlexServices(options =>
        {
            options.UseMiddleware(new Named(log, "instance"));
            options.UseMiddleware<Second>();
        });
        services.AddBitBlexStore<CounterStore>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<CounterStore>().Increment();

        Assert.AreSequenceEqual(["instance", "second"], log);
    }

    [TestMethod]
    public void TypesAndInstances_Interleave()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddBitBlexServices(options =>
        {
            options.UseMiddleware<First>();
            options.UseMiddleware(new Named(log, "instance"));
            options.UseMiddleware<Second>();
        });
        services.AddBitBlexStore<CounterStore>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<CounterStore>().Increment();

        Assert.AreSequenceEqual(["first", "instance", "second"], log);
    }

    [TestMethod]
    public void AMiddlewareTypeResolvesToOneInstancePerScope()
    {
        var services = new ServiceCollection();
        services.AddSingleton(new List<string>());
        services.AddBitBlexServices(options => options.UseMiddleware<First>());

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        // The IBlexMiddleware registration forwards to the concrete one, so injecting either way
        // inside a scope must not produce two copies with divergent state.
        var byConcrete = scope.ServiceProvider.GetRequiredService<First>();
        var byInterface = Assert.ContainsSingle(scope.ServiceProvider.GetServices<IBlexMiddleware>());
        Assert.AreSame(byConcrete, byInterface);
    }

    [TestMethod]
    public void MiddlewareRegisteredDirectlyInDi_StillParticipates()
    {
        var log = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(log);
        services.AddBitBlexServices(options => options.UseMiddleware(new Named(log, "options")));
        services.AddScoped<IBlexMiddleware>(_ => new Named(log, "container"));
        services.AddBitBlexStore<CounterStore>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<CounterStore>().Increment();

        // Appended after what the options configured, and not duplicated.
        Assert.AreSequenceEqual(["options", "container"], log);
    }

    [TestMethod]
    public void TheFirstVetoWins_AndTheOnesBehindItDoNotRun()
    {
        var seen = new List<string>();
        var services = new ServiceCollection();
        services.AddSingleton(seen);
        services.AddBitBlexServices(options =>
        {
            options.UseFilter(_ => false);
            options.UseMiddleware(new Named(seen, "after-veto"));
        });
        services.AddBitBlexStore<CounterStore>();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<CounterStore>();
        store.Increment();

        Assert.AreEqual(0, store.Count);
        Assert.IsEmpty(seen);
    }
}

using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

public abstract class BunitTestContext : IDisposable
{
    // Nullable on purpose: MSTest constructs the test class before [TestInitialize] runs, so any
    // member access in that window genuinely sees a null Context. Declaring it non-nullable with
    // `default!` would silence the compiler about the very null checks we still need below.
    protected Bunit.BunitContext? Context;

    public BunitServiceProvider Services => Context?.Services
        ?? throw new InvalidOperationException("MSTest has not started executing tests yet");

    [TestInitialize]
    public void Setup()
    {
        Context = new Bunit.BunitContext();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddBitBrouterServices();
    }

    [TestCleanup]
    public void TearDown() => Dispose();

    public void Dispose()
    {
        // bUnit v2's container may hold IAsyncDisposable-only services, which the synchronous
        // Dispose() path cannot tear down (it throws). Route disposal through the async path.
        Context?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Navigates the fake NavigationManager to <paramref name="url"/>, renders the
    /// <typeparamref name="THost"/> routing host at that location and resolves the router it set up.
    /// </summary>
    protected (IRenderedComponent<THost> Cut, IBrouter Brouter) RenderAt<THost>(string url)
        where THost : IComponent
    {
        var nav = Services.GetRequiredService<BunitNavigationManager>();
        nav.NavigateTo(url);
        var cut = RenderComponent<THost>();
        return (cut, Services.GetRequiredService<IBrouter>());
    }

    public IRenderedComponent<TComponent> RenderComponent<TComponent>()
        where TComponent : IComponent
    {
        if (Context is null)
            throw new InvalidOperationException("MSTest has not started executing tests yet");

        return Context.Render<TComponent>();
    }

    public IRenderedComponent<TComponent> RenderComponent<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder)
        where TComponent : IComponent
    {
        if (Context is null)
            throw new InvalidOperationException("MSTest has not started executing tests yet");

        return Context.Render(parameterBuilder);
    }
}

using System;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests;

public abstract class BunitTestContext : IDisposable
{
    protected Bunit.BunitContext Context = default!;

    public BunitRenderer Renderer => Context?.Renderer ?? throw new InvalidOperationException("MSTest has not started executing tests yet");

    public BunitServiceProvider Services => Context?.Services ?? throw new InvalidOperationException("MSTest has not started executing tests yet");

    [TestInitialize]
    public void Setup()
    {
        Context = new Bunit.BunitContext();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestCleanup]
    public void TearDown() => Dispose();

    public void Dispose()
    {
        // bUnit v2's container may hold IAsyncDisposable-only services, which the synchronous
        // Dispose() path cannot tear down (it throws). Route disposal through the async path.
        Context?.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    public IRenderedComponent<TComponent> RenderComponent<TComponent>()
        where TComponent : IComponent
    {
        if (Context == null)
            throw new InvalidOperationException("MSTest has not started executing tests yet");

        return Context.Render<TComponent>();
    }

    public IRenderedComponent<TComponent> RenderComponent<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder)
        where TComponent : IComponent
    {
        if (Context == null)
            throw new InvalidOperationException("MSTest has not started executing tests yet");

        return Context.Render(parameterBuilder);
    }
}

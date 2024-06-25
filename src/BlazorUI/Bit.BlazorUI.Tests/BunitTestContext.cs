using System;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests;

public abstract class BunitTestContext : IDisposable
{
    protected const string GUID_PATTERN = "^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";

    protected Bunit.TestContext Context;

    public ITestRenderer Renderer => Context?.Renderer ?? throw new InvalidOperationException("MSTest has not started executing tests yet");

    public TestServiceProvider Services => Context?.Services ?? throw new InvalidOperationException("MSTest has not started executing tests yet");

    [TestInitialize]
    public void Setup()
    {
        Context = new Bunit.TestContext();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [TestCleanup]
    public void TearDown() => Dispose();

    public void Dispose()
    {
        Context?.Dispose();
        Context = null;
    }

    public IRenderedComponent<TComponent> RenderComponent<TComponent>(params ComponentParameter[] parameters)
        where TComponent : IComponent
    {
        if (Context == null)
            throw new InvalidOperationException("MSTest has not started executing tests yet");

        return Context.RenderComponent<TComponent>(parameters);
    }

    public IRenderedComponent<TComponent> RenderComponent<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder)
        where TComponent : IComponent
    {
        if (Context == null)
            throw new InvalidOperationException("MSTest has not started executing tests yet");

        return Context.RenderComponent(parameterBuilder);
    }
}

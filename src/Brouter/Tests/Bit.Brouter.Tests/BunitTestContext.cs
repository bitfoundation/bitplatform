using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

public abstract class BunitTestContext : IDisposable
{
    // Nullable on purpose: MSTest constructs the test class before [TestInitialize] runs, so any
    // member access in that window genuinely sees a null Context. Declaring it non-nullable with
    // `default!` would silence the compiler about the very null checks we still need below.
    protected Bunit.TestContext? Context;

    public TestServiceProvider Services => Context?.Services
        ?? throw new InvalidOperationException("MSTest has not started executing tests yet");

    [TestInitialize]
    public void Setup()
    {
        Context = new Bunit.TestContext();
        Context.JSInterop.Mode = JSRuntimeMode.Loose;
        Services.AddBitBrouterServices();
    }

    [TestCleanup]
    public void TearDown() => Dispose();

    public void Dispose()
    {
        Context?.Dispose();
    }

    public IRenderedComponent<TComponent> RenderComponent<TComponent>(params ComponentParameter[] parameters)
        where TComponent : IComponent
    {
        if (Context is null)
            throw new InvalidOperationException("MSTest has not started executing tests yet");

        return Context.RenderComponent<TComponent>(parameters);
    }

    public IRenderedComponent<TComponent> RenderComponent<TComponent>(Action<ComponentParameterCollectionBuilder<TComponent>> parameterBuilder)
        where TComponent : IComponent
    {
        if (Context is null)
            throw new InvalidOperationException("MSTest has not started executing tests yet");

        return Context.RenderComponent(parameterBuilder);
    }
}

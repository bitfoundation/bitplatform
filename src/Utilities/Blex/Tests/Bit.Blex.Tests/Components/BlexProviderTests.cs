using Bit.Blex.Blazor;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Blex.Tests.Components;

/// <summary>
/// <see cref="BlexProvider"/> registers the stores and binds the renderer's dispatcher to the manager,
/// so a change from outside it -- a timer, a background task -- is reported, and
/// <see cref="BlexManager.InvokeAsync(Action)"/> can get there instead.
/// </summary>
[TestClass]
public class BlexProviderTests : BunitContext
{
    private readonly List<BlexError> _errors = [];

    private BlexManager Manager => Services.GetRequiredService<BlexManager>();

    public BlexProviderTests()
    {
        Services.AddBitBlexServices(o => o.OnError = _errors.Add);
        Services.AddBitBlexStore<CounterStore>();
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
    }

    private IRenderedComponent<BlexProvider> RenderProvider()
        => Render<BlexProvider>(p => p
            .Add(x => x.EnableDevTools, false)
            .Add(x => x.ChildContent, (RenderFragment)(b => b.AddContent(0, "app"))));

    [TestMethod]
    public void It_binds_the_renderers_dispatcher_and_still_renders_its_content()
    {
        var cut = RenderProvider();

        cut.MarkupMatches("app");
        var bound = Assert.IsInstanceOfType<BlexRendererDispatcher>(Manager.Dispatcher);
        Assert.AreSame(Renderer.Dispatcher, bound.Dispatcher);
        Assert.IsNotNull(Manager.GetStore<CounterStore>(), "the provider registers the stores");
    }

    [TestMethod]
    public async Task A_change_from_a_background_task_is_reported()
    {
        RenderProvider();
        var store = Services.GetRequiredService<CounterStore>();

        await Task.Run(store.Increment);

        var error = Assert.ContainsSingle(_errors);
        Assert.AreEqual("dispatcher", error.Source);
        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public async Task A_change_made_through_InvokeAsync_is_not()
    {
        RenderProvider();
        var store = Services.GetRequiredService<CounterStore>();

        await Task.Run(() => Manager.InvokeAsync(store.Increment));

        Assert.IsEmpty(_errors);
        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public void A_second_provider_keeps_the_first_binding()
    {
        RenderProvider();
        var first = Manager.Dispatcher;

        RenderProvider();

        Assert.AreSame(first, Manager.Dispatcher);
    }
}

using Bunit;
using Microsoft.AspNetCore.Components;

namespace Bit.Bdnd.Tests.Components;

[TestClass]
public class BdndProviderTests : BdndComponentTestBase
{
    /// <summary>Captures the manager a provider cascades.</summary>
    public sealed class ManagerProbe : ComponentBase
    {
        [CascadingParameter] public BdndDragDropManager? Manager { get; set; }
    }

    [TestMethod]
    public void AProviderCreatesAManagerForItsSubtree()
    {
        var cut = Render<BdndProvider>(p => p.AddChildContent<ManagerProbe>());

        Assert.IsNotNull(cut.FindComponent<ManagerProbe>().Instance.Manager);
    }

    [TestMethod]
    public void EveryProviderHasItsOwnManager()
    {
        var cut = Render<BdndProvider>(p => p
            .AddChildContent<ManagerProbe>()
            .AddChildContent<BdndProvider>(inner => inner.AddChildContent<ManagerProbe>()));

        var probes = cut.FindComponents<ManagerProbe>();

        Assert.HasCount(2, probes);
        Assert.AreNotSame(probes[0].Instance.Manager, probes[1].Instance.Manager);
    }

    [TestMethod]
    public void AProviderKeepsItsManagerAcrossRenders()
    {
        var cut = Render<BdndProvider>(p => p.AddChildContent<ManagerProbe>());
        var manager = cut.FindComponent<ManagerProbe>().Instance.Manager;

        cut.Render();
        cut.Render(p => p.AddChildContent<ManagerProbe>());

        Assert.AreSame(manager, cut.FindComponent<ManagerProbe>().Instance.Manager);
    }

    [TestMethod]
    public void AProviderCascadesTheManagerItIsGiven()
    {
        var cut = RenderProvider(p => p.AddChildContent<ManagerProbe>());

        Assert.AreSame(Manager, cut.FindComponent<ManagerProbe>().Instance.Manager);
    }

    [TestMethod]
    public void TwoProvidersCanShareOneManager()
    {
        var cut = Render<BdndProvider>(p => p
            .Add(x => x.Manager, Manager)
            .AddChildContent<BdndProvider>(inner => inner.Add(x => x.Manager, Manager).AddChildContent<ManagerProbe>()));

        Assert.AreSame(Manager, cut.FindComponent<ManagerProbe>().Instance.Manager);
    }

    [TestMethod]
    public void ReRenderingWithTheSameManagerIsAllowed()
    {
        var cut = RenderProvider(p => p.AddChildContent<ManagerProbe>());

        cut.Render(p => p.Add(x => x.Manager, Manager));

        Assert.AreSame(Manager, cut.FindComponent<ManagerProbe>().Instance.Manager);
    }

    [TestMethod]
    public void ReplacingTheManagerThrows()
    {
        var cut = RenderProvider(p => p.AddChildContent<ManagerProbe>());

        var error = Assert.ThrowsExactly<InvalidOperationException>(
            () => cut.Render(p => p.Add(x => x.Manager, new BdndDragDropManager())));

        Assert.AreEqual("The Manager of a BdndProvider cannot be changed after it has been set.", error.Message);
    }

    [TestMethod]
    public void SupplyingAManagerAfterTheProviderCreatedItsOwnThrows()
    {
        var cut = Render<BdndProvider>(p => p.AddChildContent<ManagerProbe>());

        Assert.ThrowsExactly<InvalidOperationException>(
            () => cut.Render(p => p.Add(x => x.Manager, new BdndDragDropManager())));
    }

    // ---- components outside a provider ----

    [TestMethod]
    public void ADraggableOutsideAProviderThrows()
    {
        var error = Assert.ThrowsExactly<InvalidOperationException>(
            () => Render<BdndDraggable<string>>(p => p.Add(x => x.Type, "box")));

        Assert.AreEqual("BdndDraggable must be placed inside a BdndProvider.", error.Message);
    }

    [TestMethod]
    public void ADropTargetOutsideAProviderThrows()
    {
        var error = Assert.ThrowsExactly<InvalidOperationException>(
            () => Render<BdndDropTarget<string>>(p => p.Add(x => x.Accept, "box")));

        Assert.AreEqual("BdndDropTarget must be placed inside a BdndProvider.", error.Message);
    }

    [TestMethod]
    public void ADragLayerOutsideAProviderThrows()
    {
        var error = Assert.ThrowsExactly<InvalidOperationException>(() => Render<BdndDragLayer<string>>());

        Assert.AreEqual("BdndDragLayer must be placed inside a BdndProvider.", error.Message);
    }

    [TestMethod]
    public void WithoutTheServicesTheErrorNamesTheMissingInterop()
    {
        // the README's troubleshooting table quotes this message
        using var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;

        var error = Assert.ThrowsExactly<InvalidOperationException>(() => context.Render<BdndProvider>(p => p
            .AddChildContent<BdndDraggable<string>>(d => d.Add(x => x.Type, "box"))));

        Assert.Contains("Cannot provide a value for property 'Interop'", error.Message, StringComparison.Ordinal);
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.Sortable;

/// <summary>
/// Where the lists take their drag coordinator from. Nothing is registered for them - an app that lazy loads the Extras
/// on WebAssembly could not register anything at startup - so the lists create their scope's coordinator themselves,
/// once per scope, and share it: one per circuit, or per renderer with a scope of its own, never shared between two.
/// </summary>
[TestClass]
public class BitSortableCoordinatorTests : BitSortableTestContext
{
    [TestMethod]
    public async Task TheCoordinatorIsCreatedOncePerScope()
    {
        // one per circuit, or per renderer with a scope of its own, as a scoped registration would give: never shared
        await using var app = new ServiceCollection().BuildServiceProvider();
        await using var first = app.CreateAsyncScope();
        await using var second = app.CreateAsyncScope();

        var coordinator = BitSortableCoordinator.Of(first.ServiceProvider);

        Assert.AreSame(coordinator, BitSortableCoordinator.Of(first.ServiceProvider));
        Assert.AreSame(coordinator, BitSortableCoordinator.Of(first.ServiceProvider.GetRequiredService<IServiceProvider>()));
        Assert.AreNotSame(coordinator, BitSortableCoordinator.Of(second.ServiceProvider));
    }

    [TestMethod]
    public void WithoutBitParamsTheListsKeepTheirParametersOwnDefaults()
    {
        var list = RenderList(["A"]);

        // the parameters' own defaults: nothing set them app-wide
        Assert.AreEqual(150, list.Instance.Animation);
        Assert.IsNull(list.Instance.Strings);
        Assert.IsNull(list.Instance.Classes);
    }

    [TestMethod]
    public async Task ADragBetweenListsWorksWithNothingRegistered()
    {
        List<string> left = ["A", "B"], right = ["X", "Y"];
        var log = new List<Raised<string>>();
        var l1 = RenderList(left, "L1", p => { p.Add(c => c.Group, "g"); Record(p, "L1", log); });
        var l2 = RenderList(right, "L2", p => { p.Add(c => c.Group, "g"); Record(p, "L2", log); });

        await DragStartAsync(l1, "A");
        await DragOverAsync(l2, 1);
        await DropAsync(l2);

        Assert.AreSequenceEqual(["B"], left);
        Assert.AreSequenceEqual(["X", "A", "Y"], right);
        Assert.AreEqual("X,A,Y", Labels(l2));
        Assert.AreEqual("Start:L1 Change:L2 ItemsChanged:L1 ItemsChanged:L2 Remove:L1 Add:L2 Sort:L1 Sort:L2 End:L1", Sequence(log));
        // both lists joined the one coordinator, and registered with the script
        Assert.HasCount(2, Coordinator.Zones);
        Assert.HasCount(2, JSInterop.Invocations[Register]);
    }
}

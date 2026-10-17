using System.Collections.Generic;
using System.Threading.Tasks;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class EffectTests
{
    [TestMethod]
    public async Task Effect_SetsLoadingDuringRun_AndClearsAfter()
    {
        var store = new DataStore();
        Assert.IsFalse(store.LoadIsLoading);

        var task = store.Load("hello");
        Assert.IsTrue(store.LoadIsLoading); // loading set synchronously before first await

        await task;
        Assert.IsFalse(store.LoadIsLoading);
        Assert.AreEqual("hello", store.Value);
        Assert.IsNull(store.LoadError);
    }

    [TestMethod]
    public async Task Effect_CapturesException_IntoError_WithoutThrowing()
    {
        var store = new DataStore { ShouldThrow = true };

        await store.Load("x"); // must not throw

        Assert.IsNotNull(store.LoadError);
        Assert.IsExactInstanceOfType<System.InvalidOperationException>(store.LoadError);
        Assert.IsFalse(store.LoadIsLoading);
    }

    [TestMethod]
    public async Task Effect_ClearsPreviousError_OnRerun()
    {
        var store = new DataStore { ShouldThrow = true };
        await store.Load("x");
        Assert.IsNotNull(store.LoadError);

        store.ShouldThrow = false;
        await store.Load("ok");

        Assert.IsNull(store.LoadError);
        Assert.AreEqual("ok", store.Value);
    }

    [TestMethod]
    public async Task Effect_RecordsExactlyOneAction()
    {
        var seen = new List<string>();
        var manager = new BlexManager(new[] { new BlexDelegateMiddleware(c => seen.Add(c.ActionName)) });
        var store = new DataStore();
        manager.Register(store);

        await store.Load("hello");

        Assert.AreSequenceEqual(["Load"], seen); // loading/error toggles are not recorded
    }

    [TestMethod]
    public async Task Effect_NotifiesOnLoadingTransition()
    {
        var store = new DataStore();
        var raised = 0;
        store.StateChanged += () => raised++;

        await store.Load("hello");

        Assert.IsTrue(raised >= 2); // at least loading=true and loading=false
    }
}

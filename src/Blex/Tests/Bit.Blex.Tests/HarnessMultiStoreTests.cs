using System.Threading.Tasks;
using Bit.Blex;
using Bit.Blex.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// The harness can bring more than one store, so cross-store coordination is testable without
/// hand-wiring a second manager -- the setup the harness exists to remove.
/// </summary>
[TestClass]
public class HarnessMultiStoreTests
{
    [TestMethod]
    public void RegisteredStores_ShareTheHarnessManager()
    {
        using var harness = BlexTestHarness.For<CounterStore>();
        var profile = harness.Register(new ProfileStore());

        harness.Store.Increment();
        profile.SignIn("ada");

        Assert.AreSequenceEqual(["counter/Increment", "profile/SignIn"], harness.Log.QualifiedNames);
        Assert.AreEqual("ada", harness.State["profile"]!["UserName"]!.GetValue<string>());
    }

    [TestMethod]
    public void CrossStoreReactions_WorkThroughTheHarness()
    {
        using var harness = BlexTestHarness.For<CounterStore>();
        var profile = harness.Register(new ProfileStore());

        using var sub = harness.Manager.SubscribeTo<ProfileStore>(_ => harness.Store.Reset());

        harness.Store.Add(7);
        Assert.AreEqual(7, harness.Store.Count);

        profile.SignOut();
        Assert.AreEqual(0, harness.Store.Count);
    }

    [TestMethod]
    public async Task DisposingTheHarness_QuiescesEveryRegisteredStore()
    {
        var failing = new FailingStore();
        Task watch;

        using (var harness = BlexTestHarness.For<CounterStore>())
        {
            harness.Register(failing);
            watch = failing.Watch();
            Assert.IsTrue(failing.WatchIsLoading);
        }

        await watch.WaitAsync(System.TimeSpan.FromSeconds(5));
        Assert.IsFalse(failing.WatchIsLoading);
    }
}

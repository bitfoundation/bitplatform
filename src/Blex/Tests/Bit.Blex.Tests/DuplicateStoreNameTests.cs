using System.Collections.Generic;
using Bit.Blex;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

/// <summary>
/// Store names key the global state tree, the DevTools slices and the persistence storage keys, so
/// a collision silently shadows one store with another. The manager reports it instead of failing
/// silently (or throwing, which would take down app startup).
/// </summary>
[TestClass]
public class DuplicateStoreNameTests
{
    [TestMethod]
    public void RegisteringTwoStoresWithTheSameName_ReportsAnError()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };

        manager.Register(new CounterStore());
        manager.Register(new CounterStore()); // both are named "counter"

        var error = Assert.ContainsSingle(errors);
        Assert.AreEqual("register", error.Source);
        Assert.Contains("counter", error.Detail!);
        Assert.AreEqual(2, manager.Stores.Count); // still registered; the app keeps working
    }

    [TestMethod]
    public void RegisteringTheSameInstanceTwice_IsSilentlyIdempotent()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var store = new CounterStore();

        manager.Register(store);
        manager.Register(store);

        Assert.IsEmpty(errors);
        Assert.ContainsSingle(manager.Stores);
    }

    [TestMethod]
    public void DistinctNames_ReportNothing()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };

        manager.Register(new CounterStore());
        manager.Register(new SettingsStore());

        Assert.IsEmpty(errors);
    }

    [TestMethod]
    public void ReRegisteringAfterUnregister_ReportsNothing()
    {
        var errors = new List<BlexError>();
        var manager = new BlexManager { OnError = errors.Add };
        var first = new CounterStore();

        manager.Register(first);
        manager.Unregister(first);
        manager.Register(new CounterStore());

        Assert.IsEmpty(errors);
    }
}

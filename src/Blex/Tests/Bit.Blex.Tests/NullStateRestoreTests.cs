using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Blex.Tests;

[TestClass]
public class NullStateRestoreTests
{
    [TestMethod]
    public void Restore_SetsReferenceField_BackToNull()
    {
        var store = new ProfileStore();
        store.SignOut(); // UserName = null
        var snapshot = store.SerializeState();

        store.SignIn("alice");
        Assert.AreEqual("alice", store.UserName);

        store.RestoreState(snapshot);
        Assert.IsNull(store.UserName); // a JSON-null property must restore to null
    }

    [TestMethod]
    public void Restore_MissingProperty_KeepsCurrentValue()
    {
        var store = new ProfileStore();
        store.SignIn("alice");

        store.RestoreState(new JsonObject { ["Age"] = 30 });

        Assert.AreEqual("alice", store.UserName); // absent from snapshot -> untouched
        Assert.AreEqual(30, store.Age);
    }

    [TestMethod]
    public void TimeTravel_RoundTripsNullValues_AcrossManager()
    {
        var store = new ProfileStore();
        var manager = new BlexManager();
        manager.Register(store);

        store.SignOut();
        var nullState = manager.CaptureGlobalState();

        store.SignIn("bob");
        manager.RestoreGlobalState(nullState);

        Assert.IsNull(store.UserName);
    }
}

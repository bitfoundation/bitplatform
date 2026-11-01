namespace Boilerplate.WorkbenchTests.Features.Screenshots;

[TestClass, TestCategory("UnitTest")]
public class WorkbenchSnapshotsTests
{
    [TestMethod]
    public void AState_Should_BeFoundUnderTheIdItWasKeptWith()
    {
        var snapshots = new WorkbenchSnapshots();

        var id = snapshots.Add("""{"buildings":{"searchText":"Rijks"}}""");

        Assert.IsNotNull(id);
        Assert.AreEqual("Rijks", snapshots.Find(id)!["buildings"]!["searchText"]!.GetValue<string>());
        Assert.IsNull(snapshots.Find("unknown"));
        Assert.IsNull(snapshots.Find(null));
    }

    [TestMethod]
    [DataRow(null)]
    [DataRow("")]
    [DataRow("not json")]
    [DataRow("[1, 2, 3]")]
    [DataRow("\"a string\"")]
    public void AStateThatIsNotAJsonObject_Should_NotBeKept(string? state)
    {
        Assert.IsNull(new WorkbenchSnapshots().Add(state));
    }

    [TestMethod]
    public void AStateBeyondTheSizeLimit_Should_NotBeKept()
    {
        var huge = $$"""{"a":"{{new string('x', WorkbenchSnapshots.MaxLength)}}"}""";

        Assert.IsNull(new WorkbenchSnapshots().Add(huge));
    }

    [TestMethod]
    public void TheOldestStates_Should_GiveWayWhenTheStoreIsFull()
    {
        var snapshots = new WorkbenchSnapshots();

        var first = snapshots.Add("{}");
        var ids = Enumerable.Range(0, 300).Select(_ => snapshots.Add("{}")).ToArray();

        Assert.IsNull(snapshots.Find(first));
        Assert.IsNotNull(snapshots.Find(ids[^1]));
    }
}

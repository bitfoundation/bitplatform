using System;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.SnackBar;

/// <summary>
/// BitSnackBarService.History: the notifications a service host showed or queued, kept up to HistoryLimit with what
/// became of them, and OnHistoryChange, raised whenever that record changes.
/// </summary>
[TestClass]
public class BitSnackBarServiceHistoryTests : BunitTestContext
{
    private BitSnackBarService RegisterService(int historyLimit = 10)
    {
        var service = new BitSnackBarService { HistoryLimit = historyLimit };
        Services.AddSingleton(service);
        return service;
    }

    private static string[] Titles(BitSnackBarService service) => [.. service.History.Select(i => i.Title)];

    [TestMethod]
    public async Task BitSnackBarServiceShouldKeepNoHistoryUntilItsLimitIsSet()
    {
        var service = RegisterService(historyLimit: 0);
        var changes = 0;
        service.OnHistoryChange += () => changes++;
        RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));

        await service.Info("a");

        Assert.AreEqual(0, service.HistoryLimit);
        Assert.IsEmpty(service.History);
        Assert.AreEqual(0, changes);
    }

    // Everything the host takes in is recorded, however it got there: through the service, through the host's own Show,
    // and the loading item of Track, which becomes its outcome in place rather than a record of its own.
    [TestMethod]
    public async Task BitSnackBarServiceShouldRecordWhatItsHostShowsOldestFirst()
    {
        var service = RegisterService();
        var host = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));
        var before = DateTimeOffset.UtcNow;

        await service.Info("through the service");
        await host.Instance.Show(new BitSnackBarItem { Title = "through the host" });
        await service.Track(Task.CompletedTask, "Saving", "Saved", ex => ex.Message);

        CollectionAssert.AreEqual(new[] { "through the service", "through the host", "Saved" }, Titles(service));
        Assert.IsTrue(service.History.All(i => i.ShownAt >= before && i.ShownAt <= DateTimeOffset.UtcNow));
        Assert.AreEqual(BitColor.Success, service.History[2].Color);
    }

    [TestMethod]
    public async Task BitSnackBarServiceShouldKeepTheLatestUpToItsLimit()
    {
        var service = RegisterService(historyLimit: 2);
        var changes = 0;
        service.OnHistoryChange += () => changes++;
        RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));

        await service.Info("1");
        await service.Info("2");
        await service.Info("3");
        CollectionAssert.AreEqual(new[] { "2", "3" }, Titles(service));

        // Lowering the limit drops the oldest at once.
        changes = 0;
        service.HistoryLimit = 1;
        CollectionAssert.AreEqual(new[] { "3" }, Titles(service));
        Assert.AreEqual(1, changes);

        service.HistoryLimit = -5;
        Assert.AreEqual(0, service.HistoryLimit);
        Assert.IsEmpty(service.History);
    }

    // A record outlives the notification on the screen, and says what took it away; its leaving is news too.
    [TestMethod]
    public async Task BitSnackBarServiceShouldKeepADismissedItemWithItsReason()
    {
        var service = RegisterService();
        var host = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));
        var item = await service.Info("a");
        var changes = 0;
        service.OnHistoryChange += () => changes++;

        await service.Close(item);

        Assert.AreEqual(0, host.Instance.Items.Count);
        Assert.AreSame(item, service.History.Single());
        Assert.AreEqual(BitSnackBarDismissReason.Programmatic, item.DismissReason);
        Assert.AreEqual(1, changes);
    }

    // An item held back for a slot was raised all the same: it is recorded as it is queued, without a time until it
    // arrives, and its arrival is news.
    [TestMethod]
    public async Task BitSnackBarServiceShouldRecordAQueuedItemAndItsArrival()
    {
        var service = RegisterService();
        RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.OverflowBehavior, BitSnackBarOverflowBehavior.Queue);
        });

        var first = await service.Info("first");
        var queued = await service.Info("queued");
        CollectionAssert.AreEqual(new[] { "first", "queued" }, Titles(service));
        Assert.IsNull(queued.ShownAt);

        var changes = 0;
        service.OnHistoryChange += () => changes++;
        await service.Close(first);

        Assert.IsNotNull(queued.ShownAt);
        CollectionAssert.AreEqual(new[] { "first", "queued" }, Titles(service));
        Assert.IsTrue(changes >= 2, "the first one left and the queued one arrived");
    }

    // Nothing that never appeared is recorded: an item the host drops for room, one shown while no host is rendered,
    // and anything an ordinary (not ServiceHost) snack bar shows.
    [TestMethod]
    public async Task BitSnackBarServiceShouldNotRecordWhatNoServiceHostShowed()
    {
        var service = RegisterService();

        await service.Info("no host yet");
        var local = RenderComponent<BitSnackBar>();
        await local.Instance.Show(new BitSnackBarItem { Title = "a local snack bar" });

        RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.OverflowBehavior, BitSnackBarOverflowBehavior.Skip);
        });
        await service.Info("shown");
        await service.Info("skipped");

        CollectionAssert.AreEqual(new[] { "shown" }, Titles(service));
    }

    [TestMethod]
    public async Task BitSnackBarServiceShouldTellOfARepeatAndAnUpdate()
    {
        var service = RegisterService();
        RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.PreventDuplicates, true);
        });
        var item = await service.Error("Save failed");
        var changes = 0;
        service.OnHistoryChange += () => changes++;

        // The repeat folds into the item already shown, which stays one record.
        Assert.AreSame(item, await service.Error("Save failed"));
        Assert.AreEqual(1, changes);
        Assert.AreEqual(1, item.DuplicateCount);
        Assert.AreSame(item, service.History.Single());

        item.Title = "Save failed again";
        await service.Update(item);
        Assert.AreEqual(2, changes);
        Assert.AreEqual("Save failed again", service.History.Single().Title);
    }

    // The loading item of Track can still be waiting for a slot when its task ends: it settles into its outcome in the
    // queue, and its record tells of that before it arrives.
    [TestMethod]
    public async Task BitSnackBarServiceShouldTellOfAQueuedItemSettlingIntoItsOutcome()
    {
        var service = RegisterService();
        var host = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.OverflowBehavior, BitSnackBarOverflowBehavior.Queue);
        });
        await service.Info("in the way");
        var work = new TaskCompletionSource();
        var tracking = service.Track(work.Task, "Saving", "Saved", ex => ex.Message);
        host.WaitForAssertion(() => CollectionAssert.AreEqual(new[] { "in the way", "Saving" }, Titles(service)));
        var changes = 0;
        service.OnHistoryChange += () => changes++;

        work.SetResult();
        await tracking;

        CollectionAssert.AreEqual(new[] { "in the way", "Saved" }, Titles(service));
        Assert.IsNull(service.History[1].ShownAt, "it is still waiting for a slot");
        Assert.AreEqual(1, changes);
    }

    [TestMethod]
    public async Task BitSnackBarServiceShouldMoveAnItemShownAgainToTheEnd()
    {
        var service = RegisterService();
        RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));
        var a = await service.Info("a");
        await service.Info("b");
        await service.Close(a);

        await service.Show(a);

        CollectionAssert.AreEqual(new[] { "b", "a" }, Titles(service));
        Assert.IsNull(a.DismissReason, "it is on the screen again");
    }

    [TestMethod]
    public async Task BitSnackBarServiceShouldForgetItsHistoryButNotTheScreen()
    {
        var service = RegisterService();
        var host = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));
        await service.Info("a");
        var changes = 0;
        service.OnHistoryChange += () => changes++;

        service.ClearHistory();
        service.ClearHistory();

        Assert.IsEmpty(service.History);
        Assert.AreEqual(1, changes, "clearing an empty history is no change");
        Assert.AreEqual(1, host.Instance.Items.Count);

        // A record made after the clear is the only one.
        await service.Info("b");
        CollectionAssert.AreEqual(new[] { "b" }, Titles(service));
    }

    [TestMethod]
    public async Task BitSnackBarServiceHistoryShouldBeASnapshot()
    {
        var service = RegisterService();
        RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));
        await service.Info("a");

        var snapshot = service.History;
        await service.Info("b");

        Assert.HasCount(1, snapshot);
        Assert.HasCount(2, service.History);
    }

    [TestMethod]
    public void BitSnackBarItemShouldHaveNoShownTimeUntilShown()
    {
        Assert.IsNull(new BitSnackBarItem { Title = "a" }.ShownAt);
    }
}

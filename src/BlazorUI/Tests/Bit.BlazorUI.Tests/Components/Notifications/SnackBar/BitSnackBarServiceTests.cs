using System;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Notifications.SnackBar;

[TestClass]
public class BitSnackBarServiceTests : BunitTestContext
{
    [TestMethod]
    public void BitSnackBarServiceIsRegisteredByTheServicesTest()
    {
        var services = new ServiceCollection();

        services.AddBitBlazorUIServices();

        Assert.IsNotNull(services.BuildServiceProvider().GetService<BitSnackBarService>());
    }

    [TestMethod]
    public async Task BitSnackBarServiceShowsThroughTheServiceHostTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));

        Assert.IsTrue(service.IsHostAvailable);
        Assert.AreSame(com.Instance, service.Host);

        var item = await service.Success("Saved", "All changes are saved.");

        Assert.AreSame(item, com.Instance.Items[0]);
        Assert.AreEqual(BitColor.Success, item.Color);
        Assert.AreEqual("Saved", com.Find(".bit-snb-ttl").TextContent.Trim());
    }

    [TestMethod]
    public async Task BitSnackBarServiceIgnoresAnUnmarkedHostTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var com = RenderComponent<BitSnackBar>();

        Assert.IsFalse(service.IsHostAvailable);

        // Without a host the item comes back unshown instead of failing the code that only wanted to report something.
        var item = await service.Info("title");

        Assert.IsNotNull(item);
        Assert.AreEqual(0, com.Instance.Items.Count);
    }

    [TestMethod]
    public void BitSnackBarServiceHostWorksWithoutTheServicesTest()
    {
        // An app that never registered the services loses this one opt-in feature, not the snack bar.
        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));

        Assert.AreEqual(1, com.FindAll(".bit-snb").Count);
    }

    [TestMethod]
    public void BitSnackBarServiceHandsOverToTheEarlierHostTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var first = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));
        var second = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));

        Assert.AreSame(second.Instance, service.Host);

        second.Render(parameters => parameters.Add(p => p.ServiceHost, false));

        Assert.AreSame(first.Instance, service.Host);
    }

    [TestMethod]
    public async Task BitSnackBarServiceForgetsADisposedHostTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));

        await ((System.IAsyncDisposable)com.Instance).DisposeAsync();

        Assert.IsFalse(service.IsHostAvailable);
    }

    [TestMethod]
    public async Task BitSnackBarServiceClosesAndUpdatesThroughTheOwnerTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var owner = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await service.Info("Uploading...");

        // A host that takes over later does not own what the earlier one is showing.
        RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));

        item.Title = "Uploaded";
        await service.Update(item);

        Assert.AreEqual("Uploaded", owner.Find(".bit-snb-ttl").TextContent.Trim());

        await service.Close(item);

        Assert.AreEqual(0, owner.Instance.Items.Count);
        Assert.AreEqual(BitSnackBarDismissReason.Programmatic, item.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarServiceClosesFromAnotherThreadTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await service.Info("title");

        // A timer or a hub callback is off the renderer's context; the owner is found from the item itself rather
        // than by reading the host's lists from there.
        await Task.Run(() => service.Close(item));

        Assert.AreEqual(0, com.Instance.Items.Count);
        Assert.AreEqual(BitSnackBarDismissReason.Programmatic, item.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarServiceLeavesAnItemThatHasLeftAloneTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        var item = await service.Info("title");

        await com.Instance.Close(item);

        var dismissed = 0;
        item.OnDismiss = _ => { dismissed++; return Task.CompletedTask; };

        await service.Close(item);
        await service.Update(item);

        Assert.AreEqual(0, dismissed);
        Assert.AreEqual(0, com.Instance.Items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarServiceClosesAQueuedItemThroughItsHostTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.MaxItems, 1);
            parameters.Add(p => p.OverflowBehavior, BitSnackBarOverflowBehavior.Queue);
        });

        await service.Info("first");
        var queued = await service.Info("second");

        Assert.AreEqual(1, com.Instance.PendingItems.Count);

        await service.Close(queued);

        Assert.AreEqual(0, com.Instance.PendingItems.Count);
        Assert.AreEqual(BitSnackBarDismissReason.Programmatic, queued.DismissReason);
    }

    [TestMethod]
    public async Task BitSnackBarServiceClearsTheCurrentHostTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var com = RenderComponent<BitSnackBar>(parameters =>
        {
            parameters.Add(p => p.ServiceHost, true);
            parameters.Add(p => p.TransitionDuration, 0);
        });

        await service.Info("first");
        await service.Error("second");

        await service.Clear();

        Assert.AreEqual(0, com.Instance.Items.Count);
    }

    [TestMethod]
    public async Task BitSnackBarServiceTracksThroughTheHostTest()
    {
        var service = new BitSnackBarService();
        Services.AddSingleton(service);

        var com = RenderComponent<BitSnackBar>(parameters => parameters.Add(p => p.ServiceHost, true));

        var result = await com.InvokeAsync(() => service.Track(Task.FromResult(42), "Loading", n => $"Got {n}", ex => "Failed"));

        Assert.AreEqual(42, result);
        Assert.AreEqual("Got 42", com.Instance.Items[0].Title);
        Assert.AreEqual(BitColor.Success, com.Instance.Items[0].Color);
    }

    [TestMethod]
    public async Task BitSnackBarServiceTrackWithoutAHostStillAwaitsTheTaskTest()
    {
        var service = new BitSnackBarService();

        // Nothing is shown, but the code that tracked its work still gets its result and its failures back.
        Assert.AreEqual(7, await service.Track(Task.FromResult(7), "Loading", n => "Done", ex => "Failed"));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.Track(Task.FromException(new InvalidOperationException()), "Loading", "Done", ex => "Failed"));
    }
}

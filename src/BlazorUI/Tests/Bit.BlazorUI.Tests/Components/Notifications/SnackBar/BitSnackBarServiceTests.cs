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
}

using Bunit;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.Bortable.Tests;

/// <summary>
/// App-wide defaults set where the services are registered: they replace a parameter's default for every list that
/// does not set the parameter itself, and leave a list's own value alone.
/// </summary>
[TestClass]
public class OptionsTests
{
    private static BunitContext Context(params Action<BortableOptions>[] configure)
    {
        var context = new BunitContext();
        context.JSInterop.Mode = JSRuntimeMode.Loose;
        foreach (var c in configure) context.Services.AddBitBortableServices(c);
        if (configure.Length == 0) context.Services.AddBitBortableServices();
        return context;
    }

    [TestMethod]
    public void TheDefaultsApplyToEveryListThatDoesNotSetTheParameter()
    {
        var german = new BortableMessages { RoleDescription = "sortierbares Element" };
        using var context = Context(o =>
        {
            o.Animation = 300;
            o.TouchDelay = 500;
            o.GhostClass = "my-ghost";
            o.Messages = german;
        });

        var list = context.Render<Bortable<string>>(p => p.Add(c => c.Items, new List<string> { "A" }));

        Assert.AreEqual(300, list.Instance.Animation);
        Assert.AreEqual(500, list.Instance.TouchDelay);
        Assert.AreEqual("my-ghost", list.Instance.GhostClass);
        Assert.AreSame(german, list.Instance.Messages);
        Assert.AreEqual("sortierbares Element", list.Find("[data-bortable-item]").GetAttribute("aria-roledescription"));
        // What is not set keeps the parameter's own default.
        Assert.AreEqual(1d, list.Instance.SwapThreshold);
    }

    [TestMethod]
    public void AListThatSetsTheParameterKeepsItsOwnValue()
    {
        using var context = Context(o => o.Animation = 300);

        var list = context.Render<Bortable<string>>(p => p
            .Add(c => c.Items, new List<string> { "A" })
            .Add(c => c.Animation, 0));
        list.Render(p => p.Add(c => c.Class, "again"));

        Assert.AreEqual(0, list.Instance.Animation);
    }

    [TestMethod]
    public void SeveralCallsAddUpInOrder()
    {
        using var context = Context(o => { o.Animation = 300; o.Delay = 50; }, o => o.Animation = 120);

        var list = context.Render<Bortable<string>>(p => p.Add(c => c.Items, new List<string> { "A" }));

        Assert.AreEqual(120, list.Instance.Animation);
        Assert.AreEqual(50, list.Instance.Delay);
    }

    [TestMethod]
    public void WithoutOptionsEveryParameterKeepsItsDefault()
    {
        using var context = Context();

        var list = context.Render<Bortable<string>>(p => p.Add(c => c.Items, new List<string> { "A" }));

        Assert.AreEqual(150, list.Instance.Animation);
        Assert.AreEqual(200, list.Instance.TouchDelay);
        Assert.IsTrue(list.Instance.Keyboard);
        Assert.IsNull(list.Instance.Messages);
    }

    [TestMethod]
    public void ConfiguringTheOptionsStillRegistersOneCoordinator()
    {
        var services = new ServiceCollection();

        services.AddBitBortableServices(o => o.Animation = 1).AddBitBortableServices(o => o.Delay = 2).AddBitBortableServices();

        Assert.ContainsSingle(services.Where(d => d.ServiceType == typeof(BortableService)));
        Assert.ThrowsExactly<ArgumentNullException>(() => services.AddBitBortableServices(null!));
    }
}

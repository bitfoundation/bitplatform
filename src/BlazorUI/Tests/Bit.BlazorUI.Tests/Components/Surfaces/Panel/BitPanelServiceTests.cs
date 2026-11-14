using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Surfaces.Panel;

/// <summary>
/// The BitPanelService: a panel shown from code is a real BitPanel, rendered by the BitPanelContainer with the parameters
/// it was shown with merged over the container's; it is closed from code with a result, or dismissed by the user and put
/// to its close guard first; its content reaches its reference; a bottom sheet shown from code keeps the snap point the
/// user moves it to; and the service is registered with the others.
/// </summary>
[TestClass]
public class BitPanelServiceTests : BunitTestContext
{
    private BitPanelService PanelService => Services.GetRequiredService<BitPanelService>();

    [TestInitialize]
    public void SetupServices()
    {
        Services.AddSingleton<BitPanelService>();
    }

    /// <summary>Content that reads its panel's reference off the cascade and can close it with a result.</summary>
    public class TestPanelContent : ComponentBase
    {
        [CascadingParameter] public BitPanelReference? PanelReference { get; set; }

        [Parameter] public string? Message { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenElement(0, "p");
            builder.AddAttribute(1, "class", "test-panel-content");
            builder.AddContent(2, Message);
            builder.CloseElement();

            builder.OpenElement(3, "button");
            builder.AddAttribute(4, "class", "test-panel-done");
            builder.AddAttribute(5, "onclick", EventCallback.Factory.Create(this, () => PanelReference!.CloseWith("done")));
            builder.CloseElement();
        }
    }

    private static readonly TimeSpan _exitWait = TimeSpan.FromSeconds(3);

    // A result that never comes fails the test rather than hanging the run.
    private static readonly TimeSpan _resultWait = TimeSpan.FromSeconds(5);

    [TestMethod]
    public async Task APanelShownFromCodeShouldBeABitPanelInTheContainer()
    {
        var container = RenderComponent<BitPanelContainer>();

        await PanelService.Show<TestPanelContent>(new Dictionary<string, object> { { nameof(TestPanelContent.Message), "Hello panel" } },
                                                  new BitPanelParameters { HeaderText = "Details", Placement = BitPlacement.Start, ShowCloseButton = true });

        container.WaitForAssertion(() =>
        {
            Assert.AreEqual(1, container.FindComponents<BitPanel>().Count);
            var panelContainer = container.Find(".bit-pnl-cnt");
            Assert.IsTrue(panelContainer.ClassList.Contains("bit-pnl-opn"));
            Assert.IsTrue(panelContainer.ClassList.Contains("bit-pnl-start"));
            Assert.AreEqual("Details", container.Find(".bit-pnl-hdr").TextContent.Trim());
            Assert.AreEqual("Hello panel", container.Find(".test-panel-content").TextContent);
            Assert.AreEqual(1, container.FindAll(".bit-pnl-cls").Count);
        });
    }

    [TestMethod]
    public async Task APanelClosedFromCodeShouldCompleteItsResultAndLeaveThePage()
    {
        var container = RenderComponent<BitPanelContainer>();

        var panelRef = await PanelService.Show<TestPanelContent>();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-pnl-opn").Count));

        await panelRef.CloseWith(42);

        Assert.AreEqual(42, await panelRef.Result.WaitAsync(_resultWait));
        Assert.IsFalse(panelRef.IsDismissed);
        Assert.AreEqual(0, PanelService.OpenModals.Count);

        // Closed at once, and kept in the page until it has slid out - or, here with no browser to slide it, until the
        // container's deadline.
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-pnl-opn").Count));
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindComponents<BitPanel>().Count), _exitWait);
    }

    [TestMethod]
    public async Task APanelThatHasSlidOutShouldLeaveThePageAtOnce()
    {
        var container = RenderComponent<BitPanelContainer>();

        var panelRef = await PanelService.Show<TestPanelContent>();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-pnl-opn").Count));
        await panelRef.Close();
        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindAll(".bit-pnl-opn").Count));

        // The transition end the browser reports once the panel is out of view.
        var panel = container.FindComponent<BitPanel>();
        await panel.InvokeAsync(() => panel.Instance._OnTransitionEnd());

        container.WaitForAssertion(() => Assert.AreEqual(0, container.FindComponents<BitPanel>().Count), TimeSpan.FromMilliseconds(500));
    }

    [TestMethod]
    public async Task TheContentShouldReachItsReferenceAndCloseThePanel()
    {
        var container = RenderComponent<BitPanelContainer>();

        var panelRef = await PanelService.Show<TestPanelContent>();
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".test-panel-done").Count));

        container.Find(".test-panel-done").Click();

        Assert.AreEqual("done", await panelRef.Result.WaitAsync(_resultWait));
        Assert.AreEqual("done", await panelRef.GetResult<string>().WaitAsync(_resultWait));
    }

    [TestMethod]
    public async Task ThePanelsCloseButtonShouldDismissIt()
    {
        var container = RenderComponent<BitPanelContainer>();

        var panelRef = await PanelService.Show<TestPanelContent>(new Dictionary<string, object>(), new BitPanelParameters { ShowCloseButton = true });
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-pnl-cls").Count));

        container.Find(".bit-pnl-cls").Click();

        Assert.IsNull(await panelRef.Result.WaitAsync(_resultWait));
        Assert.IsTrue(panelRef.IsDismissed);
        Assert.IsTrue(panelRef.IsClosed);
    }

    [TestMethod]
    public async Task ACloseGuardShouldKeepAPanelOpenUntilItAgrees()
    {
        var container = RenderComponent<BitPanelContainer>();
        var agree = false;
        var asked = 0;

        var panelRef = await PanelService.Show<TestPanelContent>(new Dictionary<string, object>(), new BitPanelParameters
        {
            ShowCloseButton = true,
            CanClose = () => { asked++; return Task.FromResult(agree); },
        });
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-pnl-cls").Count));

        container.Find(".bit-pnl-cls").Click();
        Assert.AreEqual(1, asked);
        Assert.IsFalse(panelRef.IsClosed);
        Assert.AreEqual(1, container.FindAll(".bit-pnl-opn").Count, "a refused dismissal leaves the panel open");

        Assert.IsFalse(await panelRef.TryClose());
        Assert.IsFalse(panelRef.IsClosed);

        agree = true;
        container.Find(".bit-pnl-cls").Click();
        Assert.IsTrue(panelRef.IsDismissed);
        Assert.AreEqual(3, asked, "the guard is asked once per dismissal, not a second time by the service");
    }

    [TestMethod]
    public async Task APanelsOwnParametersShouldWinOverTheContainers()
    {
        var container = RenderComponent<BitPanelContainer>(parameters =>
        {
            parameters.Add(p => p.ModalParameters, new BitPanelParameters
            {
                Placement = BitPlacement.Start,
                HeaderText = "Default",
                Classes = new() { Container = "from-container", Body = "body-from-container" },
            });
        });

        await PanelService.Show<TestPanelContent>(new Dictionary<string, object>(), new BitPanelParameters
        {
            HeaderText = "Own",
            Classes = new() { Container = "own" },
        });

        container.WaitForAssertion(() =>
        {
            var panelContainer = container.Find(".bit-pnl-cnt");
            Assert.IsTrue(panelContainer.ClassList.Contains("bit-pnl-start"), "the container's placement fills in");
            Assert.IsTrue(panelContainer.ClassList.Contains("own"));
            Assert.IsFalse(panelContainer.ClassList.Contains("from-container"));
            Assert.IsTrue(container.Find(".bit-pnl-bdy").ClassList.Contains("body-from-container"), "the parts merge one by one");
            Assert.AreEqual("Own", container.Find(".bit-pnl-hdr").TextContent.Trim());
        });
    }

    [TestMethod]
    public async Task AnUpdateShouldReRenderThePanelWithItsNewParameters()
    {
        var container = RenderComponent<BitPanelContainer>();

        var panelRef = await PanelService.Show<TestPanelContent>(new Dictionary<string, object>(), new BitPanelParameters { HeaderText = "Before" });
        container.WaitForAssertion(() => Assert.AreEqual("Before", container.Find(".bit-pnl-hdr").TextContent.Trim()));

        await panelRef.Update(p => p.HeaderText = "After");

        container.WaitForAssertion(() => Assert.AreEqual("After", container.Find(".bit-pnl-hdr").TextContent.Trim()));
    }

    [TestMethod]
    public async Task ABottomSheetShownFromCodeShouldKeepTheSnapPointTheUserMovesItTo()
    {
        var container = RenderComponent<BitPanelContainer>();

        await PanelService.Show<TestPanelContent>(new Dictionary<string, object>(), new BitPanelParameters
        {
            Placement = BitPlacement.Bottom,
            SnapPoints = [0.3, 0.6, 0.9],
            SnapPoint = 1,
        });

        container.WaitForAssertion(() => Assert.AreEqual("--bit-pnl-snp:60dvh", container.Find(".bit-pnl-cnt").GetAttribute("style")));

        container.Find(".bit-pnl-grb").Click();

        Assert.AreEqual("--bit-pnl-snp:90dvh", container.Find(".bit-pnl-cnt").GetAttribute("style"));
    }

    [TestMethod]
    public async Task TheCallbacksOfTheParametersShouldBeCalled()
    {
        var container = RenderComponent<BitPanelContainer>();
        var overlayClicks = 0;
        var escapes = 0;

        await PanelService.Show<TestPanelContent>(new Dictionary<string, object>(), new BitPanelParameters
        {
            Blocking = true,
            NoDismissOnEscape = true,
            OnOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => overlayClicks++),
            OnEscapeKeyDown = EventCallback.Factory.Create<KeyboardEventArgs>(this, () => escapes++),
        });
        container.WaitForAssertion(() => Assert.AreEqual(1, container.FindAll(".bit-pnl-ovl").Count));

        container.Find(".bit-pnl-ovl").Click();
        container.Find(".bit-pnl").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        Assert.AreEqual(1, overlayClicks);
        Assert.AreEqual(1, escapes);
        Assert.AreEqual(1, container.FindAll(".bit-pnl-opn").Count, "a blocking panel that takes no Escape stays open");
    }

    [TestMethod]
    public async Task APanelShownWithMarkupShouldShowIt()
    {
        var container = RenderComponent<BitPanelContainer>();

        var panelRef = await PanelService.Show(builder => builder.AddMarkupContent(0, "<b class=\"markup\">Markup</b>"));

        container.WaitForAssertion(() => Assert.AreEqual("Markup", container.Find(".bit-pnl-cnt .markup").TextContent));

        await panelRef.Close();
        Assert.IsNull(await panelRef.Result.WaitAsync(_resultWait));
    }

    [TestMethod]
    public void TheServiceShouldBeRegisteredWithTheOthers()
    {
        var services = new ServiceCollection();
        services.AddBitBlazorUIServices();

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        Assert.IsNotNull(scope.ServiceProvider.GetService<BitPanelService>());
    }

    [TestMethod]
    public async Task APanelShownWithNoContainerShouldSayWhichContainerIsMissing()
    {
        var logs = new List<string>();
        var service = new BitPanelService(LoggerFactory.Create(b => b.AddProvider(new ListLoggerProvider(logs))));

        var panelRef = await service.Show(builder => builder.AddContent(0, "x"));

        Assert.IsFalse(await panelRef.Rendered.WaitAsync(_resultWait));
        Assert.AreEqual(1, logs.Count(l => l.Contains("<BitPanelContainer />")));
    }

    private sealed class ListLoggerProvider(List<string> logs) : ILoggerProvider
    {
        public ILogger CreateLogger(string categoryName) => new ListLogger(logs);

        public void Dispose() { }

        private sealed class ListLogger(List<string> logs) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            {
                logs.Add(formatter(state, exception));
            }
        }
    }
}

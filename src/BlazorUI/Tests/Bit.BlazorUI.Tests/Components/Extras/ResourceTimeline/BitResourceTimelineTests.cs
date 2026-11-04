using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline.BitResourceTimelineTestInterop;

namespace Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline;

/// <summary>
/// Covers the .NET side of the BitResourceTimeline with the browser side stood in for by bUnit: the markup it renders
/// around the surface the engine draws, what it sends the engine and when, the callbacks the engine raises, the
/// windowed and batched loads, and its teardown. The engine itself (BitResourceTimelineEngine.ts) is not run here.
/// </summary>
[TestClass]
public class BitResourceTimelineTests : BunitTestContext
{
    private const string BaseShortcuts = "ArrowLeft ArrowRight ArrowUp ArrowDown Home End PageUp PageDown Enter Escape";
    private const string EditingShortcuts = " Alt+ArrowLeft Alt+ArrowRight Alt+ArrowUp Alt+ArrowDown Alt+Shift+ArrowLeft Alt+Shift+ArrowRight Alt+Shift+ArrowUp Alt+Shift+ArrowDown";
    private const string CopyShortcuts = " Control+c Control+v Meta+c Meta+v";
    private const string DeleteShortcuts = " Delete Backspace";

    // A group row, an expanded child of it and a collapsed group, as the engine reports the visible rows.
    private const string GroupRows = """
        [
            { "id": "grp", "name": "Group", "depth": 0, "hasChildren": true, "collapsed": false, "height": 0 },
            { "id": "r1", "name": "Resource 1", "depth": 1, "hasChildren": false, "collapsed": false, "height": 0 },
            { "id": "grp2", "name": "Group 2", "depth": 0, "hasChildren": true, "collapsed": true, "height": 0 }
        ]
        """;

    private JSRuntimeInvocationHandler<string[]>? _selectedIds;



    private IReadOnlyList<JSRuntimeInvocation> Calls(string identifier) => Context.JSInterop.Invocations[identifier];

    // The token the last windowed load handed the engine, which its window requests carry back.
    private long WindowSession() => (long)Calls(BeginWindowedFn).Last().Arguments[4]!;

    // Makes the browser side answer setup with a layout, which is what starts the engine (without one, as in a
    // prerender, the timeline renders its shell and waits).
    private void PlanStart(object? layout = null) => Context.JSInterop.PlanLayout(SetupFn).Return(layout ?? Layout());

    private IRenderedComponent<BitResourceTimeline> RenderTimeline(Action<ComponentParameterCollectionBuilder<BitResourceTimeline>>? extra = null,
                                                                   BitResourceTimelineConfig? config = null)
    {
        config ??= SampleConfig();

        return RenderComponent<BitResourceTimeline>(parameters =>
        {
            parameters.Add(p => p.Config, config);
            extra?.Invoke(parameters);
        });
    }

    // Starts the engine, renders the timeline and waits for its first load to land.
    private IRenderedComponent<BitResourceTimeline> RenderLoaded(Action<ComponentParameterCollectionBuilder<BitResourceTimeline>>? extra = null,
                                                                 BitResourceTimelineConfig? config = null,
                                                                 object? layout = null)
    {
        PlanStart(layout);

        var component = RenderTimeline(extra, config);

        WaitForLoads(component, 1);

        return component;
    }

    // The overlay goes away once the engine has painted the bars, after the component has taken the loaded data in.
    private void WaitForLoads(IRenderedComponent<BitResourceTimeline> component, int count)
    {
        component.WaitForAssertion(() =>
        {
            Assert.AreEqual(count, Calls(WhenRenderedFn).Count);
            Assert.AreEqual(0, component.FindAll(".bit-rtm-ldg").Count);
        }, TimeSpan.FromSeconds(5));
    }

    // What the component resolves the ids the engine reports to: the instances it holds for them, in their order,
    // leaving out the ids it does not know.
    private async Task<IReadOnlyList<BitResourceTimelineAllocation>> ResolveAsync(IRenderedComponent<BitResourceTimeline> component, params string[] ids)
    {
        _selectedIds ??= Context.JSInterop.Setup<string[]>(GetSelectedBarIdsFn, _ => true);
        _selectedIds.SetResult(ids);

        IReadOnlyList<BitResourceTimelineAllocation> result = [];
        await component.InvokeAsync(async () => result = await component.Instance.GetSelectedBarsAsync());

        return result;
    }

    // Tears the component down the way the framework does when its parent stops rendering it.
    private static Task DisposeTimelineAsync(IRenderedComponent<BitResourceTimeline> component) =>
        component.InvokeAsync(async () => await component.Instance.DisposeAsync());

    // A state a continuation on the dispatcher settles into, with no render to wait on. Polled rather than waited for
    // with WaitForAssertion, which only checks again on a render: a planned call still pending when that one times out
    // is failed by bUnit (JSRuntimeInvocationNotSetException), which ends the load under test. Checked on the
    // dispatcher, so the invocations are never read while the component is adding one.
    private async Task WaitUntil(Func<bool> condition)
    {
        for (var i = 0; i < 500; i++)
        {
            if (await Renderer.Dispatcher.InvokeAsync(condition)) return;

            await Task.Delay(10);
        }

        Assert.Fail("The expected state was never reached.");
    }

    private static JsonElement Json(string json)
    {
        using var document = JsonDocument.Parse(json);

        return document.RootElement.Clone();
    }

    private static T Arg<T>(JSRuntimeInvocation invocation, int index) => (T)invocation.Arguments[index]!;



    // ---------------------------------------------------------------- the markup

    [TestMethod]
    public void BitResourceTimelineShouldRenderTheRootAndTheViewport()
    {
        var component = RenderTimeline();

        // The surface of the renderer (a canvas, an svg or a div) is created by the script, so only the root and the
        // viewport it is created in are in the markup; the overlays come with their templates.
        var root = component.Find(".bit-rtm");
        var viewport = component.Find(".bit-rtm > .bit-rtm-vpt");

        Assert.AreEqual("DIV", root.TagName);
        Assert.AreEqual(string.Empty, viewport.TextContent.Trim());
        Assert.AreEqual(0, component.FindAll(".bit-rtm-cnr, .bit-rtm-rov, .bit-rtm-tov, .bit-rtm-ldg").Count);
    }

    [TestMethod]
    public void BitResourceTimelineShouldRenderItsIdClassAndStyleOnTheRoot()
    {
        var component = RenderTimeline(p =>
        {
            p.Add(x => x.Class, "custom-class");
            p.Add(x => x.Style, "color:red");
            p.Add(x => x.Classes, new BitResourceTimelineClassStyles { Root = "custom-root" });
            p.Add(x => x.Styles, new BitResourceTimelineClassStyles { Root = "margin:1px" });
        });

        var root = component.Find(".bit-rtm");

        Assert.AreEqual(component.Instance.UniqueId, root.Id);
        Assert.IsTrue(root.ClassList.Contains("custom-class"));
        Assert.IsTrue(root.ClassList.Contains("custom-root"));
        StringAssert.Contains(root.GetAttribute("style"), "color:red");
        StringAssert.Contains(root.GetAttribute("style"), "margin:1px");

        component.Render(p => p.Add(x => x.Id, "planner"));

        Assert.AreEqual("planner", component.Find(".bit-rtm").Id);
    }

    [TestMethod,
        DataRow(BitVisibility.Visible, null),
        DataRow(BitVisibility.Hidden, "visibility:hidden"),
        DataRow(BitVisibility.Collapsed, "display:none")]
    public void BitResourceTimelineShouldRespectVisibility(BitVisibility visibility, string? expectedStyle)
    {
        var component = RenderTimeline(p => p.Add(x => x.Visibility, visibility));

        var style = component.Find(".bit-rtm").GetAttribute("style") ?? string.Empty;

        if (expectedStyle is null)
        {
            Assert.IsFalse(style.Contains("visibility:hidden") || style.Contains("display:none"), style);
        }
        else
        {
            StringAssert.Contains(style, expectedStyle);
        }
    }

    [TestMethod]
    public void BitResourceTimelineShouldRenderTheDirectionItIsGiven()
    {
        var component = RenderTimeline(p => p.Add(x => x.Dir, BitDir.Rtl));

        Assert.AreEqual("rtl", component.Find(".bit-rtm").GetAttribute("dir"));

        component.Render(p => p.Add(x => x.Dir, (BitDir?)null));

        Assert.IsFalse(component.Find(".bit-rtm").HasAttribute("dir"));
    }

    [TestMethod]
    public void BitResourceTimelineShouldNameItsFocusableViewport()
    {
        var component = RenderTimeline(p => p.Add(x => x.AriaLabel, "Flight plan"));

        var viewport = component.Find(".bit-rtm-vpt");

        Assert.AreEqual("Flight plan", viewport.GetAttribute("aria-label"));
        Assert.AreEqual("application", viewport.GetAttribute("role"));
        Assert.AreEqual("0", viewport.GetAttribute("tabindex"));
        Assert.AreEqual("Resource timeline", viewport.GetAttribute("aria-roledescription"));
        // The root is a structural wrapper, never what is named.
        Assert.IsFalse(component.Find(".bit-rtm").HasAttribute("aria-label"));
    }

    [TestMethod]
    public void BitResourceTimelineShouldNameItsViewportThroughItsTexts()
    {
        var component = RenderTimeline();

        var viewport = component.Find(".bit-rtm-vpt");
        Assert.AreEqual("Resource timeline", viewport.GetAttribute("aria-label"));
        Assert.AreEqual("Resource timeline", viewport.GetAttribute("aria-roledescription"));

        component.Render(p => p.Add(x => x.Texts, new BitResourceTimelineTexts { AriaLabel = "Dienstplan", RoleDescription = "Zeitleiste" }));

        viewport = component.Find(".bit-rtm-vpt");
        Assert.AreEqual("Dienstplan", viewport.GetAttribute("aria-label"));
        Assert.AreEqual("Zeitleiste", viewport.GetAttribute("aria-roledescription"));

        // An AriaLabel of its own names this one timeline over the texts it shares with others.
        component.Render(p => p.Add(x => x.AriaLabel, "Flight plan"));

        Assert.AreEqual("Flight plan", component.Find(".bit-rtm-vpt").GetAttribute("aria-label"));
    }

    [TestMethod]
    public void BitResourceTimelineShouldPutTheViewportInTheTabOrderItIsGiven()
    {
        var component = RenderTimeline(p => p.Add(x => x.TabIndex, "-1"));

        Assert.AreEqual("-1", component.Find(".bit-rtm-vpt").GetAttribute("tabindex"));
    }

    [TestMethod,
        DataRow(null, null, false, false),
        DataRow(false, true, false, false),
        DataRow(null, true, false, false),
        DataRow(true, null, true, false),
        DataRow(true, false, true, false),
        DataRow(true, true, true, true)]
    public void BitResourceTimelineShouldAdvertiseTheShortcutsItHandles(bool? editable, bool? allowDelete, bool editing, bool delete)
    {
        var component = RenderTimeline(p => p.Add(x => x.Options, new BitResourceTimelineOptions { Editable = editable, AllowDelete = allowDelete }));

        // The engine edits, copies and pastes only while editing is on, and deletes only while editing and AllowDelete
        // both are (BitResourceTimelineEngine.handleKeyDown).
        var expected = BaseShortcuts + (editing ? EditingShortcuts + CopyShortcuts : string.Empty) + (delete ? DeleteShortcuts : string.Empty);

        Assert.AreEqual(expected, component.Find(".bit-rtm-vpt").GetAttribute("aria-keyshortcuts"));
    }

    [TestMethod]
    public void BitResourceTimelineShouldAdvertiseTheEditingShortcutsOnlyWhenEditable()
    {
        PlanStart();

        var component = RenderTimeline(p => p.Add(x => x.Options, new BitResourceTimelineOptions { Editable = true }));

        var shortcuts = component.Find(".bit-rtm-vpt").GetAttribute("aria-keyshortcuts")!;
        StringAssert.Contains(shortcuts, "Alt+ArrowLeft");
        StringAssert.Contains(shortcuts, "Alt+Shift+ArrowUp");

        // Options that leave Editable (and AllowDelete) unset keep what is in force, as the engine merges them.
        component.Render(p => p.Add(x => x.Options, new BitResourceTimelineOptions { AllowDelete = true }));
        component.Render(p => p.Add(x => x.Options, new BitResourceTimelineOptions()));

        shortcuts = component.Find(".bit-rtm-vpt").GetAttribute("aria-keyshortcuts")!;
        StringAssert.Contains(shortcuts, "Alt+ArrowLeft");
        StringAssert.Contains(shortcuts, "Delete");

        component.Render(p => p.Add(x => x.Options, new BitResourceTimelineOptions { Editable = false }));

        Assert.IsFalse(component.Find(".bit-rtm-vpt").GetAttribute("aria-keyshortcuts")!.Contains("Alt+"));
    }

    [TestMethod]
    public void BitResourceTimelineShouldSayItIsDisabled()
    {
        var component = RenderTimeline(p => p.Add(x => x.Disabled, true));

        Assert.IsTrue(component.Find(".bit-rtm").ClassList.Contains("bit-dis"));
        Assert.AreEqual("true", component.Find(".bit-rtm-vpt").GetAttribute("aria-disabled"));
        // The state goes on the element that stands for what is unavailable, never on the structural root.
        Assert.IsFalse(component.Find(".bit-rtm").HasAttribute("aria-disabled"));

        component.Render(p => p.Add(x => x.Disabled, false));

        Assert.IsFalse(component.Find(".bit-rtm").ClassList.Contains("bit-dis"));
        Assert.IsFalse(component.Find(".bit-rtm-vpt").HasAttribute("aria-disabled"));
    }

    [TestMethod]
    public void BitResourceTimelineShouldRenderTheTopStartContentInTheCornerTheEngineLaysOut()
    {
        var component = RenderLoaded(p => p.Add(x => x.TopStartContent, "<span class=\"corner\">Corner</span>"), layout: Layout(180, 50));

        var corner = component.Find(".bit-rtm-cnr");

        Assert.AreEqual("Corner", corner.QuerySelector(".corner")!.TextContent);
        Assert.AreEqual("width:180px;height:50px", corner.GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRespectClassesAndStylesOnEveryPart()
    {
        PlanStart();
        var setData = Context.JSInterop.SetupVoid(SetDataFn, _ => true);

        var component = RenderTimeline(p =>
        {
            p.Add(x => x.Classes, new BitResourceTimelineClassStyles
            {
                Root = "c-root",
                Viewport = "c-vpt",
                Corner = "c-cnr",
                ResourceOverlay = "c-rov",
                ResourceRow = "c-rrw",
                ResourceContent = "c-rct",
                GroupToggle = "c-tgl",
                TooltipOverlay = "c-tov",
                Loading = "c-ldg",
                Spinner = "c-spn",
                LoadingText = "c-ldt",
            });
            p.Add(x => x.Styles, new BitResourceTimelineClassStyles
            {
                Root = "margin:1px",
                Viewport = "margin:2px",
                Corner = "margin:3px",
                ResourceOverlay = "margin:4px",
                ResourceRow = "margin:5px",
                ResourceContent = "margin:6px",
                GroupToggle = "margin:7px",
                TooltipOverlay = "margin:8px",
                Loading = "margin:9px",
                Spinner = "margin:10px",
                LoadingText = "margin:11px",
            });
            p.Add(x => x.TopStartContent, "Corner");
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
            p.Add(x => x.TooltipTemplate, a => b => b.AddContent(0, a.Id));
        });

        // The loading overlay and its default parts, while the engine takes the data.
        await WaitUntil(() => Calls(SetDataFn).Count == 1);

        AssertPart(".bit-rtm-ldg", "c-ldg", "margin:9px");
        AssertPart(".bit-rtm-spn", "c-spn", "margin:10px");
        AssertPart(".bit-rtm-ldt", "c-ldt", "margin:11px");

        setData.SetVoidResult();
        WaitForLoads(component, 1);

        await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json(GroupRows)));
        await component.InvokeAsync(() => component.Instance._OnBarHover("a1", 10, 20));

        AssertPart(".bit-rtm", "c-root", "margin:1px", exact: false);
        AssertPart(".bit-rtm-vpt", "c-vpt", "margin:2px");
        AssertPart(".bit-rtm-cnr", "c-cnr", "width:150px;height:60px;margin:3px");
        AssertPart(".bit-rtm-rov", "c-rov", "top:60px;width:150px;margin:4px");
        AssertPart(".bit-rtm-rrw", "c-rrw", "height:40px;padding-left:8px;margin:5px");
        AssertPart(".bit-rtm-rct", "c-rct", "margin:6px");
        AssertPart(".bit-rtm-tgl", "c-tgl", "margin:7px");
        AssertPart(".bit-rtm-tov", "c-tov", "left:10px;top:20px;margin:8px");

        void AssertPart(string selector, string cssClass, string style, bool exact = true)
        {
            var element = component.Find(selector);

            Assert.IsTrue(element.ClassList.Contains(cssClass), $"{selector} has no {cssClass}: {element.ClassName}");

            if (exact)
            {
                Assert.AreEqual(style, element.GetAttribute("style"), selector);
            }
            else
            {
                StringAssert.Contains(element.GetAttribute("style"), style, selector);
            }
        }
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldShowTheLoadingOverlayWhileTheEngineTakesTheData()
    {
        PlanStart();
        var setData = Context.JSInterop.SetupVoid(SetDataFn, _ => true);

        var component = RenderTimeline(p => p.Add(x => x.Texts, new BitResourceTimelineTexts { Loading = "Wird geladen" }));

        await WaitUntil(() => Calls(SetDataFn).Count == 1);

        var loading = component.Find(".bit-rtm > .bit-rtm-ldg");
        Assert.AreEqual("status", loading.GetAttribute("role"));
        Assert.AreEqual("polite", loading.GetAttribute("aria-live"));
        Assert.IsNotNull(loading.QuerySelector(".bit-rtm-spn"));
        Assert.AreEqual("Wird geladen", loading.QuerySelector(".bit-rtm-ldt")!.TextContent);
        Assert.AreEqual("true", component.Find(".bit-rtm").GetAttribute("aria-busy"));

        setData.SetVoidResult();
        WaitForLoads(component, 1);

        Assert.IsFalse(component.Find(".bit-rtm").HasAttribute("aria-busy"));
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldShowTheLoadingContentInPlaceOfTheSpinner()
    {
        PlanStart();
        Context.JSInterop.SetupVoid(SetDataFn, _ => true);

        var component = RenderTimeline(p => p.Add(x => x.LoadingContent, "<em class=\"own\">Just a moment</em>"));

        await WaitUntil(() => Calls(SetDataFn).Count == 1);

        var loading = component.Find(".bit-rtm-ldg");
        Assert.AreEqual("Just a moment", loading.QuerySelector(".own")!.TextContent);
        Assert.IsNull(loading.QuerySelector(".bit-rtm-spn"));
        Assert.IsNull(loading.QuerySelector(".bit-rtm-ldt"));
    }

    [TestMethod]
    public void BitResourceTimelineShouldKeepTheAriaBusyThePageSplattedOnWhileIdle()
    {
        PlanStart();

        // Splatted the way markup writes an attribute the component has no parameter for.
        var component = Context.Render(builder =>
        {
            builder.OpenComponent<BitResourceTimeline>(0);
            builder.AddAttribute(1, nameof(BitResourceTimeline.Config), SampleConfig());
            builder.AddAttribute(2, "aria-busy", "true");
            builder.CloseComponent();
        });

        component.WaitForAssertion(() => Assert.AreEqual(1, Calls(WhenRenderedFn).Count), TimeSpan.FromSeconds(5));

        Assert.AreEqual("true", component.Find(".bit-rtm").GetAttribute("aria-busy"));
    }



    // ---------------------------------------------------------------- starting the engine

    [TestMethod]
    public void BitResourceTimelineShouldStartTheEngineOnTheViewport()
    {
        var options = new BitResourceTimelineOptions { Editable = true };
        var texts = new BitResourceTimelineTexts { AriaLabel = "Plan" };

        var component = RenderLoaded(p =>
        {
            p.Add(x => x.Options, options);
            p.Add(x => x.Texts, texts);
            p.Add(x => x.Disabled, true);
        });

        var setup = Calls(SetupFn).Single();

        Assert.AreEqual(component.Instance.UniqueId, setup.Arguments[0]);
        Assert.IsInstanceOfType<ElementReference>(setup.Arguments[1]);
        Assert.AreSame(component.Instance, Arg<DotNetObjectReference<BitResourceTimeline>>(setup, 2).Value);
        Assert.AreSame(options, setup.Arguments[3]);
        Assert.AreSame(texts, setup.Arguments[4]);
        Assert.AreEqual(true, setup.Arguments[5]);
        Assert.AreEqual(6, setup.Arguments.Count);
    }

    [TestMethod]
    public void BitResourceTimelineShouldHandTheEngineTheDefaultTextsWhenItHasNone()
    {
        RenderLoaded();

        var texts = Arg<BitResourceTimelineTexts>(Calls(SetupFn).Single(), 4);

        Assert.AreEqual("Resource timeline", texts.AriaLabel);
        Assert.AreEqual("Bar is locked", texts.BarLocked);
        Assert.AreEqual(false, Calls(SetupFn).Single().Arguments[5]);
    }

    [TestMethod]
    public void BitResourceTimelineShouldNotStartWithoutALayout()
    {
        // No layout is what the script answers where it cannot run (a prerender) or finds no viewport.
        Context.JSInterop.PlanLayout(SetupFn).Return(null);

        var component = RenderTimeline(p =>
        {
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
            p.Add(x => x.TooltipTemplate, a => b => b.AddContent(0, a.Id));
        });

        Assert.AreEqual(1, Calls(SetupFn).Count);
        Assert.AreEqual(1, Context.JSInterop.Invocations.Count, string.Join(", ", Context.JSInterop.Invocations.Select(i => i.Identifier)));
        Assert.AreEqual(0, component.FindAll(".bit-rtm-ldg").Count);

        component.Render(p => p.Add(x => x.Options, new BitResourceTimelineOptions()));

        Assert.AreEqual(0, Calls(SetOptionsFn).Count);
    }

    [TestMethod]
    public void BitResourceTimelineShouldEnableItsTemplatesBeforeTheDataLoads()
    {
        RenderLoaded(p =>
        {
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
            p.Add(x => x.TooltipTemplate, a => b => b.AddContent(0, a.Id));
        });

        var order = Context.JSInterop.Invocations.Select(i => i.Identifier).ToList();

        Assert.IsInstanceOfType<ElementReference>(Calls(EnableResourceTemplateFn).Single().Arguments[1]);
        Assert.AreEqual(1, Calls(EnableTooltipTemplateFn).Single().Arguments.Count);
        // The renderer leaves the labels to the overlay and the engine reports its rows from the very first load.
        Assert.IsTrue(order.IndexOf(SetupFn) < order.IndexOf(EnableResourceTemplateFn));
        Assert.IsTrue(order.IndexOf(EnableResourceTemplateFn) < order.IndexOf(SetDataFn));
        Assert.IsTrue(order.IndexOf(EnableTooltipTemplateFn) < order.IndexOf(SetDataFn));
    }

    [TestMethod]
    public void BitResourceTimelineShouldNotEnableTemplatesItDoesNotHave()
    {
        RenderLoaded();

        Assert.AreEqual(0, Calls(EnableResourceTemplateFn).Count);
        Assert.AreEqual(0, Calls(EnableTooltipTemplateFn).Count);
        Assert.AreEqual(0, Calls(DisableResourceTemplateFn).Count);
        Assert.AreEqual(0, Calls(DisableTooltipTemplateFn).Count);
    }

    [TestMethod]
    public void BitResourceTimelineShouldEnableTemplatesAssignedAfterItStarted()
    {
        var component = RenderLoaded();

        component.Render(p =>
        {
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
            p.Add(x => x.TooltipTemplate, a => b => b.AddContent(0, a.Id));
        });

        component.WaitForAssertion(() =>
        {
            // The overlay of the template is in the markup by the time the engine is handed it.
            Assert.IsInstanceOfType<ElementReference>(Calls(EnableResourceTemplateFn).Single().Arguments[1]);
            Assert.AreEqual(1, Calls(EnableTooltipTemplateFn).Count);
        }, TimeSpan.FromSeconds(5));

        // Rendering again with the same templates tells the engine nothing new.
        component.Render(p => p.Add(x => x.LoadingMinDurationMs, 0));

        Assert.AreEqual(1, Calls(EnableResourceTemplateFn).Count);
        Assert.AreEqual(1, Calls(EnableTooltipTemplateFn).Count);
    }

    [TestMethod]
    public void BitResourceTimelineShouldDisableTemplatesTakenAway()
    {
        var component = RenderLoaded(p =>
        {
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
            p.Add(x => x.TooltipTemplate, a => b => b.AddContent(0, a.Id));
        });

        component.Render(p =>
        {
            p.Add(x => x.ResourceTemplate, (RenderFragment<BitResourceTimelineRowContext>?)null);
            p.Add(x => x.TooltipTemplate, (RenderFragment<BitResourceTimelineAllocation>?)null);
        });

        component.WaitForAssertion(() =>
        {
            // The engine draws the resource labels and its own tooltip again.
            Assert.AreEqual(1, Calls(DisableResourceTemplateFn).Count);
            Assert.AreEqual(1, Calls(DisableTooltipTemplateFn).Count);
        }, TimeSpan.FromSeconds(5));

        Assert.AreEqual(0, component.FindAll(".bit-rtm-rov").Count);
    }



    // ---------------------------------------------------------------- options, texts and the disabled state

    [TestMethod]
    public void BitResourceTimelineShouldSendEachChangeOfItsConfigurationOnce()
    {
        var component = RenderLoaded();

        Assert.AreEqual(0, Calls(SetOptionsFn).Count, "Nothing changed since setup.");

        var options = new BitResourceTimelineOptions { BarHeight = 8 };
        component.Render(p => p.Add(x => x.Options, options));

        var call = Calls(SetOptionsFn).Single();
        Assert.AreEqual(component.Instance.UniqueId, call.Arguments[0]);
        Assert.AreSame(options, call.Arguments[1]);
        Assert.AreEqual("Resource timeline", Arg<BitResourceTimelineTexts>(call, 2).AriaLabel);
        Assert.AreEqual(false, call.Arguments[3]);

        // The same instances again change nothing.
        component.Render(p => p.Add(x => x.Options, options));
        Assert.AreEqual(1, Calls(SetOptionsFn).Count);

        var texts = new BitResourceTimelineTexts { BarLocked = "Gesperrt" };
        component.Render(p => p.Add(x => x.Texts, texts));
        Assert.AreEqual(2, Calls(SetOptionsFn).Count);
        Assert.AreSame(texts, Calls(SetOptionsFn)[1].Arguments[2]);

        component.Render(p => p.Add(x => x.Disabled, true));
        Assert.AreEqual(3, Calls(SetOptionsFn).Count);
        Assert.AreEqual(true, Calls(SetOptionsFn)[2].Arguments[3]);
        Assert.AreSame(options, Calls(SetOptionsFn)[2].Arguments[1]);
        Assert.AreSame(texts, Calls(SetOptionsFn)[2].Arguments[2]);

        // All three in one render are one call.
        component.Render(p =>
        {
            p.Add(x => x.Options, new BitResourceTimelineOptions());
            p.Add(x => x.Texts, new BitResourceTimelineTexts());
            p.Add(x => x.Disabled, false);
        });
        Assert.AreEqual(4, Calls(SetOptionsFn).Count);
        Assert.AreEqual(false, Calls(SetOptionsFn)[3].Arguments[3]);

        // A configuration change is not a data change.
        Assert.AreEqual(1, Calls(SetDataFn).Count);
    }

    [TestMethod]
    public void BitResourceTimelineShouldNotSendAnOptionsInstanceChangedInPlace()
    {
        var options = new BitResourceTimelineOptions { BarHeight = 8 };
        var component = RenderLoaded(p => p.Add(x => x.Options, options));

        options.BarHeight = 12;
        component.Render(p => p.Add(x => x.Options, options));

        Assert.AreEqual(0, Calls(SetOptionsFn).Count);
    }

    [TestMethod]
    public void BitResourceTimelineShouldLayItsCornerAndOverlayOutLikeTheEngineAfterNewOptions()
    {
        var component = RenderLoaded(p =>
        {
            p.Add(x => x.TopStartContent, "Corner");
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
        });

        Context.JSInterop.PlanLayout(SetOptionsFn).Return(Layout(220, 72, 40));

        component.Render(p => p.Add(x => x.Options, new BitResourceTimelineOptions { ResourceAxisWidth = 220, TimeAxisHeight = 72 }));

        Assert.AreEqual("width:220px;height:72px", component.Find(".bit-rtm-cnr").GetAttribute("style"));
        Assert.AreEqual("top:72px;width:220px", component.Find(".bit-rtm-rov").GetAttribute("style"));
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldSendWhatChangedWhileItsEngineWasStarting()
    {
        // StartAsync sends Options, Texts and Disabled with the setup, and OnParametersSetAsync has no engine to send a
        // change to until the setup returns: a change landing in that round trip goes out right after it.
        var setup = Context.JSInterop.PlanLayout(SetupFn);

        var component = RenderTimeline();

        var options = new BitResourceTimelineOptions { Editable = true };
        component.Render(p =>
        {
            p.Add(x => x.Options, options);
            p.Add(x => x.Disabled, true);
        });

        await component.InvokeAsync(() => setup.Return(Layout()));

        WaitForLoads(component, 1);

        var sent = Calls(SetOptionsFn);
        Assert.AreEqual(1, sent.Count, "The Options and the Disabled state set while the engine was starting were never sent to it.");
        Assert.AreSame(options, sent[0].Arguments[1]);
        Assert.AreEqual(true, sent[0].Arguments[3]);
    }



    // ---------------------------------------------------------------- the data

    [TestMethod]
    public void BitResourceTimelineShouldSendTheConfigToTheEngine()
    {
        var config = SampleConfig();

        var component = RenderLoaded(config: config);

        var setData = Calls(SetDataFn).Single();

        Assert.AreEqual(component.Instance.UniqueId, setData.Arguments[0]);
        Assert.AreSame(config.Resources, setData.Arguments[1]);
        Assert.AreEqual(0L, setData.Arguments[2]);
        Assert.AreEqual(3_600_000L, setData.Arguments[3]);
        Assert.AreSame(config.Allocations, setData.Arguments[4]);
        Assert.AreEqual(component.Instance.UniqueId, Calls(WhenRenderedFn).Single().Arguments[0]);
        Assert.AreEqual(0, Calls(BeginDataFn).Count);
        Assert.AreEqual(0, Calls(BeginWindowedFn).Count);
    }

    [TestMethod]
    public void BitResourceTimelineShouldSendTheTimeRangeAsUnixMilliseconds()
    {
        var config = SampleConfig();
        config.StartDate = new DateTimeOffset(2026, 5, 4, 8, 0, 0, TimeSpan.FromHours(3.5));
        config.EndDate = config.StartDate.AddDays(2);

        RenderLoaded(config: config);

        var setData = Calls(SetDataFn).Single();

        Assert.AreEqual(config.StartDate.ToUnixTimeMilliseconds(), setData.Arguments[2]);
        Assert.AreEqual(config.EndDate.ToUnixTimeMilliseconds(), setData.Arguments[3]);
    }

    [TestMethod]
    public void BitResourceTimelineShouldNotSendTheSameConfigAgain()
    {
        var config = SampleConfig();
        var component = RenderLoaded(config: config);

        component.Render(p => p.Add(x => x.Config, config));
        component.Render(p => p.Add(x => x.AriaLabel, "Another render of the parent"));

        Assert.AreEqual(1, Calls(SetDataFn).Count);

        var next = SampleConfig();
        component.Render(p => p.Add(x => x.Config, next));
        WaitForLoads(component, 2);

        Assert.AreEqual(2, Calls(SetDataFn).Count);
        Assert.AreSame(next.Allocations, Calls(SetDataFn)[1].Arguments[4]);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldEmptyTheEngineWhenTheConfigIsTakenAway()
    {
        var config = SampleConfig();
        var component = RenderLoaded(config: config);

        component.Render(p => p.Add(x => x.Config, null));

        await WaitUntil(() => Calls(ClearDataFn).Count == 1);
        Assert.AreEqual(component.Instance.UniqueId, Calls(ClearDataFn).Single().Arguments[0]);

        // The data it held is gone from the calls about bars too: nothing asks the engine about them any more.
        Assert.IsEmpty(await ResolveAsync(component, "a1"));
        Assert.IsFalse(await component.InvokeAsync(() => component.Instance.ScrollToAllocationAsync("a1")));
        Assert.AreEqual(0, Calls(GetSelectedBarIdsFn).Count);
        Assert.AreEqual(0, Calls(ScrollToAllocationFn).Count);

        // Another render without a Config clears nothing again, and a Config assigned again loads.
        component.Render(p => p.Add(x => x.AriaLabel, "Another render of the parent"));
        Assert.AreEqual(1, Calls(ClearDataFn).Count);

        component.Render(p => p.Add(x => x.Config, config));
        WaitForLoads(component, 2);
        Assert.AreEqual(2, Calls(SetDataFn).Count);
    }

    [TestMethod]
    public void BitResourceTimelineShouldLoadTheSameConfigAgainWhenTheLoadingModeChanges()
    {
        Context.JSInterop.Setup<long[]>(GetVisibleWindowFn, _ => true).SetResult([0L, 3_600_000L]);

        var config = SampleConfig();
        var component = RenderLoaded(config: config);

        Func<BitResourceTimelineWindow, Task<IReadOnlyList<BitResourceTimelineAllocation>>> load =
            _ => Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>>(config.Allocations);

        component.Render(p => p.Add(x => x.LoadAllocationsAsync, load));
        WaitForLoads(component, 2);

        Assert.AreEqual(1, Calls(BeginWindowedFn).Count);
        Assert.AreEqual(1, Calls(ApplyAllocationWindowFn).Count);

        // Another render with the same delegate loads nothing.
        component.Render(p => p.Add(x => x.AriaLabel, "Another render of the parent"));
        Assert.AreEqual(1, Calls(BeginWindowedFn).Count);

        component.Render(p => p.Add(x => x.LoadAllocationsAsync, null));
        WaitForLoads(component, 3);

        Assert.AreEqual(2, Calls(SetDataFn).Count);
        Assert.AreSame(config.Allocations, Calls(SetDataFn)[1].Arguments[4]);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldNotReloadTheSameConfigForAParentRenderDuringTheLoad()
    {
        PlanStart();
        var setData = Context.JSInterop.SetupVoid(SetDataFn, _ => true);
        var config = SampleConfig();

        var component = RenderTimeline(config: config);
        await WaitUntil(() => Calls(SetDataFn).Count > 0);

        // A host wiring OnViewChanged (or any other render of the parent) re-renders while the first setData is still
        // in flight; that must not queue another full load of the same instance.
        component.Render(p => p.Add(x => x.Config, config));

        setData.SetVoidResult();
        WaitForLoads(component, 1);

        Assert.AreEqual(1, Calls(SetDataFn).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldSendTheConfigAgainOnReload()
    {
        var config = SampleConfig();
        var component = RenderLoaded(config: config);

        config.Allocations.Add(Allocation("a2", "r1", 120_000, 180_000));

        await component.InvokeAsync(() => component.Instance.ReloadAsync());

        Assert.AreEqual(2, Calls(SetDataFn).Count);
        Assert.AreSame(config.Allocations, Calls(SetDataFn)[1].Arguments[4]);
        Assert.AreEqual(2, Calls(WhenRenderedFn).Count);

        // The bar added in place resolves now.
        Assert.AreSame(config.Allocations[1], (await ResolveAsync(component, "a2")).Single());
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldSendTheConfigAgainOnAReloadAskedForDuringALoad()
    {
        // A ReloadAsync queued behind an in-flight load of the same Config finds _loadedConfig set by that load once it gets
        // the gate; it is forced, so the data changed in place is sent again all the same.
        PlanStart();
        var setData = Context.JSInterop.SetupVoid(SetDataFn, _ => true);
        var config = SampleConfig();

        var component = RenderTimeline(config: config);
        await WaitUntil(() => Calls(SetDataFn).Count == 1);

        // The app changes the data in place while the first load is in flight, and asks for it to be sent again.
        config.Allocations.Add(Allocation("a2", "r1", 120_000, 180_000));
        var reload = component.InvokeAsync(() => component.Instance.ReloadAsync());

        setData.SetVoidResult();
        await reload;

        Assert.AreEqual(2, Calls(SetDataFn).Count, "The reload asked for during a load of the same Config was dropped.");
    }

    [TestMethod]
    public void BitResourceTimelineShouldLoadAConfigAssignedAfterItStarted()
    {
        PlanStart();

        var component = RenderComponent<BitResourceTimeline>();

        Assert.AreEqual(1, Calls(SetupFn).Count);
        Assert.AreEqual(0, Calls(SetDataFn).Count);

        component.Render(p => p.Add(x => x.Config, SampleConfig()));

        WaitForLoads(component, 1);
        Assert.AreEqual(1, Calls(SetDataFn).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldStreamALargeConfigInBatches()
    {
        var config = SampleConfig();
        config.Allocations = Enumerable.Range(1, 5).Select(i => Allocation($"a{i}", "r1", i * 60_000, i * 60_000 + 30_000)).ToList();

        var component = RenderLoaded(p => p.Add(x => x.LoadBatchSize, 2), config);

        var begin = Calls(BeginDataFn).Single();
        Assert.AreEqual(component.Instance.UniqueId, begin.Arguments[0]);
        Assert.AreSame(config.Resources, begin.Arguments[1]);
        Assert.AreEqual(0L, begin.Arguments[2]);
        Assert.AreEqual(3_600_000L, begin.Arguments[3]);
        Assert.AreEqual(5, begin.Arguments[4]);

        var batches = Calls(AppendAllocationsFn).Select(c => Arg<IReadOnlyList<BitResourceTimelineAllocation>>(c, 1)).ToList();
        CollectionAssert.AreEqual(new[] { 2, 2, 1 }, batches.Select(b => b.Count).ToArray());
        CollectionAssert.AreEqual(config.Allocations, batches.SelectMany(b => b).ToList());

        Assert.AreEqual(component.Instance.UniqueId, Calls(EndDataFn).Single().Arguments[0]);
        Assert.AreEqual(0, Calls(SetDataFn).Count);

        var order = Context.JSInterop.Invocations.Select(i => i.Identifier).ToList();
        Assert.IsTrue(order.LastIndexOf(AppendAllocationsFn) < order.IndexOf(EndDataFn));
        Assert.IsTrue(order.IndexOf(EndDataFn) < order.IndexOf(WhenRenderedFn));

        // Every streamed bar resolves to the app's instance.
        CollectionAssert.AreEqual(config.Allocations, (await ResolveAsync(component, "a1", "a2", "a3", "a4", "a5")).ToList());
    }

    [TestMethod, DataRow(0), DataRow(-1), DataRow(5), DataRow(10_000)]
    public void BitResourceTimelineShouldSendAConfigNoLargerThanABatchInOneCall(int batchSize)
    {
        var config = SampleConfig();
        config.Allocations = Enumerable.Range(1, 5).Select(i => Allocation($"a{i}", "r1", i * 60_000, i * 60_000 + 30_000)).ToList();

        RenderLoaded(p => p.Add(x => x.LoadBatchSize, batchSize), config);

        Assert.AreEqual(1, Calls(SetDataFn).Count);
        Assert.AreEqual(0, Calls(BeginDataFn).Count);
        Assert.AreEqual(0, Calls(AppendAllocationsFn).Count);
        Assert.AreEqual(0, Calls(EndDataFn).Count);
    }



    // ---------------------------------------------------------------- the windowed mode

    [TestMethod]
    public async Task BitResourceTimelineShouldRequestTheInitialWindowFromTheHost()
    {
        Context.JSInterop.Setup<long[]>(GetVisibleWindowFn, _ => true).SetResult([600_000L, 1_800_000L]);

        var config = SampleConfig();
        var windows = new List<BitResourceTimelineWindow>();

        var component = RenderLoaded(p => p.Add(x => x.LoadAllocationsAsync, window =>
        {
            windows.Add(window);
            return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>>(config.Allocations);
        }), config);

        // The rows and the whole range first, without a bar.
        var begin = Calls(BeginWindowedFn).Single();
        Assert.AreEqual(component.Instance.UniqueId, begin.Arguments[0]);
        Assert.AreSame(config.Resources, begin.Arguments[1]);
        Assert.AreEqual(0L, begin.Arguments[2]);
        Assert.AreEqual(3_600_000L, begin.Arguments[3]);
        Assert.AreEqual(0, Calls(SetDataFn).Count);
        Assert.AreEqual(0, Calls(BeginDataFn).Count);

        Assert.AreEqual(component.Instance.UniqueId, Calls(GetVisibleWindowFn).Single().Arguments[0]);

        var window = windows.Single();
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(600_000), window.Start);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(1_800_000), window.End);

        var apply = Calls(ApplyAllocationWindowFn).Single();
        Assert.AreEqual(component.Instance.UniqueId, apply.Arguments[0]);
        Assert.AreEqual(0L, apply.Arguments[1]);
        Assert.AreSame(config.Allocations, apply.Arguments[2]);
        Assert.AreEqual(600_000L, apply.Arguments[3]);
        Assert.AreEqual(1_800_000L, apply.Arguments[4]);
        // The window is applied with the token the engine started the windowed mode with.
        Assert.AreEqual(WindowSession(), apply.Arguments[5]);

        Assert.AreSame(config.Allocations[0], (await ResolveAsync(component, "a1")).Single());
    }

    [TestMethod]
    public void BitResourceTimelineShouldFinishAWindowedLoadWithoutAVisibleWindow()
    {
        var asked = 0;

        RenderLoaded(p => p.Add(x => x.LoadAllocationsAsync, _ =>
        {
            asked++;
            return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>>([]);
        }));

        Assert.AreEqual(0, asked);
        Assert.AreEqual(0, Calls(ApplyAllocationWindowFn).Count);
        Assert.AreEqual(1, Calls(WhenRenderedFn).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldFetchTheWindowsTheEngineAsksFor()
    {
        Context.JSInterop.Setup<long[]>(GetVisibleWindowFn, _ => true).SetResult([0L, 3_600_000L]);

        var config = SampleConfig();
        var windows = new List<BitResourceTimelineWindow>();
        var later = Allocation("a3", "r1", 5_000_000, 5_100_000);

        var component = RenderLoaded(p => p.Add(x => x.LoadAllocationsAsync, window =>
        {
            windows.Add(window);
            IReadOnlyList<BitResourceTimelineAllocation> result = windows.Count == 1
                ? [config.Allocations[0], Allocation("a2", "r1", 3_000_000, 3_100_000)]
                : [later];
            return Task.FromResult(result);
        }), config);

        Assert.AreEqual(1, (await ResolveAsync(component, "a2")).Count);

        await component.InvokeAsync(() => component.Instance._RequestAllocationWindow(1, 3_000_000, 7_200_000, WindowSession()));

        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(3_000_000), windows[1].Start);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(7_200_000), windows[1].End);

        var apply = Calls(ApplyAllocationWindowFn)[1];
        Assert.AreEqual(1L, apply.Arguments[1]);
        Assert.AreEqual(3_000_000L, apply.Arguments[3]);
        Assert.AreEqual(7_200_000L, apply.Arguments[4]);

        // The loaded bars are exactly the new window's: a1 lies outside it, and a2, though its old position overlaps
        // the window, is no longer in what the app returned for it (deleted, or moved away) - both are forgotten.
        var resolved = await ResolveAsync(component, "a1", "a2", "a3");
        Assert.AreEqual(1, resolved.Count);
        Assert.AreSame(later, resolved[0]);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldDropAWindowOfTheConfigBeforeANewOne()
    {
        Context.JSInterop.Setup<long[]>(GetVisibleWindowFn, _ => true).SetResult([0L, 3_600_000L]);

        var pending = new Queue<TaskCompletionSource<IReadOnlyList<BitResourceTimelineAllocation>>>();
        var asked = 0;

        var component = RenderLoaded(p => p.Add(x => x.LoadAllocationsAsync, _ =>
        {
            // The initial window of each load answers at once; the scroll of the first load is left fetching.
            if (++asked != 2) return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>>([Allocation("current" + asked, "r1", 0, 60_000)]);

            var fetch = new TaskCompletionSource<IReadOnlyList<BitResourceTimelineAllocation>>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending.Enqueue(fetch);
            return fetch.Task;
        }));

        var firstSession = WindowSession();

        // A scroll asks for request 5 of the first load, which is still fetching when the app assigns a new Config.
        var stale = component.InvokeAsync(() => component.Instance._RequestAllocationWindow(5, 0, 2_000_000, firstSession));
        await WaitUntil(() => pending.Count == 1);

        component.Render(p => p.Add(x => x.Config, SampleConfig()));
        await WaitUntil(() => Calls(BeginWindowedFn).Count == 2);

        var secondSession = WindowSession();
        Assert.AreNotEqual(firstSession, secondSession);

        // The old fetch answers only now that the new load has begun (its initial window waits its turn behind it).
        pending.Dequeue().SetResult([Allocation("stale", "r1", 0, 60_000)]);
        await stale;
        WaitForLoads(component, 2);

        // The old fetch reaches neither the engine nor the bars the component resolves, though its id is higher than
        // the new load's initial window.
        Assert.AreEqual(2, Calls(ApplyAllocationWindowFn).Count);
        Assert.AreEqual(secondSession, Calls(ApplyAllocationWindowFn)[1].Arguments[5]);
        Assert.AreEqual("current3", (await ResolveAsync(component, "stale", "current3")).Single().Id);

        // And a request the first load's engine sent is not fetched at all once the second one started.
        await component.InvokeAsync(() => component.Instance._RequestAllocationWindow(6, 0, 2_000_000, firstSession));
        Assert.AreEqual(3, asked);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldDropAWindowANewerRequestSuperseded()
    {
        Context.JSInterop.Setup<long[]>(GetVisibleWindowFn, _ => true).SetResult([0L, 3_600_000L]);

        var pending = new Queue<TaskCompletionSource<IReadOnlyList<BitResourceTimelineAllocation>>>();
        var asked = 0;

        var component = RenderLoaded(p => p.Add(x => x.LoadAllocationsAsync, _ =>
        {
            if (++asked == 1) return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>>([]);

            var fetch = new TaskCompletionSource<IReadOnlyList<BitResourceTimelineAllocation>>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending.Enqueue(fetch);
            return fetch.Task;
        }));

        // The first request is fetching when a scroll asks for a newer window, which waits its turn.
        var first = component.InvokeAsync(() => component.Instance._RequestAllocationWindow(1, 0, 1_000_000, WindowSession()));
        var second = component.InvokeAsync(() => component.Instance._RequestAllocationWindow(2, 0, 2_000_000, WindowSession()));
        await component.InvokeAsync(() => { });

        Assert.AreEqual(1, pending.Count);

        pending.Dequeue().SetResult([Allocation("old", "r1", 0, 60_000)]);
        await first;

        await WaitUntil(() => pending.Count == 1);
        pending.Dequeue().SetResult([Allocation("new", "r1", 0, 60_000)]);
        await second;

        // The window of the superseded request never reaches the engine, nor the bars the component resolves.
        CollectionAssert.AreEqual(new long[] { 0, 2 }, Calls(ApplyAllocationWindowFn).Select(c => (long)c.Arguments[1]!).ToArray());
        Assert.AreEqual("new", (await ResolveAsync(component, "old", "new")).Single().Id);
        Assert.AreEqual(3, asked);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldIgnoreAWindowRequestWhenNotWindowed()
    {
        var component = RenderLoaded();

        await component.InvokeAsync(() => component.Instance._RequestAllocationWindow(1, 0, 1_000, 1));

        Assert.AreEqual(0, Calls(ApplyAllocationWindowFn).Count);
    }



    // ---------------------------------------------------------------- the methods

    [TestMethod]
    public async Task BitResourceTimelineMethodsShouldDoNothingBeforeTheEngineStarted()
    {
        // The setup is not answered, as in a prerender.
        var component = RenderTimeline();
        var before = Context.JSInterop.Invocations.Count;

        await component.InvokeAsync(async () =>
        {
            await component.Instance.ClearSelectionAsync();
            Assert.AreEqual(0, await component.Instance.GetPixelsPerHourAsync());
            Assert.AreEqual(0, (await component.Instance.GetSelectedBarsAsync()).Count);
            Assert.IsFalse(await component.Instance.GoToTodayAsync());
            Assert.IsFalse(await component.Instance.PanByDaysAsync(1));
            await component.Instance.RemoveAllocationsAsync(["a1"]);
            Assert.AreEqual(0, await component.Instance.ResetZoomAsync());
            Assert.IsFalse(await component.Instance.ScrollToAllocationAsync("a1"));
            Assert.IsFalse(await component.Instance.ScrollToResourceAsync("r1"));
            Assert.IsFalse(await component.Instance.ScrollToTimeAsync(DateTimeOffset.UnixEpoch));
            await component.Instance.SelectAsync(["a1"]);
            Assert.AreEqual(0, await component.Instance.SetPixelsPerHourAsync(50));
            await component.Instance.UpsertAllocationsAsync([Allocation("a2", "r1", 0, 1)]);
            Assert.AreEqual(0, await component.Instance.ZoomInAsync());
            Assert.AreEqual(0, await component.Instance.ZoomOutAsync());
            Assert.AreEqual(0, await component.Instance.ZoomToDaysAsync(3));
            await component.Instance.ReloadAsync();
        });

        Assert.AreEqual(before, Context.JSInterop.Invocations.Count, string.Join(", ", Context.JSInterop.Invocations.Select(i => i.Identifier)));
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldZoomBeforeItHoldsDataButNotTouchItsBars()
    {
        PlanStart();
        Context.JSInterop.Setup<double>(ZoomInFn, _ => true).SetResult(80);

        var component = RenderComponent<BitResourceTimeline>();

        double zoom = 0;
        var scrolled = true;
        await component.InvokeAsync(async () =>
        {
            zoom = await component.Instance.ZoomInAsync();
            scrolled = await component.Instance.ScrollToAllocationAsync("a1");
            await component.Instance.SelectAsync(["a1"]);
        });

        Assert.AreEqual(80, zoom);
        Assert.IsFalse(scrolled);
        Assert.AreEqual(0, Calls(ScrollToAllocationFn).Count);
        Assert.AreEqual(0, Calls(SelectBarsFn).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineMethodsShouldCallTheEngineOfTheirInstance()
    {
        Context.JSInterop.Setup<double>(GetPixelsPerHourFn, _ => true).SetResult(42.5);
        Context.JSInterop.Setup<bool>(GoToNowFn, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(PanByDaysFn, _ => true).SetResult(true);
        Context.JSInterop.Setup<double>(ResetZoomFn, _ => true).SetResult(30);
        Context.JSInterop.Setup<bool>(ScrollToAllocationFn, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(ScrollToResourceFn, _ => true).SetResult(true);
        Context.JSInterop.Setup<bool>(ScrollToTimeFn, _ => true).SetResult(true);
        Context.JSInterop.Setup<double>(SetPixelsPerHourFn, _ => true).SetResult(60);
        Context.JSInterop.Setup<double>(ZoomInFn, _ => true).SetResult(80);
        Context.JSInterop.Setup<double>(ZoomOutFn, _ => true).SetResult(20);
        Context.JSInterop.Setup<double>(ZoomToDaysFn, _ => true).SetResult(12.5);

        var component = RenderLoaded();
        var timeline = component.Instance;
        var id = timeline.UniqueId;
        var time = new DateTimeOffset(2026, 5, 4, 12, 0, 0, TimeSpan.FromHours(3.5));

        await component.InvokeAsync(async () =>
        {
            await timeline.ClearSelectionAsync();
            Assert.AreEqual(42.5, await timeline.GetPixelsPerHourAsync());
            Assert.IsTrue(await timeline.GoToTodayAsync());
            Assert.IsTrue(await timeline.PanByDaysAsync(-7));
            Assert.IsTrue(await timeline.PanByDaysAsync(1, panToDayStart: true));
            Assert.AreEqual(30, await timeline.ResetZoomAsync());
            Assert.IsTrue(await timeline.ScrollToAllocationAsync("a1"));
            Assert.IsTrue(await timeline.ScrollToResourceAsync("r1"));
            Assert.IsTrue(await timeline.ScrollToTimeAsync(time));
            await timeline.SelectAsync(["a1"], additive: true);
            Assert.AreEqual(60, await timeline.SetPixelsPerHourAsync(null));
            Assert.AreEqual(60, await timeline.SetPixelsPerHourAsync(55.5));
            Assert.AreEqual(80, await timeline.ZoomInAsync());
            Assert.AreEqual(20, await timeline.ZoomOutAsync());
            Assert.AreEqual(12.5, await timeline.ZoomToDaysAsync(3));
        });

        AssertCall(ClearSelectionFn, 0, id);
        AssertCall(GetPixelsPerHourFn, 0, id);
        AssertCall(GoToNowFn, 0, id);
        AssertCall(PanByDaysFn, 0, id, -7, null);
        AssertCall(PanByDaysFn, 1, id, 1, true);
        AssertCall(ResetZoomFn, 0, id);
        AssertCall(ScrollToAllocationFn, 0, id, "a1");
        AssertCall(ScrollToResourceFn, 0, id, "r1");
        // The time is sent as the Unix milliseconds of the instant, whatever offset it was written with.
        AssertCall(ScrollToTimeFn, 0, id, time.ToUnixTimeMilliseconds());
        AssertCall(SetPixelsPerHourFn, 0, id, null);
        AssertCall(SetPixelsPerHourFn, 1, id, 55.5);
        AssertCall(ZoomInFn, 0, id);
        AssertCall(ZoomOutFn, 0, id);
        AssertCall(ZoomToDaysFn, 0, id, 3d);

        var select = Calls(SelectBarsFn).Single();
        Assert.AreEqual(id, select.Arguments[0]);
        CollectionAssert.AreEqual(new[] { "a1" }, Arg<IReadOnlyList<string>>(select, 1).ToArray());
        Assert.AreEqual(true, select.Arguments[2]);

        void AssertCall(string identifier, int index, params object?[] arguments)
        {
            CollectionAssert.AreEqual(arguments, Calls(identifier)[index].Arguments.ToArray(), identifier);
        }
    }

    [TestMethod]
    public async Task BitResourceTimelineMethodsShouldRefuseAnEmptyArgumentWithoutACall()
    {
        var component = RenderLoaded();

        await component.InvokeAsync(async () =>
        {
            Assert.IsFalse(await component.Instance.ScrollToAllocationAsync(""));
            Assert.IsFalse(await component.Instance.ScrollToResourceAsync(""));
            await component.Instance.RemoveAllocationsAsync([]);
            await component.Instance.UpsertAllocationsAsync([]);
            await component.Instance.SelectAsync(null!);
        });

        Assert.AreEqual(0, Calls(ScrollToAllocationFn).Count);
        Assert.AreEqual(0, Calls(ScrollToResourceFn).Count);
        Assert.AreEqual(0, Calls(RemoveAllocationsFn).Count);
        Assert.AreEqual(0, Calls(UpsertAllocationsFn).Count);
        Assert.AreEqual(0, Calls(SelectBarsFn).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldUpsertAllocationsIntoTheEngine()
    {
        var config = SampleConfig();
        var component = RenderLoaded(config: config);

        var extra = Allocation("a2", "r1", 120_000, 180_000);
        var replacement = Allocation("a1", "r1", 0, 90_000);
        IReadOnlyList<BitResourceTimelineAllocation> upserted = [extra, replacement];

        await component.InvokeAsync(() => component.Instance.UpsertAllocationsAsync(upserted));

        var upsert = Calls(UpsertAllocationsFn).Single();
        Assert.AreEqual(component.Instance.UniqueId, upsert.Arguments[0]);
        Assert.AreSame(upserted, upsert.Arguments[1]);

        // The new bar and the instance replacing one of the same id are what the ids resolve to from now on.
        var resolved = await ResolveAsync(component, "a1", "a2");
        Assert.AreSame(replacement, resolved[0]);
        Assert.AreSame(extra, resolved[1]);
        // The Config is not changed.
        Assert.AreEqual(1, config.Allocations.Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRemoveAllocationsFromTheEngine()
    {
        var component = RenderLoaded();

        await component.InvokeAsync(() => component.Instance.RemoveAllocationsAsync(["a1", "unknown"]));

        var remove = Calls(RemoveAllocationsFn).Single();
        Assert.AreEqual(component.Instance.UniqueId, remove.Arguments[0]);
        CollectionAssert.AreEqual(new[] { "a1", "unknown" }, Arg<IReadOnlyList<string>>(remove, 1).ToArray());

        Assert.AreEqual(0, (await ResolveAsync(component, "a1")).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldReloadWhenTheEngineFailsToApplyABatch()
    {
        var config = SampleConfig();
        var component = RenderLoaded(p => p.Add(x => x.LoadingMinDurationMs, 0), config);

        Context.JSInterop.SetupVoid(UpsertAllocationsFn, _ => true).SetException(new JSException("upsert failed"));
        Context.JSInterop.SetupVoid(RemoveAllocationsFn, _ => true).SetException(new JSException("remove failed"));

        // The engine may have taken a part of the batch: the caller is told it failed, and the data is loaded again from
        // the Config, which also drops what the component took of the batch.
        await Assert.ThrowsExactlyAsync<JSException>(() =>
            component.InvokeAsync(() => component.Instance.UpsertAllocationsAsync([Allocation("a2", "r1", 0, 1)])));

        WaitForLoads(component, 2);
        Assert.AreSame(config.Allocations, Calls(SetDataFn)[1].Arguments[4]);
        Assert.AreEqual(0, (await ResolveAsync(component, "a2")).Count);

        await Assert.ThrowsExactlyAsync<JSException>(() =>
            component.InvokeAsync(() => component.Instance.RemoveAllocationsAsync(["a1"])));

        WaitForLoads(component, 3);
        Assert.AreEqual(3, Calls(SetDataFn).Count);
        Assert.AreEqual(1, (await ResolveAsync(component, "a1")).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldSelectBarsInTheEngine()
    {
        var component = RenderLoaded();

        await component.InvokeAsync(() => component.Instance.SelectAsync(["a1"]));

        var select = Calls(SelectBarsFn).Single();
        Assert.AreEqual(component.Instance.UniqueId, select.Arguments[0]);
        Assert.AreEqual(false, select.Arguments[2]);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldResolveTheSelectedBarsToTheInstancesOfTheApp()
    {
        var config = SampleConfig();
        config.Allocations.Add(Allocation("a2", "r1", 120_000, 180_000));

        var component = RenderLoaded(config: config);

        var selected = await ResolveAsync(component, "a2", "unknown", "a1");

        Assert.AreEqual(2, selected.Count);
        Assert.AreSame(config.Allocations[1], selected[0]);
        Assert.AreSame(config.Allocations[0], selected[1]);
        Assert.AreEqual(component.Instance.UniqueId, Calls(GetSelectedBarIdsFn).Single().Arguments[0]);
    }



    // ---------------------------------------------------------------- editing callbacks

    [TestMethod]
    public async Task BitResourceTimelineShouldAskBeforeAnEditAndRestoreARefusedOne()
    {
        var config = SampleConfig();
        var allocation = config.Allocations[0];

        string? resourceDuring = null;
        DateTimeOffset? startDuring = null;
        DateTimeOffset? endDuring = null;
        BitResourceTimelineAllocationChange? seen = null;

        var component = RenderLoaded(p => p.Add(x => x.OnAllocationChanging, change =>
        {
            seen = change;
            resourceDuring = change.Allocation.ResourceId;
            startDuring = change.Allocation.StartTime;
            endDuring = change.Allocation.EndTime;
            return Task.FromResult(false);
        }), config);

        var allowed = true;
        await component.InvokeAsync(async () =>
        {
            allowed = await component.Instance._OnAllocationChanging("a1", "r2", 10_000, 70_000, "r1", 0, 60_000, "move");
        });

        Assert.IsFalse(allowed);
        Assert.AreSame(allocation, config.Allocations[0]);
        Assert.AreSame(allocation, seen!.Allocation);

        // The handler sees the preview on the very instance, and the previous values beside it.
        Assert.AreEqual("r2", resourceDuring);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(10_000), startDuring);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(70_000), endDuring);
        Assert.AreEqual("r1", seen.PreviousResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(0), seen.PreviousStartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(60_000), seen.PreviousEndTime);
        Assert.AreEqual(BitResourceTimelineAllocationChangeKind.Move, seen.Kind);

        // Refused, so the instance is back where it was.
        Assert.AreEqual("r1", allocation.ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(0), allocation.StartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(60_000), allocation.EndTime);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldAcceptAnEditWithoutAChangingHandler()
    {
        var component = RenderLoaded();

        var allowed = false;
        await component.InvokeAsync(async () =>
        {
            allowed = await component.Instance._OnAllocationChanging("a1", "r1", 10_000, 70_000, "r1", 0, 60_000, "resize");
        });

        Assert.IsTrue(allowed);
    }

    [TestMethod, DataRow("move", BitResourceTimelineAllocationChangeKind.Move),
        DataRow("resize", BitResourceTimelineAllocationChangeKind.Resize),
        DataRow("create", BitResourceTimelineAllocationChangeKind.Create),
        DataRow("Resize", BitResourceTimelineAllocationChangeKind.Resize),
        DataRow("something-new", BitResourceTimelineAllocationChangeKind.Move)]
    public async Task BitResourceTimelineShouldKeepAnAcceptedEditOnTheInstance(string kind, BitResourceTimelineAllocationChangeKind expected)
    {
        var config = SampleConfig();
        BitResourceTimelineAllocationChange? seen = null;

        var component = RenderLoaded(p => p.Add(x => x.OnAllocationChanging, change =>
        {
            seen = change;
            return Task.FromResult(true);
        }), config);

        var allowed = false;
        await component.InvokeAsync(async () =>
        {
            allowed = await component.Instance._OnAllocationChanging("a1", "r2", 10_000, 70_000, "r1", 0, 60_000, kind);
        });

        Assert.IsTrue(allowed);
        Assert.AreEqual(expected, seen!.Kind);
        Assert.AreEqual("r2", config.Allocations[0].ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(10_000), config.Allocations[0].StartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(70_000), config.Allocations[0].EndTime);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRefuseAnEditOfAnUnknownBar()
    {
        var asked = false;
        var component = RenderLoaded(p => p.Add(x => x.OnAllocationChanging, _ =>
        {
            asked = true;
            return Task.FromResult(true);
        }));

        var allowed = true;
        await component.InvokeAsync(async () =>
        {
            allowed = await component.Instance._OnAllocationChanging("unknown", "r2", 10_000, 70_000, "r1", 0, 60_000, "move");
        });

        Assert.IsFalse(allowed);
        Assert.IsFalse(asked);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRestoreAnEditWhoseHandlerThrows()
    {
        var config = SampleConfig();
        var component = RenderLoaded(p => p.Add(x => x.OnAllocationChanging, _ => throw new InvalidOperationException("boom")), config);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            component.InvokeAsync(async () => await component.Instance._OnAllocationChanging("a1", "r2", 10_000, 70_000, "r1", 0, 60_000, "move")));

        Assert.AreEqual("r1", config.Allocations[0].ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(0), config.Allocations[0].StartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(60_000), config.Allocations[0].EndTime);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldAskOnceForAnEditOfSeveralBarsAndRestoreThemAllWhenRefused()
    {
        var config = SampleConfig();
        config.Allocations.Add(Allocation("a2", "r1", 120_000, 180_000));

        IReadOnlyList<BitResourceTimelineAllocationChange>? seen = null;
        var during = new List<(string ResourceId, DateTimeOffset Start, DateTimeOffset End)>();

        var component = RenderLoaded(p => p.Add(x => x.OnAllocationsChanging, changes =>
        {
            seen = changes;
            during.AddRange(changes.Select(c => (c.Allocation.ResourceId, c.Allocation.StartTime, c.Allocation.EndTime)));
            return Task.FromResult(false);
        }), config);

        var allowed = true;
        await component.InvokeAsync(async () =>
        {
            allowed = await component.Instance._OnAllocationsChanging(["a1", "a2"], ["r2", "r2"], [10_000, 130_000], [70_000, 190_000],
                                                                      ["r1", "r1"], [0, 120_000], [60_000, 180_000], "resize");
        });

        Assert.IsFalse(allowed);
        Assert.AreEqual(2, seen!.Count);
        Assert.AreSame(config.Allocations[0], seen[0].Allocation);
        Assert.AreSame(config.Allocations[1], seen[1].Allocation);
        Assert.IsTrue(seen.All(c => c.Kind == BitResourceTimelineAllocationChangeKind.Resize));
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(120_000), seen[1].PreviousStartTime);
        Assert.AreEqual(("r2", DateTimeOffset.FromUnixTimeMilliseconds(130_000), DateTimeOffset.FromUnixTimeMilliseconds(190_000)), during[1]);

        Assert.AreEqual("r1", config.Allocations[0].ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(0), config.Allocations[0].StartTime);
        Assert.AreEqual("r1", config.Allocations[1].ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(120_000), config.Allocations[1].StartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(180_000), config.Allocations[1].EndTime);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRefuseAnEditOfSeveralBarsWithAnUnknownOneAndRestoreThePreviewedOnes()
    {
        var config = SampleConfig();
        var asked = false;

        var component = RenderLoaded(p => p.Add(x => x.OnAllocationsChanging, _ =>
        {
            asked = true;
            return Task.FromResult(true);
        }), config);

        var allowed = true;
        await component.InvokeAsync(async () =>
        {
            allowed = await component.Instance._OnAllocationsChanging(["a1", "unknown"], ["r2", "r2"], [10_000, 0], [70_000, 1],
                                                                      ["r1", "r1"], [0, 0], [60_000, 1], "move");
        });

        Assert.IsFalse(allowed);
        Assert.IsFalse(asked);
        Assert.AreEqual("r1", config.Allocations[0].ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(0), config.Allocations[0].StartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(60_000), config.Allocations[0].EndTime);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRestoreAnEditOfSeveralBarsWhoseHandlerThrows()
    {
        var config = SampleConfig();
        var component = RenderLoaded(p => p.Add(x => x.OnAllocationsChanging, _ => throw new InvalidOperationException("boom")), config);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            component.InvokeAsync(async () => await component.Instance._OnAllocationsChanging(["a1"], ["r2"], [10_000], [70_000], ["r1"], [0], [60_000], "move")));

        Assert.AreEqual("r1", config.Allocations[0].ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(0), config.Allocations[0].StartTime);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldHandAnEditOfOneBarToTheSingleBarHandler()
    {
        var config = SampleConfig();
        BitResourceTimelineAllocationChange? seen = null;

        var component = RenderLoaded(p => p.Add(x => x.OnAllocationChanging, change =>
        {
            seen = change;
            return Task.FromResult(false);
        }), config);

        var allowed = true;
        var several = true;
        var none = true;
        await component.InvokeAsync(async () =>
        {
            allowed = await component.Instance._OnAllocationsChanging(["a1"], ["r2"], [10_000], [70_000], ["r1"], [0], [60_000], "move");
            // Several bars with only the single-bar handler set are accepted, and an empty commit is not one.
            several = await component.Instance._OnAllocationsChanging(["a1", "a1"], ["r2", "r2"], [1, 1], [2, 2], ["r1", "r1"], [0, 0], [60_000, 60_000], "move");
            none = await component.Instance._OnAllocationsChanging([], [], [], [], [], [], [], "move");
        });

        Assert.IsFalse(allowed);
        Assert.AreSame(config.Allocations[0], seen!.Allocation);
        Assert.AreEqual("r1", config.Allocations[0].ResourceId);
        Assert.IsTrue(several);
        Assert.IsFalse(none);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldAcceptAnEditOfSeveralBarsWithoutAHandler()
    {
        var component = RenderLoaded();

        var allowed = false;
        await component.InvokeAsync(async () =>
        {
            allowed = await component.Instance._OnAllocationsChanging(["a1"], ["r2"], [10_000], [70_000], ["r1"], [0], [60_000], "move");
        });

        Assert.IsTrue(allowed);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRefuseAnEditOfAnUnknownBarWithoutAHandler()
    {
        var component = RenderLoaded();

        var single = true;
        var several = true;
        await component.InvokeAsync(async () =>
        {
            single = await component.Instance._OnAllocationChanging("unknown", "r2", 10_000, 70_000, "r1", 0, 60_000, "move");
            several = await component.Instance._OnAllocationsChanging(["a1", "unknown"], ["r2", "r2"], [10_000, 0], [70_000, 1],
                                                                      ["r1", "r1"], [0, 0], [60_000, 1], "move");
        });

        Assert.IsFalse(single);
        Assert.IsFalse(several);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldApplyACommittedEditToTheInstanceAndRaiseOnAllocationChanged()
    {
        var config = SampleConfig();
        var changed = new List<BitResourceTimelineAllocation>();

        var component = RenderLoaded(p => p.Add(x => x.OnAllocationChanged, (BitResourceTimelineAllocation a) => changed.Add(a)), config);

        await component.InvokeAsync(() => component.Instance._OnAllocationEdited("a1", "r2", 10_000, 70_000));
        await component.InvokeAsync(() => component.Instance._OnAllocationEdited("unknown", "r2", 10_000, 70_000));

        Assert.AreSame(config.Allocations[0], changed.Single());
        Assert.AreEqual("r2", config.Allocations[0].ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(10_000), config.Allocations[0].StartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(70_000), config.Allocations[0].EndTime);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldAskBeforeADelete()
    {
        var config = SampleConfig();
        config.Allocations.Add(Allocation("a2", "r1", 120_000, 180_000));

        var answer = false;
        var asked = new List<IReadOnlyList<string>>();

        var component = RenderLoaded(p => p.Add(x => x.OnAllocationsDeleting, ids =>
        {
            asked.Add(ids);
            return Task.FromResult(answer);
        }), config);

        var deleted = true;
        await component.InvokeAsync(async () => deleted = await component.Instance._OnAllocationsDeleting(["a1"]));

        Assert.IsFalse(deleted);
        CollectionAssert.AreEqual(new[] { "a1" }, asked.Single().ToArray());
        Assert.AreEqual(1, (await ResolveAsync(component, "a1")).Count);

        answer = true;
        await component.InvokeAsync(async () => deleted = await component.Instance._OnAllocationsDeleting(["a1"]));

        Assert.IsTrue(deleted);
        Assert.AreEqual("a2", (await ResolveAsync(component, "a1", "a2")).Single().Id);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldAcceptADeleteWithoutAHandler()
    {
        var component = RenderLoaded();

        var deleted = false;
        await component.InvokeAsync(async () => deleted = await component.Instance._OnAllocationsDeleting(["a1"]));

        Assert.IsTrue(deleted);
        Assert.AreEqual(0, (await ResolveAsync(component, "a1")).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldAskTheAppForThePastedBars()
    {
        var config = SampleConfig();
        BitResourceTimelineCopyRequest? request = null;
        var pasted = Allocation("c1", "r2", 3_600_000, 3_660_000);

        var component = RenderLoaded(p => p.Add(x => x.OnAllocationsCopying, r =>
        {
            request = r;
            return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>?>([pasted, Allocation("", "r2", 0, 1)]);
        }), config);

        IReadOnlyList<BitResourceTimelineAllocation>? created = null;
        await component.InvokeAsync(async () => created = await component.Instance._OnAllocationsCopying(["a1", "unknown"], 3_600_000, "r2"));

        Assert.AreSame(config.Allocations[0], request!.Allocations.Single());
        Assert.AreEqual(TimeSpan.FromHours(1), request.Offset);
        Assert.AreEqual("r2", request.ResourceId);

        // A bar without an id is left out: the timeline never invents ids.
        Assert.AreSame(pasted, created!.Single());
        Assert.AreSame(pasted, (await ResolveAsync(component, "c1")).Single());
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldCancelAPasteTheAppDoesNotAnswer()
    {
        var asked = 0;
        var component = RenderLoaded(p => p.Add(x => x.OnAllocationsCopying, _ =>
        {
            asked++;
            return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>?>(null);
        }));

        IReadOnlyList<BitResourceTimelineAllocation>? created = [];
        await component.InvokeAsync(async () => created = await component.Instance._OnAllocationsCopying(["a1"], 0, null));
        Assert.IsNull(created);
        Assert.AreEqual(1, asked);

        // Nothing of the clipboard is known any more: the app is not asked.
        await component.InvokeAsync(async () => created = await component.Instance._OnAllocationsCopying(["unknown"], 0, null));
        Assert.IsNull(created);
        Assert.AreEqual(1, asked);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldCancelAPasteWithoutAHandler()
    {
        var component = RenderLoaded();

        IReadOnlyList<BitResourceTimelineAllocation>? created = [];
        await component.InvokeAsync(async () => created = await component.Instance._OnAllocationsCopying(["a1"], 0, null));

        Assert.IsNull(created);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldHandTheCreatingHandlerTheResourceAndTheRange()
    {
        BitResourceTimelineCreateRequest? captured = null;

        var component = RenderLoaded(p => p.Add(x => x.OnAllocationCreating, request =>
        {
            captured = request;
            return Task.FromResult<BitResourceTimelineAllocation?>(new()
            {
                Id = "new-1",
                ResourceId = request.ResourceId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
            });
        }));

        BitResourceTimelineAllocation? created = null;
        await component.InvokeAsync(async () => created = await component.Instance._OnAllocationCreating("r1", 10_000, 70_000));

        Assert.IsNotNull(created);
        Assert.AreEqual("new-1", created.Id);
        Assert.AreEqual("r1", captured!.ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(10_000), captured.StartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(70_000), captured.EndTime);

        // The created bar is one of the bars of the timeline now.
        Assert.AreSame(created, (await ResolveAsync(component, "new-1")).Single());
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldNotAddABarTheCreatingHandlerCancelled()
    {
        var component = RenderLoaded(p => p.Add(x => x.OnAllocationCreating, _ => Task.FromResult<BitResourceTimelineAllocation?>(null)));

        BitResourceTimelineAllocation? created = Allocation("x", "r1", 0, 1);
        await component.InvokeAsync(async () => created = await component.Instance._OnAllocationCreating("r1", 10_000, 70_000));

        Assert.IsNull(created);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldNotAddACreatedBarWithoutAnIdOrWithoutAHandler()
    {
        var component = RenderLoaded(p => p.Add(x => x.OnAllocationCreating, request => Task.FromResult<BitResourceTimelineAllocation?>(Allocation("", request.ResourceId, 0, 1))));

        BitResourceTimelineAllocation? created = Allocation("x", "r1", 0, 1);
        await component.InvokeAsync(async () => created = await component.Instance._OnAllocationCreating("r1", 10_000, 70_000));
        Assert.IsNull(created);

        component.Render(p => p.Add(x => x.OnAllocationCreating, null));

        created = Allocation("x", "r1", 0, 1);
        await component.InvokeAsync(async () => created = await component.Instance._OnAllocationCreating("r1", 10_000, 70_000));
        Assert.IsNull(created);
    }



    // ---------------------------------------------------------------- pointer, selection and view callbacks

    [TestMethod]
    public async Task BitResourceTimelineShouldRaiseOnSelectionChangedWithTheInstancesOfTheApp()
    {
        var config = SampleConfig();
        config.Allocations.Add(Allocation("a2", "r1", 120_000, 180_000));
        var selections = new List<BitResourceTimelineAllocation[]>();

        var component = RenderLoaded(p => p.Add(x => x.OnSelectionChanged, (BitResourceTimelineAllocation[] s) => selections.Add(s)), config);

        await component.InvokeAsync(() => component.Instance._OnSelectionUpdated(["a2", "unknown", "a1"]));
        await component.InvokeAsync(() => component.Instance._OnSelectionUpdated([]));

        Assert.AreEqual(2, selections.Count);
        CollectionAssert.AreEqual(new[] { config.Allocations[1], config.Allocations[0] }, selections[0]);
        Assert.AreEqual(0, selections[1].Length);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRaiseOnViewChanged()
    {
        BitResourceTimelineView? view = null;

        var component = RenderLoaded(p => p.Add(x => x.OnViewChanged, (BitResourceTimelineView v) => view = v));

        await component.InvokeAsync(() => component.Instance._OnViewChanged(0, 3_600_000, 40));

        Assert.IsNotNull(view);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(0), view.Start);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(3_600_000), view.End);
        Assert.AreEqual(40, view.PixelsPerHour);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldResolveTheBarTheResourceTheTimeAndThePositionOfAClick()
    {
        var config = SampleConfig();
        BitResourceTimelinePointerArgs? args = null;

        var component = RenderLoaded(p => p.Add(x => x.OnClick, (BitResourceTimelinePointerArgs a) => args = a), config);

        await component.InvokeAsync(() => component.Instance._OnTimelineClick("a1", null, "r1", 30_000, "content", 220, 90, 480, 310,
                                                                              true, false, false, true));

        Assert.IsNotNull(args);
        Assert.AreSame(config.Allocations[0], args.Allocation);
        Assert.AreSame(config.Resources[0], args.Resource);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(30_000), args.Time);
        Assert.AreEqual(BitResourceTimelineHitArea.Content, args.Area);
        Assert.AreEqual(220, args.X);
        Assert.AreEqual(90, args.Y);
        Assert.AreEqual(480, args.ClientX);
        Assert.AreEqual(310, args.ClientY);
        Assert.IsTrue(args.CtrlKey);
        Assert.IsFalse(args.ShiftKey);
        Assert.IsFalse(args.MetaKey);
        Assert.IsTrue(args.AltKey);
        Assert.AreEqual(0, args.OverflowAllocations.Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldResolveTheHiddenBarsAndTheHitAreaOfADoubleClick()
    {
        var config = SampleConfig();
        config.Allocations.Add(Allocation("a2", "r1", 120_000, 180_000));
        BitResourceTimelinePointerArgs? args = null;

        var component = RenderLoaded(p => p.Add(x => x.OnDoubleClick, (BitResourceTimelinePointerArgs a) => args = a), config);

        await component.InvokeAsync(() => component.Instance._OnTimelineDoubleClick(null, ["a1", "unknown", "a2"], "r1", 90_000, "timeAxis", 400, 12, 500, 40,
                                                                                    false, true, false, false));

        Assert.IsNotNull(args);
        Assert.IsNull(args.Allocation);
        Assert.AreEqual(2, args.OverflowAllocations.Count);
        Assert.AreSame(config.Allocations[0], args.OverflowAllocations[0]);
        Assert.AreSame(config.Allocations[1], args.OverflowAllocations[1]);
        Assert.AreEqual(BitResourceTimelineHitArea.TimeAxis, args.Area);
        Assert.IsTrue(args.ShiftKey);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(90_000), args.Time);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRaiseOnContextMenuWithTheSharedPointerPayload()
    {
        var config = SampleConfig();
        BitResourceTimelineContextMenuArgs? args = null;

        var component = RenderLoaded(p => p.Add(x => x.OnContextMenu, (BitResourceTimelineContextMenuArgs a) => args = a), config);

        await component.InvokeAsync(() => component.Instance._OnTimelineContextMenu("a1", null, "r1", 15_000, "content", 180, 70, 300, 200,
                                                                                    false, false, true, false));

        Assert.IsNotNull(args);
        Assert.AreSame(config.Allocations[0], args.Allocation);
        Assert.AreSame(config.Resources[0], args.Resource);
        Assert.AreEqual(180, args.X);
        Assert.AreEqual(70, args.Y);
        Assert.AreEqual(300, args.ClientX);
        Assert.AreEqual(200, args.ClientY);
        Assert.IsTrue(args.MetaKey);
        Assert.AreEqual(BitResourceTimelineHitArea.Content, args.Area);
    }

    [TestMethod,
        DataRow("content", BitResourceTimelineHitArea.Content),
        DataRow("resourceAxis", BitResourceTimelineHitArea.ResourceAxis),
        DataRow("timeAxis", BitResourceTimelineHitArea.TimeAxis),
        DataRow("corner", BitResourceTimelineHitArea.Corner),
        DataRow("somewhere-new", BitResourceTimelineHitArea.Content),
        DataRow(null, BitResourceTimelineHitArea.Content)]
    public async Task BitResourceTimelineShouldReadTheHitAreaOfAClick(string? area, BitResourceTimelineHitArea expected)
    {
        BitResourceTimelinePointerArgs? args = null;

        var component = RenderLoaded(p => p.Add(x => x.OnClick, (BitResourceTimelinePointerArgs a) => args = a));

        await component.InvokeAsync(() => component.Instance._OnTimelineClick(null, null, null, null, area, 0, 0, 0, 0, false, false, false, false));

        Assert.AreEqual(expected, args!.Area);
        Assert.IsNull(args.Allocation);
        Assert.IsNull(args.Resource);
        Assert.IsNull(args.Time);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldLeaveWhatAClickDoesNotKnowOutOfItsArguments()
    {
        BitResourceTimelinePointerArgs? args = null;

        var component = RenderLoaded(p => p.Add(x => x.OnClick, (BitResourceTimelinePointerArgs a) => args = a));

        await component.InvokeAsync(() => component.Instance._OnTimelineClick("unknown", null, "nobody", 1, "content", 0, 0, 0, 0, false, false, false, false));

        Assert.IsNull(args!.Allocation);
        Assert.IsNull(args.Resource);
    }



    // ---------------------------------------------------------------- the resource template

    [TestMethod]
    public async Task BitResourceTimelineShouldRenderARowPerReportedRow()
    {
        var component = RenderTimeline(p => p.Add(x => x.ResourceTemplate,
            ctx => b => b.AddMarkupContent(0, $"<span class=\"tpl\">{ctx.Resource.Name}</span>")));

        Assert.AreEqual(0, component.FindAll(".bit-rtm-rrw").Count);

        await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json("""
            [
                { "id": "grp", "name": "Group", "depth": 0, "hasChildren": true, "collapsed": false, "height": 0 },
                { "id": "r1", "name": "Resource 1", "depth": 1, "hasChildren": false, "collapsed": false, "height": 0 }
            ]
            """)));

        var rows = component.FindAll(".bit-rtm-rov > .bit-rtm-roi > .bit-rtm-rrw");
        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("Group", rows[0].QuerySelector(".bit-rtm-rct > .tpl")!.TextContent);
        Assert.AreEqual("Resource 1", rows[1].QuerySelector(".bit-rtm-rct > .tpl")!.TextContent);

        // The group row gets an expand/collapse button; the leaf does not.
        Assert.IsNotNull(rows[0].QuerySelector(".bit-rtm-tgl"));
        Assert.IsNull(rows[1].QuerySelector(".bit-rtm-tgl"));
        Assert.AreEqual(1, component.FindAll(".bit-rtm-tgl").Count);

        // A report of other rows replaces them.
        await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json("[]")));

        Assert.AreEqual(0, component.FindAll(".bit-rtm-rrw").Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldIgnoreRowsThatAreNotAList()
    {
        var component = RenderTimeline(p => p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name)));

        await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json(GroupRows)));
        await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json("{\"id\":\"r1\"}")));

        Assert.AreEqual(0, component.FindAll(".bit-rtm-rrw").Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldHandTheResourceTemplateTheResourcesOfTheApp()
    {
        var config = SampleConfig();
        config.Resources.Insert(0, new BitResourceTimelineResource { Id = "grp", Name = "Group" });
        var contexts = new Dictionary<string, BitResourceTimelineRowContext>();

        RenderFragment<BitResourceTimelineRowContext> template = ctx => b =>
        {
            contexts[ctx.Resource.Id] = ctx;
            b.AddContent(0, ctx.Resource.Name);
        };

        var component = RenderTimeline(p => p.Add(x => x.ResourceTemplate, template), config);

        await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json("""
            [
                { "id": "grp", "name": "Group", "depth": 0, "hasChildren": true, "collapsed": true, "height": 0 },
                { "id": "r1", "name": "Resource 1", "depth": 1, "hasChildren": false, "collapsed": false, "height": 0 },
                { "id": "ghost", "name": "Not in the Config", "depth": 2, "hasChildren": false, "collapsed": false, "height": 0 }
            ]
            """)));

        Assert.AreSame(config.Resources[0], contexts["grp"].Resource);
        Assert.IsTrue(contexts["grp"].HasChildren);
        Assert.IsTrue(contexts["grp"].Collapsed);
        Assert.AreEqual(0, contexts["grp"].Depth);

        Assert.AreSame(config.Resources[1], contexts["r1"].Resource);
        Assert.AreEqual(1, contexts["r1"].Depth);
        Assert.IsFalse(contexts["r1"].HasChildren);

        // A row of a resource the Config does not hold (a Config swapped meanwhile) gets one built from the row.
        Assert.AreEqual("Not in the Config", contexts["ghost"].Resource.Name);
        Assert.AreEqual(2, contexts["ghost"].Depth);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldNameItsGroupButtonsThroughItsTexts()
    {
        var component = RenderTimeline(p => p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name)));

        await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json(GroupRows)));

        var toggles = component.FindAll(".bit-rtm-tgl");
        Assert.AreEqual(2, toggles.Count);

        Assert.AreEqual("button", toggles[0].GetAttribute("type"));
        Assert.AreEqual("true", toggles[0].GetAttribute("aria-expanded"));
        Assert.AreEqual("Collapse Group", toggles[0].GetAttribute("aria-label"));
        Assert.IsFalse(toggles[0].QuerySelector(".bit-rtm-tgi")!.ClassList.Contains("bit-rtm-tgc"));
        Assert.AreEqual("true", toggles[0].QuerySelector(".bit-rtm-tgi")!.GetAttribute("aria-hidden"));

        Assert.AreEqual("false", toggles[1].GetAttribute("aria-expanded"));
        Assert.AreEqual("Expand Group 2", toggles[1].GetAttribute("aria-label"));
        Assert.IsTrue(toggles[1].QuerySelector(".bit-rtm-tgi")!.ClassList.Contains("bit-rtm-tgc"));

        component.Render(p => p.Add(x => x.Texts, new BitResourceTimelineTexts { ExpandGroupFormat = "Aufklappen: {0}", CollapseGroupFormat = "Zuklappen: {0}" }));

        toggles = component.FindAll(".bit-rtm-tgl");
        Assert.AreEqual("Zuklappen: Group", toggles[0].GetAttribute("aria-label"));
        Assert.AreEqual("Aufklappen: Group 2", toggles[1].GetAttribute("aria-label"));
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldToggleAGroupThroughItsButton()
    {
        var component = RenderLoaded(p => p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name)));

        await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json(GroupRows)));

        var toggle = component.FindAll(".bit-rtm-tgl")[1];
        Assert.IsFalse(toggle.HasAttribute("disabled"));
        Assert.IsFalse(toggle.HasAttribute("aria-disabled"));

        await toggle.ClickAsync(new());

        var call = Calls(ToggleGroupFn).Single();
        Assert.AreEqual(component.Instance.UniqueId, call.Arguments[0]);
        Assert.AreEqual("grp2", call.Arguments[1]);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldNotToggleAGroupWhileDisabled()
    {
        var component = RenderLoaded(p =>
        {
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
            p.Add(x => x.Disabled, true);
        });

        await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json(GroupRows)));

        var toggle = component.Find(".bit-rtm-tgl");
        Assert.IsTrue(toggle.HasAttribute("disabled"));
        Assert.AreEqual("true", toggle.GetAttribute("aria-disabled"));

        // A script or an assistive technology can still deliver the click to the handler of a disabled button.
        var toggleGroup = typeof(BitResourceTimeline).GetMethod("ToggleGroupAsync", BindingFlags.Instance | BindingFlags.NonPublic)!;
        await component.InvokeAsync(() => (Task)toggleGroup.Invoke(component.Instance, ["grp"])!);

        Assert.AreEqual(0, Calls(ToggleGroupFn).Count);
    }

    [TestMethod, DataRow("fa-IR"), DataRow("ar-SA")]
    public async Task BitResourceTimelineShouldWriteTheSizesAndPositionsOfItsOverlaysInvariantly(string cultureName)
    {
        // ar-SA through TestCultures, with the Um Al Qura calendar it reads dates in, the way the culture contract tests do.
        var culture = cultureName == "ar-SA" ? TestCultures.ArSaUmAlQura : CultureInfo.GetCultureInfo(cultureName);

        await InCultureAsync(culture, async () =>
        {
            var component = RenderLoaded(p =>
            {
                p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
                p.Add(x => x.TooltipTemplate, a => b => b.AddContent(0, a.Id));
                p.Add(x => x.TopStartContent, "Corner");
            }, layout: Layout(150, 60, 36, 20));

            // A height the engine measured in fractions is rounded, a row it does not know the height of takes the minimum
            // height of the layout, and every nesting level indents by the indent of the layout.
            await component.InvokeAsync(() => component.Instance._OnResourceRowsChanged(Json("""
                [
                    { "id": "grp", "name": "Group", "depth": 0, "hasChildren": true, "collapsed": false, "height": 47.6 },
                    { "id": "r1", "name": "Resource 1", "depth": 1, "hasChildren": false, "collapsed": false, "height": 0 },
                    { "id": "r2", "name": "Resource 2", "depth": 2, "hasChildren": false, "collapsed": false, "height": 52 }
                ]
                """)));

            // A pointer position is fractional on most screens, and to the left of or above the viewport it is negative:
            // what a culture writes differently (a decimal comma or an Arabic separator, a minus sign with a bidi mark).
            await component.InvokeAsync(() => component.Instance._OnBarHover("a1", 12.5, -3.75));

            var rows = component.FindAll(".bit-rtm-rrw");
            Assert.AreEqual("height:48px;padding-left:8px", rows[0].GetAttribute("style"));
            Assert.AreEqual("height:36px;padding-left:28px", rows[1].GetAttribute("style"));
            Assert.AreEqual("height:52px;padding-left:48px", rows[2].GetAttribute("style"));
            Assert.AreEqual(FormattableString.Invariant($"left:{12.5}px;top:{-3.75}px"), component.Find(".bit-rtm-tov").GetAttribute("style"));
            Assert.AreEqual("width:150px;height:60px", component.Find(".bit-rtm-cnr").GetAttribute("style"));
            Assert.AreEqual("top:60px;width:150px", component.Find(".bit-rtm-rov").GetAttribute("style"));

            // The names of the group buttons are text for a reader, which stays in the culture.
            var marks = new Regex("[‎‏؜−٫٬]");
            var offenders = component.FindAll("*")
                                     .SelectMany(e => e.Attributes.Select(a => (Element: e, Attribute: a)))
                                     .Where(x => x.Attribute.Name is not ("aria-label" or "aria-roledescription") && marks.IsMatch(x.Attribute.Value))
                                     .Select(x => $"<{x.Element.LocalName} class=\"{x.Element.ClassName}\"> {x.Attribute.Name}=\"{x.Attribute.Value}\"")
                                     .ToList();

            Assert.IsEmpty(offenders, string.Join(Environment.NewLine, offenders));
        });
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldResizeItsCornerAndOverlayWithTheResourceColumn()
    {
        var widths = new List<int>();

        var component = RenderTimeline(p =>
        {
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddMarkupContent(0, "<span class=\"tpl\">row</span>"));
            p.Add(x => x.TopStartContent, "<span>corner</span>");
            p.Add(x => x.OnResourceAxisWidthChanged, (int w) => widths.Add(w));
        });

        await component.InvokeAsync(() => component.Instance._OnResourceAxisResized(220));

        StringAssert.Contains(component.Find(".bit-rtm-rov").GetAttribute("style"), "width:220px");
        StringAssert.Contains(component.Find(".bit-rtm-cnr").GetAttribute("style"), "width:220px");
        Assert.AreEqual(220, widths.Single());

        // The same width again still tells the app the resize was committed.
        await component.InvokeAsync(() => component.Instance._OnResourceAxisResized(220));
        CollectionAssert.AreEqual(new[] { 220, 220 }, widths);
    }



    // ---------------------------------------------------------------- the tooltip template

    [TestMethod]
    public async Task BitResourceTimelineShouldRenderTheHoveredBarThroughTheTooltipTemplate()
    {
        var config = SampleConfig();
        var hovered = new List<BitResourceTimelineAllocation>();

        RenderFragment<BitResourceTimelineAllocation> template = a => b =>
        {
            hovered.Add(a);
            b.AddMarkupContent(0, $"<b class=\"tip\">{a.Id}</b>");
        };

        var component = RenderLoaded(p => p.Add(x => x.TooltipTemplate, template), config);

        Assert.AreEqual(0, component.FindAll(".bit-rtm-tov").Count);

        await component.InvokeAsync(() => component.Instance._OnBarHover("a1", 100, 200));

        var tooltip = component.Find(".bit-rtm > .bit-rtm-tov");
        Assert.AreEqual("a1", tooltip.QuerySelector(".tip")!.TextContent);
        Assert.AreEqual("left:100px;top:200px", tooltip.GetAttribute("style"));
        Assert.AreSame(config.Allocations[0], hovered[^1]);

        // The pointer leaves the bar, or moves onto one the component does not hold.
        await component.InvokeAsync(() => component.Instance._OnBarHover(null, 0, 0));
        Assert.AreEqual(0, component.FindAll(".bit-rtm-tov").Count);

        await component.InvokeAsync(() => component.Instance._OnBarHover("a1", 100, 200));
        await component.InvokeAsync(() => component.Instance._OnBarHover("unknown", 0, 0));
        Assert.AreEqual(0, component.FindAll(".bit-rtm-tov").Count);
    }



    // ---------------------------------------------------------------- disposal

    [TestMethod]
    public async Task BitResourceTimelineShouldDisposeItsEngine()
    {
        var component = RenderLoaded();

        var reference = Arg<DotNetObjectReference<BitResourceTimeline>>(Calls(SetupFn).Single(), 2);

        await DisposeTimelineAsync(component);

        var dispose = Calls(DisposeFn).Single();
        Assert.AreEqual(component.Instance.UniqueId, dispose.Arguments[0]);
        Assert.AreEqual(1, dispose.Arguments.Count);

        // The reference the engine calls back through is released after the engine is gone.
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = reference.Value);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldRunTheScriptTeardownOnce()
    {
        var component = RenderLoaded();

        await DisposeTimelineAsync(component);
        await DisposeTimelineAsync(component);

        Assert.AreEqual(1, Calls(DisposeFn).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldBeDisposedWithItsRendererWithoutThrowing()
    {
        RenderLoaded();

        // The renderer tears the component down; the mocked script lets DisposeAsync complete.
        await Context.DisposeAsync();

        Assert.AreEqual(1, Calls(DisposeFn).Count);

        // The test cleanup disposes a context of its own.
        Context = new BunitContext();
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldStopStartingWhenDisposedDuringTheSetup()
    {
        var setup = Context.JSInterop.PlanLayout(SetupFn);

        var component = RenderTimeline(p =>
        {
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
            p.Add(x => x.TooltipTemplate, a => b => b.AddContent(0, a.Id));
        });

        var reference = Arg<DotNetObjectReference<BitResourceTimeline>>(Calls(SetupFn).Single(), 2);

        await DisposeTimelineAsync(component);

        // The setup returns after the component is gone: nothing more is sent to an engine nobody tears down again.
        await component.InvokeAsync(() => setup.Return(Layout()));
        await Task.Delay(50);
        await component.InvokeAsync(() => { });

        Assert.AreEqual(0, Calls(EnableResourceTemplateFn).Count);
        Assert.AreEqual(0, Calls(EnableTooltipTemplateFn).Count);
        Assert.AreEqual(0, Calls(SetDataFn).Count);
        Assert.AreEqual(1, Calls(DisposeFn).Count);
        Assert.ThrowsExactly<ObjectDisposedException>(() => _ = reference.Value);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldStopALoadInFlightWhenDisposed()
    {
        PlanStart();
        var setData = Context.JSInterop.SetupVoid(SetDataFn, _ => true);

        var component = RenderTimeline();
        await WaitUntil(() => Calls(SetDataFn).Count > 0);

        await DisposeTimelineAsync(component);
        setData.SetVoidResult();

        // The rest of the load is skipped rather than pushed into an engine that no longer exists (and releasing the
        // gate of the load afterwards does not throw either).
        await Task.Delay(100);
        await component.InvokeAsync(() => { });

        Assert.AreEqual(0, Calls(WhenRenderedFn).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldStopStreamingWhenDisposed()
    {
        PlanStart();
        var append = Context.JSInterop.SetupVoid(AppendAllocationsFn, _ => true);

        var config = SampleConfig();
        config.Allocations = Enumerable.Range(1, 5).Select(i => Allocation($"a{i}", "r1", i * 60_000, i * 60_000 + 30_000)).ToList();

        var component = RenderTimeline(p => p.Add(x => x.LoadBatchSize, 2), config);
        await WaitUntil(() => Calls(AppendAllocationsFn).Count == 1);

        await DisposeTimelineAsync(component);
        append.SetVoidResult();

        await Task.Delay(100);
        await component.InvokeAsync(() => { });

        Assert.AreEqual(1, Calls(AppendAllocationsFn).Count);
        Assert.AreEqual(0, Calls(EndDataFn).Count);
        Assert.AreEqual(0, Calls(WhenRenderedFn).Count);
    }

    [TestMethod]
    public async Task BitResourceTimelineCallbacksShouldDoNothingOnceDisposed()
    {
        Context.JSInterop.Setup<long[]>(GetVisibleWindowFn, _ => true).SetResult([0L, 3_600_000L]);

        var config = SampleConfig();
        var allocation = config.Allocations[0];
        var raised = new List<string>();
        var fetches = 0;

        var component = RenderLoaded(p =>
        {
            p.Add(x => x.ResourceTemplate, ctx => b => b.AddContent(0, ctx.Resource.Name));
            p.Add(x => x.TooltipTemplate, a => b => b.AddContent(0, a.Id));
            p.Add(x => x.TopStartContent, "Corner");
            p.Add(x => x.LoadAllocationsAsync, _ =>
            {
                fetches++;
                return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>>(config.Allocations);
            });
            p.Add(x => x.OnAllocationChanged, (BitResourceTimelineAllocation _) => raised.Add(nameof(BitResourceTimeline.OnAllocationChanged)));
            p.Add(x => x.OnAllocationChanging, _ => { raised.Add(nameof(BitResourceTimeline.OnAllocationChanging)); return Task.FromResult(true); });
            p.Add(x => x.OnAllocationCreating, _ => { raised.Add(nameof(BitResourceTimeline.OnAllocationCreating)); return Task.FromResult<BitResourceTimelineAllocation?>(Allocation("n", "r1", 0, 1)); });
            p.Add(x => x.OnAllocationsChanging, _ => { raised.Add(nameof(BitResourceTimeline.OnAllocationsChanging)); return Task.FromResult(true); });
            p.Add(x => x.OnAllocationsCopying, _ => { raised.Add(nameof(BitResourceTimeline.OnAllocationsCopying)); return Task.FromResult<IReadOnlyList<BitResourceTimelineAllocation>?>([Allocation("c", "r1", 0, 1)]); });
            p.Add(x => x.OnAllocationsDeleting, _ => { raised.Add(nameof(BitResourceTimeline.OnAllocationsDeleting)); return Task.FromResult(true); });
            p.Add(x => x.OnClick, (BitResourceTimelinePointerArgs _) => raised.Add(nameof(BitResourceTimeline.OnClick)));
            p.Add(x => x.OnContextMenu, (BitResourceTimelineContextMenuArgs _) => raised.Add(nameof(BitResourceTimeline.OnContextMenu)));
            p.Add(x => x.OnDoubleClick, (BitResourceTimelinePointerArgs _) => raised.Add(nameof(BitResourceTimeline.OnDoubleClick)));
            p.Add(x => x.OnResourceAxisWidthChanged, (int _) => raised.Add(nameof(BitResourceTimeline.OnResourceAxisWidthChanged)));
            p.Add(x => x.OnSelectionChanged, (BitResourceTimelineAllocation[] _) => raised.Add(nameof(BitResourceTimeline.OnSelectionChanged)));
            p.Add(x => x.OnViewChanged, (BitResourceTimelineView _) => raised.Add(nameof(BitResourceTimeline.OnViewChanged)));
        }, config);

        Assert.AreEqual(1, fetches);

        await DisposeTimelineAsync(component);

        var markup = component.Markup;
        var calls = Context.JSInterop.Invocations.Count;
        var timeline = component.Instance;
        var invoked = new HashSet<string>();

        bool changing = true, changingSeveral = true, deleting = true;
        IReadOnlyList<BitResourceTimelineAllocation>? copied = [];
        BitResourceTimelineAllocation? created = allocation;

        await component.InvokeAsync(async () =>
        {
            await timeline._RequestAllocationWindow(1, 0, 7_200_000, 1);
            invoked.Add(nameof(timeline._RequestAllocationWindow));

            timeline._OnResourceRowsChanged(Json(GroupRows));
            invoked.Add(nameof(timeline._OnResourceRowsChanged));

            await timeline._OnSelectionUpdated(["a1"]);
            invoked.Add(nameof(timeline._OnSelectionUpdated));

            await timeline._OnViewChanged(0, 1_000, 10);
            invoked.Add(nameof(timeline._OnViewChanged));

            timeline._OnBarHover("a1", 10, 20);
            invoked.Add(nameof(timeline._OnBarHover));

            await timeline._OnAllocationEdited("a1", "r2", 10_000, 70_000);
            invoked.Add(nameof(timeline._OnAllocationEdited));

            changing = await timeline._OnAllocationChanging("a1", "r2", 10_000, 70_000, "r1", 0, 60_000, "move");
            invoked.Add(nameof(timeline._OnAllocationChanging));

            changingSeveral = await timeline._OnAllocationsChanging(["a1"], ["r2"], [10_000], [70_000], ["r1"], [0], [60_000], "move");
            invoked.Add(nameof(timeline._OnAllocationsChanging));

            deleting = await timeline._OnAllocationsDeleting(["a1"]);
            invoked.Add(nameof(timeline._OnAllocationsDeleting));

            copied = await timeline._OnAllocationsCopying(["a1"], 1_000, null);
            invoked.Add(nameof(timeline._OnAllocationsCopying));

            created = await timeline._OnAllocationCreating("r1", 0, 1_000);
            invoked.Add(nameof(timeline._OnAllocationCreating));

            await timeline._OnTimelineClick("a1", null, "r1", 0, "content", 0, 0, 0, 0, false, false, false, false);
            invoked.Add(nameof(timeline._OnTimelineClick));

            await timeline._OnTimelineDoubleClick("a1", null, "r1", 0, "content", 0, 0, 0, 0, false, false, false, false);
            invoked.Add(nameof(timeline._OnTimelineDoubleClick));

            await timeline._OnTimelineContextMenu("a1", null, "r1", 0, "content", 0, 0, 0, 0, false, false, false, false);
            invoked.Add(nameof(timeline._OnTimelineContextMenu));

            await timeline._OnResourceAxisResized(300);
            invoked.Add(nameof(timeline._OnResourceAxisResized));
        });

        // A callback added to the component is added here too.
        var callbacks = typeof(BitResourceTimeline).GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                                                   .Where(m => m.IsDefined(typeof(JSInvokableAttribute)))
                                                   .Select(m => m.Name)
                                                   .Order(StringComparer.Ordinal)
                                                   .ToArray();
        CollectionAssert.AreEqual(callbacks, invoked.Order(StringComparer.Ordinal).ToArray());

        Assert.IsEmpty(raised, string.Join(", ", raised));
        Assert.AreEqual(1, fetches);
        Assert.IsFalse(changing);
        Assert.IsFalse(changingSeveral);
        Assert.IsFalse(deleting);
        Assert.IsNull(copied);
        Assert.IsNull(created);
        Assert.AreEqual("r1", allocation.ResourceId);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(0), allocation.StartTime);
        Assert.AreEqual(DateTimeOffset.FromUnixTimeMilliseconds(60_000), allocation.EndTime);
        Assert.AreEqual(calls, Context.JSInterop.Invocations.Count, "A callback called into the script after the disposal.");
        Assert.AreEqual(markup, component.Markup, "A callback rendered the component after its disposal.");
    }

    [TestMethod]
    public async Task BitResourceTimelineMethodsShouldDoNothingOnceDisposed()
    {
        var component = RenderLoaded();

        await DisposeTimelineAsync(component);

        var calls = Context.JSInterop.Invocations.Count;

        await component.InvokeAsync(async () =>
        {
            await component.Instance.ClearSelectionAsync();
            Assert.AreEqual(0, await component.Instance.GetPixelsPerHourAsync());
            Assert.AreEqual(0, (await component.Instance.GetSelectedBarsAsync()).Count);
            Assert.IsFalse(await component.Instance.GoToTodayAsync());
            Assert.IsFalse(await component.Instance.PanByDaysAsync(1));
            await component.Instance.RemoveAllocationsAsync(["a1"]);
            Assert.AreEqual(0, await component.Instance.ResetZoomAsync());
            Assert.IsFalse(await component.Instance.ScrollToAllocationAsync("a1"));
            Assert.IsFalse(await component.Instance.ScrollToResourceAsync("r1"));
            Assert.IsFalse(await component.Instance.ScrollToTimeAsync(DateTimeOffset.UnixEpoch));
            await component.Instance.SelectAsync(["a1"]);
            Assert.AreEqual(0, await component.Instance.SetPixelsPerHourAsync(50));
            await component.Instance.UpsertAllocationsAsync([Allocation("a2", "r1", 0, 1)]);
            Assert.AreEqual(0, await component.Instance.ZoomInAsync());
            Assert.AreEqual(0, await component.Instance.ZoomOutAsync());
            Assert.AreEqual(0, await component.Instance.ZoomToDaysAsync(3));
            await component.Instance.ReloadAsync();
        });

        Assert.AreEqual(calls, Context.JSInterop.Invocations.Count, string.Join(", ", Context.JSInterop.Invocations.Skip(calls).Select(i => i.Identifier)));
    }



    // ---------------------------------------------------------------- prerendering

    [TestMethod]
    public async Task BitResourceTimelineShouldPrerenderItsShell()
    {
        var html = await Prerenderer.RenderAsync<BitResourceTimeline>(PrerenderParameters());

        AssertShell(html);
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldPrerenderWithoutCallingTheScript()
    {
        // A runtime that records every call, which the loader does not take for the one of a prerender: a call tried
        // while prerendering would be seen here, where the prerender's own runtime would swallow it.
        var runtime = new RecordingJsRuntime();

        var html = await Prerenderer.RenderAsync<BitResourceTimeline>(PrerenderParameters(), services => services.AddSingleton<IJSRuntime>(runtime));

        // The renderer disposing the prerendered component afterwards runs its teardown, which the runtime of a real
        // prerender refuses (IsRuntimeInvalid) and the lazy loader skips for a script never loaded (bit-lazy: teardown).
        CollectionAssert.AreEqual(new[] { DisposeFn }, runtime.Calls, string.Join(", ", runtime.Calls));
        AssertShell(html);
    }

    private static Dictionary<string, object?> PrerenderParameters() => new()
    {
        [nameof(BitResourceTimeline.Config)] = SampleConfig(),
        [nameof(BitResourceTimeline.AriaLabel)] = "Flight plan",
        [nameof(BitResourceTimeline.TopStartContent)] = (RenderFragment)(b => b.AddContent(0, "Corner;")),
        [nameof(BitResourceTimeline.ResourceTemplate)] = (RenderFragment<BitResourceTimelineRowContext>)(ctx => b => b.AddContent(0, $"Row {ctx.Resource.Name};")),
        [nameof(BitResourceTimeline.TooltipTemplate)] = (RenderFragment<BitResourceTimelineAllocation>)(a => b => b.AddContent(0, $"Tip {a.Id};")),
    };

    private static void AssertShell(string html)
    {
        StringAssert.Contains(html, "class=\"bit-rtm");
        StringAssert.Contains(html, "class=\"bit-rtm-vpt");
        StringAssert.Contains(html, "role=\"application\"");
        StringAssert.Contains(html, "aria-label=\"Flight plan\"");
        StringAssert.Contains(html, "aria-keyshortcuts=\"" + BaseShortcuts + "\"");
        // The corner and the overlay keep the room of the default layout until the engine reports its own.
        StringAssert.Contains(html, "Corner;");
        StringAssert.Contains(html, "width:150px;height:60px");
        StringAssert.Contains(html, "top:60px;width:150px");

        // No row is reported, no bar hovered and no data loading before the engine runs.
        Assert.IsFalse(html.Contains("Row "), html);
        Assert.IsFalse(html.Contains("Tip "), html);
        Assert.IsFalse(html.Contains("bit-rtm-ldg"), html);
        Assert.IsFalse(html.Contains("aria-busy"), html);
    }

    private sealed class RecordingJsRuntime : IJSRuntime
    {
        public List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            lock (Calls) Calls.Add(identifier);

            return new ValueTask<TValue>(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
        {
            lock (Calls) Calls.Add(identifier);

            return new ValueTask<TValue>(default(TValue)!);
        }
    }



    // ---------------------------------------------------------------- the interop surface

    [TestMethod]
    public void BitResourceTimelineShouldCallEveryFunctionOfItsScriptWithItsIdFirst()
    {
        var extensions = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ResourceTimeline", "BitResourceTimelineJsRuntimeExtensions.cs");
        var facade = SourceFiles.Read("Bit.BlazorUI.Extras", "Components", "ResourceTimeline", "BitResourceTimeline.ts");

        var calls = Regex.Matches(extensions, @"jsRuntime\.Invoke(?:Void)?(?:<[^>(]+>)?\(""BitBlazorUI\.ResourceTimeline\.(\w+)""((?:,\s*\w+)*)\)")
                         .ToDictionary(m => m.Groups[1].Value,
                                       m => m.Groups[2].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        var facadeClass = Regex.Match(facade, @"export class ResourceTimeline \{.*?\n    \}\n", RegexOptions.Singleline).Value;
        var functions = Regex.Matches(facadeClass, @"\n        public static (\w+)\(([^)]*)\)")
                             .ToDictionary(m => m.Groups[1].Value,
                                           m => m.Groups[2].Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        Assert.IsGreaterThanOrEqualTo(30, calls.Count, "The calls of BitResourceTimelineJsRuntimeExtensions were not found.");
        CollectionAssert.AreEquivalent(functions.Keys.ToArray(), calls.Keys.ToArray(), "A function of the facade is not called from .NET, or a call names no function of it.");

        foreach (var (name, arguments) in calls)
        {
            var parameters = functions[name];

            Assert.AreEqual("id", arguments[0], $"{name} is not called with the id of the component first.");
            Assert.IsTrue(parameters[0].StartsWith("id: string", StringComparison.Ordinal), $"{name} does not take the id of the component first.");
            Assert.AreEqual(parameters.Length, arguments.Length, $"{name} takes {parameters.Length} arguments and is called with {arguments.Length}.");
        }
    }



    // Both the current and the default thread cultures, since a render can also run on a dispatcher thread of its own.
    private static async Task InCultureAsync(CultureInfo culture, Func<Task> action)
    {
        var (current, currentUI) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
        var (defaultCulture, defaultUICulture) = (CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture);

        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = culture;

            await action();
        }
        finally
        {
            (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture) = (current, currentUI);
            (CultureInfo.DefaultThreadCurrentCulture, CultureInfo.DefaultThreadCurrentUICulture) = (defaultCulture, defaultUICulture);
        }
    }
}

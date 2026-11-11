using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline.BitResourceTimelineTestInterop;

namespace Bit.BlazorUI.Tests.Components.Extras.ResourceTimeline;

/// <summary>
/// Covers the BitParams cascade of the ResourceTimeline: what a BitResourceTimelineParams fills in, what it leaves alone
/// because the timeline wrote it for itself, and that a cascade that goes away takes its values with it.
/// </summary>
[TestClass]
public class BitResourceTimelineParamsTests : BunitTestContext
{
    // What belongs to a single timeline rather than to a group of them: its data, its windowed provider, the templates
    // of its rows, tooltips and corner, and its events and callbacks.
    private static readonly string[] _notCascaded =
    [
        nameof(BitResourceTimeline.CascadingParameters),
        nameof(BitResourceTimeline.Config),
        nameof(BitResourceTimeline.LoadAllocationsAsync),
        nameof(BitResourceTimeline.OnAllocationChanged),
        nameof(BitResourceTimeline.OnAllocationChanging),
        nameof(BitResourceTimeline.OnAllocationCreating),
        nameof(BitResourceTimeline.OnAllocationsChanging),
        nameof(BitResourceTimeline.OnAllocationsCopying),
        nameof(BitResourceTimeline.OnAllocationsDeleting),
        nameof(BitResourceTimeline.OnClick),
        nameof(BitResourceTimeline.OnContextMenu),
        nameof(BitResourceTimeline.OnDoubleClick),
        nameof(BitResourceTimeline.OnResourceAxisWidthChanged),
        nameof(BitResourceTimeline.OnSelectionChanged),
        nameof(BitResourceTimeline.OnViewChanged),
        nameof(BitResourceTimeline.ResourceTemplate),
        nameof(BitResourceTimeline.TooltipTemplate),
        nameof(BitResourceTimeline.TopStartContent),
    ];

    private IReadOnlyList<Bunit.JSRuntimeInvocation> Calls(string identifier) => Context.JSInterop.Invocations[identifier];

    private IRenderedComponent<BitParams> RenderWithParams(BitResourceTimelineParams timelineParams,
                                                           Action<RenderTreeBuilder>? extraAttributes = null,
                                                           BitResourceTimelineConfig? config = null,
                                                           bool start = true)
    {
        if (start)
        {
            Context.JSInterop.PlanLayout(SetupFn).Return(Layout());
        }

        config ??= SampleConfig();

        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { timelineParams });
            parameters.AddChildContent(builder => BuildTimeline(builder, extraAttributes, config));
        });
    }

    private static void BuildTimeline(RenderTreeBuilder builder, Action<RenderTreeBuilder>? extraAttributes, BitResourceTimelineConfig config)
    {
        builder.OpenComponent<BitResourceTimeline>(0);
        builder.AddAttribute(1, nameof(BitResourceTimeline.Config), config);
        extraAttributes?.Invoke(builder);
        builder.CloseComponent();
    }

    private void WaitForLoad(IRenderedComponent<BitParams> component)
    {
        component.WaitForAssertion(() =>
        {
            Assert.ContainsSingle(Calls(WhenRenderedFn));
            Assert.IsEmpty(component.FindAll(".bit-rtm-ldg"));
        }, TimeSpan.FromSeconds(5));
    }

    private static BitResourceTimelineConfig FiveBars()
    {
        var config = SampleConfig();
        config.Allocations = Enumerable.Range(1, 5).Select(i => Allocation($"a{i}", "r1", i * 60_000, i * 60_000 + 30_000)).ToList();
        return config;
    }



    [TestMethod]
    public void BitResourceTimelineParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual("BitParams.BitResourceTimeline", new BitResourceTimelineParams().Name);
        Assert.AreEqual(BitResourceTimelineParams.ParamName, new BitResourceTimelineParams().Name);
        Assert.IsInstanceOfType<IBitComponentParams>(new BitResourceTimelineParams());

        var cascading = typeof(BitResourceTimeline).GetProperty(nameof(BitResourceTimeline.CascadingParameters))!.GetCustomAttribute<CascadingParameterAttribute>();
        Assert.AreEqual(BitResourceTimelineParams.ParamName, cascading!.Name);
    }

    [TestMethod]
    public void BitResourceTimelineParamsShouldCarryEveryParameterThatBelongsToAGroupOfTimelines()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitResourceTimeline).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                    .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                    .Select(p => p.Name)
                                                    .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                    .ToList();

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitResourceTimelineParams).GetProperty(name), $"BitResourceTimelineParams has no {name}.");
        }

        // And nothing it carries is something the timeline does not take.
        var own = typeof(BitResourceTimelineParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                   .Select(p => p.Name)
                                                   .Where(n => n != nameof(BitResourceTimelineParams.Name));

        foreach (var name in own)
        {
            Assert.Contains(name, parameters, $"BitResourceTimelineParams.{name} is no parameter of BitResourceTimeline.");
        }
    }

    [TestMethod]
    public void BitResourceTimelineShouldTakeTheCascadedValues()
    {
        var options = new BitResourceTimelineOptions { Editable = true };
        var texts = new BitResourceTimelineTexts { RoleDescription = "Planer" };

        var component = RenderWithParams(new BitResourceTimelineParams
        {
            Class = "cascaded",
            AriaLabel = "Dienstplan",
            Disabled = true,
            Classes = new() { Root = "cascaded-root", Viewport = "cascaded-viewport" },
            Styles = new() { Root = "margin:1px", Viewport = "margin:2px" },
            Options = options,
            Texts = texts,
        });

        WaitForLoad(component);

        var root = component.Find(".bit-rtm");
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("cascaded-root"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        Assert.Contains("margin:1px", root.GetAttribute("style")!);

        var viewport = component.Find(".bit-rtm-vpt");
        Assert.IsTrue(viewport.ClassList.Contains("cascaded-viewport"));
        Assert.AreEqual("margin:2px", viewport.GetAttribute("style"));
        Assert.AreEqual("Dienstplan", viewport.GetAttribute("aria-label"));
        Assert.AreEqual("Planer", viewport.GetAttribute("aria-roledescription"));
        Assert.AreEqual("true", viewport.GetAttribute("aria-disabled"));
        // Disabled, the editable timeline advertises only the paging keys the engine still handles.
        Assert.AreEqual("PageUp PageDown", viewport.GetAttribute("aria-keyshortcuts"));

        // The engine starts with what the cascade set, and is not configured a second time for it.
        var setup = Calls(SetupFn).Single();
        Assert.AreSame(options, setup.Arguments[3]);
        Assert.AreSame(texts, setup.Arguments[4]);
        Assert.IsTrue(Assert.IsInstanceOfType<bool>(setup.Arguments[5]));
        Assert.IsEmpty(Calls(SetOptionsFn));
    }

    [TestMethod]
    public async Task BitResourceTimelineShouldTakeTheCascadedLoading()
    {
        Context.JSInterop.SetupVoid(AppendAllocationsFn, _ => true);

        var component = RenderWithParams(new BitResourceTimelineParams
        {
            LoadBatchSize = 2,
            LoadingContent = builder => builder.AddContent(0, "Bitte warten"),
        }, config: FiveBars());

        // Polled: a WaitForAssertion timing out would have bUnit fail the append still pending, which ends the load.
        for (var i = 0; i < 500 && await Renderer.Dispatcher.InvokeAsync(() => Calls(AppendAllocationsFn).Count) == 0; i++)
        {
            await Task.Delay(10, TestContext.CancellationToken);
        }

        Assert.AreEqual(5, Calls(BeginDataFn).Single().Arguments[4]);
        Assert.HasCount(2, ((IReadOnlyList<BitResourceTimelineAllocation>)Calls(AppendAllocationsFn)[0].Arguments[1]!));
        Assert.AreEqual("Bitte warten", component.Find(".bit-rtm-ldg").TextContent);
    }

    [TestMethod]
    public void BitResourceTimelineShouldTakeTheCascadedMinimumLoadingDuration()
    {
        // Not started, so no load is held up for the five seconds.
        var component = RenderWithParams(new BitResourceTimelineParams { LoadingMinDurationMs = 5_000 }, start: false);

        var timeline = component.FindComponent<BitResourceTimeline>().Instance;

        Assert.AreEqual(5_000, timeline.LoadingMinDurationMs);
    }

    [TestMethod]
    public void BitResourceTimelineShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var cascadedOptions = new BitResourceTimelineOptions { Editable = true };
        var ownOptions = new BitResourceTimelineOptions { BarHeight = 8 };
        var ownTexts = new BitResourceTimelineTexts { AriaLabel = "Own" };

        var component = RenderWithParams(new BitResourceTimelineParams
        {
            Class = "cascaded",
            Classes = new() { Viewport = "cascaded-viewport" },
            Options = cascadedOptions,
            Texts = new() { AriaLabel = "Cascaded" },
            LoadBatchSize = 2,
            LoadingMinDurationMs = 5_000,
        }, builder =>
        {
            builder.AddAttribute(2, nameof(BitComponentBase.Class), "own");
            builder.AddAttribute(3, nameof(BitResourceTimeline.Classes), new BitResourceTimelineClassStyles { Viewport = "own-viewport" });
            builder.AddAttribute(4, nameof(BitResourceTimeline.Options), ownOptions);
            builder.AddAttribute(5, nameof(BitResourceTimeline.Texts), ownTexts);
            builder.AddAttribute(6, nameof(BitResourceTimeline.LoadBatchSize), 0);
            builder.AddAttribute(7, nameof(BitResourceTimeline.LoadingMinDurationMs), 0);
        }, FiveBars());

        WaitForLoad(component);

        var root = component.Find(".bit-rtm");
        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));

        var viewport = component.Find(".bit-rtm-vpt");
        Assert.IsTrue(viewport.ClassList.Contains("own-viewport"));
        Assert.IsFalse(viewport.ClassList.Contains("cascaded-viewport"));
        Assert.AreEqual("Own", viewport.GetAttribute("aria-label"));

        var setup = Calls(SetupFn).Single();
        Assert.AreSame(ownOptions, setup.Arguments[3]);
        Assert.AreSame(ownTexts, setup.Arguments[4]);

        // An explicit 0 sends everything in one call, whatever the cascade says.
        Assert.ContainsSingle(Calls(SetDataFn));
        Assert.IsEmpty(Calls(BeginDataFn));
        Assert.AreEqual(0, component.FindComponent<BitResourceTimeline>().Instance.LoadingMinDurationMs);
    }

    [TestMethod]
    public void BitResourceTimelineShouldDropTheCascadedValuesWithTheCascade()
    {
        var options = new BitResourceTimelineOptions { Editable = true };
        var config = SampleConfig();

        var component = RenderWithParams(new BitResourceTimelineParams
        {
            Options = options,
            Texts = new() { AriaLabel = "Cascaded" },
            LoadBatchSize = 2,
            Classes = new() { Viewport = "cascaded-viewport" },
        }, config: config);

        WaitForLoad(component);

        Assert.AreEqual("Cascaded", component.Find(".bit-rtm-vpt").GetAttribute("aria-label"));

        component.Render(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams>());
            parameters.AddChildContent(builder => BuildTimeline(builder, null, config));
        });

        var timeline = component.FindComponent<BitResourceTimeline>().Instance;

        Assert.IsNull(timeline.Options);
        Assert.IsNull(timeline.Texts);
        Assert.IsNull(timeline.Classes);
        Assert.AreEqual(10_000, timeline.LoadBatchSize);

        var viewport = component.Find(".bit-rtm-vpt");
        Assert.AreEqual("Resource timeline", viewport.GetAttribute("aria-label"));
        Assert.IsFalse(viewport.ClassList.Contains("cascaded-viewport"));
        // Options taken away set nothing, and the engine merges them: it stays editable, and says so.
        Assert.Contains("Alt+", viewport.GetAttribute("aria-keyshortcuts")!);

        // The engine is told the options and texts it was configured with are gone.
        var setOptions = Calls(SetOptionsFn).Single();
        Assert.IsNull(setOptions.Arguments[1]);
        Assert.AreEqual("Resource timeline", ((BitResourceTimelineTexts)setOptions.Arguments[2]!).AriaLabel);
    }

    public global::Microsoft.VisualStudio.TestTools.UnitTesting.TestContext TestContext { get; set; }
}

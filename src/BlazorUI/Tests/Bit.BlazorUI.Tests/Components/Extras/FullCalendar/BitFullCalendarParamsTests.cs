using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers the BitParams cascade of the full calendar: what a BitFullCalendarParams carries (everything the calendars
/// of an app agree on, nothing that makes one calendar the one it is), that every property it carries reaches a
/// calendar that did not set it, that what the calendar derives from them (its culture, its chrome, its views, its
/// bounds, its clock, its colours, its templates) follows the cascaded values, that the calendar's own markup wins,
/// and that the defaults come back once the params object is withdrawn.
/// </summary>
[TestClass]
public class BitFullCalendarParamsTests : BunitTestContext
{
    // A Wednesday; the cascaded clock is pinned to it.
    private static readonly DateTime Anchor = new(2026, 3, 18);

    // The data, the bound view/mode/date, the loading state and the callbacks describe one calendar rather than a
    // default for many, so they have no place on the params object.
    private static readonly string[] _notCascaded =
    [
        nameof(BitFullCalendar.CascadingParameters),
        nameof(BitFullCalendar.Date),
        nameof(BitFullCalendar.DefaultDate),
        nameof(BitFullCalendar.Events),
        nameof(BitFullCalendar.IsLoading),
        nameof(BitFullCalendar.Mode),
        nameof(BitFullCalendar.Resources),
        nameof(BitFullCalendar.View),
    ];

    private static List<BitFullCalendarEvent> Events() =>
    [
        new() { Id = "standup", Title = "Standup", StartDate = Anchor.AddHours(9), EndDate = Anchor.AddHours(10), Color = "brand" }
    ];

    private IRenderedComponent<BitParams> RenderWithParams(BitFullCalendarParams calendarParams, Action<RenderTreeBuilder>? extraAttributes = null, bool openOnToday = false)
    {
        return RenderComponent<BitParams>(parameters =>
        {
            parameters.Add(p => p.Parameters, new List<IBitComponentParams> { calendarParams });
            parameters.AddChildContent(builder =>
            {
                builder.OpenComponent<BitFullCalendar>(0);
                builder.AddComponentParameter(1, nameof(BitFullCalendar.Events), Events());
                if (openOnToday is false)
                {
                    builder.AddComponentParameter(2, nameof(BitFullCalendar.DefaultDate), (DateTime?)Anchor);
                }
                extraAttributes?.Invoke(builder);
                builder.CloseComponent();
            });
        });
    }

    private static bool IsEventCallback(Type type)
        => type == typeof(EventCallback) || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(EventCallback<>));

    [TestMethod]
    public void BitFullCalendarParamsShouldHaveCorrectParamName()
    {
        Assert.AreEqual($"{nameof(BitParams)}.{nameof(BitFullCalendar)}", BitFullCalendarParams.ParamName);
        Assert.AreEqual("BitParams.BitFullCalendar", BitFullCalendarParams.ParamName);
    }

    [TestMethod]
    public void BitFullCalendarParamsShouldImplementIBitComponentParams()
    {
        var @params = new BitFullCalendarParams();

        Assert.IsInstanceOfType<IBitComponentParams>(@params);
        Assert.AreEqual(BitFullCalendarParams.ParamName, @params.Name);
    }

    [TestMethod]
    public void BitFullCalendarParamsShouldCarryEveryParameterThatIsNotPerCalendar()
    {
        var baseParameters = typeof(BitComponentBase).GetProperties().Select(p => p.Name).ToHashSet();

        var parameters = typeof(BitFullCalendar).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                                                .Where(p => p.GetCustomAttribute<ParameterAttribute>() is not null)
                                                .Where(p => IsEventCallback(p.PropertyType) is false)
                                                .Select(p => p.Name)
                                                .Where(n => baseParameters.Contains(n) is false && _notCascaded.Contains(n) is false)
                                                .ToArray();

        Assert.IsTrue(parameters.Length > 0);

        foreach (var name in parameters)
        {
            Assert.IsNotNull(typeof(BitFullCalendarParams).GetProperty(name), $"BitFullCalendarParams has no {name}.");
        }
    }

    [TestMethod]
    public void BitFullCalendarParamsShouldCarryNothingTheCalendarDoesNotHave()
    {
        var own = typeof(BitFullCalendarParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                               .Where(p => p.Name != nameof(IBitComponentParams.Name));

        foreach (var property in own)
        {
            var parameter = typeof(BitFullCalendar).GetProperty(property.Name);

            Assert.IsNotNull(parameter, $"BitFullCalendar has no {property.Name}.");
            Assert.IsNotNull(parameter.GetCustomAttribute<ParameterAttribute>(), $"{property.Name} is not a parameter of BitFullCalendar.");
            Assert.IsFalse(_notCascaded.Contains(property.Name), $"{property.Name} is per calendar and must not be cascaded.");
        }
    }

    [TestMethod]
    public void BitFullCalendarShouldTakeEveryPropertyOfTheParamsObject()
    {
        var cascaded = new BitFullCalendarParams
        {
            AgendaEventTemplate = e => b => b.AddContent(0, e.Title),
            Classes = new() { Root = "cascaded-root" },
            Culture = CultureInfo.GetCultureInfo("de-DE"),
            CultureName = "fr-FR",
            DayEventTemplate = e => b => b.AddContent(0, e.Title),
            DefaultMode = BitFullCalendarMode.Timeline,
            DefaultView = BitFullCalendarView.Week,
            EventColorOptions = [new BitFullCalendarColorOption { Id = "brand", Title = "Brand", Value = "#123456" }],
            EventDetailsTemplate = e => b => b.AddContent(0, e.Title),
            EventEditorTemplate = e => b => b.AddContent(0, e.Title),
            HideFilters = true,
            HideHeader = true,
            HideSettings = true,
            MaxDate = new DateTime(2030, 12, 31),
            MinDate = new DateTime(2020, 1, 1),
            MonthCellTemplate = c => b => b.AddContent(0, c.Day),
            MonthEventTemplate = e => b => b.AddContent(0, e.Title),
            ReadOnly = true,
            ResourceTemplate = r => b => b.AddContent(0, r.Title),
            Settings = new() { Use24HourFormat = false },
            Styles = new() { Root = "color: red" },
            Texts = new() { TodayButton = "Now" },
            TimeProvider = new FixedTimeProvider(Anchor.AddHours(11)),
            TimelineEventTemplate = e => b => b.AddContent(0, e.Title),
            Views = [BitFullCalendarView.Week, BitFullCalendarView.Month],
            WeekEventTemplate = e => b => b.AddContent(0, e.Title),
        };

        var component = RenderWithParams(cascaded);
        var calendar = component.FindComponent<BitFullCalendar>().Instance;

        // Read off the params object itself, so a property added to it later is checked here without being named.
        var properties = typeof(BitFullCalendarParams).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                                                      .Where(p => p.Name != nameof(IBitComponentParams.Name))
                                                      .ToArray();

        Assert.IsTrue(properties.Length > 0);

        foreach (var property in properties)
        {
            var supplied = property.GetValue(cascaded);
            Assert.IsNotNull(supplied, $"The test leaves {property.Name} unset, so it cannot tell whether the calendar takes it.");

            var taken = typeof(BitFullCalendar).GetProperty(property.Name)!.GetValue(calendar);

            if (property.PropertyType.IsValueType || supplied is string)
            {
                Assert.AreEqual(supplied, taken, $"{property.Name} was not taken from the cascade.");
            }
            else
            {
                Assert.AreSame(supplied, taken, $"{property.Name} was not taken from the cascade as the very object supplied.");
            }
        }
    }

    [TestMethod]
    public void BitFullCalendarShouldApplyCascadedBaseParameters()
    {
        var component = RenderWithParams(new BitFullCalendarParams
        {
            AriaLabel = "Team schedule",
            Class = "cascaded",
            Dir = BitDir.Rtl,
            Disabled = true,
            Id = "cascaded-id",
            Style = "--bit-bfc-height:400px",
        });

        var root = component.Find(".bit-bfc");

        Assert.AreEqual("cascaded-id", root.Id);
        Assert.AreEqual("rtl", root.GetAttribute("dir"));
        Assert.IsTrue(root.ClassList.Contains("cascaded"));
        Assert.IsTrue(root.ClassList.Contains("bit-dis"));
        StringAssert.Contains(root.GetAttribute("style"), "--bit-bfc-height:400px");
        // A named calendar is a region, and a disabled one is presentation-only, from the cascade as from the markup.
        Assert.AreEqual("region", root.GetAttribute("role"));
        Assert.AreEqual("Team schedule", root.GetAttribute("aria-label"));
        Assert.IsTrue(component.FindComponent<BitFullCalendar>().Instance.State.ReadOnly);
    }

    [TestMethod]
    public void BitFullCalendarShouldApplyTheCascadedCulture()
    {
        var byCulture = RenderWithParams(new BitFullCalendarParams { Culture = CultureInfo.GetCultureInfo("de-DE") });
        Assert.AreEqual("de-DE", byCulture.FindComponent<BitFullCalendar>().Instance.State.Culture.Name);

        // A cascaded name takes precedence over a cascaded culture, as it does on the markup.
        var byName = RenderWithParams(new BitFullCalendarParams { Culture = CultureInfo.GetCultureInfo("de-DE"), CultureName = "fr-FR" });
        Assert.AreEqual("fr-FR", byName.FindComponent<BitFullCalendar>().Instance.State.Culture.Name);
    }

    [TestMethod]
    public void BitFullCalendarShouldApplyTheCascadedChrome()
    {
        var component = RenderWithParams(new BitFullCalendarParams { HideFilters = true, HideSettings = true, ReadOnly = true });

        var root = component.Find(".bit-bfc");

        Assert.IsNotNull(root.QuerySelector(".bit-bfc-header"));
        Assert.IsNull(root.QuerySelector(".bit-bfc-filter-row"));
        Assert.IsNull(root.QuerySelector(".bit-bfc-header-right .bit-bfc-dropdown"), "the settings gear is hidden");
        Assert.IsTrue(root.ClassList.Contains("bit-bfc-readonly"));
        Assert.IsTrue(component.FindComponent<BitFullCalendar>().Instance.State.ReadOnly);

        var headless = RenderWithParams(new BitFullCalendarParams { HideHeader = true });
        Assert.IsNull(headless.Find(".bit-bfc").QuerySelector(".bit-bfc-header"));
    }

    [TestMethod]
    public void BitFullCalendarShouldOpenOnTheCascadedViewAndMode()
    {
        var component = RenderWithParams(new BitFullCalendarParams
        {
            DefaultView = BitFullCalendarView.Week,
            DefaultMode = BitFullCalendarMode.Timeline,
        }, builder =>
        {
            builder.AddComponentParameter(10, nameof(BitFullCalendar.Resources), (IReadOnlyList<BitFullCalendarResource>)[new BitFullCalendarResource { Id = "r1", Title = "Room 1" }]);
        });

        var state = component.FindComponent<BitFullCalendar>().Instance.State;

        Assert.AreEqual(BitFullCalendarView.Week, state.View);
        Assert.AreEqual(BitFullCalendarMode.Timeline, state.Mode);
        // The timeline lays out one row per resource, plus the row of the events that have none (the Standup).
        CollectionAssert.AreEqual(
            new[] { "Room 1", "Unassigned" },
            component.FindAll(".bit-bfc-tl-resource-title").Select(t => t.TextContent.Trim()).ToArray());
    }

    [TestMethod]
    public void BitFullCalendarShouldApplyTheCascadedBoundsAndClock()
    {
        var component = RenderWithParams(new BitFullCalendarParams
        {
            MinDate = new DateTime(2026, 3, 1),
            MaxDate = new DateTime(2026, 3, 31),
            TimeProvider = new FixedTimeProvider(Anchor.AddHours(11)),
        }, openOnToday: true); // No DefaultDate, so the calendar opens on the today of the cascaded clock.

        var state = component.FindComponent<BitFullCalendar>().Instance.State;

        Assert.AreEqual(new DateTime(2026, 3, 1), state.MinDate);
        Assert.AreEqual(new DateTime(2026, 3, 31), state.MaxDate);
        Assert.AreEqual(Anchor, state.Today);
        Assert.AreEqual(Anchor, state.SelectedDate.Date);
        // March is the only month in the window, so neither step is offered.
        Assert.AreEqual(2, component.FindAll(".bit-bfc-date-nav .bit-bfc-btn-nav[aria-disabled='true']").Count);
    }

    [TestMethod]
    public void BitFullCalendarShouldDrawTheEventsInTheCascadedColors()
    {
        var component = RenderWithParams(new BitFullCalendarParams
        {
            EventColorOptions = [new BitFullCalendarColorOption { Id = "brand", Title = "Brand", Value = "#123456" }],
        });

        StringAssert.Contains(component.Find(".bit-bfc-event-badge").GetAttribute("style"), "#123456");
    }

    [TestMethod]
    public async Task BitFullCalendarShouldRenderTheCascadedEventTemplates()
    {
        var component = RenderWithParams(new BitFullCalendarParams
        {
            MonthEventTemplate = e => b => b.AddMarkupContent(0, $"<i class='month-tpl'>{e.Title}</i>"),
            WeekEventTemplate = e => b => b.AddMarkupContent(0, $"<i class='week-tpl'>{e.Title}</i>"),
            DayEventTemplate = e => b => b.AddMarkupContent(0, $"<i class='day-tpl'>{e.Title}</i>"),
            AgendaEventTemplate = e => b => b.AddMarkupContent(0, $"<i class='agenda-tpl'>{e.Title}</i>"),
            TimelineEventTemplate = e => b => b.AddMarkupContent(0, $"<i class='timeline-tpl'>{e.Title}</i>"),
            ResourceTemplate = r => b => b.AddMarkupContent(0, $"<i class='resource-tpl'>{r.Title}</i>"),
        }, builder =>
        {
            builder.AddComponentParameter(10, nameof(BitFullCalendar.Resources), (IReadOnlyList<BitFullCalendarResource>)[new BitFullCalendarResource { Id = "r1", Title = "Room 1" }]);
        });

        var calendar = component.FindComponent<BitFullCalendar>();

        Assert.AreEqual("Standup", component.Find(".bit-bfc-event-badge .month-tpl").TextContent);

        await calendar.InvokeAsync(() => calendar.Instance.ChangeView(BitFullCalendarView.Week));
        Assert.AreEqual("Standup", component.Find(".bit-bfc-event-block .week-tpl").TextContent);

        await calendar.InvokeAsync(() => calendar.Instance.ChangeView(BitFullCalendarView.Day));
        Assert.AreEqual("Standup", component.Find(".bit-bfc-event-block .day-tpl").TextContent);

        await calendar.InvokeAsync(() => calendar.Instance.ChangeView(BitFullCalendarView.Agenda));
        Assert.AreEqual("Standup", component.Find(".bit-bfc-agenda-item .agenda-tpl").TextContent);

        await calendar.InvokeAsync(() => calendar.Instance.ChangeView(BitFullCalendarView.Week));
        await calendar.InvokeAsync(() => calendar.Instance.ChangeMode(BitFullCalendarMode.Timeline));
        Assert.AreEqual("Standup", component.Find("[data-bit-bfc-event='standup'] .timeline-tpl").TextContent);
        Assert.AreEqual("Room 1", component.Find(".resource-tpl").TextContent);
    }

    [TestMethod]
    public void BitFullCalendarShouldKeepItsOwnValuesOverTheCascadedOnes()
    {
        var component = RenderWithParams(new BitFullCalendarParams
        {
            Class = "cascaded",
            CultureName = "fr-FR",
            DefaultView = BitFullCalendarView.Week,
            HideHeader = true,
            ReadOnly = true,
            Texts = new() { TodayButton = "Now" },
        }, builder =>
        {
            builder.AddComponentParameter(10, nameof(BitFullCalendar.Class), "own");
            builder.AddComponentParameter(11, nameof(BitFullCalendar.CultureName), "de-DE");
            builder.AddComponentParameter(12, nameof(BitFullCalendar.DefaultView), (BitFullCalendarView?)BitFullCalendarView.Month);
            builder.AddComponentParameter(13, nameof(BitFullCalendar.HideHeader), false);
            builder.AddComponentParameter(14, nameof(BitFullCalendar.ReadOnly), false);
            builder.AddComponentParameter(15, nameof(BitFullCalendar.Texts), new BitFullCalendarTexts { TodayButton = "Own today" });
        });

        var root = component.Find(".bit-bfc");
        var state = component.FindComponent<BitFullCalendar>().Instance.State;

        Assert.IsTrue(root.ClassList.Contains("own"));
        Assert.IsFalse(root.ClassList.Contains("cascaded"));
        Assert.AreEqual("de-DE", state.Culture.Name);
        Assert.AreEqual(BitFullCalendarView.Month, state.View);
        // HideHeader and ReadOnly written as false by the calendar opt it out of the cascaded true.
        Assert.IsNotNull(root.QuerySelector(".bit-bfc-header"));
        Assert.IsFalse(state.ReadOnly);
        Assert.AreEqual("Own today", root.QuerySelector(".bit-bfc-header-left > .bit-bfc-btn")!.TextContent.Trim());
    }

    [TestMethod]
    public void BitFullCalendarShouldNotBeAffectedByAnEmptyParamsObject()
    {
        var cascaded = RenderWithParams(new BitFullCalendarParams());
        var plain = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, Events());
            parameters.Add(p => p.DefaultDate, Anchor);
        });

        var root = cascaded.Find(".bit-bfc");

        Assert.AreEqual(plain.Find(".bit-bfc").ClassName, root.ClassName);
        Assert.AreEqual(5, root.QuerySelectorAll(".bit-bfc-view-tab").Length);
        Assert.IsNotNull(root.QuerySelector(".bit-bfc-filter-row"));
        Assert.AreEqual("Today", root.QuerySelector(".bit-bfc-header-left > .bit-bfc-btn")!.TextContent.Trim());
        Assert.AreEqual(plain.Instance.State.Culture.Name, cascaded.FindComponent<BitFullCalendar>().Instance.State.Culture.Name);
    }

    [TestMethod]
    public void BitFullCalendarShouldGoBackToItsOwnDefaultsWhenTheParamsAreWithdrawn()
    {
        var component = RenderComponent<BitFullCalendarCascadingParamsTest>();
        var first = component.Find("#first-calendar");
        Assert.IsTrue(first.ClassList.Contains("cascaded"));
        Assert.AreEqual(2, first.QuerySelectorAll(".bit-bfc-view-tab").Length);

        component.Render(parameters => parameters.Add(p => p.WithParams, false));

        first = component.Find("#first-calendar");
        Assert.IsFalse(first.ClassList.Contains("cascaded"));
        Assert.AreEqual(5, first.QuerySelectorAll(".bit-bfc-view-tab").Length);
        Assert.IsNotNull(first.QuerySelector(".bit-bfc-filter-row"));
        Assert.IsFalse(first.QuerySelector(".bit-bfc-header")!.ClassList.Contains("cascaded-header"));
        Assert.IsNull(first.QuerySelector(".bit-bfc-body")!.GetAttribute("style"));
        Assert.AreEqual("Today", first.QuerySelector(".bit-bfc-header-left > .bit-bfc-btn")!.TextContent.Trim());

        var calendars = component.FindComponents<BitFullCalendar>();
        Assert.IsTrue(calendars.All(c => ReferenceEquals(c.Instance.Settings, BitFullCalendarCascadingParamsTest.SharedSettings) is false));
        Assert.IsTrue(calendars.All(c => c.Instance.State.Use24HourFormat));
    }

    private sealed class FixedTimeProvider(DateTime localNow) : TimeProvider
    {
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;

        public override DateTimeOffset GetUtcNow() => new(DateTime.SpecifyKind(localNow, DateTimeKind.Utc));
    }
}

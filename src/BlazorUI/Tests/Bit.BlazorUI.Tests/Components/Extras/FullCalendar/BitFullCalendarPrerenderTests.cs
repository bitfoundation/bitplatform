using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Components.Extras.FullCalendar;

/// <summary>
/// Covers what a BitFullCalendar sends before the page is interactive (a prerender, or a static SSR page): no
/// OnAfterRender runs, so whatever the app does with the initial range has to happen before it.
/// </summary>
[TestClass]
public class BitFullCalendarPrerenderTests : BunitTestContext
{
    // A Wednesday.
    private static readonly DateTime Anchor = new(2031, 6, 18);

    [TestMethod]
    public async Task BitFullCalendarShouldPrerenderTheEventsLoadedForTheInitialRange()
    {
        var html = await Prerenderer.RenderAsync<RangeLoadingHost>(new Dictionary<string, object?>
        {
            [nameof(RangeLoadingHost.View)] = BitFullCalendarView.Month,
        });

        // The app fetches the events of the range the calendar reports, the way the OnDateChange docs suggest.
        StringAssert.Contains(html, "Loaded for 2031-06-01");
    }

    [TestMethod]
    public async Task BitFullCalendarShouldPrerenderTheEventsLoadedForTheInitialRangeOfADefaultView()
    {
        // A DefaultView reports its range while the parameters are applied, rather than as the initial range.
        var html = await Prerenderer.RenderAsync<RangeLoadingHost>(new Dictionary<string, object?>
        {
            [nameof(RangeLoadingHost.View)] = BitFullCalendarView.Week,
        });

        StringAssert.Contains(html, "Loaded for 2031-06-15");
    }

    [TestMethod]
    public async Task BitFullCalendarShouldPrerenderWhenTheRangeHandlerCallsJavaScript()
    {
        // JavaScript cannot be called before the page is interactive; the interactive render reports the range again.
        var html = await Prerenderer.RenderAsync<JsCallingHost>();

        StringAssert.Contains(html, "bit-bfc");
    }

    [TestMethod]
    public async Task BitFullCalendarShouldPrerenderWhenTheRangeHandlerFails()
    {
        // A fetch meant for the browser (a relative URL against a server HttpClient with no BaseAddress) fails on
        // the server; the page still renders, and the interactive render reports the range again.
        var html = await Prerenderer.RenderAsync<FailingHost>();

        StringAssert.Contains(html, "bit-bfc");
    }

    [TestMethod]
    public void BitFullCalendarShouldRaiseTheInitialRangeOnceWhenInteractive()
    {
        var host = RenderComponent<RangeLoadingHost>(parameters => parameters.Add(p => p.View, BitFullCalendarView.Week));

        host.WaitForAssertion(() => StringAssert.Contains(host.Markup, "Loaded for 2031-06-15"));
        Assert.AreEqual(1, host.Instance.Ranges.Count);
        Assert.AreEqual(BitFullCalendarView.Week, host.Instance.Ranges[0].View);
    }

    [TestMethod]
    public void BitFullCalendarShouldRaiseTheInitialRangeOfAMonthOnce()
    {
        var ranges = new List<BitFullCalendarDateChangeEventArgs>();
        var component = RenderComponent<BitFullCalendar>(parameters =>
        {
            parameters.Add(p => p.Events, new List<BitFullCalendarEvent>());
            parameters.Add(p => p.CultureName, "en-US");
            parameters.Add(p => p.DefaultDate, Anchor);
            parameters.Add(p => p.OnDateChange, (BitFullCalendarDateChangeEventArgs r) => ranges.Add(r));
        });

        // Neither a re-render nor the after-render pass reports it a second time.
        component.Render(parameters => parameters.Add(p => p.IsLoading, true));

        Assert.AreEqual(1, ranges.Count);
        Assert.AreEqual(new DateTime(2031, 6, 1), ranges[0].Start);
    }

    /// <summary>
    /// An app page whose range handler calls JavaScript.
    /// </summary>
    private sealed class JsCallingHost : ComponentBase
    {
        [Inject] private IJSRuntime Js { get; set; } = default!;

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitFullCalendar>(0);
            builder.AddComponentParameter(1, nameof(BitFullCalendar.CultureName), "en-US");
            builder.AddComponentParameter(2, nameof(BitFullCalendar.DefaultDate), Anchor);
            builder.AddComponentParameter(3, nameof(BitFullCalendar.OnDateChange), EventCallback.Factory.Create<BitFullCalendarDateChangeEventArgs>(this, HighlightRangeAsync));
            builder.CloseComponent();
        }

        private async Task HighlightRangeAsync(BitFullCalendarDateChangeEventArgs range)
        {
            await Js.InvokeVoidAsync("highlightRange", range.Start, range.End);
        }
    }

    /// <summary>
    /// An app page whose range handler fetches a URL only the browser can resolve.
    /// </summary>
    private sealed class FailingHost : ComponentBase
    {
        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitFullCalendar>(0);
            builder.AddComponentParameter(1, nameof(BitFullCalendar.CultureName), "en-US");
            builder.AddComponentParameter(2, nameof(BitFullCalendar.DefaultDate), Anchor);
            builder.AddComponentParameter(3, nameof(BitFullCalendar.OnDateChange), EventCallback.Factory.Create<BitFullCalendarDateChangeEventArgs>(this, FetchRangeAsync));
            builder.CloseComponent();
        }

        private async Task FetchRangeAsync(BitFullCalendarDateChangeEventArgs range)
        {
            await Task.Yield();

            throw new InvalidOperationException("An invalid request URI was provided. Either the request URI must be an absolute URI or BaseAddress must be set.");
        }
    }

    /// <summary>
    /// An app page that fetches the events of whatever range the calendar reports.
    /// </summary>
    private sealed class RangeLoadingHost : ComponentBase
    {
        private List<BitFullCalendarEvent> _events = [];

        [Parameter] public BitFullCalendarView View { get; set; }

        public List<BitFullCalendarDateChangeEventArgs> Ranges { get; } = [];

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<BitFullCalendar>(0);
            builder.AddComponentParameter(1, nameof(BitFullCalendar.Events), _events);
            builder.AddComponentParameter(2, nameof(BitFullCalendar.CultureName), "en-US");
            builder.AddComponentParameter(3, nameof(BitFullCalendar.DefaultDate), Anchor);
            builder.AddComponentParameter(4, nameof(BitFullCalendar.DefaultView), View);
            builder.AddComponentParameter(5, nameof(BitFullCalendar.OnDateChange), EventCallback.Factory.Create<BitFullCalendarDateChangeEventArgs>(this, LoadRangeAsync));
            builder.CloseComponent();
        }

        private async Task LoadRangeAsync(BitFullCalendarDateChangeEventArgs range)
        {
            Ranges.Add(range);

            // A real fetch completes later than the call that started it.
            await Task.Yield();

            _events =
            [
                new BitFullCalendarEvent
                {
                    Id = "loaded",
                    Title = $"Loaded for {range.Start:yyyy-MM-dd}",
                    StartDate = Anchor.AddHours(9),
                    EndDate = Anchor.AddHours(10),
                }
            ];
        }
    }
}

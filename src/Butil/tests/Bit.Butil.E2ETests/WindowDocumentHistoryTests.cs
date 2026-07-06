using Bit.Butil.E2ETests.Infrastructure;
using NUnit.Framework;

namespace Bit.Butil.E2ETests;

[Parallelizable(ParallelScope.Self)]
public class WindowDocumentHistoryTests : ButilPageTest
{
    [Test]
    public async Task Performance_Now_Returns_PositiveValue()
    {
        await ClickAndExpectAsync("perf-now", "perf:now:True");
    }

    [Test]
    public async Task Window_Btoa_Atob_Roundtrip()
    {
        // "butil" base64-encoded is "YnV0aWw=".
        await ClickAndExpectAsync("window-base64", "window:b64:YnV0aWw=/butil");
    }

    [Test]
    public async Task Document_SetTitle_Then_GetTitle_Roundtrips()
    {
        await ClickAndExpectAsync("doc-title", "doc:title:butil-e2e-title");
    }

    [Test]
    public async Task Location_GetHref_Reports_The_Current_Page()
    {
        await ClickAndExpectAsync("loc-href", "loc:href:True");
    }

    [Test]
    public async Task History_PushState_Increments_Length()
    {
        await ClickAndExpectAsync("history-state", "history:len:True");
    }

    [Test]
    public async Task History_ReplaceState_Then_GetState_Roundtrips_Typed_Payload()
    {
        await ClickAndExpectAsync("history-replace", "history:state:7/replaced");
    }

    [Test]
    public async Task History_ScrollRestoration_Set_Then_Get_Roundtrips()
    {
        await ClickAndExpectAsync("history-scroll", "history:scroll:Manual");
    }

    [Test]
    public async Task Location_Parts_Report_Protocol_Pathname_And_Origin()
    {
        await ClickAndExpectAsync("loc-parts", "loc:parts:True/True/True");
    }

    [Test]
    public async Task Document_Meta_Reports_Visibility_Charset_And_Url()
    {
        // A headless foreground page reports "Visible"; charset and url are always populated.
        await ClickAndExpectAsync("doc-meta", "doc:meta:Visible/True/True");
    }

    [Test]
    public async Task Window_Metrics_Report_Positive_Inner_Size()
    {
        await ClickAndExpectAsync("window-metrics", "window:metrics:True");
    }

    [Test]
    public async Task Window_MatchMedia_Evaluates_A_Query()
    {
        await ClickAndExpectAsync("window-matchmedia", "window:media:True");
    }
}

using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class WindowDocumentHistoryTests : ButilPageTest
{
    [TestMethod]
    public async Task Performance_Now_Returns_PositiveValue()
    {
        await ClickAndExpectAsync("perf-now", "perf:now:True");
    }

    [TestMethod]
    public async Task Window_Btoa_Atob_Roundtrip()
    {
        // "butil" base64-encoded is "YnV0aWw=".
        await ClickAndExpectAsync("window-base64", "window:b64:YnV0aWw=/butil");
    }

    [TestMethod]
    public async Task Document_SetTitle_Then_GetTitle_Roundtrips()
    {
        await ClickAndExpectAsync("doc-title", "doc:title:butil-e2e-title");
    }

    [TestMethod]
    public async Task Location_GetHref_Reports_The_Current_Page()
    {
        await ClickAndExpectAsync("loc-href", "loc:href:True");
    }

    [TestMethod]
    public async Task History_PushState_Increments_Length()
    {
        await ClickAndExpectAsync("history-state", "history:len:True");
    }

    [TestMethod]
    public async Task History_ReplaceState_Then_GetState_Roundtrips_Typed_Payload()
    {
        await ClickAndExpectAsync("history-replace", "history:state:7/replaced");
    }

    [TestMethod]
    public async Task History_ScrollRestoration_Set_Then_Get_Roundtrips()
    {
        await ClickAndExpectAsync("history-scroll", "history:scroll:Manual");
    }

    [TestMethod]
    public async Task Location_Parts_Report_Protocol_Pathname_And_Origin()
    {
        await ClickAndExpectAsync("loc-parts", "loc:parts:True/True/True");
    }

    [TestMethod]
    public async Task Document_Meta_Reports_Visibility_Charset_And_Url()
    {
        // A headless foreground page reports "Visible"; charset and url are always populated.
        await ClickAndExpectAsync("doc-meta", "doc:meta:Visible/True/True");
    }

    [TestMethod]
    public async Task Window_Metrics_Report_Positive_Inner_Size()
    {
        await ClickAndExpectAsync("window-metrics", "window:metrics:True");
    }

    [TestMethod]
    public async Task Window_MatchMedia_Evaluates_A_Query()
    {
        await ClickAndExpectAsync("window-matchmedia", "window:media:True");
    }
}

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
}

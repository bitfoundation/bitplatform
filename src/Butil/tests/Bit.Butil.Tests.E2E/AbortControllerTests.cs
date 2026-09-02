using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class AbortControllerTests : ButilPageTest
{
    [TestMethod]
    public async Task Abort_Sets_Aborted_And_Keeps_The_First_Reason()
    {
        // before/after/reason - and the harness aborts twice, so this also proves the second
        // abort cannot overwrite the reason the signal already carries.
        await ClickAndExpectAsync("abort-roundtrip", "abort:roundtrip:False/True/first");
    }

    [TestMethod]
    public async Task OnAbort_Receives_The_Reason()
    {
        await ClickAndExpectAsync("abort-listener", "abort:listener:listener-reason");
    }

    [TestMethod]
    public async Task OnAbort_On_An_Already_Aborted_Signal_Fires_Immediately()
    {
        // The 'abort' event never fires twice, so a late listener would otherwise never run at all.
        await ClickAndExpectAsync("abort-late-listener", "abort:late:already-gone");
    }

    [TestMethod]
    public async Task One_Signal_Reaches_Every_Listener_On_It()
    {
        await ClickAndExpectAsync("abort-shared", "abort:shared:shared/shared");
    }

    [TestMethod]
    public async Task Any_Aborts_With_The_Reason_Of_Whichever_Source_Fired()
    {
        await ClickAndExpectAsync("abort-any", "abort:any:from-right/True");
    }

    [TestMethod]
    public async Task Timeout_Aborts_Itself_After_The_Delay()
    {
        await ClickAndExpectAsync("abort-timeout", "abort:timeout:True/True");
    }

    [TestMethod]
    public async Task Disposing_A_Handle_Releases_It_Without_Aborting()
    {
        // Both readings are False: not aborted before release, and still not aborted after - a
        // released signal is inert, and the late Abort in the harness is a no-op rather than a throw.
        await ClickAndExpectAsync("abort-release", "abort:release:False/False");
    }
}

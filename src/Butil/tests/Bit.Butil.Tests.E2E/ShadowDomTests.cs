using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

[TestClass]
public class ShadowDomTests : ButilPageTest
{
    [TestMethod]
    public async Task An_Open_Root_Can_Be_Queried_And_Names_Its_Host()
    {
        await ClickAndExpectAsync("shadow-attach", "shadow:attach:open/div/inside/div");
    }

    [TestMethod]
    public async Task A_Selector_From_The_Document_Cannot_Reach_Into_A_Shadow_Root()
    {
        await ClickAndExpectAsync("shadow-attach", "shadow:attach:open");
        // Zero from the document, one through the root - the isolation, stated as a number.
        await ClickAndExpectAsync("shadow-isolated", "shadow:isolated:0/1");
    }

    [TestMethod]
    public async Task A_Closed_Root_Is_Unreachable_And_A_Second_Attach_Is_Refused()
    {
        await ClickAndExpectAsync("shadow-closed", "shadow:closed:True/True");
    }
}

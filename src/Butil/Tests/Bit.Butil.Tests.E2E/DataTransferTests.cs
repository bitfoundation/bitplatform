using Bit.Butil.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Butil.Tests.E2E;

/// <summary>
/// A headless browser has no mouse to drag with, so the drop is dispatched as a synthetic
/// <c>DragEvent</c> carrying a real <c>DataTransfer</c> with a real <c>File</c> in it. Everything
/// after the event - the listener, the payload extraction, the file being readable afterwards - is
/// the same path a user's drop takes.
/// </summary>
[TestClass]
public class DataTransferTests : ButilPageTest
{
    private const string DropScript = """
        () => {
            const transfer = new DataTransfer();
            transfer.items.add(new File(['hello world'], 'dropped.txt', { type: 'text/plain' }));
            transfer.setData('text/plain', 'dragged text');
            document.getElementById('dt-target')
                .dispatchEvent(new DragEvent('drop', { dataTransfer: transfer, bubbles: true, cancelable: true }));
        }
        """;

    [TestMethod]
    public async Task A_Drop_Reports_Its_Files_And_Its_Text()
    {
        await ClickAndExpectAsync("dt-listen", "dt:listening");
        await Page.EvaluateAsync(DropScript);

        // one file / its name / 'hello world' is 11 bytes / the text item alongside it
        await Assertions.Expect(Page.Locator("#status"))
            .ToContainTextAsync("dt:drop:1/dropped.txt/11/dragged text", new() { Timeout = 15_000 });
    }

    [TestMethod]
    public async Task A_Dropped_File_Is_Readable_After_The_Event_And_Gone_After_Release()
    {
        await ClickAndExpectAsync("dt-listen", "dt:listening");
        await Page.EvaluateAsync(DropScript);
        await Assertions.Expect(Page.Locator("#status")).ToContainTextAsync("dt:drop:1", new() { Timeout = 15_000 });

        await ClickAndExpectAsync("dt-read", "dt:read:11/hello world/True");
    }

    [TestMethod]
    public async Task Configuring_A_Drag_Source_Makes_It_Draggable_And_Disposing_Undoes_It()
    {
        await ClickAndExpectAsync("dt-source-config", "dt:source:true/True");
    }
}

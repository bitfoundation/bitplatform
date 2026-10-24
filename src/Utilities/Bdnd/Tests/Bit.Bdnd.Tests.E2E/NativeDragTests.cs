using Bit.Bdnd.Tests.E2E.Infrastructure;
using Microsoft.Playwright;
using static Microsoft.Playwright.Assertions;

namespace Bit.Bdnd.Tests.E2E;

/// <summary>
/// Native drags, which start outside the library: a link dragged from the page itself (a real browser drag, with the
/// DataTransfer the browser fills), and files or text from the operating system, simulated by raising the drag events
/// with a DataTransfer created in the page. The module tells such a drag from one of its own, matches its types
/// against what the target accepts, reads the payload during <c>drop</c> - the only moment the browser allows it -
/// and keeps the browser from opening or following what was dropped.
/// </summary>
[TestClass]
public sealed class NativeDragTests : BdndTest
{
    private const string TwoFiles =
        """
        dt.items.add(new File(['hello'], 'hello.txt', { type: 'text/plain' }));
        dt.items.add(new File([new Uint8Array(2048)], 'data.bin'))
        """;

    [TestMethod]
    public async Task Files_dragged_in_from_the_system_are_listed_with_their_name_type_size_and_contents()
    {
        var page = await OpenAsync("/other/native-files");
        var bin = page.Locator(".dustbin");
        var files = await NativeDataTransferAsync(page, TwoFiles);

        // Hovering: the target reports itself over, for a native file drag.
        await DispatchDragAsync(bin, "dragenter", files);
        await Expect(bin).ToHaveTextAsync("Release to drop the files");
        await Expect(bin).ToHaveCSSAsync("background-color", "rgb(21, 101, 192)");

        // The module accepts the dragover, or the browser would not raise a drop here.
        await DispatchDragAsync(bin, "dragover", files);
        var over = await LastDragOverAsync(bin);
        Assert.IsTrue(over.Inside && over.Prevented, $"the dragover of a file drag was not accepted: {over}");

        await DispatchDragAsync(bin, "drop", files);

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Dropped files (2)" })).ToBeVisibleAsync();
        var rows = page.Locator(".demo-table tbody tr");
        await Expect(rows).ToHaveCountAsync(2);
        // the text file's contents are streamed from the browser into .NET
        await Expect(rows.Nth(0).Locator("td")).ToHaveTextAsync(["hello.txt", "text/plain", "5 B", "hello"]);
        await Expect(rows.Nth(1).Locator("td")).ToHaveTextAsync(["data.bin", "-", "2 KB", "-"]);

        // No dragleave follows a drop: the hover is cleared all the same, and the browser was kept from opening the files.
        await Expect(bin).ToHaveTextAsync("Drag files here from your OS");
        var (count, prevented) = await DropsAsync(page.MainFrame);
        Assert.AreEqual((1, 1), (count, prevented), "the drop reached the page and was kept from the browser");
    }

    [TestMethod]
    public async Task A_target_taking_images_only_refuses_other_files_while_they_hover_and_leaves_them_out_of_a_drop()
    {
        var page = await OpenAsync("/other/native-files");
        var zone = page.Locator(".native-images");

        // a text file: its MIME type, which the browser tells while dragging, is not an image
        var text = await NativeDataTransferAsync(page, "dt.items.add(new File(['x'], 'notes.txt', { type: 'text/plain' }))");
        await DispatchDragAsync(zone, "dragenter", text);
        await DispatchDragAsync(zone, "dragover", text);
        var refused = await LastDragOverAsync(zone);
        Assert.AreEqual("none", refused.DropEffect, $"a drag of no image is refused: {refused}");
        await Expect(zone).ToHaveTextAsync("Images only");
        // even a drop the browser would not raise is not taken
        await DispatchDragAsync(zone, "drop", text);
        await WaitForQuietAsync(page);
        await Expect(page.Locator(".native-images-status")).ToHaveCountAsync(0);

        // an image and a text file: taken, and the text file left out on drop
        var mixed = await NativeDataTransferAsync(page,
            "dt.items.add(new File([new Uint8Array(10)], 'cat.png', { type: 'image/png' })); dt.items.add(new File(['x'], 'notes.txt', { type: 'text/plain' }))");
        await DispatchDragAsync(zone, "dragenter", mixed);
        await DispatchDragAsync(zone, "dragover", mixed);
        var taken = await LastDragOverAsync(zone);
        Assert.IsTrue(taken.Prevented, $"a drag with an image is taken: {taken}");
        await Expect(zone).ToHaveTextAsync("Release to drop the images");
        await DispatchDragAsync(zone, "drop", mixed);

        await Expect(page.Locator(".native-images-status")).ToHaveTextAsync("1 image dropped (1 file left out).");
    }

    [TestMethod]
    public async Task A_target_listing_an_extension_takes_any_file_drag_and_sorts_the_files_out_on_drop()
    {
        var page = await OpenAsync("/other/native-files");
        var zone = page.Locator(".native-documents");
        var files = await NativeDataTransferAsync(page,
            "dt.items.add(new File(['%PDF'], 'report.pdf', { type: 'application/pdf' })); dt.items.add(new File([new Uint8Array(4)], 'cat.png', { type: 'image/png' }))");

        // the PDF's MIME type matches no MIME type the target lists, but its extension does: names are known at the
        // drop only, so the drag is taken while it hovers
        await DispatchDragAsync(zone, "dragenter", files);
        await DispatchDragAsync(zone, "dragover", files);
        Assert.IsTrue((await LastDragOverAsync(zone)).Prevented, "the drag is taken");
        await Expect(zone).ToHaveTextAsync("Release to drop the documents");
        await DispatchDragAsync(zone, "drop", files);

        await Expect(page.Locator(".native-documents-status")).ToHaveTextAsync("1 document dropped (1 file left out).");
    }

    [TestMethod]
    public async Task A_target_hands_over_no_more_files_than_its_limit()
    {
        var page = await OpenAsync("/other/native-files");
        var zone = page.Locator(".native-images");
        var files = await NativeDataTransferAsync(page, string.Join("; ",
            Enumerable.Range(1, 4).Select(i => $"dt.items.add(new File([new Uint8Array(4)], 'p{i}.png', {{ type: 'image/png' }}))")));

        await DispatchDragAsync(zone, "dragenter", files);
        await DispatchDragAsync(zone, "dragover", files);
        await DispatchDragAsync(zone, "drop", files);

        await Expect(page.Locator(".native-images-status")).ToHaveTextAsync("3 images dropped (1 file left out).");
    }

    [TestMethod]
    public async Task A_drop_of_many_files_reaches_dotnet_in_full()
    {
        // more file descriptions than one message carries: the rest are read page by page
        var page = await OpenAsync("/other/native-files");
        var bin = page.Locator(".dustbin");
        var files = await NativeDataTransferAsync(page,
            "for (let i = 0; i < 120; i++) dt.items.add(new File(['x'], 'file-' + i + '.bin'))");

        await DispatchDragAsync(bin, "dragenter", files);
        await DispatchDragAsync(bin, "dragover", files);
        await DispatchDragAsync(bin, "drop", files);

        await Expect(page.GetByRole(AriaRole.Heading, new() { Name = "Dropped files (100 of 120)" })).ToBeVisibleAsync();
        await Expect(page.Locator(".demo-table tbody tr")).ToHaveCountAsync(100);
        await Expect(page.Locator(".demo-table tbody tr").Last.Locator("td").First).ToHaveTextAsync("file-99.bin");
    }

    [TestMethod]
    public async Task A_file_drag_that_leaves_the_target_without_dropping_clears_its_hover()
    {
        var page = await OpenAsync("/other/native-files");
        var bin = page.Locator(".dustbin");
        var files = await NativeDataTransferAsync(page, TwoFiles);

        await DispatchDragAsync(bin, "dragenter", files);
        await Expect(bin).ToHaveTextAsync("Release to drop the files");

        await DispatchDragAsync(bin, "dragleave", files);
        await Expect(bin).ToHaveTextAsync("Drag files here from your OS");
        await Expect(page.Locator(".demo-table")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task A_link_dragged_onto_the_target_is_read_as_html_text_and_url_without_being_followed()
    {
        var page = await OpenAsync("/other/native-html");
        var bin = page.Locator(".dustbin");
        var link = page.GetByRole(AriaRole.Link, new() { Name = "link inside" });
        var url = page.Url;

        // A real drag: Chromium drags a link with its URL, its text and its markup.
        await StartDragAsync(page, link);
        await DragToAsync(page, (await BoxAsync(bin)).Centre, () => Expect(bin).ToHaveTextAsync("Release (__NATIVE_HTML__)"));

        // The target lists HTML first, so the drag is matched as HTML; every type it carried was read.
        var result = page.Locator(".native-drop-result");
        await Expect(result.Locator("h4")).ToHaveTextAsync("Last native drop - __NATIVE_HTML__");
        await Expect(result).ToContainTextAsync("URLs: https://react-dnd.github.io/react-dnd");
        await Expect(result).ToContainTextAsync("Text: https://react-dnd.github.io/react-dnd");
        // WebKit lists text/html for a dragged link and hands over none: there only the URL and the text are carried
        if (Browser.BrowserType.Name != "webkit")
        {
            var html = await result.Locator("pre").TextContentAsync() ?? "";
            StringAssert.Contains(html, "href=\"https://react-dnd.github.io/react-dnd\"", "the dropped markup");
            StringAssert.Contains(html, ">link inside</a>", "the dropped markup");
        }

        // Nothing followed the link: no navigation, no new tab, and the target is idle again.
        await Expect(bin).ToHaveTextAsync("Drag selected text / a link here");
        Assert.AreEqual(url, page.Url, "the drop navigated the page");
        Assert.HasCount(1, page.Context.Pages, "the drop opened another page");
        var (count, prevented) = await DropsAsync(page.MainFrame);
        Assert.AreEqual((1, 1), (count, prevented), "the drop reached the page and was kept from the browser");
    }

    [TestMethod]
    public async Task An_in_app_drag_carries_its_native_data_out_and_is_no_native_drop_inside_the_page()
    {
        var page = await OpenAsync("/other/native-html");
        var bin = page.Locator(".dustbin");
        var source = page.Locator(".native-out");

        await DragAsync(page, source, bin);

        var start = (await DragStartsAsync(page.MainFrame)).Last();
        Assert.IsNotNull(start.Tag, "an in-app drag, tagged by the module");
        CollectionAssert.IsSubsetOf(new[] { "text/plain", "text/uri-list" }, start.Types, string.Join(", ", start.Types));
        Assert.AreEqual("copy", start.EffectAllowed);
        // the page's own native target does not take it for a native drag
        await Expect(page.Locator(".native-drop-result")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task Dropped_text_is_matched_by_its_type_and_a_drag_of_a_type_the_target_does_not_list_is_refused()
    {
        var page = await OpenAsync("/other/native-html");
        var bin = page.Locator(".dustbin");
        var result = page.Locator(".native-drop-result");

        var text = await NativeDataTransferAsync(page, "dt.setData('text/plain', 'some dragged words')");
        await DispatchDragAsync(bin, "dragenter", text);
        await Expect(bin).ToHaveTextAsync("Release (__NATIVE_TEXT__)");
        await DispatchDragAsync(bin, "dragover", text);
        await DispatchDragAsync(bin, "drop", text);

        await Expect(result.Locator("h4")).ToHaveTextAsync("Last native drop - __NATIVE_TEXT__");
        await Expect(result).ToContainTextAsync("Text: some dragged words");
        await Expect(bin).ToHaveTextAsync("Drag selected text / a link here");

        // Files are not among the types this target lists: it does not light up, it answers the dragover with the
        // no-drop effect, and a drop (which a browser would not even raise) is ignored.
        var files = await NativeDataTransferAsync(page, TwoFiles);
        await DispatchDragAsync(bin, "dragenter", files);
        await DispatchDragAsync(bin, "dragover", files);
        var over = await LastDragOverAsync(bin);
        Assert.IsTrue(over.Inside && over.Prevented, $"the module did not answer the dragover of a refused drag: {over}");
        Assert.AreEqual("none", over.DropEffect, $"a refused native drag must show the no-drop effect: {over}");
        await DispatchDragAsync(bin, "drop", files);
        await WaitForQuietAsync(page);

        await Expect(bin).ToHaveTextAsync("Drag selected text / a link here");
        await Expect(result.Locator("h4")).ToHaveTextAsync("Last native drop - __NATIVE_TEXT__");
    }
}

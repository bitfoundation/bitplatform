using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitFileInput (<c>/e2e/file-input</c>): files picked through the real file chooser that the
/// Browse button (and the Browse method, and a LabelTemplate calling it) opens, set on the input, dropped onto the
/// field and pasted into it; Multiple, Directory, Append, AutoReset and duplicates; MaxSize, MinSize, MaxCount,
/// MaxTotalSize, Accept, AllowedExtensions, Capture and a custom FileValidator with the image dimensions it can read;
/// previews and file icons; removing files and where the focus goes; the localizable texts, the size formatter and the
/// announcements; HideFileList and the FileViewTemplate; every event; the public API (Browse, Reset, ReadContentAsync,
/// OpenReadStreamAsync, RemoveFile, FocusAsync); the accessibility tree; the variants and the drop zone; the BitParams
/// cascade; EditForm validation; every Color and Size; Style / Class / Styles / Classes; the public CSS variables;
/// RTL; the disabled and read-only contracts; runtime re-renders, prerendering, unmounting and reduced motion.
/// </summary>
[TestClass]
public class BitFileInputE2ETests : E2ETestBase
{
    private const string Route = "/e2e/file-input";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator FileField(string id) => Page.Locator($"#{id} input.bit-fin-fi");

    private ILocator Browse(string id) => Page.Locator($"#{id} button.bit-fin-lbl");

    private ILocator Items(string id) => Page.Locator($"#{id} .bit-fin-itm");

    private ILocator Item(string id, string name) => Page.Locator($"#{id} .bit-fin-itm").Filter(new() { Has = Page.Locator($"[title='{name}']") });

    private Task ExpectTextAsync(string id, string text) => Expect(Page.Locator($"#{id}")).ToHaveTextAsync(text);

    private static FilePayload Text(string name, string content = "hello", string mime = "text/plain")
        => new() { Name = name, MimeType = mime, Buffer = Encoding.UTF8.GetBytes(content) };

    private static FilePayload Sized(string name, int size, string mime = "application/octet-stream")
        => new() { Name = name, MimeType = mime, Buffer = new byte[size] };

    private async Task<FilePayload> SampleAsync(string name, string mime)
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}/samples/{name}");
        Assert.IsTrue(response.Ok, $"The sample {name} is not served.");
        return new() { Name = name, MimeType = mime, Buffer = await response.BodyAsync() };
    }

    private Task SelectAsync(string id, params FilePayload[] files) => FileField(id).SetInputFilesAsync(files);

    private async Task PickThroughChooserAsync(ILocator opener, params FilePayload[] files)
    {
        var chooser = await Page.RunAndWaitForFileChooserAsync(() => opener.ClickAsync());
        await chooser.SetFilesAsync(files);
    }

    /// <summary>
    /// Fires the drag and drop of text files onto an element, the way a file dragged from the desktop arrives.
    /// </summary>
    private Task DropAsync(ILocator target, params string[] names)
    {
        return target.EvaluateAsync(@"(el, names) => {
            const data = new DataTransfer();
            for (const name of names) data.items.add(new File(['dropped ' + name], name, { type: 'text/plain' }));
            const fire = type => el.dispatchEvent(new DragEvent(type, { dataTransfer: data, bubbles: true, cancelable: true }));
            fire('dragenter');
            fire('dragover');
            fire('drop');
        }", names);
    }

    private Task PasteAsync(ILocator target, params string[] names)
    {
        return target.EvaluateAsync(@"(el, names) => {
            const data = new DataTransfer();
            for (const name of names) data.items.add(new File(['pasted ' + name], name, { type: 'text/plain' }));
            el.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
        }", names);
    }

    /// <summary>
    /// Waits until a field accepts (or refuses) a drag of files, which is the state the script of the drop zone is
    /// updated to after a parameter change - a round trip that renders nothing of its own.
    /// </summary>
    private Task WaitForDropAcceptanceAsync(ILocator target, bool accepted)
    {
        return target.EvaluateAsync(@"(el, accepted) => new Promise(resolve => {
            const probe = () => {
                const data = new DataTransfer();
                data.items.add(new File(['probe'], 'probe.txt', { type: 'text/plain' }));
                el.dispatchEvent(new DragEvent('dragenter', { dataTransfer: data, bubbles: true, cancelable: true }));
                const now = el.classList.contains('bit-fin-drg');
                el.dispatchEvent(new DragEvent('dragend', { bubbles: true }));
                if (now === accepted) resolve(); else setTimeout(probe, 50);
            };
            probe();
        })", accepted);
    }

    private Task<string> CssAsync(ILocator locator, string property) => locator.EvaluateAsync<string>($"el => getComputedStyle(el).getPropertyValue('{property}')");

    private static double Px(string value) => double.Parse(value.Replace("px", ""), CultureInfo.InvariantCulture);

    private Task<string> ResolveColorAsync(string value)
    {
        return Page.EvaluateAsync<string>(@"value => {
            const probe = document.createElement('span');
            probe.style.color = value;
            document.body.appendChild(probe);
            const color = getComputedStyle(probe).color;
            probe.remove();
            return color;
        }", value);
    }

    private static string TempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), "bit-fin-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    // ---------------------------------------------------------------- lifecycle

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheInputBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"fin-basic\"");
        StringAssert.Contains(html, "bit-fin-lbl");
        StringAssert.Contains(html, "Browse or drop a file");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync(Route);
        await Expect(Browse("fin-basic")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Unmount_RemovingTheInputWithPreviews_LeavesNoServerOrConsoleError()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-unmount", await SampleAsync("fileinput-photo.png", "image/png"), Text("a.txt"));
        await Expect(Items("fin-unmount")).ToHaveCountAsync(2);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Root("fin-unmount")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Browse("fin-unmount")).ToBeVisibleAsync();
        await Page.ClickAsync("#btn-unmount");
        await Expect(Root("fin-unmount")).ToHaveCountAsync(0);

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Runtime_ChangingParametersFromThePage_ReRendersTheInput()
    {
        await OpenAsync(Route);

        var root = Root("fin-runtime");
        await Page.ClickAsync("#btn-runtime-color");
        await Expect(root).ToHaveClassAsync(HasClass("bit-fin-err"));
        var before = Px(await CssAsync(Browse("fin-runtime"), "min-height"));
        await Page.ClickAsync("#btn-runtime-size");
        await Expect(root).ToHaveClassAsync(HasClass("bit-fin-lg"));
        Assert.IsTrue(Px(await CssAsync(Browse("fin-runtime"), "min-height")) > before);

        await Expect(FileField("fin-runtime")).Not.ToHaveAttributeAsync("multiple", new Regex(".*"));
        await Page.ClickAsync("#btn-runtime-multiple");
        await Expect(FileField("fin-runtime")).ToHaveAttributeAsync("multiple", "");

        await Page.ClickAsync("#btn-runtime-dropzone");
        await Expect(root).ToHaveClassAsync(HasClass("bit-fin-dzn"));
        await Expect(root.Locator(".bit-fin-dzi")).ToHaveCountAsync(1);

        await Page.ClickAsync("#btn-runtime-label");
        await Expect(Browse("fin-runtime")).ToHaveTextAsync("Changed label");
    }

    // ---------------------------------------------------------------- basic

    [TestMethod]
    public async Task Basic_BrowsingAFile_ListsItAndBindsItsName()
    {
        await OpenAsync(Route);

        var button = Browse("fin-basic");
        await Expect(button).ToHaveTextAsync("Browse or drop a file");
        await Expect(button).ToHaveAttributeAsync("title", "Pick one file from your device");
        await Expect(button).ToHaveAttributeAsync("type", "button");

        var chooser = await Page.RunAndWaitForFileChooserAsync(() => button.ClickAsync());
        Assert.IsFalse(chooser.IsMultiple);
        await chooser.SetFilesAsync(Text("notes.txt"));

        await ExpectTextAsync("basic-value", "notes.txt");
        await ExpectTextAsync("basic-changes", "1");
        await ExpectTextAsync("basic-files", "notes.txt");

        var item = Items("fin-basic").First;
        await Expect(item).ToHaveClassAsync(HasClass("bit-fin-vld"));
        await Expect(item.Locator(".bit-fin-fnm")).ToHaveTextAsync("notes");
        await Expect(item.Locator(".bit-fin-fnx")).ToHaveTextAsync(".txt");
        await Expect(item.Locator(".bit-fin-fs")).ToHaveTextAsync("5 bytes");
        await Expect(Root("fin-basic").Locator(".bit-fin-lvr")).ToHaveTextAsync(new Regex("^1 file selected\\.​?$"));
        await Expect(Root("fin-basic").Locator("input.bit-input-hidden")).ToHaveValueAsync("notes.txt");

        // Another pick replaces the first.
        await SelectAsync("fin-basic", Sized("data.bin", 3000));
        await ExpectTextAsync("basic-value", "data.bin");
        await Expect(Items("fin-basic")).ToHaveCountAsync(1);
        await Expect(Items("fin-basic").First.Locator(".bit-fin-fs")).ToHaveTextAsync("2KB");
    }

    [TestMethod]
    public async Task Basic_ThePageClearingTheValue_ClearsTheSelection()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-basic", Text("a.txt"));
        await ExpectTextAsync("basic-value", "a.txt");

        await Page.ClickAsync("#btn-basic-clear");
        await Expect(Items("fin-basic")).ToHaveCountAsync(0);
        await ExpectTextAsync("basic-value", "null");
    }

    // ---------------------------------------------------------------- disabled & read-only

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndTakesNoFile()
    {
        await OpenAsync(Route);

        var root = Root("fin-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Browse("fin-disabled")).ToBeDisabledAsync();
        await Expect(Browse("fin-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(FileField("fin-disabled")).ToBeDisabledAsync();
        await Expect(root.Locator("input.bit-input-hidden")).ToBeDisabledAsync();

        var choosers = 0;
        Page.FileChooser += (_, _) => choosers++;
        await Browse("fin-disabled").ClickAsync(new() { Force = true });
        await root.ClickAsync(new() { Force = true });
        await DropAsync(root, "dropped.txt");
        await PasteAsync(root, "pasted.txt");
        await NextFramesAsync();

        Assert.AreEqual(0, choosers);
        await ExpectTextAsync("disabled-clicks", "0");
        await ExpectTextAsync("disabled-files", "");
        Assert.AreNotEqual(await CssAsync(Browse("fin-basic"), "background-color"), await CssAsync(Browse("fin-disabled"), "background-color"));

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Browse("fin-disabled")).ToBeEnabledAsync();
        await PickThroughChooserAsync(Browse("fin-disabled"), Text("enabled.txt"));
        await ExpectTextAsync("disabled-files", "enabled.txt");
        await Expect(Page.Locator("#disabled-clicks")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task ReadOnly_OpensNoChooserAndTakesNoDropOrPaste()
    {
        await OpenAsync(Route);

        var root = Root("fin-readonly");
        await Expect(root).ToHaveClassAsync(HasClass("bit-fin-rdl"));
        // Kept focusable (no disabled attribute), only said to be unavailable.
        await Expect(Browse("fin-readonly")).Not.ToHaveAttributeAsync("disabled", new Regex(".*"));
        await Expect(Browse("fin-readonly")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(FileField("fin-readonly")).ToBeDisabledAsync();
        await Expect(root.Locator("input.bit-input-hidden")).ToHaveAttributeAsync("readonly", "");

        var choosers = 0;
        Page.FileChooser += (_, _) => choosers++;
        await Browse("fin-readonly").ClickAsync(new() { Force = true });
        await DropAsync(root, "dropped.txt");
        await PasteAsync(root, "pasted.txt");
        await NextFramesAsync();

        Assert.AreEqual(0, choosers);
        await ExpectTextAsync("readonly-files", "");

        // It keeps its place in the tab order.
        await Browse("fin-readonly").FocusAsync();
        await Expect(Browse("fin-readonly")).ToBeFocusedAsync();
    }

    // ---------------------------------------------------------------- drop & paste

    [TestMethod]
    public async Task Drop_DroppedFilesAreTakenAndTheFieldShowsTheDragOverIt()
    {
        await OpenAsync(Route);

        var root = Root("fin-drop");
        await root.EvaluateAsync(@"el => {
            const data = new DataTransfer();
            data.items.add(new File(['x'], 'x.txt', { type: 'text/plain' }));
            el.dispatchEvent(new DragEvent('dragenter', { dataTransfer: data, bubbles: true, cancelable: true }));
        }");
        await Expect(root).ToHaveClassAsync(HasClass("bit-fin-drg"));
        await Expect(root).ToHaveClassAsync(HasClass("custom-dragging"));
        await Expect(root).ToHaveCSSAsync("outline-color", "rgb(1, 2, 3)");

        await root.EvaluateAsync(@"el => {
            const data = new DataTransfer();
            data.items.add(new File(['x'], 'x.txt', { type: 'text/plain' }));
            el.dispatchEvent(new DragEvent('dragleave', { dataTransfer: data, bubbles: true, cancelable: true }));
        }");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-fin-drg"));
        await Expect(root).Not.ToHaveClassAsync(HasClass("custom-dragging"));

        await DropAsync(root, "a.txt", "b.txt");
        await ExpectTextAsync("drop-files", "a.txt|b.txt");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-fin-drg"));

        // A single file input takes the first of several dropped files.
        await DropAsync(Root("fin-drop-single"), "first.txt", "second.txt");
        await ExpectTextAsync("drop-single-files", "first.txt");

        await Page.ClickAsync("#btn-toggle-drop");
        await WaitForDropAcceptanceAsync(root, false);
        await DropAsync(root, "ignored.txt");
        await NextFramesAsync();
        await ExpectTextAsync("drop-files", "a.txt|b.txt");
    }

    [TestMethod]
    public async Task Paste_PastedFilesAreTakenUnlessAllowPasteIsOff()
    {
        await OpenAsync(Route);

        var root = Root("fin-drop");
        await PasteAsync(root, "clip.txt");
        await ExpectTextAsync("drop-files", "clip.txt");

        // The script is updated with both switches at once, so the drop switch tells when the paste one has landed.
        await Page.ClickAsync("#btn-toggle-paste");
        await Page.ClickAsync("#btn-toggle-drop");
        await WaitForDropAcceptanceAsync(root, false);
        await PasteAsync(root, "ignored.txt");
        await NextFramesAsync();
        await ExpectTextAsync("drop-files", "clip.txt");

        // Dropping works again with the paste still turned off.
        await Page.ClickAsync("#btn-toggle-drop");
        await WaitForDropAcceptanceAsync(root, true);
        await DropAsync(root, "dropped.txt");
        await ExpectTextAsync("drop-files", "dropped.txt");
    }

    // ---------------------------------------------------------------- description, multiple, directory

    [TestMethod]
    public async Task Description_DescribesTheBrowseButtonAndAcceptFiltersTheChooser()
    {
        await OpenAsync(Route);

        await Expect(Root("fin-description").Locator(".bit-fin-dsc")).ToHaveTextAsync("PDF or DOCX, up to 5 MB.");
        await Expect(Browse("fin-description")).ToHaveAccessibleDescriptionAsync("PDF or DOCX, up to 5 MB.");
        await Expect(FileField("fin-description")).ToHaveAttributeAsync("accept", ".pdf,.docx");

        await Expect(Root("fin-description-template").Locator(".bit-fin-dsc .dsc-tpl")).ToHaveTextAsync("Images only");
        await Expect(Browse("fin-description-template")).ToHaveAccessibleDescriptionAsync("Images only");
    }

    [TestMethod]
    public async Task Multiple_OpensAMultiSelectChooserAndListsEveryFile()
    {
        await OpenAsync(Route);

        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Browse("fin-multiple").ClickAsync());
        Assert.IsTrue(chooser.IsMultiple);
        await chooser.SetFilesAsync([Text("a.txt"), Text("b.txt"), Text("c.txt")]);

        await ExpectTextAsync("multiple-files", "a.txt|b.txt|c.txt");
        await Expect(Items("fin-multiple")).ToHaveCountAsync(3);
        await Expect(Root("fin-multiple").Locator(".bit-fin-lvr")).ToHaveTextAsync(new Regex("^3 files selected\\.​?$"));
    }

    [TestMethod]
    public async Task Directory_PicksAFolderAndShowsWhereEachFileCameFrom()
    {
        await OpenAsync(Route);

        await Expect(FileField("fin-directory")).ToHaveAttributeAsync("webkitdirectory", "");

        var folder = TempDirectory();
        try
        {
            Directory.CreateDirectory(Path.Combine(folder, "docs"));
            await File.WriteAllTextAsync(Path.Combine(folder, "root.txt"), "root");
            await File.WriteAllTextAsync(Path.Combine(folder, "docs", "readme.txt"), "readme");

            await FileField("fin-directory").SetInputFilesAsync(folder);

            var name = Path.GetFileName(folder);
            await Expect(Page.Locator("#directory-files")).ToHaveTextAsync(new Regex($"{name}/docs/readme.txt"));
            await Expect(Page.Locator("#directory-files")).ToHaveTextAsync(new Regex($"{name}/root.txt"));
            await Expect(Items("fin-directory")).ToHaveCountAsync(2);
            var paths = Root("fin-directory").Locator(".bit-fin-fp");
            await Expect(paths).ToHaveCountAsync(2);
            await Expect(paths.Filter(new() { HasTextRegex = new Regex($"^{name}/docs$") })).ToHaveCountAsync(1);
            await Expect(paths.Filter(new() { HasTextRegex = new Regex($"^{name}$") })).ToHaveCountAsync(1);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    // ---------------------------------------------------------------- removable

    [TestMethod]
    public async Task Removable_TheRemoveButtonDropsTheFileAndHandsTheFocusOn()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-removable", Text("a.txt"), Text("b.txt"), Text("c.txt"));
        await ExpectTextAsync("removable-value", "a.txt,b.txt,c.txt");

        var remove = Items("fin-removable").First.Locator(".bit-fin-rbt");
        await Expect(remove).ToHaveAttributeAsync("aria-label", "Remove a.txt");
        await Expect(remove).ToHaveAttributeAsync("title", "Remove");
        await Expect(remove.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Delete"));

        await remove.ClickAsync();
        await ExpectTextAsync("removable-value", "b.txt,c.txt");
        await Expect(Items("fin-removable").First.Locator(".bit-fin-rbt")).ToBeFocusedAsync();
        await Expect(Items("fin-removable").First.Locator(".bit-fin-rbt")).ToHaveAttributeAsync("aria-label", "Remove b.txt");

        // From the keyboard too, and the last one hands the focus back to the browse button.
        await Page.Keyboard.PressAsync("Enter");
        await ExpectTextAsync("removable-value", "c.txt");
        // The focus is handed on once the removal has rendered; a key pressed before that lands nowhere.
        await Expect(Items("fin-removable").First.Locator(".bit-fin-rbt")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Space");
        await ExpectTextAsync("removable-value", "null");
        await Expect(Browse("fin-removable")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task Removable_TheRemoveButtonTitleAndIconCanBeReplaced()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-remove-icon", Text("x.txt"));
        var remove = Items("fin-remove-icon").First.Locator(".bit-fin-rbt");
        await Expect(remove).ToHaveAttributeAsync("title", "Discard");
        await Expect(remove).ToHaveAttributeAsync("aria-label", "Discard x.txt");
        await Expect(remove.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));

        await SelectAsync("fin-remove-external", Text("y.txt"));
        await Expect(Items("fin-remove-external").First.Locator(".bit-fin-rbt i")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid\s+fa-trash(\s|$)"));
    }

    // ---------------------------------------------------------------- append, auto reset, duplicates

    [TestMethod]
    public async Task Append_AddsToTheListAndFlagsADuplicate()
    {
        await OpenAsync(Route);

        var folder = TempDirectory();
        try
        {
            var a = Path.Combine(folder, "a.txt");
            var b = Path.Combine(folder, "b.txt");
            await File.WriteAllTextAsync(a, "aaa");
            await File.WriteAllTextAsync(b, "bbb");

            await FileField("fin-append").SetInputFilesAsync(a);
            await FileField("fin-append").SetInputFilesAsync(b);
            await ExpectTextAsync("append-value", "a.txt,b.txt");

            await FileField("fin-append").SetInputFilesAsync(a);
            await Expect(Items("fin-append")).ToHaveCountAsync(3);
            var duplicate = Items("fin-append").Nth(2);
            await Expect(duplicate).ToHaveClassAsync(HasClass("bit-fin-inv"));
            await Expect(duplicate.Locator(".bit-fin-msg")).ToHaveTextAsync("This file has already been picked.");
            await ExpectTextAsync("append-value", "a.txt,b.txt");

            // Removing the original makes the second copy the only one, and valid.
            await Items("fin-append").First.Locator(".bit-fin-rbt").ClickAsync();
            await Expect(Items("fin-append")).ToHaveCountAsync(2);
            await Expect(Root("fin-append").Locator(".bit-fin-inv")).ToHaveCountAsync(0);
            await ExpectTextAsync("append-value", "b.txt,a.txt");
        }
        finally
        {
            Directory.Delete(folder, true);
        }

        // Without Append a new pick replaces the old one.
        await SelectAsync("fin-replace", Text("one.txt"), Text("two.txt"));
        await SelectAsync("fin-replace", Text("three.txt"));
        await ExpectTextAsync("replace-value", "three.txt");
    }

    [TestMethod]
    public async Task AutoReset_ClearsTheSelectionWhenTheChooserOpensAgain()
    {
        await OpenAsync(Route);

        await PickThroughChooserAsync(Browse("fin-autoreset"), Text("first.txt"));
        await ExpectTextAsync("autoreset-value", "first.txt");

        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Browse("fin-autoreset").ClickAsync());
        await Expect(Items("fin-autoreset")).ToHaveCountAsync(0);
        await ExpectTextAsync("autoreset-value", "null");
        await chooser.SetFilesAsync(Text("second.txt"));
        await ExpectTextAsync("autoreset-value", "second.txt");
    }

    // ---------------------------------------------------------------- limits

    [TestMethod]
    public async Task SizeLimits_RejectTooBigAndTooSmallFilesWithTheirMessages()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-size", Sized("big.bin", 3000), Sized("tiny.bin", 5), Sized("fine.bin", 100));

        await Expect(Item("fin-size", "big.bin")).ToHaveClassAsync(HasClass("bit-fin-inv"));
        await Expect(Item("fin-size", "big.bin").Locator(".bit-fin-msg")).ToHaveTextAsync("The file is too big!");
        await Expect(Item("fin-size", "tiny.bin").Locator(".bit-fin-msg")).ToHaveTextAsync("The file is too small!");
        await Expect(Item("fin-size", "fine.bin")).ToHaveClassAsync(HasClass("bit-fin-vld"));
        await ExpectTextAsync("size-value", "fine.bin");
        await Expect(Root("fin-size").Locator(".bit-fin-lvr")).ToHaveTextAsync(new Regex("^3 files selected\\. 2 of them invalid\\.​?$"));
    }

    [TestMethod]
    public async Task CountAndTotalLimits_InvalidateTheFilesPastThemAndRecoverOnRemove()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-count", Sized("1.bin", 500), Sized("2.bin", 500), Sized("3.bin", 500), Sized("4.bin", 500));
        await Expect(Item("fin-count", "4.bin").Locator(".bit-fin-msg")).ToHaveTextAsync("Three at most.");
        await ExpectTextAsync("count-value", "1.bin,2.bin,3.bin");

        await Item("fin-count", "1.bin").Locator(".bit-fin-rbt").ClickAsync();
        await Expect(Root("fin-count").Locator(".bit-fin-inv")).ToHaveCountAsync(0);
        await ExpectTextAsync("count-value", "2.bin,3.bin,4.bin");

        await SelectAsync("fin-count", Sized("big.bin", 1600));
        await Expect(Item("fin-count", "big.bin").Locator(".bit-fin-msg")).ToHaveTextAsync(new Regex("Three at most\\.|Too much in total\\."));

        await Item("fin-count", "2.bin").Locator(".bit-fin-rbt").ClickAsync();
        await Item("fin-count", "3.bin").Locator(".bit-fin-rbt").ClickAsync();
        await ExpectTextAsync("count-value", "4.bin,big.bin");
        await SelectAsync("fin-count", Sized("over.bin", 1000));
        await Expect(Item("fin-count", "over.bin").Locator(".bit-fin-msg")).ToHaveTextAsync("Too much in total.");
    }

    [TestMethod]
    public async Task Limits_WithoutTheirOwnMessages_SayTheDefaultOnes()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-default-messages", Sized("big.txt", 3000), Sized("tiny.txt", 5), Sized("bad.pdf", 100), Sized("ok.txt", 100), Sized("second.txt", 100));

        await Expect(Item("fin-default-messages", "big.txt").Locator(".bit-fin-msg")).ToHaveTextAsync("The file size is larger than the max size");
        await Expect(Item("fin-default-messages", "tiny.txt").Locator(".bit-fin-msg")).ToHaveTextAsync("The file size is smaller than the min size");
        await Expect(Item("fin-default-messages", "bad.pdf").Locator(".bit-fin-msg")).ToHaveTextAsync("The file type is not allowed");
        await Expect(Item("fin-default-messages", "second.txt").Locator(".bit-fin-msg")).ToHaveTextAsync("The maximum number of files is exceeded");
        await Expect(Item("fin-default-messages", "ok.txt")).ToHaveClassAsync(HasClass("bit-fin-vld"));
    }

    // ---------------------------------------------------------------- accept & allowed extensions

    [TestMethod]
    public async Task AllowedExtensions_FilterTheChooserAndRejectOtherTypes()
    {
        await OpenAsync(Route);

        await Expect(FileField("fin-accept")).ToHaveAttributeAsync("accept", "image/*");
        await Expect(FileField("fin-allowed")).ToHaveAttributeAsync("accept", ".pdf,.docx,image/*");
        await Expect(FileField("fin-capture")).ToHaveAttributeAsync("capture", "environment");
        await Expect(FileField("fin-basic")).Not.ToHaveAttributeAsync("accept", new Regex(".*"));

        await SelectAsync("fin-allowed", Sized("a.pdf", 10, "application/pdf"), Sized("b.DOCX", 10, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
                          Sized("c.png", 10, "image/png"), Sized("d.txt", 10, "text/plain"));

        await ExpectTextAsync("allowed-value", "a.pdf,b.DOCX,c.png");
        await Expect(Item("fin-allowed", "d.txt").Locator(".bit-fin-msg")).ToHaveTextAsync("Only PDF, DOCX or images.");
    }

    // ---------------------------------------------------------------- custom validation

    [TestMethod]
    public async Task FileValidator_RejectsWhatItSaysAndCanReadImageDimensions()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-validator", Sized("empty.txt", 0), Text("full.txt"));
        await Expect(Item("fin-validator", "empty.txt").Locator(".bit-fin-msg")).ToHaveTextAsync("The file is empty.");
        await ExpectTextAsync("validator-value", "full.txt");

        await SelectAsync("fin-dimensions", await SampleAsync("fileinput-photo.png", "image/png"), await SampleAsync("fileinput-wide.png", "image/png"));
        await ExpectTextAsync("dimensions", "fileinput-photo.png:40x30|fileinput-wide.png:120x20");
        await Expect(Item("fin-dimensions", "fileinput-wide.png").Locator(".bit-fin-msg")).ToHaveTextAsync("Too wide: 120px.");
        await Expect(Item("fin-dimensions", "fileinput-photo.png")).ToHaveClassAsync(HasClass("bit-fin-vld"));
    }

    // ---------------------------------------------------------------- preview & icons

    [TestMethod]
    public async Task Preview_ShowsAThumbnailForAnImageAndAnIconPerFileKind()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-preview", await SampleAsync("fileinput-photo.png", "image/png"), await SampleAsync("fileinput-doc.pdf", "application/pdf"),
                          Sized("letter.docx", 10, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
                          Sized("sheet.csv", 10, "text/csv"), Sized("archive.zip", 10, "application/zip"), Text("plain.txt"),
                          Sized("clip.mp4", 10, "video/mp4"), Sized("song.mp3", 10, "audio/mpeg"), Sized("blob.bin", 10));

        var thumbnail = Item("fin-preview", "fileinput-photo.png").Locator("img.bit-fin-prv");
        await Expect(thumbnail).ToHaveAttributeAsync("src", new Regex("^blob:"));
        await Expect(thumbnail).ToHaveAttributeAsync("alt", "");
        Assert.IsTrue(await thumbnail.EvaluateAsync<bool>("img => img.complete && img.naturalWidth === 40"), "The thumbnail did not load the image.");

        foreach (var (name, icon) in new[]
        {
            ("fileinput-doc.pdf", "PDF"), ("letter.docx", "WordDocument"), ("sheet.csv", "ExcelDocument"), ("archive.zip", "ZipFolder"),
            ("plain.txt", "TextDocument"), ("clip.mp4", "Video"), ("song.mp3", "MusicInCollection"), ("blob.bin", "Page")
        })
        {
            await Expect(Item("fin-preview", name).Locator(".bit-fin-fio i")).ToHaveClassAsync(HasClass($"bit-icon--{icon}"));
        }

        await SelectAsync("fin-icon-selector", Sized("a.pdf", 10, "application/pdf"), Text("b.txt"), Sized("c.bin", 10));
        await Expect(Item("fin-icon-selector", "a.pdf").Locator(".bit-fin-fio i")).ToHaveClassAsync(HasClass("bit-icon--PDF"));
        await Expect(Item("fin-icon-selector", "b.txt").Locator(".bit-fin-fio i")).ToHaveClassAsync(HasClass("custom-text-icon"));
        await Expect(Item("fin-icon-selector", "c.bin").Locator(".bit-fin-fio")).ToHaveCountAsync(0);
    }

    // ---------------------------------------------------------------- localization & hidden list

    [TestMethod]
    public async Task Localization_FormatsTheSizesNamesTheButtonsAndWordsTheAnnouncement()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-localized", Sized("ok.bin", 500), Sized("big.bin", 1500));

        await Expect(Item("fin-localized", "big.bin").Locator(".bit-fin-fs")).ToHaveTextAsync("1,500 bytes");
        await Expect(Item("fin-localized", "big.bin").Locator(".bit-fin-msg")).ToHaveTextAsync("Too large.");
        await Expect(Item("fin-localized", "ok.bin").Locator(".bit-fin-rbt")).ToHaveAttributeAsync("title", "Delete it");
        await Expect(Item("fin-localized", "ok.bin").Locator(".bit-fin-rbt")).ToHaveAttributeAsync("aria-label", "Delete it ok.bin");
        await Expect(Root("fin-localized").Locator("[role='list']")).ToHaveAttributeAsync("aria-label", "Chosen documents");
        await Expect(Root("fin-localized").Locator(".bit-fin-lvr")).ToHaveTextAsync(new Regex("^Picked 2, 1 bad​?$"));
    }

    [TestMethod]
    public async Task HideFileList_KeepsTheListOutOfThePageButReportsTheFiles()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-hidden-list", Text("a.txt"), Text("b.txt"));
        await ExpectTextAsync("hidden-list", "a.txt|b.txt");
        await Expect(Root("fin-hidden-list").Locator("[role='list']")).ToHaveCountAsync(0);
        await Expect(Items("fin-hidden-list")).ToHaveCountAsync(0);
    }

    // ---------------------------------------------------------------- events

    [TestMethod]
    public async Task Events_ChangeInvalidAndRemoveFireWithTheirFiles()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-events", Sized("good.bin", 100), Sized("big.bin", 2000));

        await ExpectTextAsync("change-count", "1");
        await ExpectTextAsync("change-args", "good.bin|big.bin");
        await ExpectTextAsync("invalid-count", "1");
        await ExpectTextAsync("invalid-args", "big.bin:The file size is larger than the max size");
        await ExpectTextAsync("onchange-value", "good.bin");

        await Item("fin-events", "good.bin").Locator(".bit-fin-rbt").ClickAsync();
        await ExpectTextAsync("remove-count", "1");
        await ExpectTextAsync("remove-args", "good.bin");
        await ExpectTextAsync("change-count", "2");
        await ExpectTextAsync("change-args", "big.bin");
        await ExpectTextAsync("invalid-count", "2");
        await ExpectTextAsync("onchange-value", "null");
    }

    // ---------------------------------------------------------------- templates

    [TestMethod]
    public async Task Templates_ALabelTemplateBrowsesAndAFileViewTemplateDrawsEachFile()
    {
        await OpenAsync(Route);

        var root = Root("fin-templates");
        await Expect(root).ToHaveClassAsync(HasClass("bit-fin-nlb"));
        await Expect(root.Locator("button.bit-fin-lbl")).ToHaveCountAsync(0);

        await PickThroughChooserAsync(Page.Locator("#custom-browse"), Text("one.txt"), Sized("two.png", 20, "image/png"));

        var cards = root.Locator(".file-card");
        await Expect(cards).ToHaveCountAsync(2);
        await Expect(root.Locator("[role='listitem'] .file-card")).ToHaveCountAsync(2);
        await Expect(cards.Nth(0)).ToHaveAttributeAsync("data-name", "one.txt");
        await Expect(cards.Nth(0)).ToHaveAttributeAsync("data-index", "0");
        await Expect(cards.Nth(1).Locator(".file-card-size")).ToHaveTextAsync("20");
        await Expect(cards.Nth(1).Locator(".file-card-type")).ToHaveTextAsync("image/png");
        await Expect(cards.Nth(1).Locator(".file-card-ext")).ToHaveTextAsync(".png");
        await Expect(root.Locator(".bit-fin-itm")).ToHaveCountAsync(0);

        await cards.Nth(0).Locator(".file-card-remove").ClickAsync();
        await Expect(cards).ToHaveCountAsync(1);
        await Expect(cards.Nth(0)).ToHaveAttributeAsync("data-index", "0");
    }

    // ---------------------------------------------------------------- public api

    [TestMethod]
    public async Task PublicApi_BrowsesReadsRemovesResetsAndFocuses()
    {
        await OpenAsync(Route);

        await Expect(Root("fin-api").Locator("button.bit-fin-lbl")).ToHaveCountAsync(0);

        await PickThroughChooserAsync(Page.Locator("#api-browse"), Text("first.txt", "hello"), Text("second.txt", "a longer content"));
        await ExpectTextAsync("api-files", "first.txt|second.txt");
        await Expect(Page.Locator("#api-input-id")).ToHaveTextAsync(await FileField("fin-api").GetAttributeAsync("id") ?? "");

        await Page.ClickAsync("#api-read");
        await ExpectTextAsync("api-content", "5:hello");
        await Page.ClickAsync("#api-stream");
        await ExpectTextAsync("api-stream-length", "16");

        await Page.ClickAsync("#api-remove-first");
        await ExpectTextAsync("api-files", "second.txt");

        await Page.ClickAsync("#api-reset");
        await ExpectTextAsync("api-files", "");

        await SelectAsync("fin-api", Text("x.txt"), Text("y.txt"));
        await ExpectTextAsync("api-files", "x.txt|y.txt");
        await Page.ClickAsync("#api-remove-all");
        await ExpectTextAsync("api-files", "");

        // FocusAsync is a no-op without a browse button, and focuses the button otherwise.
        await Page.ClickAsync("#api-focus");
        await Expect(Page.Locator("#api-focus")).ToBeFocusedAsync();
        await Page.ClickAsync("#api-focus-labelled");
        await Expect(Browse("fin-api-focus")).ToBeFocusedAsync();
    }

    // ---------------------------------------------------------------- accessibility

    [TestMethod]
    public async Task Accessibility_TheButtonTheListAndTheMessagesAreNamedAndDescribed()
    {
        await OpenAsync(Route);

        await Expect(Browse("fin-a11y")).ToHaveAccessibleNameAsync("Select documents to attach");
        await Expect(Browse("fin-a11y")).ToHaveAccessibleDescriptionAsync("Up to 1 KB each.");
        await Expect(FileField("fin-a11y")).ToHaveAttributeAsync("aria-label", "Select documents to attach");
        await Expect(FileField("fin-basic")).ToHaveAttributeAsync("aria-labelledby", await Browse("fin-basic").GetAttributeAsync("id") ?? "");

        await SelectAsync("fin-a11y", Sized("ok.bin", 10), Sized("big.bin", 2000));
        var list = Root("fin-a11y").Locator("[role='list']");
        await Expect(list).ToHaveAttributeAsync("aria-label", "Selected files");
        await Expect(list.Locator("[role='listitem']")).ToHaveCountAsync(2);
        await Expect(list).Not.ToHaveAttributeAsync("tabindex", new Regex(".*"));
        await Expect(Item("fin-a11y", "big.bin").Locator(".bit-fin-rbt")).ToHaveAccessibleDescriptionAsync("The file size is larger than the max size");
        await Expect(Item("fin-a11y", "ok.bin").Locator(".bit-fin-rbt")).Not.ToHaveAttributeAsync("aria-describedby", new Regex(".*"));

        var live = Root("fin-a11y").Locator(".bit-fin-lvr");
        await Expect(live).ToHaveAttributeAsync("role", "status");
        await Expect(live).ToHaveAttributeAsync("aria-live", "polite");

        // A list with no remove buttons is a tab stop of its own, so it can be scrolled from the keyboard.
        await SelectAsync("fin-list-focus", Text("a.txt"));
        await Expect(Root("fin-list-focus").Locator("[role='list']")).ToHaveAttributeAsync("tabindex", "0");
    }

    [TestMethod]
    public async Task Keyboard_EnterAndSpaceOnTheBrowseButtonOpenTheChooser()
    {
        await OpenAsync(Route);

        await Browse("fin-multiple").FocusAsync();
        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Page.Keyboard.PressAsync("Enter"));
        await chooser.SetFilesAsync(Text("enter.txt"));
        await ExpectTextAsync("multiple-files", "enter.txt");

        await Browse("fin-multiple").FocusAsync();
        chooser = await Page.RunAndWaitForFileChooserAsync(() => Page.Keyboard.PressAsync("Space"));
        await chooser.SetFilesAsync(Text("space.txt"));
        await ExpectTextAsync("multiple-files", "space.txt");
    }

    // ---------------------------------------------------------------- variants & drop zone

    [TestMethod]
    public async Task Variant_FillOutlineAndTextPaintTheBrowseButtonApart()
    {
        await OpenAsync(Route);

        await Expect(Root("fin-var-default")).ToHaveClassAsync(HasClass("bit-fin-fil"));
        await Expect(Root("fin-var-fil")).ToHaveClassAsync(HasClass("bit-fin-fil"));
        await Expect(Root("fin-var-otl")).ToHaveClassAsync(HasClass("bit-fin-otl"));
        await Expect(Root("fin-var-txt")).ToHaveClassAsync(HasClass("bit-fin-txt"));

        var fill = Browse("fin-var-fil");
        var outline = Browse("fin-var-otl");
        var text = Browse("fin-var-txt");
        var primary = await CssAsync(fill, "background-color");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", primary);
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(outline, "background-color"));
        Assert.AreEqual(primary, await CssAsync(outline, "border-top-color"));
        Assert.AreEqual(primary, await CssAsync(outline, "color"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(text, "background-color"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(text, "border-top-color"));
        Assert.AreEqual(primary, await CssAsync(text, "color"));

        // Disabled: the fill is greyed, the outline keeps a grey rule, the text stays bare.
        Assert.AreNotEqual(primary, await CssAsync(Browse("fin-var-fil-dis"), "background-color"));
        Assert.AreNotEqual(primary, await CssAsync(Browse("fin-var-otl-dis"), "border-top-color"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(Browse("fin-var-txt-dis"), "background-color"));

        // Hovering the outline fills it with the hover shade.
        await outline.HoverAsync();
        await Expect(outline).Not.ToHaveCSSAsync("background-color", "rgba(0, 0, 0, 0)");
    }

    [TestMethod]
    public async Task DropZone_DrawsADashedAreaWithAnIconAndDefaultsToOutline()
    {
        await OpenAsync(Route);

        var root = Root("fin-dropzone");
        await Expect(root).ToHaveClassAsync(HasClass("bit-fin-dzn"));
        await Expect(root).ToHaveClassAsync(HasClass("bit-fin-otl"));
        await Expect(Browse("fin-dropzone")).ToHaveCSSAsync("border-top-style", "dashed");
        Assert.IsTrue((await Browse("fin-dropzone").BoundingBoxAsync())!.Height >= 87.5);
        var icon = root.Locator(".bit-fin-dzi");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--CloudUpload"));
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");

        await Expect(Root("fin-dropzone-folder").Locator(".bit-fin-dzi")).ToHaveClassAsync(HasClass("bit-icon--FabricFolder"));
        await Expect(Root("fin-dropzone-external").Locator(".bit-fin-dzi")).ToHaveClassAsync(new Regex(@"(^|\s)bi\s+bi-cloud-arrow-up(\s|$)"));

        await DropAsync(root, "zone.txt");
        await Expect(Items("fin-dropzone")).ToHaveCountAsync(1);
    }

    // ---------------------------------------------------------------- cascade & form

    [TestMethod]
    public async Task CascadingParameters_FillWhatTheInputLeavesUnsetAndYieldToWhatItSets()
    {
        await OpenAsync(Route);

        var cascaded = Root("fin-cascade-1");
        await Expect(Browse("fin-cascade-1")).ToHaveTextAsync("Cascaded label");
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-fin-suc"));
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-fin-lg"));
        await Expect(FileField("fin-cascade-1")).ToHaveAttributeAsync("multiple", "");
        await SelectAsync("fin-cascade-1", Sized("big.bin", 3000), Sized("ok.bin", 10));
        await Expect(Item("fin-cascade-1", "big.bin").Locator(".bit-fin-msg")).ToHaveTextAsync("Cascaded message.");
        await Expect(Item("fin-cascade-1", "ok.bin").Locator(".bit-fin-rbt")).ToHaveCountAsync(1);

        await Expect(Browse("fin-cascade-own")).ToHaveTextAsync("Its own label");
        await SelectAsync("fin-cascade-own", Sized("mid.bin", 3000));
        await Expect(Item("fin-cascade-own", "mid.bin")).ToHaveClassAsync(HasClass("bit-fin-vld"));

        await Expect(Browse("fin-cascade-outside")).ToHaveTextAsync("Outside");
        await Expect(Root("fin-cascade-outside")).Not.ToHaveClassAsync(HasClass("bit-fin-suc"));
        await Expect(FileField("fin-cascade-outside")).Not.ToHaveAttributeAsync("multiple", new Regex(".*"));
    }

    [TestMethod]
    public async Task Validation_ARequiredFileFailsTheFormUntilAnAllowedOneIsPicked()
    {
        await OpenAsync(Route);

        var root = Root("fin-form");
        var hidden = root.Locator("input.bit-input-hidden");
        await Expect(hidden).ToHaveAttributeAsync("name", "resume");
        await Expect(hidden).ToHaveAttributeAsync("data-input", "resume");
        await Expect(hidden).Not.ToHaveAttributeAsync("required", new Regex(".*"));

        await Page.ClickAsync("#fin-form-submit");
        await ExpectTextAsync("form-result", "invalid");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Browse("fin-form")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#fin-form-msg")).ToHaveTextAsync("Attach your resume.");

        await SelectAsync("fin-form", Text("resume.txt"));
        await Expect(Item("fin-form", "resume.txt")).ToHaveClassAsync(HasClass("bit-fin-inv"));
        await ExpectTextAsync("form-value", "null");

        await SelectAsync("fin-form", Sized("resume.pdf", 50, "application/pdf"));
        await ExpectTextAsync("form-value", "resume.pdf");
        await Expect(hidden).ToHaveValueAsync("resume.pdf");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Browse("fin-form")).Not.ToHaveAttributeAsync("aria-invalid", new Regex(".*"));

        await Page.ClickAsync("#fin-form-submit");
        await ExpectTextAsync("form-result", "valid");
    }

    // ---------------------------------------------------------------- color & size

    [TestMethod]
    public async Task Color_EveryValuePaintsTheBrowseButtonInItsRole()
    {
        await OpenAsync(Route);

        string[] slugs = ["pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err", "pbg", "sbg", "tbg", "pfg", "sfg", "tfg", "pbr", "sbr", "tbr"];

        var unset = await CssAsync(Browse("fin-color-none"), "background-color");
        var colors = new Dictionary<string, string>();

        foreach (var slug in slugs)
        {
            var id = $"fin-color-{slug}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-fin-{slug}"));

            var role = await Root(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-fin-clr').trim()");
            var text = await Root(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-fin-clr-txt').trim()");
            var expected = await ResolveColorAsync(role);

            await Expect(Browse(id)).ToHaveCSSAsync("background-color", expected);
            await Expect(Browse(id)).ToHaveCSSAsync("color", await ResolveColorAsync(text));
            colors[slug] = expected;
        }

        Assert.AreEqual(unset, colors["pri"], "Unset is not primary.");
        Assert.IsTrue(colors.Values.Distinct().Count() >= 12, string.Join(", ", colors.Select(c => $"{c.Key}={c.Value}")));
    }

    [TestMethod]
    [DataRow("sm", "md")]
    [DataRow("md", "lg")]
    public async Task Size_ScalesTheBrowseButton(string smaller, string larger)
    {
        await OpenAsync(Route);

        await Expect(Root($"fin-size-{smaller}")).ToHaveClassAsync(HasClass($"bit-fin-{smaller}"));
        await Expect(Root($"fin-size-{larger}")).ToHaveClassAsync(HasClass($"bit-fin-{larger}"));
        Assert.IsTrue(Px(await CssAsync(Browse($"fin-size-{larger}"), "min-height")) > Px(await CssAsync(Browse($"fin-size-{smaller}"), "min-height")), "height");
        Assert.IsTrue(Px(await CssAsync(Browse($"fin-size-{larger}"), "font-size")) > Px(await CssAsync(Browse($"fin-size-{smaller}"), "font-size")), "font size");
    }

    // ---------------------------------------------------------------- style & class

    [TestMethod]
    public async Task StyleClassAndHtmlAttributes_LandOnTheRoot()
    {
        await OpenAsync(Route);

        await Expect(Root("fin-style")).ToHaveCSSAsync("margin-left", "13px");
        await Expect(Root("fin-style")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("fin-style")).ToHaveAttributeAsync("data-test", "attr-value");
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-styles", await SampleAsync("fileinput-photo.png", "image/png"), Text("a.txt"), Sized("big.bin", 2000));

        var root = Root("fin-styles");
        await Expect(root).ToHaveCSSAsync("padding-top", "5px");
        await Expect(root.Locator(".bit-fin-lbl")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(root.Locator(".bit-fin-dzi")).ToHaveCSSAsync("color", "rgb(1, 1, 1)");
        await Expect(root.Locator(".bit-fin-dsc")).ToHaveCSSAsync("color", "rgb(2, 2, 2)");
        await Expect(root.Locator(".bit-fin-fl")).ToHaveCSSAsync("padding-top", "4px");
        await Expect(root.Locator(".bit-fin-itm").First).ToHaveCSSAsync("padding-bottom", "3px");
        await Expect(root.Locator(".bit-fin-prv")).ToHaveCSSAsync("opacity", "0.9");
        await Expect(root.Locator(".bit-fin-fio").First).ToHaveCSSAsync("opacity", "0.8");
        await Expect(root.Locator(".bit-fin-fn").First).ToHaveCSSAsync("color", "rgb(3, 3, 3)");
        await Expect(root.Locator(".bit-fin-fs").First).ToHaveCSSAsync("color", "rgb(5, 5, 5)");
        await Expect(root.Locator(".bit-fin-msg")).ToHaveCSSAsync("color", "rgb(6, 6, 6)");
        await Expect(root.Locator(".bit-fin-rbt").First).ToHaveCSSAsync("opacity", "0.7");
        await Expect(root.Locator(".bit-fin-rbt i").First).ToHaveCSSAsync("color", "rgb(7, 7, 7)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-classes", await SampleAsync("fileinput-photo.png", "image/png"), Text("a.txt"), Sized("big.bin", 2000));

        var root = Root("fin-classes");
        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        foreach (var (part, cssClass) in new[]
        {
            (".bit-fin-lbl", "c-lbl"), (".bit-fin-dzi", "c-dzi"), (".bit-fin-dsc", "c-dsc"), (".bit-fin-fl", "c-fl"), (".bit-fin-itm", "c-itm"),
            (".bit-fin-prv", "c-prv"), (".bit-fin-fio", "c-fio"), (".bit-fin-fn", "c-fn"), (".bit-fin-fs", "c-fs"), (".bit-fin-msg", "c-msg"),
            (".bit-fin-rbt", "c-rbt"), (".bit-fin-rbt i", "c-rmi")
        })
        {
            await Expect(root.Locator(part).First).ToHaveClassAsync(HasClass(cssClass));
        }
    }

    [TestMethod]
    public async Task StylesAndClasses_ReachTheFilePathOfAFolderPick()
    {
        await OpenAsync(Route);

        var folder = TempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "in-folder.txt"), "x");
            await Expect(FileField("fin-directory")).ToHaveAttributeAsync("webkitdirectory", "");
            await FileField("fin-directory").SetInputFilesAsync(folder);
            await Expect(Root("fin-directory").Locator(".bit-fin-fp")).ToHaveTextAsync(Path.GetFileName(folder));
            await Expect(Root("fin-directory").Locator(".bit-fin-fnc")).ToHaveAttributeAsync("title", $"{Path.GetFileName(folder)}/in-folder.txt");
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    // ---------------------------------------------------------------- css variables

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheInput()
    {
        await OpenAsync(Route);

        await SelectAsync("fin-vars", await SampleAsync("fileinput-photo.png", "image/png"), Text("a.txt"), Sized("big.bin", 2000),
                          Text("b.txt"), Text("c.txt"), Text("d.txt"));

        var root = Root("fin-vars");
        var button = Browse("fin-vars");
        Assert.IsTrue((await root.BoundingBoxAsync())!.Width <= 300.5);
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(218, 165, 32)");
        await Expect(button).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(button).ToHaveCSSAsync("min-height", "50px");
        await Expect(button).ToHaveCSSAsync("padding-top", "7px");
        await Expect(button).ToHaveCSSAsync("font-size", "19px");
        await Expect(button).ToHaveCSSAsync("font-weight", "700");
        await Expect(button).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(root.Locator(".bit-fin-dsc")).ToHaveCSSAsync("color", "rgb(4, 5, 6)");
        await Expect(root.Locator(".bit-fin-dsc")).ToHaveCSSAsync("font-size", "11px");

        var valid = Item("fin-vars", "a.txt");
        await Expect(valid).ToHaveCSSAsync("background-color", "rgb(7, 8, 9)");
        await Expect(valid).ToHaveCSSAsync("border-top-color", "rgb(10, 11, 12)");
        await Expect(valid).ToHaveCSSAsync("border-top-left-radius", "6px");
        await Expect(valid).ToHaveCSSAsync("margin-top", "13px");
        await Expect(valid).ToHaveCSSAsync("font-size", "15px");
        await Expect(valid).ToHaveCSSAsync("color", "rgb(13, 14, 15)");
        await Expect(valid.Locator(".bit-fin-fic")).ToHaveCSSAsync("padding-top", "9px");
        await Expect(valid.Locator(".bit-fin-fs")).ToHaveCSSAsync("color", "rgb(16, 17, 18)");
        await Expect(valid.Locator(".bit-fin-fio")).ToHaveCSSAsync("color", "rgb(25, 26, 27)");

        var invalid = Item("fin-vars", "big.bin");
        await Expect(invalid).ToHaveCSSAsync("border-top-color", "rgb(19, 20, 21)");
        await Expect(invalid).ToHaveCSSAsync("background-color", "rgb(22, 23, 24)");
        await Expect(invalid.Locator(".bit-fin-msg")).ToHaveCSSAsync("color", "rgb(19, 20, 21)");

        var preview = root.Locator(".bit-fin-prv");
        await Expect(preview).ToHaveCSSAsync("width", "30px");
        await Expect(preview).ToHaveCSSAsync("border-top-left-radius", "3px");
        await Expect(root.Locator(".bit-fin-fl")).ToHaveCSSAsync("max-height", "80px");
        Assert.IsTrue(await root.Locator(".bit-fin-fl").EvaluateAsync<bool>("el => el.scrollHeight > el.clientHeight"), "The list does not scroll.");
        await Expect(root.Locator(".bit-fin-rbt").First).ToHaveCSSAsync("color", "rgb(28, 29, 30)");
    }

    [TestMethod]
    public async Task CssVariables_DropZoneDisabledDragAndExplicitParameters()
    {
        await OpenAsync(Route);

        var zone = Browse("fin-vars-dropzone");
        await Expect(zone).ToHaveCSSAsync("min-height", "120px");
        await Expect(zone).ToHaveCSSAsync("padding-top", "12px");
        await Expect(zone).ToHaveCSSAsync("border-top-left-radius", "10px");
        await Expect(zone).ToHaveCSSAsync("border-top-width", "4px");
        await Expect(zone).ToHaveCSSAsync("border-top-style", "dotted");
        await Expect(Root("fin-vars-dropzone").Locator(".bit-fin-dzi")).ToHaveCSSAsync("font-size", "40px");

        await Expect(Browse("fin-vars-disabled")).ToHaveCSSAsync("background-color", "rgb(40, 41, 42)");
        await Expect(Browse("fin-vars-disabled")).ToHaveCSSAsync("color", "rgb(43, 44, 45)");

        await Browse("fin-vars-hover").HoverAsync();
        await Expect(Browse("fin-vars-hover")).ToHaveCSSAsync("background-color", "rgb(46, 47, 48)");

        await Root("fin-vars").EvaluateAsync(@"el => {
            const data = new DataTransfer();
            data.items.add(new File(['x'], 'x.txt', { type: 'text/plain' }));
            el.dispatchEvent(new DragEvent('dragenter', { dataTransfer: data, bubbles: true, cancelable: true }));
        }");
        await Expect(Browse("fin-vars")).ToHaveCSSAsync("background-color", "rgb(31, 32, 33)");

        await Expect(Browse("fin-vars-explicit")).Not.ToHaveCSSAsync("background-color", "rgb(218, 165, 32)");
        Assert.AreEqual(await CssAsync(Browse("fin-color-err"), "background-color"), await CssAsync(Browse("fin-vars-explicit"), "background-color"));
        await Expect(Browse("fin-vars-explicit")).Not.ToHaveCSSAsync("min-height", "50px");
    }

    // ---------------------------------------------------------------- rtl

    [TestMethod]
    public async Task Rtl_MirrorsTheFileRows()
    {
        await OpenAsync(Route);

        await Expect(Root("fin-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("fin-rtl")).ToHaveAttributeAsync("dir", "rtl");

        await SelectAsync("fin-rtl", await SampleAsync("fileinput-photo.png", "image/png"));
        var row = Items("fin-rtl").First;
        var preview = await row.Locator(".bit-fin-prv").BoundingBoxAsync();
        var remove = await row.Locator(".bit-fin-rbt").BoundingBoxAsync();
        Assert.IsTrue(preview!.X > remove!.X, "The preview is not on the right and the remove button on the left.");

        await Expect(Root("fin-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await SelectAsync("fin-rtl-inherited", Text("a.txt"));
        var name = await Items("fin-rtl-inherited").First.Locator(".bit-fin-fn").BoundingBoxAsync();
        var inheritedRemove = await Items("fin-rtl-inherited").First.Locator(".bit-fin-rbt").BoundingBoxAsync();
        Assert.IsTrue(name!.X > inheritedRemove!.X, "The inherited direction does not mirror the row.");
    }

    // ---------------------------------------------------------------- motion

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheTransitionsUnlessForceAnimationRestoresThem()
    {
        await OpenAsync(Route);

        var button = Browse("fin-motion");
        StringAssert.StartsWith(await CssAsync(button, "transition-duration"), "0.1s");

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(button).Not.ToHaveCSSAsync("transition-duration", new Regex("^0\\.1s"));
        var reduced = (await CssAsync(button, "transition-duration")).Split(',')[0].Trim();
        Assert.IsTrue(double.Parse(reduced.TrimEnd('s'), CultureInfo.InvariantCulture) < 0.001, reduced);

        await Expect(Root("fin-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        StringAssert.StartsWith(await CssAsync(Browse("fin-motion-forced"), "transition-duration"), "0.1s");
    }
}

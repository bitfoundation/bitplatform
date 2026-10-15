using System.Collections.Concurrent;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.Inputs;

/// <summary>
/// End-to-end tests of BitFileUpload (<c>/e2e/file-upload</c>) against the host's <c>/api/upload</c>,
/// <c>/api/upload-fail</c>, <c>/api/upload-slow</c> and <c>DELETE /api/upload</c> endpoints: picking files through the
/// real chooser, the input, a drop (onto the drop area and onto a DropZoneSelector zone, rejected drags included) and a
/// paste; uploading per file, automatically, all at once and through the public API; progress, pause and resume, cancel,
/// retry by hand and automatic retries, timeouts, the concurrency queue and the batch actions; chunked uploads and the
/// headers, query strings and form fields of every request (static and from providers); removing files from the list
/// and from the server, including a removal that fails; preloaded files; Append, AutoReset and duplicates; every
/// validation and its message; previews and image dimensions; the label, its icon and the description; templates;
/// every event; the accessibility tree; variants and the drop area; the BitParams cascade; EditForm validation; every
/// Color and Size; the icons; Style / Class / Styles / Classes; the public CSS variables; RTL; the disabled and read-only
/// contracts; runtime re-renders, prerendering, unmounting mid-upload and reduced motion.
/// </summary>
[TestClass]
public class BitFileUploadE2ETests : E2ETestBase
{
    private const string Route = "/e2e/file-upload";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator FileField(string id) => Page.Locator($"#{id} input.bit-upl-fi");

    private ILocator Browse(string id) => Page.Locator($"#{id} button.bit-upl-lbl");

    private ILocator Items(string id) => Page.Locator($"#{id} .bit-upl-itm");

    private ILocator Item(string id, string name) => Page.Locator($"#{id} .bit-upl-itm").Filter(new() { Has = Page.Locator($"[title='{name}']") });

    private ILocator Action(string id, string name, string title) => Item(id, name).Locator($"button.bit-upl-usi[title='{title}']");

    private ILocator Status(string id, string name) => Item(id, name).Locator(".bit-upl-us");

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
    /// Records the requests the page sends to the endpoints whose path contains the given text.
    /// </summary>
    private ConcurrentQueue<IRequest> Capture(string path)
    {
        var requests = new ConcurrentQueue<IRequest>();
        Page.Request += (_, request) =>
        {
            if (request.Url.Contains(path, StringComparison.Ordinal)) requests.Enqueue(request);
        };
        return requests;
    }

    private async Task WaitForCountAsync(ConcurrentQueue<IRequest> requests, int count)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (requests.Count < count && DateTime.UtcNow < deadline) await NextFramesAsync();
        Assert.AreEqual(count, requests.Count, "Not the expected number of requests.");
    }

    /// <summary>
    /// Records, in the page, the form data entries and the headers of every XMLHttpRequest the uploader sends: a
    /// multipart body holding a file is out of reach of the browser's request events.
    /// </summary>
    private Task InstallUploadSpyAsync()
    {
        return Page.AddInitScriptAsync(@"(() => {
            window.__uploads = [];
            const open = XMLHttpRequest.prototype.open;
            const setHeader = XMLHttpRequest.prototype.setRequestHeader;
            const send = XMLHttpRequest.prototype.send;
            XMLHttpRequest.prototype.open = function (method, url) { this.__spy = { method, url, headers: {}, fields: [] }; return open.apply(this, arguments); };
            XMLHttpRequest.prototype.setRequestHeader = function (name, value) { if (this.__spy) this.__spy.headers[name.toLowerCase()] = value; return setHeader.apply(this, arguments); };
            XMLHttpRequest.prototype.send = function (body) {
                if (this.__spy) {
                    if (body instanceof FormData) {
                        for (const [key, value] of body.entries()) this.__spy.fields.push(key + '=' + (value instanceof File ? 'file:' + value.name : value));
                    }
                    window.__uploads.push(this.__spy);
                }
                return send.apply(this, arguments);
            };
        })()");
    }

    private Task<string> UploadSpyAsync(string expression) => Page.EvaluateAsync<string>(expression);

    private Task DropAsync(ILocator target, string type, params string[] names)
    {
        return target.EvaluateAsync(@"(el, [type, names]) => {
            const data = new DataTransfer();
            for (const name of names) data.items.add(new File(['dropped ' + name], name, { type }));
            const fire = t => el.dispatchEvent(new DragEvent(t, { dataTransfer: data, bubbles: true, cancelable: true }));
            fire('dragenter');
            fire('dragover');
            fire('drop');
        }", new object[] { type, names });
    }

    private Task DragEnterAsync(ILocator target, string type, params string[] names)
    {
        return target.EvaluateAsync(@"(el, [type, names]) => {
            const data = new DataTransfer();
            for (const name of names) data.items.add(new File(['x'], name, { type }));
            el.dispatchEvent(new DragEvent('dragenter', { dataTransfer: data, bubbles: true, cancelable: true }));
        }", new object[] { type, names });
    }

    private Task DragEndAsync(ILocator target) => target.EvaluateAsync("el => el.dispatchEvent(new DragEvent('dragend', { bubbles: true }))");

    private Task PasteAsync(ILocator target, params string[] names)
    {
        return target.EvaluateAsync(@"(el, names) => {
            const data = new DataTransfer();
            for (const name of names) data.items.add(new File(['pasted ' + name], name, { type: 'text/plain' }));
            el.dispatchEvent(new ClipboardEvent('paste', { clipboardData: data, bubbles: true, cancelable: true }));
        }", names);
    }

    private Task WaitForDropAcceptanceAsync(ILocator target, bool accepted)
    {
        return target.EvaluateAsync(@"(el, accepted) => new Promise(resolve => {
            const probe = () => {
                const data = new DataTransfer();
                data.items.add(new File(['probe'], 'probe.txt', { type: 'text/plain' }));
                el.dispatchEvent(new DragEvent('dragenter', { dataTransfer: data, bubbles: true, cancelable: true }));
                const now = el.classList.contains('bit-upl-drg');
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
        var path = Path.Combine(Path.GetTempPath(), "bit-upl-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    // ---------------------------------------------------------------- lifecycle

    [TestMethod]
    public async Task Prerender_ThePageHtml_ContainsTheUploadBeforeTheCircuitConnects()
    {
        var response = await Page.APIRequest.GetAsync($"{BaseUrl}{Route}");
        var html = await response.TextAsync();

        Assert.IsTrue(response.Ok);
        StringAssert.Contains(html, "id=\"upl-basic\"");
        StringAssert.Contains(html, "bit-upl-lbl");
        StringAssert.Contains(html, "Select or drag and drop files");
        StringAssert.Contains(html, ">Loading<");

        await OpenAsync(Route);
        await Expect(Browse("upl-basic")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task Unmount_RemovingTheUploadMidUpload_LeavesNoServerOrConsoleError()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-unmount", await SampleAsync("fileinput-photo.png", "image/png"));
        await Expect(Items("upl-unmount").Locator("[role='progressbar']")).ToHaveCountAsync(1);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Root("upl-unmount")).ToHaveCountAsync(0);
        await Page.ClickAsync("#btn-unmount");
        await Expect(Browse("upl-unmount")).ToBeVisibleAsync();

        Assert.IsFalse(await IsCircuitDeadAsync());
        await AssertNoServerErrorsAsync();
    }

    [TestMethod]
    public async Task Runtime_ChangingParametersFromThePage_ReRendersTheUpload()
    {
        await OpenAsync(Route);

        var root = Root("upl-runtime");
        await Page.ClickAsync("#btn-runtime-color");
        await Expect(root).ToHaveClassAsync(HasClass("bit-upl-err"));
        var before = Px(await CssAsync(Browse("upl-runtime"), "min-height"));
        await Page.ClickAsync("#btn-runtime-size");
        await Expect(root).ToHaveClassAsync(HasClass("bit-upl-lg"));
        Assert.IsTrue(Px(await CssAsync(Browse("upl-runtime"), "min-height")) > before);
        await Page.ClickAsync("#btn-runtime-droparea");
        await Expect(root).ToHaveClassAsync(HasClass("bit-upl-dra"));
        await Expect(root.Locator(".bit-upl-lic")).ToHaveClassAsync(HasClass("bit-icon--CloudUpload"));
        await Page.ClickAsync("#btn-runtime-label");
        await Expect(Browse("upl-runtime")).ToHaveTextAsync("Changed label");
    }

    // ---------------------------------------------------------------- basic

    [TestMethod]
    public async Task Basic_APickedFileWaitsForItsUploadButtonAndThenUploads()
    {
        await OpenAsync(Route);

        await Expect(Browse("upl-default")).ToHaveTextAsync("Browse");
        var requests = Capture("/api/upload");

        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Browse("upl-basic").ClickAsync());
        Assert.IsFalse(chooser.IsMultiple);
        await chooser.SetFilesAsync(Text("a.txt"));

        var item = Item("upl-basic", "a.txt");
        await Expect(item).ToHaveClassAsync(HasClass("bit-upl-ip"));
        await Expect(item.Locator(".bit-upl-fn")).ToHaveTextAsync("a.txt");
        await Expect(item.Locator(".bit-upl-fs")).ToHaveTextAsync("0 bytes/5 bytes");
        await Expect(item.Locator(".bit-upl-pct")).ToHaveTextAsync("0%");
        await ExpectTextAsync("basic-value", "a.txt");
        Assert.AreEqual(0, requests.Count, "A pick uploaded nothing by itself.");

        var upload = Action("upl-basic", "a.txt", "Upload");
        await Expect(upload).ToHaveAttributeAsync("aria-label", "Upload a.txt");
        await Expect(upload.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Play"));
        await upload.ClickAsync();

        await Expect(Status("upl-basic", "a.txt")).ToHaveTextAsync("File upload succeeded");
        await Expect(item).ToHaveClassAsync(HasClass("bit-upl-uld"));
        await Expect(item.Locator(".bit-upl-pct")).ToHaveTextAsync("100%");
        await Expect(item.Locator(".bit-upl-fs")).ToHaveTextAsync("5 bytes/5 bytes");
        await Expect(item.Locator("button.bit-upl-usi")).ToHaveCountAsync(0);
        await ExpectTextAsync("basic-complete", "a.txt:Completed:200");
        Assert.AreEqual(1, requests.Count);
        Assert.AreEqual("POST", requests.First().Method);
    }

    [TestMethod]
    public async Task Basic_ThePageClearingTheValue_ClearsTheList()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-basic", Text("a.txt"));
        await ExpectTextAsync("basic-value", "a.txt");
        await Page.ClickAsync("#btn-basic-clear");
        await Expect(Items("upl-basic")).ToHaveCountAsync(0);
        await Expect(Root("upl-basic").Locator("[role='list']")).ToHaveCountAsync(0);
    }

    // ---------------------------------------------------------------- disabled & read-only

    [TestMethod]
    public async Task Disabled_RendersTheDisabledMarkupAndTakesNoFile()
    {
        await OpenAsync(Route);

        var root = Root("upl-disabled");
        await Expect(root).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Browse("upl-disabled")).ToBeDisabledAsync();
        await Expect(Browse("upl-disabled")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(FileField("upl-disabled")).ToBeDisabledAsync();

        var choosers = 0;
        Page.FileChooser += (_, _) => choosers++;
        await Browse("upl-disabled").ClickAsync(new() { Force = true });
        await root.ClickAsync(new() { Force = true });
        await DropAsync(root, "text/plain", "dropped.txt");
        await PasteAsync(root, "pasted.txt");
        await NextFramesAsync();

        Assert.AreEqual(0, choosers);
        await ExpectTextAsync("disabled-clicks", "0");
        await ExpectTextAsync("disabled-files", "");

        await Page.ClickAsync("#btn-toggle-disabled");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-dis"));
        await PickThroughChooserAsync(Browse("upl-disabled"), Text("enabled.txt"));
        await ExpectTextAsync("disabled-files", "enabled.txt");
        await Expect(Page.Locator("#disabled-clicks")).Not.ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task ReadOnly_OpensNoChooserAndTakesNoDropOrPaste()
    {
        await OpenAsync(Route);

        var root = Root("upl-readonly");
        await Expect(root).ToHaveClassAsync(HasClass("bit-upl-rdl"));
        await Expect(Browse("upl-readonly")).Not.ToHaveAttributeAsync("disabled", new Regex(".*"));
        await Expect(Browse("upl-readonly")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(FileField("upl-readonly")).ToBeDisabledAsync();

        var choosers = 0;
        Page.FileChooser += (_, _) => choosers++;
        await Browse("upl-readonly").ClickAsync(new() { Force = true });
        await DropAsync(root, "text/plain", "dropped.txt");
        await PasteAsync(root, "pasted.txt");
        await NextFramesAsync();

        Assert.AreEqual(0, choosers);
        await ExpectTextAsync("readonly-files", "");
        await Browse("upl-readonly").FocusAsync();
        await Expect(Browse("upl-readonly")).ToBeFocusedAsync();
    }

    // ---------------------------------------------------------------- label & description

    [TestMethod]
    public async Task Label_TheIconItsPlacementAndTheDescription()
    {
        await OpenAsync(Route);

        var icon = Root("upl-label-icon").Locator(".bit-upl-lic");
        await Expect(icon).ToHaveClassAsync(HasClass("bit-icon--Upload"));
        await Expect(icon).ToHaveAttributeAsync("aria-hidden", "true");
        await Expect(Browse("upl-label-icon")).ToHaveAccessibleDescriptionAsync("PDF or DOCX, up to 5 MB.");
        await Expect(FileField("upl-label-icon")).ToHaveAttributeAsync("accept", ".pdf,.docx");
        await Expect(FileField("upl-label-icon")).ToHaveAttributeAsync("aria-describedby", await Root("upl-label-icon").Locator(".bit-upl-dsc").GetAttributeAsync("id") ?? "");

        // An End placement puts the icon after the text.
        await Expect(Root("upl-label-end")).ToHaveClassAsync(HasClass("bit-upl-eni"));
        var startIcon = await icon.BoundingBoxAsync();
        var startButton = await Browse("upl-label-icon").BoundingBoxAsync();
        var endIcon = await Root("upl-label-end").Locator(".bit-upl-lic").BoundingBoxAsync();
        var endButton = await Browse("upl-label-end").BoundingBoxAsync();
        Assert.IsTrue(startIcon!.X - startButton!.X < endIcon!.X - endButton!.X, "The icon is not moved to the end.");
        Assert.IsTrue(endIcon.X + endIcon.Width > endButton.X + endButton.Width / 2, "The End icon is not in the end half.");

        await Expect(Root("upl-label-end").Locator(".bit-upl-dsc .dsc-tpl")).ToHaveTextAsync("Images only");
        await Expect(Root("upl-label-external").Locator(".bit-upl-lic")).ToHaveClassAsync(new Regex(@"(^|\s)fa-solid\s+fa-upload(\s|$)"));
        await Expect(Root("upl-hide-label").Locator("button.bit-upl-lbl")).ToHaveCountAsync(0);
        await Expect(Root("upl-basic").Locator(".bit-upl-lic")).ToHaveCountAsync(0);
    }

    // ---------------------------------------------------------------- drop & paste

    [TestMethod]
    public async Task DropArea_TakesDroppedFilesAndShowsTheDrag()
    {
        await OpenAsync(Route);

        var root = Root("upl-drop-area");
        await Expect(root).ToHaveClassAsync(HasClass("bit-upl-dra"));
        await Expect(root.Locator(".bit-upl-lic")).ToHaveClassAsync(HasClass("bit-icon--CloudUpload"));
        await Expect(Browse("upl-drop-area")).ToHaveCSSAsync("border-top-style", "dashed");
        Assert.IsTrue((await Browse("upl-drop-area").BoundingBoxAsync())!.Height >= 127.5);

        await DragEnterAsync(root, "text/plain", "x.txt");
        await Expect(root).ToHaveClassAsync(HasClass("bit-upl-drg"));
        await DragEndAsync(root);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-upl-drg"));

        await DropAsync(root, "text/plain", "a.txt", "b.txt");
        await ExpectTextAsync("drop-area-files", "a.txt|b.txt");
    }

    [TestMethod]
    public async Task DropZoneSelector_ExtendsTheDropToAnotherElementOfThePage()
    {
        await OpenAsync(Route);

        var zone = Page.Locator("#upl-external-zone");
        await DragEnterAsync(zone, "text/plain", "x.txt");
        await Expect(zone).ToHaveClassAsync(HasClass("drop-zone-dragging"));
        await Expect(zone).ToHaveClassAsync(HasClass("bit-upl-drg"));
        await Expect(zone).ToHaveCSSAsync("outline-color", "rgb(1, 2, 3)");
        await Expect(Root("upl-drop-zone")).ToHaveClassAsync(HasClass("drop-zone-dragging"));
        await DragEndAsync(zone);
        await Expect(zone).Not.ToHaveClassAsync(HasClass("drop-zone-dragging"));

        await DropAsync(zone, "text/plain", "far.txt");
        await ExpectTextAsync("drop-zone-files", "far.txt");
    }

    [TestMethod]
    public async Task DragRejected_AFileOfAnotherTypeOrTooManyFilesAreRefusedWhileDragged()
    {
        await OpenAsync(Route);

        var root = Root("upl-drop-reject");
        await DragEnterAsync(root, "image/png", "a.png");
        await Expect(root).ToHaveClassAsync(HasClass("bit-upl-drg"));
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-upl-drj"));
        await DragEndAsync(root);

        await DragEnterAsync(root, "text/plain", "a.txt");
        await Expect(root).ToHaveClassAsync(HasClass("bit-upl-drj"));
        await Expect(root).ToHaveClassAsync(HasClass("custom-rejected"));
        // The refused drag repaints the area's rule (after its color transition).
        await Expect(Browse("upl-drop-reject")).Not.ToHaveCSSAsync("border-top-color", await CssAsync(Browse("upl-drop-area"), "border-top-color"));
        await DragEndAsync(root);
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-upl-drj"));

        await DragEnterAsync(root, "image/png", "1.png", "2.png", "3.png");
        await Expect(root).ToHaveClassAsync(HasClass("bit-upl-drj"));
        await DragEndAsync(root);
    }

    [TestMethod]
    public async Task DropAndPaste_CanEachBeTurnedOff()
    {
        await OpenAsync(Route);

        // A paste belongs to the uploader the focus is in.
        var root = Root("upl-drop-toggle");
        await Browse("upl-drop-toggle").FocusAsync();
        await PasteAsync(root, "clip.txt");
        await ExpectTextAsync("drop-toggle-files", "clip.txt");
        await DropAsync(root, "text/plain", "drop.txt");
        await ExpectTextAsync("drop-toggle-files", "drop.txt");

        await Page.ClickAsync("#btn-toggle-paste");
        await Page.ClickAsync("#btn-toggle-drop");
        await WaitForDropAcceptanceAsync(root, false);
        await PasteAsync(root, "ignored.txt");
        await DropAsync(root, "text/plain", "ignored.txt");
        await NextFramesAsync();
        await ExpectTextAsync("drop-toggle-files", "drop.txt");

        await Page.ClickAsync("#btn-toggle-drop");
        await WaitForDropAcceptanceAsync(root, true);
        await DropAsync(root, "text/plain", "again.txt");
        await ExpectTextAsync("drop-toggle-files", "again.txt");
    }

    // ---------------------------------------------------------------- removable

    [TestMethod]
    public async Task Remove_APendingFileLeavesTheListAndAnUploadedOneIsDeletedFromTheServer()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-remove", Text("a.txt"), Text("b.txt"));
        await ExpectTextAsync("remove-value", "a.txt,b.txt");

        var remove = Action("upl-remove", "a.txt", "Remove");
        await Expect(remove).ToHaveAttributeAsync("aria-label", "Remove a.txt");
        await Expect(remove.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Delete"));
        await remove.ClickAsync();
        await Expect(Items("upl-remove")).ToHaveCountAsync(1);
        await ExpectTextAsync("remove-value", "b.txt");
        await ExpectTextAsync("remove-complete", "a.txt");

        await Action("upl-remove", "b.txt", "Upload").ClickAsync();
        await Expect(Status("upl-remove", "b.txt")).ToHaveTextAsync("File upload succeeded");
        await Action("upl-remove", "b.txt", "Remove").ClickAsync();
        await ExpectTextAsync("remove-complete", "b.txt");
        await Expect(Items("upl-remove")).ToHaveCountAsync(0);
        await ExpectTextAsync("remove-failed", "");
        await ExpectTextAsync("remove-value", "null");
    }

    [TestMethod]
    public async Task Remove_AFailingServerRemovalKeepsTheFileAndSaysSo()
    {
        // Under WebAssembly the removal request is sent by the browser, which logs its refusal as a console error; the
        // refusal is the subject. Over a circuit the server sends it, and the page logs nothing.
        AllowConsoleErrors = IsWasm;

        await OpenAsync(Route);

        await SelectAsync("upl-remove-fail", Text("x.txt"));
        await Expect(Status("upl-remove-fail", "x.txt")).ToHaveTextAsync("File upload succeeded");

        var remove = Action("upl-remove-fail", "x.txt", "Delete");
        await Expect(remove).ToHaveAttributeAsync("aria-label", "Delete x.txt");
        await Expect(remove.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
        await remove.ClickAsync();

        await Expect(Status("upl-remove-fail", "x.txt")).ToHaveTextAsync("Could not delete.");
        await Expect(Item("upl-remove-fail", "x.txt")).ToHaveClassAsync(HasClass("bit-upl-fld"));
        await ExpectTextAsync("remove-fail-status", "x.txt:RemoveFailed:405");
    }

    // ---------------------------------------------------------------- selection behavior

    [TestMethod]
    public async Task AutoUpload_UploadsEveryPickedFileStraightAway()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-auto", Text("a.txt"), Text("b.txt"));
        await ExpectTextAsync("auto-complete", "a.txt|b.txt");
        await Expect(Root("upl-auto").Locator(".bit-upl-uld")).ToHaveCountAsync(2);
        await Expect(Root("upl-auto").Locator("button[title='Upload']")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task AppendAndDuplicates_AddToTheListAndRejectTheSameFileTwice()
    {
        await OpenAsync(Route);

        var folder = TempDirectory();
        try
        {
            var a = Path.Combine(folder, "a.txt");
            var b = Path.Combine(folder, "b.txt");
            await File.WriteAllTextAsync(a, "aaa");
            await File.WriteAllTextAsync(b, "bbb");

            await FileField("upl-append").SetInputFilesAsync(a);
            await FileField("upl-append").SetInputFilesAsync(b);
            await ExpectTextAsync("append-value", "a.txt,b.txt");

            await FileField("upl-append").SetInputFilesAsync(a);
            await Expect(Items("upl-append")).ToHaveCountAsync(3);
            await Expect(Root("upl-append").Locator(".bit-upl-fld .bit-upl-us")).ToHaveTextAsync("Already picked.");
            await ExpectTextAsync("append-value", "a.txt,b.txt");
        }
        finally
        {
            Directory.Delete(folder, true);
        }

        await SelectAsync("upl-replace", Text("one.txt"), Text("two.txt"));
        await SelectAsync("upl-replace", Text("three.txt"));
        await ExpectTextAsync("replace-value", "three.txt");
        await Expect(Items("upl-replace")).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task AutoReset_EmptiesTheListWhenTheChooserOpensAgain()
    {
        await OpenAsync(Route);

        await PickThroughChooserAsync(Browse("upl-autoreset"), Text("first.txt"));
        await ExpectTextAsync("autoreset-value", "first.txt");

        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Browse("upl-autoreset").ClickAsync());
        await Expect(Items("upl-autoreset")).ToHaveCountAsync(0);
        await chooser.SetFilesAsync(Text("second.txt"));
        await ExpectTextAsync("autoreset-value", "second.txt");
    }

    // ---------------------------------------------------------------- validation

    [TestMethod]
    public async Task Validation_EveryRuleRejectsWithItsMessageAndARejectedFileOffersNoUpload()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-limits", Sized("big.bin", 3000), Sized("tiny.bin", 5), Sized("wrong.pdf", 100, "application/pdf"),
                          Sized("bad.bin", 100), Sized("ok.bin", 100));

        await Expect(Status("upl-limits", "big.bin")).ToHaveTextAsync("Too big.");
        await Expect(Status("upl-limits", "tiny.bin")).ToHaveTextAsync("Too small.");
        await Expect(Status("upl-limits", "wrong.pdf")).ToHaveTextAsync("Wrong type.");
        await Expect(Status("upl-limits", "bad.bin")).ToHaveTextAsync("Bad name.");
        await ExpectTextAsync("limits-value", "ok.bin");

        var rejected = Item("upl-limits", "big.bin");
        await Expect(rejected).ToHaveClassAsync(HasClass("bit-upl-fld"));
        await Expect(rejected.Locator(".bit-upl-pct")).ToHaveCountAsync(0);
        await Expect(rejected.Locator("button[title='Upload']")).ToHaveCountAsync(0);
        await Expect(Action("upl-limits", "ok.bin", "Upload")).ToHaveCountAsync(1);

        await Expect(FileField("upl-limits")).ToHaveAttributeAsync("accept", ".bin,.txt");
        await Expect(FileField("upl-accept")).ToHaveAttributeAsync("accept", ".png,.jpg");
    }

    [TestMethod]
    public async Task Validation_CountAndTotalSizeInvalidateTheFilesPastThem()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-count", Sized("1.bin", 1000), Sized("2.bin", 1000), Sized("3.bin", 1000));
        await Expect(Status("upl-count", "3.bin")).ToHaveTextAsync(new Regex("Two at most\\.|Too much in total\\."));
        await ExpectTextAsync("count-value", "1.bin,2.bin");

        await Action("upl-count", "1.bin", "Remove").ClickAsync();
        await Action("upl-count", "2.bin", "Remove").ClickAsync();
        await Action("upl-count", "3.bin", "Remove").ClickAsync();
        await Expect(Items("upl-count")).ToHaveCountAsync(0);

        await SelectAsync("upl-count", Sized("x.bin", 1500), Sized("y.bin", 1500));
        await Expect(Status("upl-count", "y.bin")).ToHaveTextAsync("Too much in total.");
        await ExpectTextAsync("count-value", "x.bin");
    }

    [TestMethod]
    public async Task DirectoryAndCapture_PickAFolderAndAskForTheCamera()
    {
        await OpenAsync(Route);

        await Expect(FileField("upl-directory")).ToHaveAttributeAsync("webkitdirectory", "");
        await Expect(FileField("upl-capture")).ToHaveAttributeAsync("capture", "environment");

        var folder = TempDirectory();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(folder, "one.txt"), "1");
            await File.WriteAllTextAsync(Path.Combine(folder, "two.txt"), "2");
            await FileField("upl-directory").SetInputFilesAsync(folder);
            await Expect(Items("upl-directory")).ToHaveCountAsync(2);
            await Expect(Page.Locator("#directory-files")).ToHaveTextAsync(new Regex("one\\.txt"));
            await Expect(Page.Locator("#directory-files")).ToHaveTextAsync(new Regex("two\\.txt"));
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }

    // ---------------------------------------------------------------- preview

    [TestMethod]
    public async Task Preview_ShowsAThumbnailForAnImageAndAGlyphForTheRest()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-preview", await SampleAsync("fileinput-photo.png", "image/png"), await SampleAsync("fileinput-doc.pdf", "application/pdf"),
                          Text("notes.txt"), Sized("clip.mp4", 10, "video/mp4"), Sized("song.mp3", 10, "audio/mpeg"));

        var thumbnail = Item("upl-preview", "fileinput-photo.png").Locator("img.bit-upl-prv");
        await Expect(thumbnail).ToHaveAttributeAsync("src", new Regex("^blob:"));
        await Expect(thumbnail).ToHaveAttributeAsync("alt", "");
        Assert.IsTrue(await thumbnail.EvaluateAsync<bool>("img => img.complete && img.naturalWidth === 40"), "The thumbnail did not load.");

        foreach (var (name, glyph) in new[] { ("fileinput-doc.pdf", "PDF"), ("notes.txt", "TextDocument"), ("clip.mp4", "Video"), ("song.mp3", "MusicInCollection") })
        {
            var icon = Item("upl-preview", name).Locator(".bit-upl-pvi i");
            await Expect(icon).ToHaveClassAsync(HasClass($"bit-icon--{glyph}"));
            await Expect(Item("upl-preview", name).Locator(".bit-upl-pvi")).ToHaveAttributeAsync("aria-hidden", "true");
        }

        await SelectAsync("upl-dimensions", await SampleAsync("fileinput-photo.png", "image/png"), await SampleAsync("fileinput-wide.png", "image/png"));
        await ExpectTextAsync("dimensions", "fileinput-photo.png:40x30|fileinput-wide.png:120x20");
        await Expect(Status("upl-dimensions", "fileinput-wide.png")).ToHaveTextAsync("Too wide: 120px.");
    }

    // ---------------------------------------------------------------- events

    [TestMethod]
    public async Task Events_FireThroughAFilesWholeLifeWithItsOwnHeadersAndFields()
    {
        await InstallUploadSpyAsync();
        await OpenAsync(Route);

        await SelectAsync("upl-events", Sized("ok.bin", 100), Sized("big.bin", 2000));
        await ExpectTextAsync("events-log", "change:2;invalid:big.bin");

        // After a selection OnFilesChange carries the one file whose status moved.
        await Action("upl-events", "ok.bin", "Upload").ClickAsync();
        await Expect(Page.Locator("#events-log")).ToHaveTextAsync("change:2;invalid:big.bin;change:1;uploading:ok.bin;change:1;complete:ok.bin;all:2");
        Assert.IsTrue(int.Parse(await Page.Locator("#progress-count").InnerTextAsync()) > 0, "OnProgress never fired.");

        Assert.AreEqual("ok.bin", await UploadSpyAsync("() => window.__uploads[0].headers['x-per-file']"));
        StringAssert.Contains(await UploadSpyAsync("() => window.__uploads[0].fields.join('&')"), "perfile=ok.bin");
    }

    // ---------------------------------------------------------------- chunked

    [TestMethod]
    public async Task Chunked_SendsTheFileInChunksEachWithItsByteRange()
    {
        await OpenAsync(Route);

        var requests = Capture("/api/upload");
        await SelectAsync("upl-chunked", Sized("chunks.bin", 3500));
        await Action("upl-chunked", "chunks.bin", "Upload").ClickAsync();

        await ExpectTextAsync("chunk-done", "chunks.bin");
        await WaitForCountAsync(requests, 4);

        var ranges = new List<string>();
        foreach (var request in requests)
        {
            var headers = await request.AllHeadersAsync();
            ranges.Add($"{headers["content-range"]}|{headers["bit_chunk_from"]}|{headers["bit_chunk_to"]}|{headers["bit_file_size"]}");
        }

        CollectionAssert.AreEqual(new[]
        {
            "bytes 0-999/3500|0|1000|3500",
            "bytes 1000-1999/3500|1000|2000|3500",
            "bytes 2000-2999/3500|2000|3000|3500",
            "bytes 3000-3499/3500|3000|3500|3500"
        }, ranges);
        await Expect(Page.Locator("#chunk-progress")).ToHaveTextAsync(new Regex("^[1-3]000$"));
    }

    [TestMethod]
    public async Task Chunked_AutoChunkSizeStartsAtHalfAMegabyte()
    {
        await OpenAsync(Route);

        var requests = Capture("/api/upload");
        await SelectAsync("upl-chunked-auto", Sized("big.bin", 700 * 1024));
        await Action("upl-chunked-auto", "big.bin", "Upload").ClickAsync();
        await ExpectTextAsync("chunk-auto-done", "big.bin");

        Assert.AreEqual(2, requests.Count, "A 700 KB file at 512 KB a chunk is two requests.");
        var first = await requests.First().AllHeadersAsync();
        Assert.AreEqual($"bytes 0-{512 * 1024 - 1}/{700 * 1024}", first["content-range"]);
    }

    [TestMethod]
    public async Task Chunked_APausedFileResumesFromTheLastChunkThatMadeIt()
    {
        await OpenAsync(Route);

        var requests = Capture("/api/upload-slow");
        await SelectAsync("upl-chunked-slow", Sized("slow.bin", 2500));
        await Action("upl-chunked-slow", "slow.bin", "Upload").ClickAsync();

        // The first chunk lands after the slow endpoint's two seconds; the second is then in flight.
        await ExpectTextAsync("chunk-slow-progress", "1000");
        await WaitForCountAsync(requests, 2);
        await Action("upl-chunked-slow", "slow.bin", "Pause").ClickAsync();
        await Expect(Item("upl-chunked-slow", "slow.bin")).ToHaveClassAsync(HasClass("bit-upl-psd"));

        await Action("upl-chunked-slow", "slow.bin", "Upload").ClickAsync();
        await WaitForCountAsync(requests, 3);
        var resumed = await requests.Last().AllHeadersAsync();
        Assert.AreEqual("bytes 1000-1999/2500", resumed["content-range"]);
    }

    // ---------------------------------------------------------------- http requests

    [TestMethod]
    public async Task HttpRequests_CarryTheQueryStringsHeadersFormFieldsAndFieldName()
    {
        await InstallUploadSpyAsync();
        await OpenAsync(Route);

        var requests = Capture("/api/upload");
        await SelectAsync("upl-http", Text("invoice.txt"));
        await Expect(Status("upl-http", "invoice.txt")).ToHaveTextAsync("File upload succeeded");

        var request = requests.Single();
        Assert.AreEqual("POST", request.Method);
        StringAssert.Contains(request.Url, "qs1=qsValue1");
        Assert.AreEqual("value1", (await request.AllHeadersAsync())["header1"]);
        var fields = await UploadSpyAsync("() => window.__uploads[0].fields.join('&')");
        StringAssert.Contains(fields, "folder=invoices");
        StringAssert.Contains(fields, "document=file:invoice.txt");
        Assert.AreEqual("POST", await UploadSpyAsync("() => window.__uploads[0].method"));
    }

    [TestMethod]
    public async Task HttpRequests_ProvidersAreAskedRightBeforeTheRequest()
    {
        await OpenAsync(Route);

        var requests = Capture("/api/upload");
        await SelectAsync("upl-providers", Text("p.txt"));
        await ExpectTextAsync("providers-done", "p.txt");

        var request = requests.Single();
        StringAssert.Contains(request.Url, "/api/upload?");
        StringAssert.Contains(request.Url, "sig=signed");
        Assert.IsFalse(request.Url.Contains("upload-fail", StringComparison.Ordinal), "The UploadUrlProvider was not used.");
        Assert.AreEqual("Bearer token-1", (await request.AllHeadersAsync())["authorization"]);
    }

    [TestMethod]
    public async Task UploadTimeout_FailsARequestThatTakesTooLong()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-timeout", Text("t.txt"));
        await ExpectTextAsync("timeout-failed", "t.txt");
        await Expect(Status("upl-timeout", "t.txt")).ToHaveTextAsync("File upload failed");
    }

    // ---------------------------------------------------------------- retry

    [TestMethod]
    public async Task Retry_AFailedUploadOffersARetryThatSendsItAgain()
    {
        // The browser logs every refused request of /api/upload-fail as a console error; the failure is the subject.
        AllowConsoleErrors = true;

        await OpenAsync(Route);

        var requests = Capture("/api/upload-fail");
        await SelectAsync("upl-retry", Text("r.txt"));
        await Action("upl-retry", "r.txt", "Upload").ClickAsync();

        await Expect(Status("upl-retry", "r.txt")).ToHaveTextAsync("File upload failed");
        await Expect(Item("upl-retry", "r.txt")).ToHaveClassAsync(HasClass("bit-upl-fld"));
        await ExpectTextAsync("retry-failed", "r.txt:500");

        var retry = Action("upl-retry", "r.txt", "Try again");
        await Expect(retry).ToHaveAttributeAsync("aria-label", "Try again r.txt");
        await Expect(retry.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Refresh"));
        await retry.ClickAsync();
        await WaitForCountAsync(requests, 2);
        await Expect(Status("upl-retry", "r.txt")).ToHaveTextAsync("File upload failed");
    }

    [TestMethod]
    public async Task AutoRetries_RetryAFailingUploadTheGivenNumberOfTimes()
    {
        // The browser logs every refused request of /api/upload-fail as a console error; the failure is the subject.
        AllowConsoleErrors = true;

        await OpenAsync(Route);

        var requests = Capture("/api/upload-fail");
        await SelectAsync("upl-auto-retry", Text("a.txt"));
        await ExpectTextAsync("auto-retry-failed", "a.txt");
        Assert.AreEqual(3, requests.Count, "One attempt and two automatic retries.");

        await SelectAsync("upl-retry-provider", Text("p.txt"));
        await ExpectTextAsync("provider-failed", "1,2");
    }

    [TestMethod]
    public async Task ShouldAutoRetry_CanRefuseTheRetries()
    {
        // The browser logs every refused request of /api/upload-fail as a console error; the failure is the subject.
        AllowConsoleErrors = true;

        await OpenAsync(Route);

        var requests = Capture("/api/upload-fail");
        await SelectAsync("upl-no-retry", Text("n.txt"));
        await ExpectTextAsync("no-retry-failed", "n.txt");
        await NextFramesAsync();
        Assert.AreEqual(1, requests.Count);
    }

    // ---------------------------------------------------------------- concurrency & batch

    [TestMethod]
    public async Task ConcurrentUploads_QueueTheFilesPastTheLimitAndLetOneBeCancelled()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-concurrent", Text("1.txt"), Text("2.txt"), Text("3.txt"));

        await Expect(Root("upl-concurrent").Locator("[role='progressbar']")).ToHaveCountAsync(1);
        await Expect(Status("upl-concurrent", "2.txt")).ToHaveTextAsync("In the queue");
        await Expect(Status("upl-concurrent", "3.txt")).ToHaveTextAsync("In the queue");
        await Expect(Action("upl-concurrent", "3.txt", "Upload")).ToHaveCountAsync(0);

        await Action("upl-concurrent", "3.txt", "Cancel").ClickAsync();
        await Expect(Status("upl-concurrent", "3.txt")).ToHaveTextAsync("File upload canceled");

        await ExpectTextAsync("concurrent-done", "1.txt|2.txt|3.txt");
        await Expect(Root("upl-concurrent").Locator(".bit-upl-uld")).ToHaveCountAsync(2);
    }

    [TestMethod]
    public async Task BatchActions_UploadAllAndClearActOnTheWholeList()
    {
        await OpenAsync(Route);

        var root = Root("upl-batch");
        await Expect(root.Locator(".bit-upl-bat")).ToHaveCountAsync(0);
        await SelectAsync("upl-batch", Text("a.txt"), Text("b.txt"));

        var buttons = root.Locator(".bit-upl-bab");
        await Expect(buttons).ToHaveTextAsync(["Upload all", "Cancel all", "Clear"]);
        await Expect(buttons.Nth(1)).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(buttons.Nth(0)).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));

        await buttons.Nth(0).ClickAsync();
        await ExpectTextAsync("batch-done", "a.txt|b.txt");
        await Expect(root.Locator(".bit-upl-uld")).ToHaveCountAsync(2);
        await Expect(buttons.Nth(0)).ToHaveAttributeAsync("aria-disabled", "true");

        await buttons.Nth(2).ClickAsync();
        await Expect(Items("upl-batch")).ToHaveCountAsync(0);
        await Expect(root.Locator(".bit-upl-bat")).ToHaveCountAsync(0);
        await Expect(Browse("upl-batch")).ToBeFocusedAsync();
    }

    [TestMethod]
    public async Task BatchActions_CancelAllStopsEverythingInFlightAndQueued()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-batch-slow", Text("1.txt"), Text("2.txt"));
        var buttons = Root("upl-batch-slow").Locator(".bit-upl-bab");
        await Expect(buttons).ToHaveTextAsync(["Send everything", "Stop everything", "Start over"]);

        await buttons.Nth(0).ClickAsync();
        await Expect(Root("upl-batch-slow").Locator("[role='progressbar']")).ToHaveCountAsync(1);
        await Expect(buttons.Nth(1)).Not.ToHaveAttributeAsync("aria-disabled", new Regex(".*"));
        await Expect(buttons.Nth(2)).ToHaveAttributeAsync("aria-disabled", "true");

        await buttons.Nth(1).ClickAsync();
        await Expect(Status("upl-batch-slow", "1.txt")).ToHaveTextAsync("Stopped.");
        await Expect(Status("upl-batch-slow", "2.txt")).ToHaveTextAsync("Stopped.");
        await Expect(Root("upl-batch-slow").Locator(".bit-upl-psd")).ToHaveCountAsync(2);
    }

    // ---------------------------------------------------------------- progress, pause, cancel

    [TestMethod]
    public async Task Progress_AnUploadInFlightCanBePausedAndResumed()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-progress", Sized("slow.bin", 500));
        await ExpectTextAsync("progress-status", "Pending");
        await Action("upl-progress", "slow.bin", "Send").ClickAsync();

        var bar = Item("upl-progress", "slow.bin").Locator("[role='progressbar']");
        await Expect(bar).ToHaveAttributeAsync("aria-label", "slow.bin");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemin", "0");
        await Expect(bar).ToHaveAttributeAsync("aria-valuemax", "100");
        await Expect(bar).ToHaveAttributeAsync("aria-valuetext", new Regex("^\\d+%$"));
        await ExpectTextAsync("progress-status", "InProgress");

        var pause = Action("upl-progress", "slow.bin", "Hold");
        await Expect(pause).ToHaveAttributeAsync("aria-label", "Hold slow.bin");
        await Expect(pause.Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Pause"));
        await Expect(Action("upl-progress", "slow.bin", "Stop").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Cancel"));
        await pause.ClickAsync();

        await Expect(Item("upl-progress", "slow.bin")).ToHaveClassAsync(HasClass("bit-upl-psd"));
        await Expect(bar).ToHaveCountAsync(1);
        await Expect(Action("upl-progress", "slow.bin", "Send")).ToHaveCountAsync(1);

        await Action("upl-progress", "slow.bin", "Send").ClickAsync();
        await Expect(Status("upl-progress", "slow.bin")).ToHaveTextAsync("File upload succeeded", new() { Timeout = 15000 });
        await ExpectTextAsync("progress-overall", "100");
        await ExpectTextAsync("progress-total", "500");
        await ExpectTextAsync("progress-uploaded", "500");
    }

    [TestMethod]
    public async Task Progress_AnUploadInFlightCanBeCancelledAndRetried()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-progress", Sized("c.bin", 200));
        await Action("upl-progress", "c.bin", "Send").ClickAsync();
        await Expect(Item("upl-progress", "c.bin").Locator("[role='progressbar']")).ToHaveCountAsync(1);

        await Action("upl-progress", "c.bin", "Stop").ClickAsync();
        await Expect(Status("upl-progress", "c.bin")).ToHaveTextAsync("File upload canceled");
        await Expect(Item("upl-progress", "c.bin")).ToHaveClassAsync(HasClass("bit-upl-psd"));
        // A canceled file is retried by the upload button, which falls back to the UploadButtonTitle.
        await Expect(Action("upl-progress", "c.bin", "Send")).ToHaveCountAsync(1);
        await Expect(Action("upl-progress", "c.bin", "Send").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Refresh"));
    }

    [TestMethod]
    public async Task FileList_FormatsTheSizesNamesTheListAndCanBeHidden()
    {
        await OpenAsync(Route);

        await SelectAsync("upl-formatter", Sized("f.bin", 1500));
        await Expect(Item("upl-formatter", "f.bin").Locator(".bit-upl-fs")).ToHaveTextAsync("0 B/1,500 B");
        await Expect(Root("upl-formatter").Locator("[role='list']")).ToHaveAttributeAsync("aria-label", "Documents to upload");

        await SelectAsync("upl-basic", Text("a.txt"));
        await Expect(Root("upl-basic").Locator("[role='list']")).ToHaveAttributeAsync("aria-label", "Selected files");

        await SelectAsync("upl-hidden-view", Text("h.txt"));
        await ExpectTextAsync("hidden-files", "h.txt");
        await Expect(Root("upl-hidden-view").Locator("[role='list']")).ToHaveCountAsync(0);
    }

    // ---------------------------------------------------------------- templates & api

    [TestMethod]
    public async Task Templates_ALabelTemplateBrowsesAndAFileViewTemplateDrivesTheFiles()
    {
        await OpenAsync(Route);

        await Expect(Root("upl-templates").Locator("button.bit-upl-lbl")).ToHaveCountAsync(0);
        await PickThroughChooserAsync(Page.Locator("#custom-browse"), Text("one.txt"), Text("two.txt"));

        var cards = Root("upl-templates").Locator(".file-card");
        await Expect(cards).ToHaveCountAsync(2);
        await Expect(Root("upl-templates").Locator("[role='listitem'] .file-card")).ToHaveCountAsync(2);
        await Expect(cards.Nth(0)).ToHaveAttributeAsync("data-status", "Pending");

        await cards.Nth(0).Locator(".file-card-upload").ClickAsync();
        await Expect(cards.Nth(0)).ToHaveAttributeAsync("data-status", "Completed");

        await cards.Nth(0).Locator(".file-card-remove").ClickAsync();
        await Expect(cards).ToHaveCountAsync(1);
        await Expect(cards.Nth(0)).ToHaveAttributeAsync("data-name", "two.txt");
    }

    [TestMethod]
    public async Task PublicApi_UploadsPausesCancelsRemovesResetsAndFocuses()
    {
        await OpenAsync(Route);

        await Expect(Root("upl-api").Locator("button.bit-upl-lbl")).ToHaveCountAsync(0);
        await PickThroughChooserAsync(Page.Locator("#api-browse"), Text("a.txt"), Text("b.txt"));
        await ExpectTextAsync("api-files", "a.txt:Pending|b.txt:Pending");
        await Expect(Page.Locator("#api-input-id")).ToHaveTextAsync(await FileField("upl-api").GetAttributeAsync("id") ?? "");

        await Page.ClickAsync("#api-upload");
        await Page.ClickAsync("#api-refresh");
        await ExpectTextAsync("api-status", "InProgress");
        await ExpectTextAsync("api-files", "a.txt:InProgress|b.txt:InProgress");

        await Page.ClickAsync("#api-pause");
        await ExpectTextAsync("api-files", "a.txt:Paused|b.txt:Paused");

        await Page.ClickAsync("#api-upload-first");
        await ExpectTextAsync("api-files", "a.txt:Completed|b.txt:Paused");

        await Page.ClickAsync("#api-upload");
        await Page.ClickAsync("#api-cancel");
        await ExpectTextAsync("api-files", "a.txt:Completed|b.txt:Canceled");

        await Page.ClickAsync("#api-remove-first");
        await ExpectTextAsync("api-files", "a.txt:Removed|b.txt:Canceled");
        await ExpectTextAsync("api-removing", "False");

        await Page.ClickAsync("#api-remove-all");
        await ExpectTextAsync("api-files", "a.txt:Removed|b.txt:Removed");

        await Page.ClickAsync("#api-reset");
        await Page.ClickAsync("#api-refresh");
        await ExpectTextAsync("api-files", "");

        await Page.ClickAsync("#api-focus");
        await Expect(Page.Locator("#api-focus")).ToBeFocusedAsync();
        await Page.ClickAsync("#api-focus-labelled");
        await Expect(Browse("upl-api-focus")).ToBeFocusedAsync();
    }

    // ---------------------------------------------------------------- preloaded

    [TestMethod]
    public async Task PreloadedFiles_AreListedAsAlreadyUploadedAndCanBeRemovedFromTheServer()
    {
        await OpenAsync(Route);

        var invoice = Item("upl-preloaded", "invoice.pdf");
        await Expect(invoice).ToHaveClassAsync(HasClass("bit-upl-pre"));
        await Expect(Status("upl-preloaded", "invoice.pdf")).ToHaveTextAsync("Saved with this record");
        await Expect(invoice.Locator(".bit-upl-fs")).ToHaveTextAsync("2KB");
        await Expect(invoice.Locator(".bit-upl-pct")).ToHaveCountAsync(0);
        await Expect(Item("upl-preloaded", "photo.png").Locator(".bit-upl-fs")).ToHaveTextAsync("4KB");

        await SelectAsync("upl-preloaded", Text("new.txt"));
        await Expect(Items("upl-preloaded")).ToHaveCountAsync(3);

        await Action("upl-preloaded", "invoice.pdf", "Remove").ClickAsync();
        await Expect(Item("upl-preloaded", "invoice.pdf")).ToHaveCountAsync(0);
        await Expect(Items("upl-preloaded")).ToHaveCountAsync(2);
    }

    // ---------------------------------------------------------------- accessibility

    [TestMethod]
    public async Task Accessibility_TheButtonInputListAndAnnouncementsAreNamed()
    {
        await OpenAsync(Route);

        await Expect(Browse("upl-a11y")).ToHaveAccessibleNameAsync("Select a document to upload");
        await Expect(Browse("upl-a11y")).ToHaveAccessibleDescriptionAsync("One document.");
        await Expect(FileField("upl-a11y")).ToHaveAttributeAsync("aria-label", "Select a document to upload");
        await Expect(FileField("upl-basic")).ToHaveAttributeAsync("aria-labelledby", await Browse("upl-basic").GetAttributeAsync("id") ?? "");

        var live = Root("upl-announce").Locator(".bit-upl-lvr");
        await Expect(live).ToHaveAttributeAsync("role", "status");
        await Expect(live).ToHaveAttributeAsync("aria-live", "polite");
        await SelectAsync("upl-announce", Text("a.txt"), Text("b.txt"));
        await Expect(live).ToHaveTextAsync(new Regex("^2 picked, 2 done​?$"));
        await Expect(Root("upl-announce").Locator("[role='list']")).ToHaveAttributeAsync("aria-label", "Documents to upload");
        await Expect(Root("upl-announce").Locator("[role='listitem']")).ToHaveCountAsync(2);
    }

    // ---------------------------------------------------------------- variant & cascade & form

    [TestMethod]
    public async Task Variant_FillOutlineAndTextPaintTheBrowseButtonApart()
    {
        await OpenAsync(Route);

        await Expect(Root("upl-var-fil")).ToHaveClassAsync(HasClass("bit-upl-fil"));
        await Expect(Root("upl-var-otl")).ToHaveClassAsync(HasClass("bit-upl-otl"));
        await Expect(Root("upl-var-txt")).ToHaveClassAsync(HasClass("bit-upl-txt"));
        await Expect(Root("upl-basic")).ToHaveClassAsync(HasClass("bit-upl-fil"));

        var primary = await CssAsync(Browse("upl-var-fil"), "background-color");
        Assert.AreNotEqual("rgba(0, 0, 0, 0)", primary);
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(Browse("upl-var-otl"), "background-color"));
        Assert.AreEqual(primary, await CssAsync(Browse("upl-var-otl"), "border-top-color"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(Browse("upl-var-txt"), "background-color"));
        Assert.AreEqual("rgba(0, 0, 0, 0)", await CssAsync(Browse("upl-var-txt"), "border-top-color"));
        Assert.AreEqual(primary, await CssAsync(Browse("upl-var-txt"), "color"));

        Assert.AreNotEqual(primary, await CssAsync(Browse("upl-var-fil-dis"), "background-color"));
        Assert.AreNotEqual(primary, await CssAsync(Browse("upl-var-otl-dis"), "border-top-color"));
        await Expect(Root("upl-var-drop-dis")).ToHaveClassAsync(HasClass("bit-dis"));
        Assert.AreNotEqual(await CssAsync(Browse("upl-drop-area"), "background-color"), await CssAsync(Browse("upl-var-drop-dis"), "background-color"));
    }

    [TestMethod]
    public async Task CascadingParameters_FillWhatTheUploadLeavesUnsetAndYieldToWhatItSets()
    {
        await OpenAsync(Route);

        var cascaded = Root("upl-cascade-1");
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-upl-otl"));
        await Expect(cascaded).ToHaveClassAsync(HasClass("bit-upl-suc"));
        await Expect(FileField("upl-cascade-1")).ToHaveAttributeAsync("multiple", "");
        await SelectAsync("upl-cascade-1", Sized("big.bin", 3000), Sized("ok.bin", 10));
        await Expect(Status("upl-cascade-1", "big.bin")).ToHaveTextAsync("Cascaded message.");
        await Expect(Action("upl-cascade-1", "ok.bin", "Remove")).ToHaveCountAsync(1);
        await Action("upl-cascade-1", "ok.bin", "Upload").ClickAsync();
        await Expect(Status("upl-cascade-1", "ok.bin")).ToHaveTextAsync("File upload succeeded");

        var own = Root("upl-cascade-own");
        await Expect(own).ToHaveClassAsync(HasClass("bit-upl-fil"));
        await SelectAsync("upl-cascade-own", Sized("mid.bin", 3000));
        await Expect(Item("upl-cascade-own", "mid.bin")).Not.ToHaveClassAsync(HasClass("bit-upl-fld"));

        await Expect(Root("upl-cascade-outside")).Not.ToHaveClassAsync(HasClass("bit-upl-suc"));
        await Expect(FileField("upl-cascade-outside")).Not.ToHaveAttributeAsync("multiple", new Regex(".*"));
    }

    [TestMethod]
    public async Task FormValidation_ARequiredUploadFailsTheFormUntilAFileIsPicked()
    {
        await OpenAsync(Route);

        var root = Root("upl-form");
        var hidden = root.Locator("input.bit-input-hidden");
        await Expect(hidden).ToHaveAttributeAsync("name", "attachments");
        await Expect(hidden).ToHaveAttributeAsync("data-input", "attachments");

        await Page.ClickAsync("#upl-form-submit");
        await ExpectTextAsync("form-result", "invalid");
        await Expect(root).ToHaveClassAsync(HasClass("bit-inv"));
        await Expect(Browse("upl-form")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#upl-form-msg")).ToHaveTextAsync("Add at least one attachment.");

        await SelectAsync("upl-form", Text("a.txt"), Text("b.txt"));
        await Expect(hidden).ToHaveValueAsync("a.txt,b.txt");
        await Expect(root).Not.ToHaveClassAsync(HasClass("bit-inv"));
        await Page.ClickAsync("#upl-form-submit");
        await ExpectTextAsync("form-result", "valid");
    }

    // ---------------------------------------------------------------- color, icons, size

    [TestMethod]
    public async Task Color_EveryValuePaintsTheBrowseButtonInItsRole()
    {
        await OpenAsync(Route);

        string[] slugs = ["pri", "sec", "ter", "inf", "suc", "wrn", "swr", "err", "pbg", "sbg", "tbg", "pfg", "sfg", "tfg", "pbr", "sbr", "tbr"];
        var unset = await CssAsync(Browse("upl-color-none"), "background-color");
        var colors = new Dictionary<string, string>();

        foreach (var slug in slugs)
        {
            var id = $"upl-color-{slug}";
            await Expect(Root(id)).ToHaveClassAsync(HasClass($"bit-upl-{slug}"));
            var role = await Root(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-upl-clr').trim()");
            var text = await Root(id).EvaluateAsync<string>("el => getComputedStyle(el).getPropertyValue('--bit-upl-clr-txt').trim()");
            var expected = await ResolveColorAsync(role);
            await Expect(Browse(id)).ToHaveCSSAsync("background-color", expected);
            await Expect(Browse(id)).ToHaveCSSAsync("color", await ResolveColorAsync(text));
            colors[slug] = expected;
        }

        Assert.AreEqual(unset, colors["pri"], "Unset is not primary.");
        Assert.IsTrue(colors.Values.Distinct().Count() >= 12, string.Join(", ", colors.Select(c => $"{c.Key}={c.Value}")));
    }

    [TestMethod]
    public async Task Icons_ExternalIconsAndIconNamesReplaceTheActionGlyphs()
    {
        // The browser logs every refused request of /api/upload-fail as a console error; the failure is the subject.
        AllowConsoleErrors = true;

        await OpenAsync(Route);

        await SelectAsync("upl-icons", Text("i.txt"));
        await Expect(Action("upl-icons", "i.txt", "Upload").Locator("i")).ToHaveClassAsync(HasClass("ext-upload"));
        await Expect(Action("upl-icons", "i.txt", "Remove").Locator("i")).ToHaveClassAsync(HasClass("ext-remove"));
        await Action("upl-icons", "i.txt", "Upload").ClickAsync();
        await Expect(Action("upl-icons", "i.txt", "Pause").Locator("i")).ToHaveClassAsync(HasClass("ext-pause"));
        await Expect(Action("upl-icons", "i.txt", "Cancel").Locator("i")).ToHaveClassAsync(HasClass("ext-cancel"));
        await Action("upl-icons", "i.txt", "Cancel").ClickAsync();
        await Expect(Action("upl-icons", "i.txt", "Retry").Locator("i")).ToHaveClassAsync(HasClass("ext-retry"));

        await SelectAsync("upl-icon-names", Text("n.txt"));
        await Expect(Status("upl-icon-names", "n.txt")).ToHaveTextAsync("File upload failed");
        await Expect(Action("upl-icon-names", "n.txt", "Retry").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Sync"));
        await Expect(Action("upl-icon-names", "n.txt", "Drop").Locator("i")).ToHaveClassAsync(HasClass("bit-icon--Blocked"));
    }

    [TestMethod]
    [DataRow("sm", "md")]
    [DataRow("md", "lg")]
    public async Task Size_ScalesTheBrowseButton(string smaller, string larger)
    {
        await OpenAsync(Route);

        await Expect(Root($"upl-size-{smaller}")).ToHaveClassAsync(HasClass($"bit-upl-{smaller}"));
        await Expect(Root($"upl-size-{larger}")).ToHaveClassAsync(HasClass($"bit-upl-{larger}"));
        Assert.IsTrue(Px(await CssAsync(Browse($"upl-size-{larger}"), "min-height")) > Px(await CssAsync(Browse($"upl-size-{smaller}"), "min-height")), "height");
        Assert.IsTrue(Px(await CssAsync(Browse($"upl-size-{larger}"), "font-size")) > Px(await CssAsync(Browse($"upl-size-{smaller}"), "font-size")), "font size");
    }

    // ---------------------------------------------------------------- style & class

    [TestMethod]
    public async Task StyleClassAndHtmlAttributes_LandOnTheRoot()
    {
        await OpenAsync(Route);

        await Expect(Root("upl-style")).ToHaveCSSAsync("margin-left", "13px");
        await Expect(Root("upl-style")).ToHaveClassAsync(HasClass("custom-class"));
        await Expect(Root("upl-style")).ToHaveAttributeAsync("data-test", "attr-value");
    }

    [TestMethod]
    public async Task Styles_ReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("upl-styles");
        await SelectAsync("upl-styles", await SampleAsync("fileinput-photo.png", "image/png"), Text("a.txt"), Sized("big.bin", 2000));

        await Expect(root).ToHaveCSSAsync("padding-top", "5px");
        await Expect(root.Locator(".bit-upl-lbl")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(root.Locator(".bit-upl-lic")).ToHaveCSSAsync("color", "rgb(1, 1, 1)");
        await Expect(root.Locator(".bit-upl-dsc")).ToHaveCSSAsync("color", "rgb(2, 2, 2)");
        await Expect(root.Locator(".bit-upl-fl")).ToHaveCSSAsync("padding-top", "4px");
        await Expect(root.Locator(".bit-upl-itm").First).ToHaveCSSAsync("padding-bottom", "3px");
        await Expect(root.Locator("img.bit-upl-prv")).ToHaveCSSAsync("opacity", "0.9");
        await Expect(root.Locator(".bit-upl-pvi i").First).ToHaveCSSAsync("color", "rgb(3, 3, 3)");
        await Expect(root.Locator(".bit-upl-fn").First).ToHaveCSSAsync("color", "rgb(4, 4, 4)");
        await Expect(root.Locator(".bit-upl-fs").First).ToHaveCSSAsync("color", "rgb(5, 5, 5)");
        await Expect(root.Locator(".bit-upl-pct").First).ToHaveCSSAsync("color", "rgb(6, 6, 6)");
        await Expect(Status("upl-styles", "big.bin")).ToHaveCSSAsync("color", "rgb(7, 7, 7)");
        await Expect(Action("upl-styles", "a.txt", "Upload")).ToHaveCSSAsync("opacity", "0.8");
        await Expect(Action("upl-styles", "a.txt", "Upload").Locator("i")).ToHaveCSSAsync("color", "rgb(8, 8, 8)");
        await Expect(Action("upl-styles", "a.txt", "Remove")).ToHaveCSSAsync("opacity", "0.7");
        await Expect(Action("upl-styles", "a.txt", "Remove").Locator("i")).ToHaveCSSAsync("color", "rgb(9, 9, 9)");
        await Expect(root.Locator(".bit-upl-bat")).ToHaveCSSAsync("padding-top", "6px");
        await Expect(root.Locator(".bit-upl-bab").First).ToHaveCSSAsync("letter-spacing", "2px");

        await Action("upl-styles", "a.txt", "Upload").ClickAsync();
        await Expect(Item("upl-styles", "a.txt").Locator(".bit-upl-pbc")).ToHaveCSSAsync("opacity", "0.6");
        await Expect(Item("upl-styles", "a.txt").Locator(".bit-upl-pgb")).ToHaveCSSAsync("opacity", "0.5");
        await Expect(Action("upl-styles", "a.txt", "Pause")).ToHaveCSSAsync("opacity", "0.4");
        await Expect(Action("upl-styles", "a.txt", "Pause").Locator("i")).ToHaveCSSAsync("color", "rgb(10, 10, 10)");
        await Expect(Action("upl-styles", "a.txt", "Cancel")).ToHaveCSSAsync("opacity", "0.3");
        await Expect(Action("upl-styles", "a.txt", "Cancel").Locator("i")).ToHaveCSSAsync("color", "rgb(11, 11, 11)");
    }

    [TestMethod]
    public async Task Classes_ReachEveryPart()
    {
        await OpenAsync(Route);

        var root = Root("upl-classes");
        await SelectAsync("upl-classes", await SampleAsync("fileinput-photo.png", "image/png"), Text("a.txt"), Sized("big.bin", 2000));

        await Expect(root).ToHaveClassAsync(HasClass("c-root"));
        foreach (var (part, cssClass) in new[]
        {
            (".bit-upl-lbl", "c-lbl"), (".bit-upl-lic", "c-lic"), (".bit-upl-dsc", "c-dsc"), (".bit-upl-fl", "c-fl"), (".bit-upl-itm", "c-itm"),
            ("img.bit-upl-prv", "c-prv"), (".bit-upl-pvi i", "c-pvi"), (".bit-upl-fn", "c-fn"), (".bit-upl-fs", "c-fs"), (".bit-upl-pct", "c-pct"),
            (".bit-upl-us", "c-us"), (".bit-upl-bat", "c-bat"), (".bit-upl-bab", "c-bab")
        })
        {
            await Expect(root.Locator(part).First).ToHaveClassAsync(HasClass(cssClass));
        }

        await Expect(Action("upl-classes", "a.txt", "Upload")).ToHaveClassAsync(HasClass("c-ubt"));
        await Expect(Action("upl-classes", "a.txt", "Upload").Locator("i")).ToHaveClassAsync(HasClass("c-uic"));
        await Expect(Action("upl-classes", "a.txt", "Remove")).ToHaveClassAsync(HasClass("c-rbt"));
        await Expect(Action("upl-classes", "a.txt", "Remove").Locator("i")).ToHaveClassAsync(HasClass("c-ric"));

        await Action("upl-classes", "a.txt", "Upload").ClickAsync();
        await Expect(Item("upl-classes", "a.txt").Locator(".bit-upl-pbc")).ToHaveClassAsync(HasClass("c-pbc"));
        await Expect(Item("upl-classes", "a.txt").Locator(".bit-upl-pgb")).ToHaveClassAsync(HasClass("c-pgb"));
        await Expect(Action("upl-classes", "a.txt", "Pause")).ToHaveClassAsync(HasClass("c-pbt"));
        await Expect(Action("upl-classes", "a.txt", "Pause").Locator("i")).ToHaveClassAsync(HasClass("c-pic"));
        await Expect(Action("upl-classes", "a.txt", "Cancel")).ToHaveClassAsync(HasClass("c-cbt"));
        await Expect(Action("upl-classes", "a.txt", "Cancel").Locator("i")).ToHaveClassAsync(HasClass("c-cic"));
    }

    // ---------------------------------------------------------------- css variables

    [TestMethod]
    public async Task CssVariables_SetOnAnAncestor_RestyleTheUpload()
    {
        await OpenAsync(Route);

        var root = Root("upl-vars");
        var button = Browse("upl-vars");
        Assert.IsTrue((await root.BoundingBoxAsync())!.Width <= 320.5);
        await Expect(button).ToHaveCSSAsync("background-color", "rgb(218, 165, 32)");
        await Expect(button).ToHaveCSSAsync("color", "rgb(1, 2, 3)");
        await Expect(button).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(button).ToHaveCSSAsync("border-top-left-radius", "7px");
        await Expect(button).ToHaveCSSAsync("min-height", "50px");
        await Expect(button).ToHaveCSSAsync("padding-top", "6px");
        await Expect(button).ToHaveCSSAsync("font-size", "19px");
        await Expect(button).ToHaveCSSAsync("font-weight", "700");
        await Expect(root.Locator(".bit-upl-dsc")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(root.Locator(".bit-upl-dsc")).ToHaveCSSAsync("font-size", "11px");

        await SelectAsync("upl-vars", Text("a.txt"), Sized("big.bin", 2000));
        var item = Item("upl-vars", "a.txt");
        await Expect(item).ToHaveCSSAsync("color", "rgb(10, 11, 12)");
        await Expect(item).ToHaveCSSAsync("background-color", "rgb(13, 14, 15)");
        await Expect(item).ToHaveCSSAsync("border-top-color", "rgb(16, 17, 18)");
        await Expect(item).ToHaveCSSAsync("border-top-left-radius", "5px");
        await Expect(item).ToHaveCSSAsync("font-size", "15px");
        await Expect(Status("upl-vars", "big.bin")).ToHaveCSSAsync("color", "rgb(22, 23, 24)");
        await Expect(Action("upl-vars", "a.txt", "Upload")).ToHaveCSSAsync("color", "rgb(25, 26, 27)");
        await Action("upl-vars", "a.txt", "Upload").ClickAsync();
        await Expect(Status("upl-vars", "a.txt")).ToHaveCSSAsync("color", "rgb(19, 20, 21)");
    }

    [TestMethod]
    public async Task CssVariables_DropAreaDisabledDragAndExplicitParameters()
    {
        await OpenAsync(Route);

        var zone = Browse("upl-vars-drop");
        await Expect(zone).ToHaveCSSAsync("min-height", "140px");
        await Expect(zone).ToHaveCSSAsync("border-top-style", "dotted");
        await Expect(zone).ToHaveCSSAsync("background-color", "rgb(30, 31, 32)");
        await Expect(Root("upl-vars-drop").Locator(".bit-upl-lic")).ToHaveCSSAsync("color", "rgb(33, 34, 35)");

        await Expect(Browse("upl-vars-disabled")).ToHaveCSSAsync("background-color", "rgb(39, 40, 41)");

        await DragEnterAsync(Root("upl-vars"), "text/plain", "x.txt");
        await Expect(Browse("upl-vars")).Not.ToHaveCSSAsync("background-color", "rgb(218, 165, 32)");
        await DragEndAsync(Root("upl-vars"));

        await Expect(Browse("upl-vars-explicit")).Not.ToHaveCSSAsync("background-color", "rgb(218, 165, 32)");
        Assert.AreEqual(await CssAsync(Browse("upl-color-err"), "background-color"), await CssAsync(Browse("upl-vars-explicit"), "background-color"));
        await Expect(Browse("upl-vars-explicit")).Not.ToHaveCSSAsync("min-height", "50px");
    }

    // ---------------------------------------------------------------- rtl & motion

    [TestMethod]
    public async Task Rtl_MirrorsTheFileRowsAndTheLabel()
    {
        await OpenAsync(Route);

        await Expect(Root("upl-rtl")).ToHaveClassAsync(HasClass("bit-rtl"));
        await Expect(Root("upl-rtl")).ToHaveAttributeAsync("dir", "rtl");
        var icon = await Root("upl-rtl").Locator(".bit-upl-lic").BoundingBoxAsync();
        var button = await Browse("upl-rtl").BoundingBoxAsync();
        Assert.IsTrue(icon!.X + icon.Width / 2 > button!.X + button.Width / 2, "The label icon is not at the start (right) of the button.");

        await SelectAsync("upl-rtl", Text("a.txt"));
        var name = await Item("upl-rtl", "a.txt").Locator(".bit-upl-fn").BoundingBoxAsync();
        var action = await Action("upl-rtl", "a.txt", "Upload").BoundingBoxAsync();
        Assert.IsTrue(name!.X > action!.X, "The row is not mirrored.");

        await Expect(Root("upl-rtl-inherited")).Not.ToHaveClassAsync(HasClass("bit-rtl"));
        await SelectAsync("upl-rtl-inherited", Text("b.txt"));
        var inheritedName = await Item("upl-rtl-inherited", "b.txt").Locator(".bit-upl-fn").BoundingBoxAsync();
        var inheritedAction = await Action("upl-rtl-inherited", "b.txt", "Upload").BoundingBoxAsync();
        Assert.IsTrue(inheritedName!.X > inheritedAction!.X, "The inherited direction does not mirror the row.");
    }

    [TestMethod]
    public async Task ReducedMotion_CollapsesTheTransitionsUnlessForceAnimationRestoresThem()
    {
        await OpenAsync(Route);

        var button = Browse("upl-motion");
        var full = await CssAsync(button, "transition-duration");
        Assert.IsTrue(full.StartsWith("0.1s", StringComparison.Ordinal) || full.StartsWith("0.2s", StringComparison.Ordinal), full);

        await Page.EmulateMediaAsync(new() { ReducedMotion = ReducedMotion.Reduce });

        await Expect(button).Not.ToHaveCSSAsync("transition-duration", full);
        var reduced = (await CssAsync(button, "transition-duration")).Split(',')[0].Trim();
        Assert.IsTrue(double.Parse(reduced.TrimEnd('s'), CultureInfo.InvariantCulture) < 0.001, reduced);

        await Expect(Root("upl-motion-forced")).ToHaveClassAsync(HasClass("bit-fam"));
        Assert.AreEqual(full, await CssAsync(Browse("upl-motion-forced"), "transition-duration"));
    }
}

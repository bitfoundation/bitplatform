using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Bit.BlazorUI.Tests.Performance;

/// <summary>
/// Browser regression tests for the three inputs that joined the EditForm last - BitColorPicker, BitFileInput and
/// BitFileUpload (/regression/form-inputs) - in a real form of a real engine: a required one that is empty reaches the
/// form's own validation rather than the browser's, the state of a failed validation reaches the control the user
/// operates, the EditContext hears of what the user did and of nothing the app did, and a read-only file input opens no
/// dialog from a label of the page's own. Run them with BROWSER=firefox and BROWSER=webkit as well.
/// </summary>
[TestClass]
[TestCategory("Browser")]
[RequiresBrowser] // opt-in: RUN_BROWSER_TESTS=1 dotnet test --filter FullyQualifiedName~BitFormInputsBrowserTests
public class BitFormInputsBrowserTests : PerformanceTestBase
{
    private static readonly HttpClient _http = new();

    private static readonly Regex Invalid = new(@"\bbit-inv\b");

    private readonly List<string> _pageErrors = [];

    [TestMethod]
    public async Task AnEmptyRequiredFileInputReachesTheValidationOfTheForm()
    {
        await OpenPage();

        // the browser blocks a submit over an empty required control, and one nobody can see would never show why.
        foreach (var id in new[] { "picker", "files", "uploads" })
        {
            Assert.IsFalse(await Page.Locator($"#{id} input.bit-input-hidden").EvaluateAsync<bool>("el => el.hasAttribute('required')"),
                           $"the hidden input of #{id} is required");
        }

        var restingColor = await BackgroundOf("#files .bit-fin-lbl");

        await Page.Locator("#submit").ClickAsync();

        await Expect(Page.Locator("#invalid-count")).ToHaveTextAsync("1");
        await Expect(Page.Locator("#valid-count")).ToHaveTextAsync("0");

        // the failed validation is shown and declared on the button the user operates; the picker and the upload,
        // which hold a color and a preloaded file, pass.
        await Expect(Page.Locator("#files")).ToHaveClassAsync(Invalid);
        await Expect(Page.Locator("#files .bit-fin-lbl")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#files-msg")).Not.ToBeEmptyAsync();
        await Expect(Page.Locator("#uploads")).Not.ToHaveClassAsync(Invalid);
        await Expect(Page.Locator("#uploads .bit-upl-lbl")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#picker")).Not.ToHaveClassAsync(Invalid);

        // the error color reaches the browse button.
        await Expect(Page.Locator("#files .bit-fin-lbl")).Not.ToHaveCSSAsync("background-color", restingColor);

        await Page.Locator("#files .bit-fin-fi").SetInputFilesAsync(TextFile("a.txt"));

        await Expect(Page.Locator("#files-value")).ToHaveTextAsync("a.txt");
        await Expect(Page.Locator("#files")).Not.ToHaveClassAsync(Invalid);
        await Expect(Page.Locator("#files .bit-fin-lbl")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#files-msg")).ToBeEmptyAsync();
        await Expect(Page.Locator("#files input.bit-input-hidden")).ToHaveValueAsync("a.txt");
        // read once the transition of the button back to its resting color has run.
        await Expect(Page.Locator("#files .bit-fin-lbl")).ToHaveCSSAsync("background-color", restingColor);

        await Page.Locator("#submit").ClickAsync();

        await Expect(Page.Locator("#valid-count")).ToHaveTextAsync("1");
    }

    [TestMethod]
    public async Task WhatTheAppDoesLeavesTheFormUnmodified()
    {
        await OpenPage();

        // the preloaded files of the record are what the upload's value names from the start, unmodified.
        await Expect(Page.Locator("#uploads-value")).ToHaveTextAsync("invoice.pdf");
        await Expect(Page.Locator("#color-value")).ToHaveTextAsync("#FF0000");
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("no");

        // a Format the app changes rewrites the color in its notation, which nobody picked.
        await Page.Locator("#btn-rgb").ClickAsync();

        await Expect(Page.Locator("#color-value")).ToHaveTextAsync("rgb(255,0,0)");
        await Expect(Page.Locator("#picker input.bit-input-hidden")).ToHaveValueAsync("rgb(255,0,0)");
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("no");

        // a swatch the user picks is an edit.
        await Page.Locator("#picker .bit-clp-prt").Nth(1).ClickAsync();

        await Expect(Page.Locator("#color-value")).ToHaveTextAsync("rgb(0,255,0)");
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("yes");

        // and so is a move of the keyboard on the saturation area.
        await Page.Locator("#btn-unmodified").ClickAsync();
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("no");

        await Page.Locator("#picker .bit-clp-rec").FocusAsync();
        await Page.Keyboard.PressAsync("ArrowDown");

        await Expect(Page.Locator("#color-value")).Not.ToHaveTextAsync("rgb(0,255,0)");
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("yes");
    }

    [TestMethod]
    public async Task AnAutoResetBrowseLeavesTheFieldValidWhileTheDialogIsOpen()
    {
        await OpenPage();

        await Page.Locator("#files .bit-fin-fi").SetInputFilesAsync(TextFile("a.txt"));
        await Expect(Page.Locator("#files-value")).ToHaveTextAsync("a.txt");
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("yes");

        await Page.Locator("#btn-unmodified").ClickAsync();
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("no");

        // the dialog is taken by the test and left without a choice, which is a dismissed dialog.
        await OpenAndDismissTheDialog(() => Page.Locator("#files .bit-fin-lbl").ClickAsync());

        // the list empties before the dialog opens, as AutoReset says, and the value follows it - but the field is
        // neither marked modified nor validated, so it does not turn red behind the dialog.
        await Expect(Page.Locator("#files-value")).ToBeEmptyAsync();
        await Expect(Page.Locator("#files [role=listitem]")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("no");
        await Expect(Page.Locator("#files-msg")).ToBeEmptyAsync();
        await Expect(Page.Locator("#files")).Not.ToHaveClassAsync(Invalid);

        // the selection the dialog delivers next is validated as usual.
        await Page.Locator("#files .bit-fin-fi").SetInputFilesAsync(TextFile("b.txt"));

        await Expect(Page.Locator("#files-value")).ToHaveTextAsync("b.txt");
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("yes");
    }

    [TestMethod]
    public async Task AReadOnlyFileInputOpensNoDialogFromALabelOfThePage()
    {
        await OpenPage();

        // the same label opens the dialog of an editable one, which is what makes the read-only case mean anything.
        var chooser = await Page.RunAndWaitForFileChooserAsync(() => Page.Locator("#rw-files-label").ClickAsync(),
                                                               new() { Timeout = 5000 });
        await chooser.SetFilesAsync(TextFile("a.txt"));
        await Expect(Page.Locator("#rw-files [role=listitem]")).ToHaveCountAsync(1);

        foreach (var label in new[] { "#ro-files-label", "#ro-uploads-label" })
        {
            Assert.IsFalse(await OpensADialog(() => Page.Locator(label).ClickAsync()), $"{label} opened a dialog");
        }

        await Expect(Page.Locator("#ro-files .bit-fin-fi")).ToBeDisabledAsync();
        await Expect(Page.Locator("#ro-uploads .bit-upl-fi")).ToBeDisabledAsync();
        await Expect(Page.Locator("#ro-files [role=listitem]")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task TheValueOfAnUploadFollowsItsFilesAndTheModel()
    {
        await OpenPage();

        await Page.Locator("#uploads .bit-upl-fi").SetInputFilesAsync(TextFile("b.txt"));

        await Expect(Page.Locator("#uploads-value")).ToHaveTextAsync("invoice.pdf,b.txt");
        await Expect(Page.Locator("#uploads-changes")).ToHaveTextAsync("invoice.pdf,b.txt");

        await Page.Locator("#uploads [aria-label='Upload b.txt']").ClickAsync();
        await Expect(Page.Locator("#uploads")).ToContainTextAsync("b.txt");

        // a model the app empties resets the selection; the record's file stays, and the value names it again.
        await Page.Locator("#btn-unmodified").ClickAsync();
        await Page.Locator("#btn-clear-model").ClickAsync();

        await Expect(Page.Locator("#uploads-value")).ToHaveTextAsync("invoice.pdf");
        await Expect(Page.Locator("#uploads input.bit-input-hidden")).ToHaveValueAsync("invoice.pdf");
        await Expect(Page.Locator("#uploads [aria-label='Upload b.txt']")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("no");
        await Expect(Page.Locator("#uploads-changes")).ToBeEmptyAsync();

        // a failed upload takes its file out of the value and validates the field, but is no edit the base OnChange
        // reports.
        await Page.Locator("#upload-fails").CheckAsync();
        await Page.Locator("#uploads .bit-upl-fi").SetInputFilesAsync(TextFile("c.txt"));

        await Expect(Page.Locator("#uploads-value")).ToHaveTextAsync("invoice.pdf,c.txt");

        await Page.Locator("#btn-unmodified").ClickAsync();
        await Page.Locator("#uploads [aria-label='Upload c.txt']").ClickAsync();

        await Expect(Page.Locator("#uploads-value")).ToHaveTextAsync("invoice.pdf");
        await Expect(Page.Locator("#uploads [aria-label='Retry c.txt']")).ToHaveCountAsync(1);
        await Expect(Page.Locator("#modified")).ToHaveTextAsync("yes");
        await Expect(Page.Locator("#uploads-changes")).ToBeEmptyAsync();
    }

    [TestMethod]
    public async Task AnUnboundInputSitsThroughTheValidationOfItsForm()
    {
        await OpenPage();

        await Page.Locator("#email input").FillAsync("not an email");
        await Page.Locator("#email input").PressAsync("Tab");

        await Expect(Page.Locator("#email")).ToHaveClassAsync(Invalid);

        await Page.Locator("#unbound input").FillAsync("typed");
        await Page.Locator("#unbound input").PressAsync("Tab");

        // the circuit is still up: the unbound field reports its value, and the form still submits.
        await Expect(Page.Locator("#unbound-value")).ToHaveTextAsync("typed");
        await Expect(Page.Locator("#unbound")).Not.ToHaveClassAsync(Invalid);

        await Page.Locator("#submit").ClickAsync();

        await Expect(Page.Locator("#invalid-count")).ToHaveTextAsync("1");
    }

    // An exception a handler threw shows on the page only as an error in the console or a circuit the server tore down,
    // so every test ends by reading both.
    [TestCleanup]
    public async Task AssertNoErrors()
    {
        // the upload endpoint that refuses every chunk answers with a 500 on purpose, which the browser logs.
        var errors = _pageErrors.Where(e => e.Contains("favicon", StringComparison.OrdinalIgnoreCase) is false &&
                                            e.Contains("status of 500", StringComparison.Ordinal) is false).ToList();

        Assert.AreEqual(0, errors.Count, $"the page reported: {string.Join(" || ", errors)}");

        var json = await _http.GetStringAsync($"{BaseUrl}/api/server-errors");
        var serverErrors = JsonSerializer.Deserialize<string[]>(json) ?? [];

        Assert.AreEqual(0, serverErrors.Length, $"the server logged: {string.Join(" || ", serverErrors)}");
    }

    private async Task OpenPage()
    {
        await _http.DeleteAsync($"{BaseUrl}/api/server-errors");

        await Page.GotoAsync($"{BaseUrl}/regression/form-inputs");
        await WaitForStatus("Ready");

        // Listened to only once this page's circuit is up: leaving the page the test started on aborts that page's
        // own negotiation with the server, which Firefox reports as a NetworkError of the page that is left.
        Page.Console += (_, message) =>
        {
            if (message.Type == "error") _pageErrors.Add(message.Text);
        };
        Page.PageError += (_, error) => _pageErrors.Add(error);
    }

    // A dialog opened from a click that went through the server and back may or may not keep the activation an engine
    // asks for, so a dialog that never opens is no failure here: what is checked is the page around it.
    private async Task OpenAndDismissTheDialog(Func<Task> action)
    {
        try
        {
            await Page.RunAndWaitForFileChooserAsync(action, new() { Timeout = 3000 });
        }
        catch (TimeoutException)
        {
        }
    }

    private async Task<bool> OpensADialog(Func<Task> action)
    {
        try
        {
            await Page.RunAndWaitForFileChooserAsync(action, new() { Timeout = 2000 });
            return true;
        }
        catch (TimeoutException)
        {
            return false;
        }
    }

    private Task<string> BackgroundOf(string selector)
    {
        return Page.Locator(selector).EvaluateAsync<string>("el => getComputedStyle(el).backgroundColor");
    }

    private static FilePayload TextFile(string name) => new()
    {
        Name = name,
        MimeType = "text/plain",
        Buffer = Encoding.UTF8.GetBytes($"the content of {name}")
    };
}

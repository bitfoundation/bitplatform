using System.Text.Json;
using System.Text.RegularExpressions;

namespace Bit.BlazorUI.Tests.E2E.AI;

/// <summary>
/// Drives BitAiForm at /e2e/aiform in a real browser, over a form built from a JSON schema: a control per property with
/// its label and hint, the required fields and the invalid values flagged (in the inputs' own error messages, aria-invalid
/// and the alert of a list) and nothing sent, an error cleared by fixing its field, a valid form sent as flat values and
/// as JSON in the schema's shape, the entries of a list added and removed, prefilled values, Cancel, Submitted, Busy,
/// Disabled, the keyboard, the theme tokens and the public variables, Styles/Classes, and right-to-left.
/// </summary>
[TestClass]
public class BitAiFormE2ETests : E2ETestBase
{
    private const string Route = "/e2e/aiform";

    private ILocator Root(string id) => Page.Locator($"#{id}");

    private ILocator Text(string id) => Page.Locator($"#{id}");

    // A control is found by the id the form gives its field (the form's id and the field's path); the ones of a nested
    // object, whose paths the form hashes into their ids, by their place in its group.
    private ILocator Field(string name, string form = "frm-main") => name switch
    {
        "City" => Page.Locator($"#{form}-address .bit-aifrm-fld").Nth(0).Locator("input"),
        "Zip" => Page.Locator($"#{form}-address .bit-aifrm-fld").Nth(1).Locator("input"),
        _ => Page.Locator($"#{form}-{char.ToLowerInvariant(name[0])}{name[1..].Replace(" ", "")} input, #{form}-{char.ToLowerInvariant(name[0])}{name[1..].Replace(" ", "")} textarea").First,
    };

    private ILocator Submit(string id) => Page.Locator($"#{id} .bit-aifrm-sbt");

    private ILocator Cancel(string id) => Page.Locator($"#{id} .bit-aifrm-cnl");

    private ILocator Tags => Page.Locator("#frm-main-tags");

    private ILocator TagInputs => Page.Locator("#frm-main-tags .bit-aifrm-itm input");

    private async Task PickPlanAsync(string plan)
    {
        var field = Page.Locator("#frm-main-plan .bit-drp-wrp");
        await field.ClickAsync();

        var callout = Page.Locator($"[id='{await field.GetAttributeAsync("id")}-callout']");
        await Expect(callout).ToBeVisibleAsync();
        await callout.GetByRole(AriaRole.Option, new() { Name = plan, Exact = true }).ClickAsync();
        await Expect(callout).ToBeHiddenAsync();
        await Expect(Page.Locator("#frm-main-plan .bit-drp-tcn")).ToHaveTextAsync(plan);
    }

    private async Task FillValidFormAsync()
    {
        await Field("Name").FillAsync("Ada");
        await Field("Email").FillAsync("ada@example.com");
        await Page.Locator("#frm-main-age input").FillAsync("36");
        await Page.Locator("#frm-main-age input").PressAsync("Tab");
        await PickPlanAsync("Pro");
        await Page.Locator("#frm-main-subscribe label").ClickAsync();
        await Expect(Page.Locator("#frm-main-subscribe input")).ToBeCheckedAsync();
        await Field("City").FillAsync("London");
        await Tags.Locator(".bit-aifrm-add").ClickAsync();
        await Expect(TagInputs).ToHaveCountAsync(1);
        await TagInputs.Nth(0).FillAsync("math");
    }

    [TestMethod]
    public async Task TheSchema_RendersAControlPerProperty_WithItsLabelAndHint()
    {
        await OpenAsync(Route);

        await Expect(Page.Locator("#frm-main .bit-aifrm-ttl")).ToHaveTextAsync("Book a demo");
        await Expect(Page.Locator("#frm-main .bit-aifrm-des")).ToHaveTextAsync("Tell us who you are and we will be in touch.");

        await Expect(Field("Name")).ToHaveAttributeAsync("required", "");
        await Expect(Field("Name")).ToHaveAttributeAsync("minlength", "2");
        await Expect(Page.Locator("#frm-main-name")).ToContainTextAsync("Your full name.");
        await Expect(Field("Email")).ToHaveAttributeAsync("type", "email");
        await Expect(Page.Locator("#frm-main").GetByRole(AriaRole.Spinbutton, new() { Name = "Age" })).ToHaveAttributeAsync("aria-valuemin", "18");
        await Expect(Page.Locator("#frm-main").GetByRole(AriaRole.Spinbutton, new() { Name = "Age" })).ToHaveAttributeAsync("aria-valuemax", "120");
        await Expect(Page.Locator("#frm-main").GetByRole(AriaRole.Combobox, new() { Name = "Plan" })).ToHaveAttributeAsync("aria-required", "true");
        await Expect(Page.Locator("#frm-main").GetByRole(AriaRole.Checkbox, new() { Name = "Subscribe to the newsletter" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("#frm-main-notes textarea")).ToHaveAttributeAsync("rows", "4");

        // A nested object and a list are groups named by their legends.
        await Expect(Page.Locator("#frm-main").GetByRole(AriaRole.Group, new() { Name = "Address" })).ToHaveCountAsync(1);
        await Expect(Page.Locator("#frm-main").GetByRole(AriaRole.Group, new() { Name = "Tags" })).ToHaveCountAsync(1);
        await Expect(Field("City")).ToHaveAttributeAsync("required", "");
        await Expect(Field("Zip")).Not.ToHaveAttributeAsync("required", new Regex(".*"));
        await Expect(Tags.Locator(".bit-aifrm-req")).ToHaveAttributeAsync("aria-hidden", "true");

        await Expect(Submit("frm-main")).ToHaveTextAsync("Submit");
        await Expect(Cancel("frm-main")).ToHaveTextAsync("Cancel");
    }

    [TestMethod]
    public async Task Submit_WithTheRequiredFieldsEmpty_FlagsEachOne_AndSendsNothing()
    {
        await OpenAsync(Route);

        await Submit("frm-main").ClickAsync();

        await Expect(Page.Locator("#frm-main-name .bit-tfl-erm")).ToHaveTextAsync("Name is required.");
        await Expect(Field("Name")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#frm-main-email .bit-tfl-erm")).ToHaveTextAsync("Email is required.");
        await Expect(Page.Locator("#frm-main-plan .bit-drp-erm")).ToHaveTextAsync("Plan is required.");
        await Expect(Page.Locator("#frm-main").Locator(".bit-tfl-erm", new() { HasText = "City is required." })).ToHaveCountAsync(1);

        // A list draws its own message, as an alert the group points at, and outlines itself in the error color.
        var tagsError = Page.Locator("#frm-main-tags-error");
        await Expect(tagsError).ToHaveTextAsync("Tags is required.");
        await Expect(tagsError).ToHaveAttributeAsync("role", "alert");
        await Expect(Tags).ToHaveAttributeAsync("aria-describedby", "frm-main-tags-error");
        await Expect(Tags).ToHaveClassAsync(HasClass("bit-aifrm-inv"));
        var error = await Page.ResolveColorAsync("var(--bit-clr-err)");
        await Expect(Tags).ToHaveCSSAsync("border-top-color", error);
        await Expect(tagsError).ToHaveCSSAsync("color", error);

        // What is not required is not flagged.
        await Expect(Page.Locator("#frm-main-age .bit-nfl-erm")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#frm-main-notes .bit-tfl-erm")).ToHaveCountAsync(0);

        await Expect(Text("frm-count")).ToHaveTextAsync("0");
        await Expect(Text("frm-json")).ToHaveTextAsync("-");
    }

    [TestMethod]
    public async Task Submit_WithInvalidValues_ExplainsEachOne()
    {
        await OpenAsync(Route);

        await FillValidFormAsync();
        await Field("Name").FillAsync("A");
        await Field("Email").FillAsync("not-an-email");
        await Page.Locator("#frm-main-age input").FillAsync("12");
        await Page.Locator("#frm-main-age input").PressAsync("Tab");
        await Field("Zip").FillAsync("12");

        await Submit("frm-main").ClickAsync();

        await Expect(Page.Locator("#frm-main-name .bit-tfl-erm")).ToHaveTextAsync("Name must be at least 2 characters.");
        await Expect(Page.Locator("#frm-main-email .bit-tfl-erm")).ToHaveTextAsync("Email must be an email address.");
        await Expect(Page.Locator("#frm-main-age .bit-nfl-erm")).ToHaveTextAsync("Age must be at least 18.");
        await Expect(Page.Locator("#frm-main").Locator(".bit-tfl-erm", new() { HasText = "Zip is not in the expected format." })).ToHaveCountAsync(1);
        await Expect(Page.Locator("#frm-main-age input")).ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Text("frm-count")).ToHaveTextAsync("0");

        // Each value is checked against what the schema says, one at a time.
        await Expect(Page.Locator("#frm-main-plan .bit-drp-erm")).ToHaveCountAsync(0);
        await Expect(Page.Locator("#frm-main-tags-error")).ToHaveCountAsync(0);
    }

    [TestMethod]
    public async Task FixingAField_ClearsItsError_AndLeavesTheOthers()
    {
        await OpenAsync(Route);

        await Submit("frm-main").ClickAsync();
        await Expect(Page.Locator("#frm-main-name .bit-tfl-erm")).ToHaveTextAsync("Name is required.");
        await Expect(Page.Locator("#frm-main-email .bit-tfl-erm")).ToHaveTextAsync("Email is required.");

        await Field("Name").FillAsync("Grace");

        await Expect(Page.Locator("#frm-main-name .bit-tfl-erm")).ToHaveCountAsync(0);
        await Expect(Field("Name")).Not.ToHaveAttributeAsync("aria-invalid", "true");
        await Expect(Page.Locator("#frm-main-email .bit-tfl-erm")).ToHaveTextAsync("Email is required.");

        // Adding an entry clears the message of the list.
        await Tags.Locator(".bit-aifrm-add").ClickAsync();
        await Expect(Page.Locator("#frm-main-tags-error")).ToHaveCountAsync(0);
        await Expect(Tags).Not.ToHaveClassAsync(HasClass("bit-aifrm-inv"));
    }

    [TestMethod]
    public async Task Submit_AValidForm_SendsTheValues_AndTheJsonInTheShapeOfTheSchema()
    {
        await OpenAsync(Route);

        await FillValidFormAsync();
        await Submit("frm-main").ClickAsync();

        await Expect(Text("frm-count")).ToHaveTextAsync("1");
        await Expect(Text("frm-values")).ToHaveTextAsync("address.city=London;age=36;email=ada@example.com;name=Ada;plan=pro;subscribe=true;tags[0]=math");

        await Expect(Text("frm-json")).Not.ToHaveTextAsync("-");
        using var json = JsonDocument.Parse(await Text("frm-json").TextContentAsync() ?? "{}");
        var root = json.RootElement;
        Assert.AreEqual("Ada", root.GetProperty("name").GetString());
        Assert.AreEqual("ada@example.com", root.GetProperty("email").GetString());
        Assert.AreEqual(JsonValueKind.Number, root.GetProperty("age").ValueKind);
        Assert.AreEqual(36, root.GetProperty("age").GetInt32());
        Assert.AreEqual("pro", root.GetProperty("plan").GetString());
        Assert.AreEqual(JsonValueKind.True, root.GetProperty("subscribe").ValueKind);
        Assert.AreEqual("London", root.GetProperty("address").GetProperty("city").GetString());
        Assert.AreEqual(JsonValueKind.Array, root.GetProperty("tags").ValueKind);
        CollectionAssert.AreEqual(new[] { "math" }, root.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToArray());
        Assert.IsFalse(root.TryGetProperty("notes", out _), "A field left empty is left out.");

        // The form stays as it is, for the host to mark it submitted.
        await Expect(Submit("frm-main")).ToBeVisibleAsync();
    }

    [TestMethod]
    public async Task AList_TakesEntriesUpToItsMaximum_AndEachOneIsRemovedByItsNamedButton()
    {
        await OpenAsync(Route);

        var add = Tags.Locator(".bit-aifrm-add");
        await Expect(add).ToHaveTextAsync("Add item");
        await add.ClickAsync();
        await TagInputs.Nth(0).FillAsync("one");
        await add.ClickAsync();
        await TagInputs.Nth(1).FillAsync("two");
        await add.ClickAsync();
        await TagInputs.Nth(2).FillAsync("three");

        // An entry of a list of text has no label of its own: it is named after the list and its place in it.
        await Expect(Tags.GetByRole(AriaRole.Textbox, new() { Name = "Tags 2", Exact = true })).ToHaveValueAsync("two");

        // Three is the most it takes, so the add button goes away.
        await Expect(TagInputs).ToHaveCountAsync(3);
        await Expect(add).ToHaveCountAsync(0);

        var removeSecond = Tags.GetByRole(AriaRole.Button, new() { Name = "Remove Tags 2", Exact = true });
        await Expect(removeSecond).ToHaveAttributeAsync("title", "Remove Tags 2");
        await removeSecond.ClickAsync();

        // The entries after it move down a slot.
        await Expect(TagInputs).ToHaveCountAsync(2);
        await Expect(TagInputs.Nth(0)).ToHaveValueAsync("one");
        await Expect(TagInputs.Nth(1)).ToHaveValueAsync("three");
        await Expect(Tags.Locator(".bit-aifrm-add")).ToHaveCountAsync(1);

        // An entry left empty is required once it is there.
        await Tags.Locator(".bit-aifrm-add").ClickAsync();
        await Expect(TagInputs).ToHaveCountAsync(3);
        await Submit("frm-main").ClickAsync();
        await Expect(Tags.Locator(".bit-tfl-erm", new() { HasText = "Tags 3 is required." })).ToHaveCountAsync(1);
    }

    [TestMethod]
    public async Task PrefilledValues_SeedTheControlsAndTheList()
    {
        await OpenAsync(Route);

        var form = Page.Locator("#frm-prefill");
        await Expect(Field("Name", "frm-prefill")).ToHaveValueAsync("Ada Lovelace");
        await Expect(form.GetByRole(AriaRole.Spinbutton, new() { Name = "Age" })).ToHaveValueAsync("36");
        await Expect(form.Locator(".bit-drp-tcn")).ToHaveTextAsync("Pro");
        await Expect(form.GetByRole(AriaRole.Checkbox, new() { Name = "Subscribe to the newsletter" })).ToBeCheckedAsync();
        await Expect(Field("City", "frm-prefill")).ToHaveValueAsync("London");

        var tags = form.Locator(".bit-aifrm-grp .bit-aifrm-itm input");
        await Expect(tags).ToHaveCountAsync(2);
        await Expect(tags.Nth(0)).ToHaveValueAsync("math");
        await Expect(tags.Nth(1)).ToHaveValueAsync("engines");

        await Field("Email", "frm-prefill").FillAsync("ada@example.com");
        await form.Locator(".bit-aifrm-sbt").ClickAsync();

        await Expect(Text("frm-prefill-json")).Not.ToHaveTextAsync("-");
        using var json = JsonDocument.Parse(await Text("frm-prefill-json").TextContentAsync() ?? "{}");
        CollectionAssert.AreEqual(new[] { "math", "engines" }, json.RootElement.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToArray());
        Assert.AreEqual(36, json.RootElement.GetProperty("age").GetInt32());
    }

    [TestMethod]
    public async Task Cancel_RaisesOnCancel_AndAnEmptyCancelLabelHidesIt()
    {
        await OpenAsync(Route);

        await Cancel("frm-main").ClickAsync();
        await Expect(Text("frm-cancels")).ToHaveTextAsync("1");
        await Expect(Text("frm-count")).ToHaveTextAsync("0");
        await Expect(Page.Locator("#frm-main-name .bit-tfl-erm")).ToHaveCountAsync(0);

        await Expect(Cancel("frm-nocancel")).ToHaveCountAsync(0);
        await Expect(Submit("frm-nocancel")).ToHaveTextAsync("Rename");
    }

    [TestMethod]
    public async Task Submitted_ReplacesTheActionsWithAConfirmation()
    {
        await OpenAsync(Route);

        await Expect(Submit("frm-submitted")).ToHaveCountAsync(0);
        await Expect(Cancel("frm-submitted")).ToHaveCountAsync(0);

        var done = Page.Locator("#frm-submitted .bit-aifrm-sbm .bit-tag");
        await Expect(done).ToHaveTextAsync("Submitted");
        await Expect(done.Locator("i.bit-tag-icn")).ToHaveClassAsync(HasClass("bit-icon--Accept"));

        // The actions keep their side of the row.
        var form = await Root("frm-submitted").RectAsync();
        var tag = await done.RectAsync();
        Assert.IsTrue(form.Right - tag.Right < 40, "The confirmation sits at the end of the row.");
    }

    [TestMethod]
    public async Task Busy_DisablesTheActions_AndSendsNothing()
    {
        await OpenAsync(Route);

        await Expect(Submit("frm-busy")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Cancel("frm-busy")).ToHaveAttributeAsync("aria-disabled", "true");

        // The fields stay editable.
        var name = Field("FileName", "frm-busy");
        await Expect(name).ToBeEditableAsync();
        await name.FillAsync("report.pdf");
        await Submit("frm-busy").ClickAsync(new() { Force = true });
        await Submit("frm-busy").DispatchEventAsync("click");

        // A submission of another form, after it, proves the presses were answered.
        await Cancel("frm-main").ClickAsync();
        await Expect(Text("frm-cancels")).ToHaveTextAsync("1");
        await Expect(Text("frm-busy-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Disabled_DisablesEveryControl_AndSendsNothing()
    {
        await OpenAsync(Route);

        var form = Root("frm-dis");
        await Expect(form).ToHaveClassAsync(HasClass("bit-dis"));
        await Expect(Field("FileName", "frm-dis")).ToBeDisabledAsync();
        await Expect(Submit("frm-dis")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(Cancel("frm-dis")).ToHaveAttributeAsync("aria-disabled", "true");
        await Expect(form.Locator(".bit-aifrm-ttl")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-dis)"));

        await Submit("frm-dis").ClickAsync(new() { Force = true });
        await Submit("frm-dis").DispatchEventAsync("click");
        await Cancel("frm-main").ClickAsync();
        await Expect(Text("frm-cancels")).ToHaveTextAsync("1");
        await Expect(Text("frm-dis-count")).ToHaveTextAsync("0");
    }

    [TestMethod]
    public async Task Keyboard_TabWalksTheFieldsInOrder_AndEnterOnSubmitSends()
    {
        await OpenAsync(Route);

        await Field("Name").FocusAsync();
        await Page.Keyboard.InsertTextAsync("Ada");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Field("Email")).ToBeFocusedAsync();
        await Page.Keyboard.InsertTextAsync("ada@example.com");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#frm-main-age input")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#frm-main-plan .bit-drp-wrp")).ToBeFocusedAsync();

        // The dropdown opens on the keyboard and picks with Enter.
        await Page.Keyboard.PressAsync("ArrowDown");
        var callout = Page.Locator($"[id='{await Page.Locator("#frm-main-plan .bit-drp-wrp").GetAttributeAsync("id")}-callout']");
        await Expect(callout).ToBeVisibleAsync();
        await Expect(callout.GetByRole(AriaRole.Option, new() { Name = "Free" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("ArrowDown");
        await Expect(callout.GetByRole(AriaRole.Option, new() { Name = "Pro" })).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(Page.Locator("#frm-main-plan .bit-drp-tcn")).ToHaveTextAsync("Pro");
        await Expect(Page.Locator("#frm-main-plan .bit-drp-wrp")).ToBeFocusedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#frm-main-subscribe input")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync(" ");
        await Expect(Page.Locator("#frm-main-subscribe input")).ToBeCheckedAsync();

        await Page.Keyboard.PressAsync("Tab");
        await Expect(Page.Locator("#frm-main-notes textarea")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Field("City")).ToBeFocusedAsync();
        await Page.Keyboard.InsertTextAsync("London");
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Field("Zip")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Tab");
        await Expect(Tags.Locator(".bit-aifrm-add")).ToBeFocusedAsync();
        await Page.Keyboard.PressAsync("Enter");
        await Expect(TagInputs).ToHaveCountAsync(1);
        await TagInputs.Nth(0).FocusAsync();
        await Page.Keyboard.InsertTextAsync("math");

        await Submit("frm-main").FocusAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Expect(Text("frm-count")).ToHaveTextAsync("1");
        await Expect(Text("frm-values")).ToHaveTextAsync("address.city=London;email=ada@example.com;name=Ada;plan=pro;subscribe=true;tags[0]=math");
    }

    [TestMethod]
    public async Task Style_TheThemeTokensPaintTheForm()
    {
        await OpenAsync(Route);

        var form = Root("frm-nocancel");
        await Expect(form).ToHaveCSSAsync("background-color", await Page.ResolveColorAsync("var(--bit-clr-bg-sec)"));
        await Expect(form).ToHaveCSSAsync("border-top-color", await Page.ResolveColorAsync("var(--bit-clr-brd-pri)"));
        await Expect(form).ToHaveCSSAsync("border-top-style", "solid");
        await Expect(form).ToHaveCSSAsync("display", "flex");
        await Expect(form).ToHaveCSSAsync("flex-direction", "column");
        await Expect(form.Locator(".bit-aifrm-ttl")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-pri)"));
        await Expect(Page.Locator("#frm-main .bit-aifrm-des")).ToHaveCSSAsync("color", await Page.ResolveColorAsync("var(--bit-clr-fg-sec)"));
        await Expect(Page.Locator("#frm-main .bit-aifrm-act")).ToHaveCSSAsync("justify-content", "flex-end");

        // The actions are a neutral outlined Cancel and a primary Submit, Cancel first.
        await Expect(Cancel("frm-main")).ToHaveClassAsync(HasClass("bit-btn-otl"));
        await Expect(Submit("frm-main")).ToHaveClassAsync(HasClass("bit-btn-fil"));
        var cancel = await Cancel("frm-main").RectAsync();
        var submit = await Submit("frm-main").RectAsync();
        Assert.IsTrue(cancel.Right <= submit.Left + 1, "Cancel comes before Submit.");
    }

    [TestMethod]
    public async Task Style_ThePublicVariables_RestyleTheForm_OnTheInstanceOrAnAncestor()
    {
        await OpenAsync(Route);

        var form = Root("frm-style");
        await Expect(form).ToHaveCSSAsync("background-color", "rgb(1, 2, 3)");
        await Expect(form).ToHaveCSSAsync("border-top-color", "rgb(4, 5, 6)");
        await Expect(form).ToHaveCSSAsync("border-top-width", "3px");
        await Expect(form).ToHaveCSSAsync("border-top-left-radius", "9px");
        await Expect(form).ToHaveCSSAsync("padding-top", "21px");
        await Expect(form).ToHaveCSSAsync("row-gap", "17px");
        await Expect(form.Locator(".bit-aifrm-ttl")).ToHaveCSSAsync("color", "rgb(7, 8, 9)");
        await Expect(form.Locator(".bit-aifrm-ttl")).ToHaveCSSAsync("font-size", "23px");
        await Expect(form.Locator(".bit-aifrm-act")).ToHaveCSSAsync("justify-content", "flex-start");

        await Expect(Root("frm-style-ancestor")).ToHaveCSSAsync("background-color", "rgb(10, 11, 12)");

        await Expect(Root("frm-style-parts")).ToHaveClassAsync(HasClass("frm-custom-root"));
        await Expect(Page.Locator("#frm-style-parts .bit-aifrm-ttl")).ToHaveClassAsync(HasClass("frm-custom-title"));
        await Expect(Page.Locator("#frm-style-parts .bit-aifrm-ttl")).ToHaveCSSAsync("letter-spacing", "3px");
        await Expect(Page.Locator("#frm-style-parts .bit-aifrm-act")).ToHaveCSSAsync("column-gap", "19px");
    }

    [TestMethod]
    public async Task Rtl_TheFormReadsRightToLeft_AndItsActionsSitOnTheLeft()
    {
        await OpenAsync(Route);

        var form = Root("frm-rtl");
        await Expect(form).ToHaveAttributeAsync("dir", "rtl");
        await Expect(form).ToHaveCSSAsync("direction", "rtl");

        // flex-end is the inline end: the left, right to left; and Cancel stays first in reading order, on the right.
        var formRect = await form.RectAsync();
        var submit = await Submit("frm-rtl").RectAsync();
        var cancel = await Cancel("frm-rtl").RectAsync();
        Assert.IsTrue(submit.Left - formRect.Left < 60, "The actions sit at the left edge.");
        Assert.IsTrue(submit.Right <= cancel.Left + 1, "Cancel is on the right of Submit.");

        var label = await Page.Locator("#frm-rtl .bit-tfl-lbl").RectAsync();
        var input = await Page.Locator("#frm-rtl input").RectAsync();
        Assert.IsTrue(Math.Abs(label.Right - input.Right) < 8, "The label lines up with the right edge of its input.");
    }
}

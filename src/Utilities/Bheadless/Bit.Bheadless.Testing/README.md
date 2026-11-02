# Bit.Bheadless.Testing

Accessibility test tooling for Razor components, built on [bUnit](https://bunit.dev): a static WAI-ARIA rule engine, accessible names, focus order, keyboard helpers and a recording live-region announcer. bUnit is test-framework neutral, so it works with MSTest, xUnit and NUnit; the samples below use MSTest.

| API | What it does |
| --- | --- |
| `cut.ShouldBeAccessible()` / `cut.Audit()` | Static WAI-ARIA 1.2 rule engine over the rendered markup (see rules below). Throws `BitBhA11yException` listing every violation, or returns a `BitBhA11yReport`. `BitBhA11y.Audit(node)` audits any AngleSharp node. |
| `element.AccessibleName()` / `AccessibleDescription()` | The W3C accessible name and description computation (accname 1.2, HTML-AAM): `aria-labelledby`, `aria-label`, `<label>`, `alt`, `title`, embedded control values, name from content, hidden content. |
| `element.Role()` | Explicit or implicit role, as browsers compute it. |
| `cut.TabSequence()` | The keyboard Tab order: positive `tabindex` first; disabled, unrendered and `inert` content skipped; one stop per native radio group. |
| `element.Press("ArrowDown", shift: true)`, `PressSequence(…)`, `Type("abc")` | Keyboard helpers (`key` and US-layout `code`; `Type` holds Shift for uppercase letters and shifted symbols; `repeat: true` for a held key). Like a browser, one press reaches both `onkeydown` and the library's owned `onbitbhkeydown`. |
| `element.PointerClick()` | A pointer click that reaches both `onclick` and the library's owned `onbitbhclick` - click grid cells and tree items with it rather than bUnit's `Click()`, which only dispatches `onclick`. |
| `cut.ActiveDescendant(element)` | The element referenced by `aria-activedescendant`. |
| `context.LastFocusRequest()` | The id of the element Bit.Bheadless last asked the browser to focus (bUnit has no real focus). |
| `BitBhTestAnnouncer` | Records live-region announcements; `await announcer.WaitForAsync("2 results")` waits for debounced ones. |
| `BitBhA11yReport.ToMarkdown()` / `ToJson()` | CI-friendly reports (e.g. write to `$GITHUB_STEP_SUMMARY`). |

**Generic vs. Bit.Bheadless-specific.** The audit, accessible names and roles, the Tab sequence, the keyboard helpers and `ActiveDescendant()` only read rendered markup, so they work for any Razor component. `BitBhTestContext` (its browser emulation), `AddBitBheadlessServices()`, `LastFocusRequest()` and `BitBhTestAnnouncer` plug into the Bit.Bheadless runtime (its services, JS interop calls and `IBitBhAnnouncer`), which the package depends on.

## Getting started

```bash
dotnet add package Bit.Bheadless.Testing
```

Derive your test classes from **`BitBhTestContext`** (a bUnit `BunitContext`):

```csharp
[TestClass]
public class SwitchTests : BitBhTestContext
{
    [TestMethod]
    public void Switch_is_named_and_toggles()
    {
        var cut = Render<BitBhSwitch>(p => p.AddUnmatched("aria-label", "Wi-Fi"));
        var toggle = cut.Find("[role=switch]");

        Assert.AreEqual("Wi-Fi", toggle.AccessibleName());
        toggle.Click();
        Assert.AreEqual("true", cut.Find("[role=switch]").GetAttribute("aria-checked"));
        cut.ShouldBeAccessible();
    }
}
```

A `.razor` test file inherits the context, marks itself as a test class and keeps only an `@code` block (with `@using Microsoft.VisualStudio.TestTools.UnitTesting`, `Bunit`, `Bit.Bheadless` and `Bit.Bheadless.Testing` in `_Imports.razor`):

```razor
@* MenuTests.razor *@
@inherits BitBhTestContext
@attribute [TestClass]

@code {
    [TestMethod]
    public void Menu_is_accessible_and_keyboard_operable()
    {
        var cut = Render(
            @<BitBhMenu>
                <BitBhMenuButton>Options</BitBhMenuButton>
                <BitBhMenuItems>
                    <BitBhMenuItem>Edit</BitBhMenuItem>
                    <BitBhMenuItem>Delete</BitBhMenuItem>
                </BitBhMenuItems>
            </BitBhMenu>);

        cut.Find("button").Press("ArrowDown");

        var menu = cut.Find("[role=menu]");
        Assert.AreEqual("Edit", cut.ActiveDescendant(menu)!.AccessibleName());
        cut.ShouldBeAccessible();
    }
}
```

`BitBhTestContext`:

- sets bUnit's JS interop to loose mode and registers the Bit.Bheadless services;
- replaces the announcer with a recording `BitBhTestAnnouncer` (the `Announcer` property);
- **emulates the browser runtime's DOM queries**: item collections (options, menu items, tree items, roving items …) learn their document order and rendered text from the rendered markup, so arrow-key order, typeahead and text filtering behave as they do in a browser.

Already have your own base class? Call `this.AddBitBheadlessServices()` (an extension of `BunitContext`) in its constructor to get the services and the recording announcer, without the DOM emulation.

## Rules

| Rule | Checks |
| --- | --- |
| `duplicate-id` | ids are unique (every element sharing an id is reported) |
| `aria-valid-role` | `role` values exist in WAI-ARIA 1.2, DPUB-ARIA 1.1 or Graphics-ARIA (the first recognised token of a fallback list counts) |
| `aria-valid-attr-value` | ARIA attribute values have the right type (true/false, tristate, integer, number, token — tokens compared case-insensitively) and range values lie within their bounds |
| `aria-allowed-attr` | role-specific attributes are allowed on the element's role |
| `aria-required-attr` | required states and properties are present (e.g. `aria-valuenow` on `slider` and `meter`) |
| `idref-exists` | `aria-labelledby`, `aria-describedby`, `aria-controls`, `aria-activedescendant`, `for` … point at existing ids |
| `aria-required-children` | composite widgets own only allowed roles (listbox → option / group, tree → treeitem / group …; such a group only holds the container's items), `aria-owns` included |
| `aria-required-parent` | options, tabs, menu items, tree items, rows, cells … sit inside the right container, `aria-owns` included |
| `accessible-name` | roles that require a name (ARIA 1.2) have one |
| `aria-hidden-focus` | nothing in the Tab order inside `aria-hidden="true"` |
| `nested-interactive` | no interactive content inside buttons, links and similar |
| `tabindex` | no positive `tabindex` |
| `presentation-conflict` | `role="presentation"` / `"none"` isn't ignored because of focusability or global ARIA |
| `activedescendant-target` | `aria-activedescendant` targets are owned / controlled by the element |
| `form-label` | form fields (including password, date, file … inputs) are labelled |

Configure with `BitBhA11yOptions`: `.Without("rule-id")`, `MinimumImpact`, `IsFragment` (ignore references that may live elsewhere on the page).

## Reporting in CI

```csharp
var report = cut.Audit();
File.AppendAllText(Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY") ?? "a11y.md", report.ToMarkdown("Checkout form"));
Assert.IsTrue(report.Passed, report.ToString());
```

## Scope

Static checks catch structural ARIA mistakes early and fast, in the same test run as your behaviour tests. Without stylesheets, visibility and layout come from the markup only (`hidden`, inline `display` / `visibility`, `inert`, closed `details` / `dialog`). The checks complement — they don't replace — in-browser scanning (e.g. axe-core via Playwright, which also checks colour contrast and layout) and testing with real assistive technology.

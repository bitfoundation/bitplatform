using ModelContextProtocol.Server;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Bit.Borm.Demo.Server.Controllers;

/// <summary>
/// Ready-made workflows for the three things people actually ask a form library for: add it to an
/// app, build a form with it, and work out why a form does not behave.
/// <para>
/// Each prompt spends its words on the order to call the tools in and on the mistakes that are
/// expensive to make - the ones that produce a form that throws only once it renders, renders as
/// plain HTML, or quietly accepts what it should refuse, rather than a compiler error - because the
/// failure mode of an agent with a handful of tools is not ignorance, it is calling them in a
/// sequence that skips the check which would have caught the bug.
/// </para>
/// </summary>
[McpServerPromptType]
public static class McpPrompts
{
    [McpServerPrompt(Name = "add-borm-to-app", Title = "Add Bit.Borm to a Blazor app")]
    [Description("Walks through adding Bit.Borm to an existing Blazor app, in the right order for its hosting model.")]
    public static string AddBormToApp(
        // The values go into the prompt's own argument schema, which is what a client offers as
        // completions - so picking the model is a choice from a list rather than a spelling test.
        [AllowedValues("unknown", "web-app", "webassembly", "server", "hybrid")]
        [Description("The app's Blazor hosting model: web-app, webassembly, server or hybrid. Pass 'unknown' to have it determined from the project first.")] string hostingModel = "unknown")
    {
        return $"""
            Add Bit.Borm to this Blazor app. Its hosting model is: {hostingModel}.

            Work in this order:

            1. If the hosting model is 'unknown', determine it from the project first: look for
               AddInteractiveServerComponents / AddInteractiveWebAssemblyComponents in Program.cs, for a separate
               .Client project, for a standalone wwwroot/index.html, and for a MAUI/WPF/Windows Forms BlazorWebView host,
               then continue with what you find.
            2. Call `GetBormSetupGuide` with that hosting model and follow it.
            3. Register `builder.Services.AddBorm();` in EVERY container that renders a form - in a Blazor Web App with a
               WebAssembly or Auto client that is both the server's and the client's Program.cs, because every page is
               prerendered on the server first. Missing it in one of them is the most common mistake: the form throws
               "There is no registered service of type 'Bit.Borm.IBormFieldRendererRegistry'" as soon as it renders.
            4. DO link the stylesheet from the host page - `_content/Bit.Borm/bit-borm.css` (App.razor, or
               wwwroot/index.html). Nothing injects it, and without it the form renders as plain, unstyled HTML.
               There is no script to add: Borm has no JavaScript.
            5. Add `@using Bit.Borm` to `_Imports.razor`.
            6. Put the form on an interactively rendered page: in static server rendering it cannot respond to input.
            7. Only if the app uses FluentValidation, add the separate `Bit.Borm.FluentValidation` package.
            8. Build the app and fix what the compiler says.

            Show me the diff for each file you change, and say which container(s) `AddBorm()` went into and why.
            """;
    }

    [McpServerPrompt(Name = "implement-borm-form", Title = "Implement a form")]
    [Description("Implements a form or a form feature with Bit.Borm - a schema from a model, the fluent builder or JSON Schema, validation, conditional fields, cascading options, computed values, wizards, repeaters, custom renderers, theming - using its real API rather than a guessed one.")]
    public static string ImplementBormForm(
        [Description("What the form or feature should do, in your own words - e.g. 'a sign-up form generated from a C# model' or 'a field that only shows when a checkbox is ticked'.")] string feature)
    {
        return $"""
            Implement this with Bit.Borm: {feature}

            Work in this order:

            1. Call `SearchBorm` with the request above. It searches the guide, every public member, the setup guides,
               the theme and the demo's working forms at once, and every hit tells you the exact follow-up call to make.
            2. Read the best hit in full with the call it hands you, before writing any code. When a demo page does what
               you need, fetch it with `GetBormSourceFile` and follow its shape.
            3. Call `GetBormApi` for every Borm type you are about to use - `BormFieldBuilder` for what a field can be
               told, `BormView` for the component's parameters, `BormState` for the runtime state - and match the member
               names, signatures and defaults exactly. There is one namespace, `Bit.Borm`, and every public type is
               prefixed `Borm`.
            4. Choose the source of the schema deliberately: `BormSchemaGenerator.Generate<T>()` when a model and its
               DataAnnotations already say it, `BormBuilder.For<T>()` to refine it with refactor-safe expressions, and
               `BormJsonSchemaImporter.TryImport` (never `Import`) for a document that comes from outside the code.
               Build a `BormDefinition` once and keep it; `Clone()` it before tailoring a shared one.
            5. Prefer the library's own mechanism over hand-rolling one: `VisibleWhen`/`RequiredWhen`/`ReadOnlyWhen` with
               `ClearOnHide` for conditional fields, `OptionsFrom(..., dependsOn: ...)` for options that reload, `Computed`
               for derived values, `OnChange` for "when A changes, write B", `RevalidateOn`/`MatchesField` for rules that read
               another field, `Step` for wizards, `Array`/`ArrayOf` for repeaters, `Normalize`/`Trim` for tidying a value,
               `RegisterCustom` for a control of your own, and the `--bit-borm-*` properties `GetBormTheme` lists for
               restyling. Paths resolve against the object that owns the field, so inside a repeater a condition means that row.
            6. Render it with `<BormView Definition="..." Model="..." OnValidSubmit="..." />` (or `Data` for a dictionary,
               or `State` for a `BormState` you build and dispose yourself), and give two forms on one page their own `IdPrefix`.
            7. Call `Validate()` on the definition in a test - or run a JSON Schema through `InspectBormSchema` - to catch
               conditions that point nowhere, steps naming missing fields and settings a control cannot honour.
            8. Write the code, then say which tool call each non-obvious choice came from.
            """;
    }

    [McpServerPrompt(Name = "debug-borm-form", Title = "Debug a form")]
    [Description("Diagnoses a Bit.Borm problem - a form that throws when it renders, renders unstyled, ignores input, refuses to submit, shows a field it should hide or hides one it should show, keeps a stale error, does not reload its options, or imports a JSON Schema wrongly.")]
    public static string DebugBormForm(
        [Description("What goes wrong, with the page, the fields or the schema involved if you know them.")] string symptom)
    {
        return $"""
            Diagnose this Bit.Borm problem: {symptom}

            Work in this order:

            1. Check the setup against `GetBormSetupGuide` for this hosting model first - the three setup failures:
               `AddBorm()` missing from a container that renders the form (it throws about IBormFieldRendererRegistry - in a
               Blazor Web App check both Program.cs files), the stylesheet `_content/Bit.Borm/bit-borm.css` not linked from
               the host page (the form renders unstyled), and a page without an interactive render mode (it ignores input).
            2. Call `Validate()` on the definition and read every diagnostic: duplicate names, arrays with no item template,
               steps naming missing fields, conditions or dependencies pointing nowhere, a comparison rule that never
               revalidates. For a JSON Schema, pass it to `InspectBormSchema`: it imports it with the real importer and also
               reports what the importer drops silently - unreadable conditions, unknown x- extensions, dangling $refs and
               patterns .NET cannot compile.
            3. Call `SearchBorm` with the symptom and read the guide section it points to - "Validation", "When validation
               runs", "Rules that read another field", "Conditional fields and wizards", "Async and cascading options" and
               "Ids on the page" cover the usual causes.
            4. Remember how paths resolve: a condition, a computed dependency, `dependsOn` and `RevalidateOn` are read
               against the object that owns the field before the root, so inside a repeater they mean that row. A hidden
               field is not validated, and a wizard step only validates its own fields.
            5. If a page's own markup does not update, check how the change happened: fields follow the state by themselves,
               but markup that reads `BormState` needs `StateChanged` subscribed (and unsubscribed on dispose).
            6. Confirm the API you are relying on with `GetBormApi` before concluding it is a bug - check the actual default
               of the member involved (for example `BormState.ValidationTrigger`, `BormView.FocusFirstError`,
               `BormFieldDefinition.UpdateOn`).

            Tell me the cause and the fix, and cite the tool call that established it.
            """;
    }
}

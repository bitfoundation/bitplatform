# Bit.Borm

A schema-driven form engine for Blazor. Describe a form once - from a C# type, with a fluent builder or as a JSON
Schema document - and Borm renders it, validates it and keeps its state: conditional visibility, requiredness and
read-only fields, computed values, cascading options, multi-step wizards and array/repeater fields included. Every
decision is made in C#; the renderers are plain, accessible HTML with a pluggable renderer registry, and there is no
JavaScript at all.

```razor
<BormView Definition="_definition" Model="_model" OnValidSubmit="Save" />

@code {
    private readonly Contact _model = new();

    private readonly BormDefinition _definition = BormBuilder.For<Contact>()
        .Title("Contact us")
        .Field(x => x.Name, f => f.Required().Placeholder("Your name"))
        .Field(x => x.Email, f => f.AsEmail().Required().Trim())
        .Field(x => x.Company, f => f.VisibleWhen(nameof(Contact.IsBusiness), BormConditionOperator.IsTrue))
        .Build();

    private Task Save(BormState state) => Task.CompletedTask; // _model is populated and valid here
}
```

## What it is for

Sign-up, checkout and settings pages, admin and back-office CRUD screens, surveys and questionnaires, onboarding
wizards, configuration editors driven by a JSON Schema the server owns, and any other screen where the shape of a
form is better described as data than written out by hand field by field.

The schema is plain C#: a `BormDefinition` holding `BormFieldDefinition`s, which can be generated, built, imported,
checked, cloned, tailored and exported with no UI at all. The runtime state, `BormState`, is just as independent of
the page: it reads and writes your model (or a dictionary), runs the rules, evaluates the conditions and walks the
wizard, which is what makes a form testable without a browser.

## Highlights

- **Three ways in**: generate a schema from a model's properties and DataAnnotations, build one with a strongly
  typed fluent API (`x => x.Address.City`, refactor-safe), or import a JSON Schema document (draft-07 to 2020-12,
  with `$ref`, `allOf`, `if`/`then`/`else`, `dependentRequired`, `dependentSchemas` and discriminated
  `oneOf`/`anyOf` subforms) - and export any form back out again.
- **Rich field types**: text, search, password, email, URL, number, select, combobox, multi-select, radio,
  checkbox and switch, date, time, date-time, month and week, range, colour, file upload, tags, formatted text
  (Markdown with a safe preview), a signature pad with a typed alternative, static headings, nested objects and
  repeating arrays - and input masks (`(000) 000-0000`) on any text field.
- **Uploads that start when a file is chosen**: progress, cancel and retry beside each file, the files judged before
  they are sent, what the server returned stored in their place, and a submit made meanwhile that waits for them.
- **Validation**: required, length, range (numbers, dates and times), pattern, email, URL, UUID, compare,
  multiple-of, unique items, collection size, file size and type, custom sync and async rules, form-level rules,
  conditional rules, DataAnnotations' `IValidatableObject`, and FluentValidation through
  `Bit.Borm.FluentValidation`.
- **When it runs**: on submit, on blur or on change, separately before and after the first submit; dependent
  fields revalidate when the field they read changes; async rules are debounced, superseded and cancelled.
- **Conditional behaviour**: show, hide, disable, lock (read-only) or require fields and wizard steps based on
  other values - per row inside a repeater - and clear the values of branches the user abandoned.
- **Data that follows the form**: computed values, async and cascading options, lookups that search a service as
  the user types, change handlers, value normalisation (`Trim()`, `Normalize(...)`), dirty and touched tracking,
  snapshots and resets.
- **Expressions**: totals, conditions and rules written as text (`{Quantity} * {UnitPrice}`, `{Total} >= 10000`) -
  exported to JSON Schema and back, so a form a server sends can compute and judge with no code behind it, and
  evaluated by a bounded interpreter that can read the answers and nothing else.
- **Steps**: a wizard with per-step validation, conditional steps and a clickable stepper - or the same steps as
  tabs, an accordion or titled sections - and a review that reads every answer back in words.
- **Long forms**: undo and redo of every change, typing coalesced into one step; a repeater of hundreds of rows
  shown a page at a time, with rows needing attention on other pages pointed out and focus taken to them.
- **No interactivity needed**: a named form posts like any HTML form from a page rendered on the server alone, judged
  there exactly as an interactive one would be.
- **A designer**: `BormDesigner` builds a form by clicking - its fields, their rules and conditions, a live preview
  and the JSON Schema it comes to - out of the same builder an application uses.
- **Unfinished work kept**: versioned drafts without the sensitive answers, autosave, and a guard that asks before
  the user leaves unsaved changes; a submit that says how it ended (invalid, rejected, failed, cancelled or
  succeeded) and can be cancelled, with a server's `ProblemDetails` errors placed on their fields.
- **Accessible by default**: real labels, `aria-required` / `aria-invalid` / `aria-describedby`, grouped radio and
  multi-select controls, focus management after a failed submit, live announcements and an error summary that
  links to each field.
- **Yours to restyle and extend**: every colour is a `--bit-borm-*` custom property with light and dark themes,
  every built-in string goes through `IBormMessageProvider` - and comes in twelve languages besides English with
  `AddBormTranslations()` - the schema's own text through `IBormSchemaLocalizer` or the translations it carries, any control can be replaced through the renderer registry, and Blazor's own
  `InputBase` controls and `ValidationSummary` work inside a form through the `EditContext` it can provide.

## Quick start

```bash
dotnet add package Bit.Borm
```

```razor
@* _Imports.razor *@
@using Bit.Borm
```

```csharp
// Program.cs
builder.Services.AddBorm();
```

```html
<!-- App.razor or index.html -->
<link rel="stylesheet" href="_content/Bit.Borm/bit-borm.css" />
```

Then render a `BormView` with a `Definition` and a `Model`, as above. The full walkthrough is in
[getting started](#getting-started).

## Requirements

- .NET 10 or .NET 11
- Blazor WebAssembly, Blazor Server, Blazor Web App or Blazor Hybrid
- `FluentValidation` 12 only when the optional `Bit.Borm.FluentValidation` package is used

## The demo

The demo site, [borm.bitplatform.dev](https://borm.bitplatform.dev), opens on an overview with a live form, and its
documentation - starting at `/getting-started` - is a set of working forms, one per topic of this guide:
`/quickstart` (a schema from a C# type), `/builder` (the fluent builder), `/conditions` (every kind of condition),
`/json-schema` (a live JSON Schema editor with round-trip export and the schema's checks), `/designer` (the visual
designer), `/field-types` (every built-in control), `/layout`, `/field-polish`, `/combobox-tags`, `/search-lookup`,
`/files`, `/rich-fields` (masks, Markdown, signatures and uploads), `/composition`, `/custom-renderer`,
`/blazor-inputs` (Blazor's own inputs through the provided `EditContext`), `/theming` (the tokens, colour modes and
accessibility), `/localization`, `/static-form` (a form with no interactivity), `/array` (repeaters and computed
totals), `/per-row-conditions`, `/wizard`, `/tabs-review` (tabs, accordion, sections and a review step),
`/undo-long-lists`, `/validation`, `/dependent-fields`, `/settled-answers`, `/data-annotations`, `/fluent-validation`,
`/cascading`, `/state-api`, `/saving` (how a submit ends, and a server's errors), `/expressions` (with a console that
evaluates any expression), `/drafts` (autosave and the unsaved-changes guard), `/api-reference` (`BormView` and
`BormState` at a glance) and `/mcp-server`. Every page can show its own source, and Ctrl+K (or `/`) searches them
all. The site re-tints Borm's `--bit-borm-*` tokens to its own light and dark theme, which is what
[Theming](#theming) describes.

To run it locally:

```bash
dotnet run --project src/Utilities/Borm/Demo/Server
```

[`Samples`](Samples/) is the hosting harness rather than the docs: one shared [Core](Samples/Core/) project of pages
(a form generated from a C# type, the fluent builder with conditions, an async rule and a custom renderer, a wizard
with a conditional step and a review, a JSON Schema form with a repeater and computed totals, and a form that posts
with no interactivity), hosted unchanged under [Server](Samples/Server/), [WASM](Samples/Wasm/) and
[Auto](Samples/Auto/) so the same forms run, prerendered first, in each render mode - and under [MAUI](Samples/Maui/)
(Android, iOS, Mac Catalyst and Windows), where a `BlazorWebView` runs them as a Blazor Hybrid app - all but the form
that posts, which has no server to post to there. Each host is also a
minimal example of the wiring its platform needs: `AddBorm()` in every container that renders the pages - called
through `AddCoreServices()` in each host's `Program.cs` (`MauiProgram.cs` in the MAUI app) - and the
`_content/Bit.Borm/bit-borm.css` link in its `App.razor` (`wwwroot/index.html` in the MAUI app).

## How it fits together

```text
BormSchemaGenerator     a C# type and its DataAnnotations  \
BormBuilder<T>          a fluent, expression-based builder   >--  BormDefinition   the schema: fields, rules, steps
BormJsonSchemaImporter  a JSON Schema document             /          |
                                                                        |
BormState               values (through IBormDataAccessor), validation, conditions, computed values,
                        options, dirty/touched tracking, the wizard's position
                                                                        |
BormView / BormField    render it through IBormFieldRendererRegistry: BormTextInput, BormSelectInput, ...
                        or your own components
```

Two packages ship it:

| Package | Contents |
| --- | --- |
| `Bit.Borm` | Everything: the schema model, the builders, the generator, JSON Schema import and export, the state, the rules, the components and the stylesheet. No dependencies beyond Blazor itself. |
| `Bit.Borm.FluentValidation` | `UseFluentValidation()`, which runs a model's FluentValidation validator alongside the schema's own rules. |

All public types live in the single `Bit.Borm` namespace and are prefixed with `Borm`, so one `@using Bit.Borm`
brings everything into scope.

## MCP server

The demo hosts a [Model Context Protocol](https://modelcontextprotocol.io) server at
`https://borm.bitplatform.dev/mcp`, so an AI agent writing form code reads the library itself - its API, this guide
and the demo's working forms - instead of guessing at an API it half remembers.

```json
{
  "mcpServers": {
    "borm": { "type": "http", "url": "https://borm.bitplatform.dev/mcp" }
  }
}
```

Start with `SearchBorm`, which searches the guide, every public type and member, the setup guides, the stylesheet's
theme properties and classes, and the demo's pages at once, and points each hit at the exact follow-up call.
`GetBormSetupGuide` gives the wiring for one hosting model, `GetBormGuideSection` one section of this guide,
`GetBormApi` one public type with its members, real defaults and documentation, `GetBormTheme` the `--bit-borm-*`
custom properties with their light and dark values, and `GetBormSourceFile` one of the demo's working pages; each
returns its index when called without a key. `InspectBormSchema` runs a JSON Schema document through the library's
own importer and schema checks and reports the form it becomes - every field with its type, rules and conditions,
the diagnostics, and the document Borm exports back - or, called with no schema, returns a sample one. Every tool is
read-only.

## Documentation

- [Getting started](#getting-started)
- [Generating a schema from a C# type](#generating-a-schema-from-a-c-type)
- [The fluent builder](#the-fluent-builder)
- [JSON Schema](#json-schema)
- [Validation](#validation)
- [Conditional fields and wizards](#conditional-fields-and-wizards)
- [Async and cascading options](#async-and-cascading-options)
- [Computed values](#computed-values)
- [Expressions](#expressions)
- [Combobox and tags](#combobox-and-tags)
- [Change handlers](#change-handlers)
- [Drafts and autosave](#drafts-and-autosave)
- [Layout](#layout)
- [Field presentation](#field-presentation)
- [Checking a schema](#checking-a-schema)
- [Custom renderers](#custom-renderers)
- [The BormView component](#the-bormview-component)
- [BormState at a glance](#bormstate-at-a-glance)
- [Theming](#theming)
- [Accessibility](#accessibility)
- [Localization](#localization)
- [Hosting models](#hosting-models)
- [Key types](#key-types)

## Getting started

### Install

```bash
dotnet add package Bit.Borm
```

Add the namespace to `_Imports.razor`:

```razor
@using Bit.Borm
```

Register the rendering services - the field renderer registry - in every container that renders a form. In a Blazor
Web App that is both the server and the WebAssembly client, since every page is prerendered on the server first:

```csharp
builder.Services.AddBorm();
```

Link the stylesheet from your host page (`App.razor` or `index.html`); it is not injected for you:

```html
<link rel="stylesheet" href="_content/Bit.Borm/bit-borm.css" />
```

Without it the form still works, as plain HTML: the stylesheet only adds the theme, the grid and the error colours.

### A first form

Annotate a plain model and let the schema generator do the work:

```csharp
using System.ComponentModel.DataAnnotations;

public class Contact
{
    [Required, MaxLength(80)]
    public string Name { get; set; } = "";

    [Required, EmailAddress]
    public string Email { get; set; } = "";

    [Range(18, 120)]
    public int Age { get; set; }
}
```

```razor
<BormView Definition="_definition"
          Model="_model"
          OnValidSubmit="HandleValidSubmit" />

@code {
    private readonly BormDefinition _definition = BormSchemaGenerator.Generate<Contact>();
    private readonly Contact _model = new();

    private Task HandleValidSubmit(BormState state)
    {
        // _model is populated and valid here
        return Task.CompletedTask;
    }
}
```

`BormView` builds a `BormState` for the definition and the model, renders every field, and calls
`OnValidSubmit` once the submit passes validation. Supply a `State` instead when the page wants to hold the state
itself - to wire up a validator, read its dirty fields or drive it from code.

### Ids on the page

Every DOM id the form renders is derived from a field's path and starts with `borm_`. Two forms on one page that
share field names would share ids - and a label or an error-summary link in one would point into the other - so
give each its own prefix with `IdPrefix` on `BormView` (or `state.IdPrefix`).

## Generating a schema from a C# type

`BormSchemaGenerator.Generate<T>()` reads a model's public properties and their DataAnnotations. Attributes keep the
meaning DataAnnotations gives them, so the form never accepts what the model's own validation would then reject:

- `[RegularExpression]` must match the **whole** value (the pattern is anchored as `^(?:…)$`).
- `[Range]` honours `MinimumIsExclusive` / `MaximumIsExclusive`, and text bounds are read in the current culture
  unless `ParseLimitsInInvariantCulture` is set.
- `[MinLength]`, `[MaxLength]` and `[Length]` count **items** on any collection - a repeater, a multi-select over
  `List<TEnum>`, a list of files - and characters on text.
- `[Url]` accepts `http`, `https` and `ftp`, as `UrlAttribute` does.
- `[FileExtensions]` on a `string` checks the file name it holds; on an `IBrowserFile` it becomes the picker's
  `accept` list.
- `[Display(GroupName = …)]` puts consecutive fields in one `<fieldset>`; `[Display(Name, Description, Prompt,
  Order)]` become the label, the help text, the placeholder and the order.

Only what a form can actually edit is generated. A value that is replaced when it is edited - a scalar, a struct, an
array - needs a **public** setter (a `private`/`internal` one is rendered read-only, and the accessor refuses writes
through it). A class-typed group or list is edited in place, so `Shipping { get; private set; } = new()` and
`Lines { get; private set; } = new()` stay fully editable. `byte[]` / `Stream` properties are left out - a file
input produces an `IBrowserFile`, so declare one of those and read it in your submit handler - and the typed builder
refuses one unless you give it a `CustomRenderer`. Nested `struct`s are editable like classes.

## The fluent builder

```csharp
var definition = BormBuilder.For<Contact>()
    .Title("Contact us")
    .Field(x => x.Name, f => f.Required().Placeholder("Your name"))
    .Field(x => x.Email, f => f.AsEmail().Required())
    .Field(x => x.Age, f => f.Range(18, 120))
    .Build();
```

Constraints replace, rather than stack on top of, the rules generated from DataAnnotations, so restating
`[Required]` in the builder to give it a better message produces **one** error, not two.

Nested members, object groups and arrays are all expression-based:

```csharp
BormBuilder.For<Order>()
    .Field(x => x.Customer.Email, f => f.Required())          // binds to "Customer.Email"
    .Object(x => x.ShippingAddress, a => a
        .Field(p => p.Street)
        .Field(p => p.City))
    .Array(x => x.Lines, i => i
        .Field(l => l.Product, f => f.Required())
        .Field(l => l.Quantity, f => f.Range(1, 999)))
    .Build();
```

The untyped builder is handy when you have no compiled model; the form then binds to a dictionary:

```csharp
var definition = BormBuilder.Create()
    .Title("Feedback")
    .Text("subject", f => f.Required())
    .Field("body", BormFieldType.TextArea, f => f.AsTextArea(rows: 6))
    .Select("topic", f => f.Options(("bug", "Bug"), ("idea", "Idea")))
    .ArrayOf("tags", BormFieldType.Text, f => f.Items(max: 5).UniqueItems())
    .Build();
```

```razor
<BormView Definition="definition" Data="_data" OnValidSubmit="Send" />

@code {
    private readonly Dictionary<string, object?> _data = new();
}
```

## JSON Schema

```csharp
var definition = BormJsonSchemaImporter.Import(jsonSchemaString);
```

The importer covers draft-07 through 2020-12: `type` (including nullable unions such as `["string","null"]`),
boolean subschemas (`true` accepts anything; a `false` property is left out), `properties`, `required`, `enum`,
`const`, `format`, `default`, `examples`, `readOnly`, `minimum`/`maximum` and their exclusive forms (draft-04's
boolean `exclusiveMinimum` too, as OpenAPI 3.0 writes it), `multipleOf`, `minLength`/`maxLength`, `pattern`,
`items`, `minItems`/`maxItems`, `uniqueItems`, `$ref` into `$defs`/`definitions`, `allOf` (merged),
`if`/`then`/`else` and `dependentRequired`/`dependencies` - the last two mapped onto conditional requiredness.

A few of those deserve a sentence each:

- **`$ref`** keeps the keywords written beside it, and they win over the definition's own - that is how
  `{"$ref":"#/$defs/Address","title":"Billing address"}` labels a shared shape. A *recursive* reference (a category
  with sub-categories) describes an unbounded tree, which no form can render, so it is followed one level - a node
  and its children - and the property that would repeat it again is left out; a repeater whose rows refer back that
  far gets a text box per row instead. Recursion spelled through `allOf` (pydantic v1 and Swashbuckle wrap described
  references that way) is recognised the same way, and a definition that both extends and contains another is not
  mistaken for recursion.
- **`allOf`** means every branch applies. Annotations (`title`, `description`, `x-` hints) cannot all apply, so the
  later one wins and the outer schema has the last word; constraints combine - `required` and `dependentRequired`
  are unioned, a property (or an array's `items`) described by two branches keeps both descriptions, the stricter of
  two bounds wins, two `pattern`s are both enforced, two `enum`s keep only the values both allow, and each branch's
  `if`/`then`/`else` is applied on its own. `type`, `format`, `const` and `multipleOf` take the later branch's word.
- **`if`/`then`/`else`** becomes a condition only when the `if` pins properties to a `const` or an `enum`
  (optionally with a `required` list, read as "once this is filled in"). An `if` with anything else in it is ignored
  as a whole rather than half-applied.
- **Exclusive bounds** stay exclusive: `"exclusiveMinimum": 0` is "greater than 0", not "at least 0.000000001", and
  it exports as `exclusiveMinimum` again.
- **Typed choices** keep their type: an `enum` of integers, numbers or booleans gives the field a `ValueType`, so
  choosing "2" stores the number 2 and the export says `"type":"integer"`.
- **`format`** adds the matching rule as well as the input type - `email`, `uri` and `uuid` (which is enforced
  alongside any `pattern` the document also gives).

An array with no single item schema - no `items` at all, or the tuple form - still renders: each row is a text box.

An array of a closed set of choices that may not repeat (`{"type":"array","uniqueItems":true,"items":{"enum":[…]}}`)
imports as a multi-select rather than a repeater of dropdowns - the same rule react-jsonschema-form uses. Without
`uniqueItems` the document is explicitly allowing the same value twice, which only a repeater can express, so it
stays one. A multi-select exports in that same standard shape - its choices on `items`, where every JSON Schema
validator looks for them - and the older top-level `enum` spelling still imports.

`anyOf`/`oneOf` is handled for the two shapes that describe a single control: a null union
(`{"anyOf":[{"type":"string"},{"type":"null"}]}`) collapses to the branch it wraps, and a list of `const` branches
with titles (with or without a `{"type":"null"}` branch, and with or without `$ref`s) becomes a labelled select.

A union of *object* shapes describes a choice of subforms, and becomes one when the data says which branch an
answer is - when a property tells them apart. That property is OpenAPI's `discriminator.propertyName` when the
schema names one (its values from each branch's `const`, the `mapping`, or the definition's name, which is what
Swashbuckle and NSwag write for an `allOf` hierarchy), and otherwise the first property every branch pins to a
`const` of its own. It becomes a required choice labelled by the branches' titles; every other property a branch
adds shows only while that branch is chosen, is cleared when it stops being, and is required while its branch
requires it; properties every branch shares always show. A union with nothing in the data to tell its branches
apart is still left alone rather than bound to a branch the document never committed to.

`dependentSchemas` (and draft-07's schema-valued `dependencies`) adds fields that show once the property they hang
off is answered, and makes properties already there required then. The react-jsonschema-form idiom - a dependency
whose `oneOf` branches each pin the answer to one of its values - asks a different set of questions per answer.
The export writes the form these become (a choice and conditional fields), not the original union.

### Untrusted documents

Use `TryImport` when the schema comes from a user. Besides malformed JSON, it reports a document that is not a
schema, and one that goes past a budget no real form comes near: 10,000 fields, objects and arrays nested 32 levels
deep, `allOf` nested 64 levels deep, 80,000 `allOf`/`$ref` merges, or 250,000 choices and suggestions (10,000,000
characters of them) read across its fields. A property name with a `.`, `[` or `]` in it is refused too, since a field
name is one step of a path. A few hundred bytes of `$ref`s can describe millions of fields, and those documents are
refused rather than built:

```csharp
if (BormJsonSchemaImporter.TryImport(json, out var definition, out var error))
    Render(definition);
else
    ShowError(error);
```

### Extensions and export

UI intent that JSON Schema has no vocabulary for travels in `x-` extensions, and all of it round-trips through
`BormJsonSchemaExporter.Export(definition)`:

| Extension | Meaning |
| --- | --- |
| `x-widget` | Force a control: `select`, `textarea`, `radio`, `combobox`, `multiselect`, `tags`, `range`, `switch`, `file`, `static`, `hidden`, `array`, `month`, `week`, `markdown`, `signature`, … A document that names its control is taken at its word; one that does not gets the friendliest default for the shape it describes. JSON Schema has no format for a month or a week, so their values travel as the pickers' text (`2024-05`, `2024-W19`). |
| `x-allowCustom`, `x-enum` | A combobox that also accepts answers on no list. Its choices travel as `x-enum`, because `enum` means "one of these and nothing else" to every validator; a combobox with a plain `enum` is a closed choice and rejects anything else. |
| `x-renderer` | A custom renderer key registered with `RegisterCustom`. |
| `x-order`, `x-group`, `x-placeholder`, `x-autocomplete`, `x-inputMode`, `x-autofocus` | Field metadata. |
| `x-columns`, `x-colSpan` | Grid layout. |
| `x-prefix`, `x-suffix`, `x-showLabel`, `x-characterCount` | Presentation. |
| `x-updateOn`, `x-debounce` | When the value is written back. |
| `x-step` | The control's granularity, as distinct from a `multipleOf` constraint. |
| `x-accept`, `x-multiple`, `x-maxFileSize` | File upload constraints. |
| `x-visibleWhen`, `x-disabledWhen`, `x-readOnlyWhen`, `x-requiredWhen` | Conditions, as an [expression](#expressions) string (`"{total} > 100 and {country} = 'NL'"`), as `{"field":…,"op":…,"value":…}`, or as `{"all":[…]}` / `{"any":[…]}` of either. `op` is an operator name (any case) and defaults to `Equals` when absent; a condition whose `op` names no operator, or a group with a member that cannot be read, is dropped as a whole rather than guessed at. An expression that does not parse fails the import instead, naming the field and the keyword. |
| `x-computed` | An [expression](#expressions) the field's value is derived from - `"{quantity} * {unitPrice}"`. Its dependencies are read from it, and the field is read-only unless the document says `"readOnly": false`. |
| `x-rules` | [Expressions](#expressions) the value must satisfy: `[{"expression": "len({code}) = 6", "message": "…", "severity": "warning"}]`, or a bare string. On the root, a rule may name the `field` its message belongs on. A rule with no message uses the provider's `BormMessageKeys.Rule` text. |
| `x-revalidateOn` | Paths whose change revalidates this field - the other half of a cross-field rule. |
| `x-clearOnHide` | Clear the value when the field is hidden. |
| `x-sensitive` | The value must not outlive the page: left out of [drafts](#drafts-and-autosave). Passwords are sensitive without it. |
| `x-pageSize` | How many rows of a [long list](#long-lists) are shown at once. |
| `x-mask` / `x-maskStoresRaw` | The [shape](#values-in-a-fixed-shape) the value is written in, `(000) 000-0000`, and whether the model holds only what filled its slots. |
| `x-version` | On the root: which version of the form this is, recorded in every draft so one saved against an older version can be recognised and migrated. |
| `x-i18n` | The key a field's texts are translated under (see [the schema's own text](#the-schemas-own-text)). |
| `x-translations` | On the root: the form's texts per culture, `{"de": {"form.title": "…", "name.label": "…"}}`. |
| `x-attributes`, `x-inputAttributes` | Renderer hints and extra HTML attributes. |
| `x-steps` | Wizard steps, each with `id`, `title`, `fields` and an optional `visibleWhen`. |
| `x-stepLayout` | How the steps are shown: `wizard` (the default), `tabs`, `accordion` or `sections`. |
| `enumNames`, `x-enumGroups`, `x-enumDisabled` | Labels matching the `enum` values, the `<optgroup>` each belongs to, and the values that may not be chosen. |
| `examples` | `<datalist>` suggestions. A tag list's go on `items` with its per-tag `minLength`, `maxLength` and `pattern`. |

Conditions, computed values and rules written as [expressions](#expressions) export as the strings they were
written as. Those backed by code (`VisibleWhen(predicate)`, `Must(predicate)`, `MustAsync`, `Computed(delegate)`,
`OnChange`, `Normalize`, a delegate stashed in `Attributes`) have no JSON form and are omitted from the export
rather than approximated.

## Designing a form visually

`BormDesigner` builds a form without code - for the people who own a form's questions but not the application:

```razor
<BormDesigner @bind-Value="_definition" />

@code {
    private BormDefinition? _definition;   // an existing schema to edit, or null to start empty
}
```

A palette adds any kind of field; the list of fields chooses one, and moves, copies or deletes it; its properties -
kind, name, label, help, placeholder, required and read-only, lengths and limits, a pattern or a mask, options one per
line, a group and a column span, and [expressions](#expressions) for when it shows, when it is required and what it
is computed as - are a Borm form of their own, which says when an expression or a mask does not read or a name is
taken. The form's title, description and columns are settings of their own. A live preview renders the form as it
stands, and keeps its answers as fields change; the JSON Schema beneath it can be copied out, or replaced and loaded
back in. Every change is built through the same builder an application uses, and handed out through `ValueChanged`.

Read in, an existing schema keeps what the designer has no place for: an options provider, a condition or a rule
written in code, renderer hints, steps (a new field joins the last one), translations and form-level rules - and a
field holding such code says so. Objects, repeaters, hidden values and fields drawn by a renderer of their own are kept
as they are, kind and all - their label, help, conditions and the like can still be changed - and edited in JSON.

The list is a list of buttons first: each field is chosen, moved, copied and deleted with buttons named for it ("Move
Email up"), focus stays where the user is, and each change is announced ("Email is now number 2 of 3"). Dragging a
field onto another moves it too, for a mouse. Every word it shows goes through `IBormMessageProvider`.

## Validation

Built-in rules are added through the field builder:

```csharp
.Field(x => x.Username, f => f
    .Required("Username is required")
    .MinLength(3)
    .MaxLength(20)
    .Pattern("^[a-z0-9_]+$", "Lowercase letters, numbers and underscores only"))
```

Custom, async and cross-field rules:

```csharp
.Field(x => x.Password, f => f
    .AsPassword()
    .Must(value => value is string s && s.Length >= 8, "At least 8 characters"))

.Field(x => x.ConfirmPassword, f => f
    .AsPassword()
    .MatchesField(nameof(Model.Password), "Password"))

.Field(x => x.Username, f => f
    .MustAsync(async ctx => await users.IsAvailableAsync((string?)ctx.Value ?? ""), "That username is taken."))
```

Async rules are skipped while the user types and run on blur and on submit - and on the pause of a debounced field,
which is what debouncing is for:

```csharp
.Field(x => x.Username, f => f
    .UpdateOnInput(debounceMilliseconds: 400)   // one write per pause, not one per character
    .MustAsync(async ctx => await users.IsAvailableAsync((string?)ctx.Value ?? "", ctx.CancellationToken),
               "That username is taken."))
```

`state.IsValidatingField(path)` reports the lookup while it is in flight, so a spinner goes beside *that* box rather
than the whole form declaring itself busy. A run that is superseded - the user typed again, pressed Next twice,
reset the form - has its answer discarded, and `ctx.CancellationToken` is signalled so the lookup itself can stop.

A field's synchronous rules run first, and its asynchronous ones only when those found no error: a username that
does not match its pattern is not sent to the server to ask whether it is taken, which would cost a request and
answer with a second complaint about a value the user has to change anyway. Set `RunAsyncRulesOnInvalidFields` to
run them regardless. `GetFieldState(path).Status` says where a field stands - `Unvalidated`, `Pending` (a lookup in
flight), `Invalid` or `Valid` - so a "✓ available" can appear once the lookup has answered and not before; a value
that changes is `Unvalidated` again until it is judged.

### Rules that read another field

A cross-field rule lives on one of the two fields it compares, so only one of the two changes ever runs it. Get the
confirmation wrong, then fix the *password* to agree with what you typed, and the complaint under the confirmation
box is now false - and stays there until you go back and touch a field that was already right. `RevalidateOn`
closes that:

```csharp
.Field(x => x.End, f => f
    .RevalidateOn(nameof(Booking.Start))
    .Must(ctx => …, "End must be on or after start."))
```

`MatchesField(...)` and `[Compare]` register their own dependency, so a confirm-password field needs nothing extra.
Paths resolve against the object that owns the field before falling back to the root, exactly as conditions and
computed dependencies do, so a rule on a repeater's item template means *that row*; naming a container covers
everything inside it.

A dependent that has nothing to say is left alone - a field the user has never visited on a form nobody has
submitted does not start showing errors because a different field changed. The point is to correct a verdict already
on screen, never to bring one forward. React Hook Form spells this `deps`; TanStack Form spells it
`onChangeListenTo`. `Definition.Validate()` reports a comparison rule that never revalidates.

Rules can also be scoped to a condition, the way FluentValidation's `.When(…)` works:

```csharp
.Field(x => x.CompanyName, f => f
    .When(nameof(Model.IsBusiness), BormConditionOperator.IsTrue, null, w => w
        .Required("Business accounts need a company name.")
        .MinLength(3)))
```

Rules that need the whole model live on the form:

```csharp
BormBuilder.For<Booking>()
    .Field(x => x.Start)
    .Field(x => x.End)
    .MustAll(m => m.End >= m.Start, "End must be on or after start.", nameof(Booking.End))
    .Build();
```

### Tidying a value instead of complaining about it

A rule that rejects `" a@b.com "` for its surrounding spaces is technically correct and of no use to anyone: the
user cannot see the difference, and fixing the message inside the rule leaves the untidy value in the model anyway.
`Normalize` fixes the value:

```csharp
.Field(x => x.Email, f => f.AsEmail().Required().Trim())

.Field(x => x.Reference, f => f
    .Normalize(v => v is string s ? s.Trim().ToUpperInvariant() : v))
```

It runs when the user *leaves* the field, and once more over every field on submit - so a field nobody visited is
tidied too. Never per keystroke, which would eat the space between two words the moment it was typed. `Trim()` turns
a value that is nothing but whitespace into null, so a required field is not satisfied by `"   "`. React Hook Form
spells this `setValueAs`.

### Values in a fixed shape

`Mask` writes a value in a fixed shape - a phone number, an IBAN, a postcode:

```csharp
.Field(x => x.Phone, f => f.Mask("(000) 000-0000"))
.Field(x => x.Iban, f => f.Mask("AA00 0000 0000 0000 0000 00", storeRaw: true))
```

`0` is a digit, `A` a letter (written in capitals), `a` a letter as typed and `*` either; a backslash makes the next
character literal, and anything else is literal already. When the user leaves the box what they typed is put into
the shape: the literals they type stand for themselves and any other separator is passed over, so `5551234567`,
`555-123-4567` and `(555) 123-4567` are the same answer. A value that does not fit is left as typed and reported
with the shape to write it in (`Enter it in the form (###) ###-####.`, `BormMessageKeys.Mask`). `storeRaw: true`
stores only what filled the slots - `5551234567` - while the box and a review still show it formatted. A mask of
digits alone asks a phone for its number pad. `x-mask` and `x-maskStoresRaw` carry it through JSON Schema.

The box is not rewritten under the caret as the user types. That needs script to put the caret back, and a box that
changes what is being typed into it is one assistive technology describes badly - which is why GOV.UK's advice is
not to use input masks at all. Formatting the answer once it is given keeps the benefit and drops the cost.

### Date and time windows

`Range` takes dates and times as well as numbers. The bound is rendered as the control's own `min` / `max` - so the
picker will not offer a date the form would then refuse - *and* enforced by the rule, so a value typed past the
picker is reported rather than quietly accepted:

```csharp
.Field(x => x.Arrival, f => f.Range(DateTime.Today, DateTime.Today.AddMonths(6)))
.Field(x => x.CheckIn, f => f.Range(new TimeOnly(14, 0), new TimeOnly(22, 0)))
```

`[Range(typeof(DateTime), "2024-01-01", "2024-12-31")]` on a model means the same thing.

A `DateTimeOffset` is shown on its own clock, and what the user types is given back the offset the value already
had, so editing one never shifts it to another instant or another zone.

`AsMonth()` and `AsWeek()` put the browser's month and week pickers on a field - a card's expiry, a delivery
slot. A `DateOnly`, `DateTime` or `DateTimeOffset` holds the period's first day (the 1st of the month, the ISO
week's Monday); with no CLR type, or a `string` property, the value is the picker's own text, `2024-05` or
`2024-W19`. Firefox and Safari on the desktop have no such pickers and show a text box, so anything else typed into
one is reported as an entry the field cannot accept rather than stored. `Range` bounds work as they do for dates.

### Details worth knowing

- **`Required()` on a checkbox means "must be ticked"** - the same thing HTML's own `required` attribute means, and
  what "I accept the terms" always needs. On any other field type, `false` is a perfectly good value.
- **A value the model cannot accept is reported, not swallowed.** Typing `abc` into a field bound to an `int` leaves
  the model on its last valid value and raises a validation message naming the entry, rather than silently
  discarding it on the next render. A fraction offered to a whole-number field is refused rather than rounded.
- **`UniqueItems()` compares content.** Two rows are duplicates when they hold the same values - a dictionary row by
  its keys and everything beneath them, a model row by its public *scalar* properties (its navigation-style members
  are never read) - and numbers compare by value, so `1` and `1.0m` are the same item while `"1"` and `1` are not.

### When validation runs

```csharp
var state = new BormState(definition, accessor)
{
    ValidationTrigger   = BormValidationTrigger.OnBlur,   // before the first submit
    RevalidationTrigger = BormValidationTrigger.OnChange  // after it
};
```

Or let `BormView` build the state and configure it in place, which keeps the `Definition` + `Model` shorthand:

```razor
<BormView Definition="_definition" Model="_model"
          ConfigureState="s => s.ValidationTrigger = BormValidationTrigger.OnBlur" />
```

A field that already shows an error always revalidates eagerly, so the error clears as soon as it is fixed.

### How much is reported at once

By default a field shows every rule it currently breaks. Set `SingleErrorPerField` to show only the first - React
Hook Form's default, and the right call for a password with four constraints on it, where four complaints under one
box read as four problems rather than one field to go and fix:

```csharp
state.SingleErrorPerField = true;
```

Every rule still runs, so the submit decision is unchanged; this is about what the user reads. Warnings sit alongside
the error rather than competing with it.

### Editing an existing record

`Reset()` restores the values the form was *constructed* with, which after a save round-trip are the wrong ones.
`Reset(values)` writes new ones and makes them the baseline, so `IsFormDirty`, an undo button and an unsaved-changes
prompt all mean what they say again:

```csharp
state.Reset(new Dictionary<string, object?>
{
    ["Customer"] = stored.Customer,
    ["Lines[0].Description"] = stored.Lines[0].Description,
});
```

`Snapshot()` is the other half: it maps every path the schema binds to onto its value - one entry per repeater row
included - which is exactly what `Reset(values)` takes back, so the two together are a draft save and restore:

```csharp
var draft = state.Snapshot();     // keep it in memory
// … the user comes back …
state.Reset(draft);
```

A snapshot holds live objects; for a draft that has to outlive the page - written to storage as JSON, without
passwords, and restored into a newer version of the form - use [`BormDraft`](#drafts-and-autosave).

`ValidateFieldAsync(path)` checks one answer without judging the whole form, and `GetFieldState(path)` answers
everything about one field at once:

```csharp
await state.ValidateFieldAsync("Customer");

var field = state.GetFieldState("Customer");
// field.IsTouched, field.IsDirty, field.IsInvalid, field.Error, field.Messages
```

Errors the server finds after a submit go back onto the fields they belong to with `SetServerError(path, message)`,
or `SetServerErrors(...)`, which replaces the form's messages with a whole batch from one failed request. An ASP.NET
Core `ValidationProblemDetails` goes in as it arrives - keys are matched to fields without regard to case, and the
`$.` System.Text.Json puts in front of its own is dropped - with an optional map for an API whose names are not the
form's:

```csharp
private async Task Save(BormState state, CancellationToken cancellationToken)
{
    var response = await http.PostAsJsonAsync("api/orders", _model, cancellationToken);
    if (response.StatusCode == HttpStatusCode.BadRequest
        && await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(cancellationToken) is { } problem)
        state.SetServerErrors(problem.Errors);              // the submit now reports Rejected
    else
        response.EnsureSuccessStatusCode();                 // throwing reports Failed
}

// await state.SubmitAsync(Save);
```

### How a submit ends

`SubmitOutcome` says how the last submit ended, which is more than valid-or-not: `Invalid` (the form's own rules
refused it, and the handler never ran), `Rejected` (the handler recorded errors with `SetServerError` or
`SetServerErrors` - a server that said no), `Failed` (the handler threw; the exception is in `SubmitError` and is
rethrown), `Cancelled`, or `Succeeded`. `SubmitAsync` returns true only for the last of those, and
`IsSubmitSuccessful` is the same thing. An error the user makes while the handler runs - letters typed into a number
box during a slow save - is theirs to fix, and does not turn a save that went through into a rejected one.

The overload whose handlers take `(state, cancellationToken)` hands them a token that `CancelSubmit()` and disposing
the form signal; a handler wired through `BormView.OnValidSubmit` reads it from `state.SubmitCancellationToken`.
`ReadOnlyWhileSubmitting` holds every field still while the handler runs, so a slow save cannot leave the form
showing values other than the ones sent. Nothing is ever retried behind the application's back, and the values
are left exactly as they were, so "try again" is simply submitting again.

Every message also says where it came from - `Source` is `Rule`, `External`, `Server` or `Conversion` - and, for a
rule with a key, `Code` is that key (`required`, `maxLength`, …), stable across languages for a test or a custom
message component to key off. Neither takes part in equality: two messages saying the same thing about the same
field are one message.

### DataAnnotations and `IValidatableObject`

A schema generated from a model already carries its `[Required]`, `[StringLength]`, `[Range]` and friends as field
rules. What no attribute can express is a rule that reads more than one property - and in .NET that is
`IValidatableObject`:

```csharp
var state = new BormState(definition, new BormModelDataAccessor(model))
    .UseDataAnnotations();
```

Only the cross-property layer is added: running `Validator` over the properties again would put two
differently-worded copies of "required" under the same box. Pass `UseDataAnnotations(includePropertyAttributes:
true)` when the schema came from somewhere that never saw those attributes - a JSON Schema document rendered over a
typed model.

A result naming one member lands on that field, one naming several puts the same complaint under each of them, and
one naming none becomes a form-level message. `IValidatableObject.Validate` is called directly, exactly once per
pass, rather than through `Validator.TryValidateObject`, which skips it entirely once any property attribute has
failed - the user would otherwise fix the last required field, submit again, and be told about something new.

### FluentValidation

FluentValidation support lives in its own package, so a form that does not use it does not carry it:

```bash
dotnet add package Bit.Borm.FluentValidation
```

Wire up a validator on the form state. Failure property paths (e.g. `Address.City`, `Items[0].Product`) map directly
onto field paths, and failures landing on a field the schema is currently hiding are dropped - a validator sees the
whole model and knows nothing about the form's conditions, so without that a rule on an abandoned branch would refuse
the submit and point at a control that is not on the page.

```csharp
var state = new BormState(definition, new BormModelDataAccessor(model), serviceProvider)
    .UseFluentValidation(new ContactValidator());
```

```razor
<BormView State="state" OnValidSubmit="HandleValidSubmit" />
```

If your validators are registered in DI, call `state.UseFluentValidation()` (no argument) and the matching
`IValidator<TModel>` is resolved from the service provider; pass `throwIfMissing: true` to catch a forgotten
registration during development.

`UseFluentValidation()` and `UseDataAnnotations()` compose, so a model that carries both a validator and its own
`IValidatableObject` can use both - a complaint they report identically on the same field is still read once.
`BormExternalValidator.CombineWith(...)` does the same for a hand-written external validator.

## Conditional fields and wizards

Show, disable, lock or conditionally require fields based on other values:

```csharp
.Field(x => x.CompanyName, f => f
    .VisibleWhen(nameof(Model.IsBusiness), BormConditionOperator.IsTrue)
    .RequiredWhen(nameof(Model.IsBusiness), BormConditionOperator.IsTrue)
    .ClearOnHide())
```

`ReadOnlyWhen` is for an answer that is *settled* rather than one that does not apply - an invoice that has been
sent, a field the approver owns:

```csharp
.Field(x => x.Reference, f => f
    .ReadOnlyWhen(nameof(Model.Confirmed), BormConditionOperator.IsTrue))
```

Reach for it rather than `DisabledWhen` whenever the value still matters to the person reading the form. A disabled
control leaves the tab order and is not announced at all, so "the invoice has been sent, here is its reference"
becomes a value a keyboard or screen-reader user cannot get to; read-only keeps it reachable, readable and copyable.
On an object or a repeater, `ReadOnly` / `ReadOnlyWhen` makes everything inside it read-only the same way; a
read-only repeater also locks its add, remove and reorder buttons.

`ClearOnHide` empties the value as soon as the field disappears, so a branch the user abandoned never contributes
data to the submitted model.

### Per-row conditions

Inside a repeater, a condition means **that row**. Paths are resolved against the object that owns the field before
falling back to the root, exactly as a computed value's dependencies are - so one row can ask for an email while the
next asks for a phone number:

```csharp
.Array(x => x.Contacts, row => row
    .Field(c => c.Kind, f => f.AsRadio().Options(("email", "Email"), ("phone", "Phone")))
    .Field(c => c.Email, f => f
        .VisibleWhen("Kind", BormConditionOperator.Equals, "email")   // this row's Kind
        .RequiredWhen("Kind", BormConditionOperator.Equals, "email")
        .ClearOnHide()))
```

### Wizards

Split a form into steps:

```csharp
BormBuilder.For<Order>()
    .Step("details", s => s.Title("Details").Fields("Name", "Email"))
    .Step("shipping", s => s.Title("Shipping").Fields("Address", "City", "Zip"))
    .Build();
```

`BormView` renders a stepper, Back/Next navigation, and validates each step before advancing. Hidden steps are
skipped and the visible ones stay contiguously numbered.

Pressing Enter in a text box on a step that is not the last one advances, exactly as the Next button does - the
browser would otherwise submit the whole form and answer a user who is halfway through with a wall of errors about
questions they have not been asked yet.

Every step the user has already walked past is clickable - forwards as well as back, since each was validated on the
way through - so coming back to fix one answer does not mean pressing Next through the rest of the form. A step never
reached stays inert, because jumping to it would skip the validation that gates it. `AllowStepNavigation="false"`
turns the links off entirely, and `state.IsStepReachable(i)` / `state.FurthestStepIndex` expose the same information
to a custom stepper.

A hidden step is hidden in every sense: its fields are not rendered, not validated on submit, and never the reason a
form refuses to submit. If a condition hides the step the user is standing on, the wizard falls back to the nearest
visible one rather than stranding them. Changing step moves focus to the new step's content, so the change is not
invisible to a keyboard or screen-reader user.

### Tabs, accordion and sections

A long record the user moves around in freely - a settings page, a customer's details - is not a process to walk
through in order. The same steps can be shown as tabs, an accordion or titled sections:

```csharp
BormBuilder.For<Customer>()
    .StepLayout(BormStepLayout.Tabs)         // or Accordion, Sections; Wizard is the default
    .Step("contact", s => s.Title("Contact").Fields("Name", "Email", "Phone"))
    .Step("billing", s => s.Title("Billing").Fields("Vat", "Address"))
    .Step("tax", s => s.Title("Tax").Fields("TaxId").VisibleWhen("{Country} = 'NL'"))
    .Build();
```

- **Tabs** follow the WAI-ARIA tabs pattern: one tab stop for the list, the arrow keys, Home and End moving
  between tabs, and the panel labelled by its tab. Any tab can be opened without validating the one left.
- **Accordion** uses the browser's own `<details>` disclosure, the first section open; a closed section keeps its
  fields in the page, so nothing it holds is lost.
- **Sections** are titled regions one after another, all open.

In all three, Submit validates everything (a hidden tab or section never blocks it), and Enter in a text box
submits rather than moving on. A failed submit turns to the first tab with something to fix, or opens every section
that has something, so focus and the error summary's links can reach it; a tab or section holding errors the user
has been shown carries a count that is also read out (`BormMessageKeys.StepErrors`), never colour alone. Drafts
remember which tab was open. `x-stepLayout` carries the choice through JSON Schema, and `BormStepsView` renders the
layouts for a page that composes its own form.

### Review before submitting

`Review(name, heading)` adds a "check your answers" summary - put it on a wizard's last step:

```csharp
BormBuilder.For<Application>()
    .Step("you", s => s.Title("About you").Fields("Name", "Email"))
    .Step("plan", s => s.Title("Your plan").Fields("Plan", "Seats", "Start"))
    .Review("summary", "Check your answers")
    .Step("review", s => s.Title("Review").Fields("summary"))
    .Build();
```

Every answer of every other visible step is read back in words - an option's label rather than its stored value,
Yes or No, a number with its prefix and suffix, a date as the reader's culture writes it, a file by its name, a
repeater row by row - and a question nobody answered says so. A sensitive answer (a password, a field marked
`Sensitive()`) is never shown. Each step has a "Change" button back to it, named for the step for assistive
technology, which moves focus with the step as the stepper does. `<BormReview State="…" />` places the same
summary anywhere - a confirmation page, a printable record - and `x-attributes: {"review": true}` on a static field
carries it through JSON Schema.

## Async and cascading options

Options can be loaded on demand and refreshed when another field changes:

```csharp
.Field(x => x.City, f => f
    .Required()
    .OptionsFrom(
        load:      async ctx => await cities.ForCountryAsync(ctx.Value("Country") as string, ctx.CancellationToken),
        value:     c => c.Code,
        label:     c => c.Name,
        dependsOn: nameof(Model.Country)))
```

When `Country` changes the cached options are dropped and the now-invalid selection is cleared. Dependencies resolve
against the object that owns the field before falling back to the root - exactly as conditions and computed
dependencies do - so `dependsOn: "Country"` inside a repeater means *that row's* country, and naming a container
(`dependsOn: "Address"`) covers everything inside it. The provider reads the same way: inside a repeater,
`ctx.Value("Country")` is that row's country.

A lookup that goes over the network fails for ordinary reasons, so a provider that throws is recorded rather than
thrown: the control renders "options could not be loaded" instead of an empty dropdown, the failure is available as
`state.OptionsError(path)` and announced through `OptionsLoadFailed`, and `InvalidateOptions(path)` retries.

```csharp
state.OptionsLoadFailed += (path, ex) => logger.LogWarning(ex, "Options for {Path} failed", path);
```

Static choices come from `Options(...)`, and an enum's members from `OptionsFromEnum<TEnum>()` (a property typed as
an enum gets them without asking, labelled by `[Display(Name)]` where there is one).

## Computed values

A field can derive its value from the rest of the form instead of being typed in - a line total, a full name, a
price after discount:

```csharp
BormBuilder.For<Invoice>()
    .Field(x => x.Customer, f => f.Required())
    .Array(x => x.Lines, line => line
        .Field(l => l.Quantity)
        .Field(l => l.UnitPrice)
        // Evaluated against the *line*, so it works on every row without knowing its index.
        .Computed(l => l.LineTotal, l => l.Quantity * l.UnitPrice,
                  dependsOn: ["Quantity", "UnitPrice"]))
    .Computed(x => x.Total, m => m.Lines.Sum(l => l.Quantity * l.UnitPrice),
              dependsOn: [nameof(Invoice.Lines)])
    .Build();
```

Computed fields are read-only by default, seeded when the form is created, and refreshed whenever a declared
dependency changes - including changes inside an array. One computed field may depend on another; a set of formulas
that reference each other in a cycle settles rather than recursing.

Dependencies are named relative to the object that owns the field, so `"Quantity"` on a repeater row means that
row's quantity. The untyped builder takes a context instead: `f.Computed(ctx => ctx.Sibling("width"), "width")`,
or an [expression](#expressions): `f.Computed("{width} * {height}")`.

## Expressions

A condition, a computed value or a rule can be written as text instead of C#. Nothing changes about how it
behaves, but text travels: it is exported to JSON Schema and imported back, so a form a server sends as JSON can
compute, decide and judge with no code behind it.

```csharp
BormBuilder.For<Invoice>()
    .Array(x => x.Lines, line => line
        .Field(l => l.Quantity)
        .Field(l => l.UnitPrice)
        .Computed(l => l.LineTotal, "{Quantity} * {UnitPrice}"))         // this row's quantity and price
    .Field(x => x.Discount)
    .Computed(x => x.Total, "sum({Lines[].LineTotal}) - coalesce({Discount}, 0)")
    .Field(x => x.Approver, f => f.RequiredWhen("{Total} >= 10000"))
    .Field(x => x.DueDate, f => f.Must("{DueDate} > {IssueDate}", "The due date comes after the issue date."))
    .MustAll("count({Lines}) > 0", "Add at least one line.", nameof(Invoice.Lines))
    .Build();
```

```json
"total":    { "type": "number", "x-computed": "sum({lines[].lineTotal}) - coalesce({discount}, 0)" },
"approver": { "type": "string", "x-requiredWhen": "{total} >= 10000" },
"dueDate":  { "type": "string", "format": "date",
              "x-rules": [{ "expression": "{dueDate} > {issueDate}", "message": "The due date comes after the issue date." }] }
```

`VisibleWhen`, `DisabledWhen`, `ReadOnlyWhen`, `RequiredWhen`, `When` and a step's `VisibleWhen` each take an
expression; `Computed(expression)` makes the field read-only; `Must(expression, message, severity)` registers
`RevalidateOn` for every other field it reads, so correcting either side of a comparison clears the message; and
`MustAll(expression, message, field)` is a form-level rule. The fields an expression reads are its dependencies,
so it is re-evaluated exactly when one of them changes - there is no `dependsOn` to keep in step.

### The language

- **Fields** are written in braces: `{Email}`, `{Address.City}`, `{Lines[0].Product}`. A path resolves against the
  object that owns the field first, so on a repeater's row template `{Quantity}` is that row's. `[]` means every
  row: `{Lines[].Total}` is the list of every line's total, and `{Orders[].Lines[].Qty}` flattens a list of lists.
- **Values**: numbers (`12`, `0.5`, `1e3`), text in single or double quotes (a doubled quote or a backslash
  escapes one; any other backslash is kept, so a pattern reads as itself), `true`, `false`, `null`, and lists
  `['a', 'b']`.
- **Operators**: `+ - * / %`, `=` (or `==`), `!=` (or `<>`), `< <= > >=`, `in`, `not in`, `and` (`&&`), `or`
  (`||`), `not` (`!`). Keywords ignore case. Comparisons do not chain: `1 < {x} < 5` is refused; write
  `1 < {x} and {x} < 5`.
- **Numbers are decimals**, so `0.1 + 0.2` is `0.3` and money adds up. Dividing by zero has no answer (null).
- **Text** joins with `+`, and a missing part joins as nothing: `{First} + ' ' + {Last}`.
- **Dates** compare as points in time whether they are `DateOnly`, `DateTime`, `DateTimeOffset` or ISO text;
  a date plus a number moves it by that many days, and one date minus another is the days between them.
- **Comparisons** are the ones `BormConditionOperator` makes: numbers by value, text without regard to case.

| Kind | Functions |
| --- | --- |
| Logic | `iif(condition, then, else)` (evaluates only the branch it takes), `coalesce(a, b, …)`, `isEmpty(x)`, `isNotEmpty(x)` |
| Text | `len(x)` / `length(x)` (a list's item count too), `lower`, `upper`, `trim`, `concat(…)`, `substring(text, start, length?)`, `contains`, `startsWith`, `endsWith`, `matches(text, 'pattern')` |
| Numbers | `abs`, `round(x, digits?)` (halves away from zero), `floor`, `ceil`, `number(x)`, `text(x)` |
| Aggregates | `sum`, `avg`, `min`, `max`, `count` - each takes values, lists or both, and skips what has not been answered |
| Dates | `today()`, `now()`, `year`, `month`, `day`, `age(birthDate)`, `dateDiff(from, to, unit?)` (`'days'`, `'weeks'`, `'months'`, `'years'`, `'hours'`, `'minutes'`), `addDays`, `addMonths`, `addYears` |

### Missing answers

A field nobody has answered reads as `null`, and in a yes/no position null is no: `not {Subscribe}` holds for a
box nobody ticked, exactly as `BormConditionOperator.IsFalse` does. A comparison that needs a missing answer is
*unknown* instead, and unknown follows SQL's three-valued logic: while the age is missing, `{Age} >= 18 or
{Override}` is true once the override is ticked and unknown otherwise, and `{Age} >= 18 and {Override}` is false
while the override is unticked and unknown otherwise. A condition that is
unknown is false, so a field depending on a missing answer stays hidden. A **rule** that is unknown passes - a
rule comparing two dates has nothing to say until both are given, and saying a value is missing is `Required()`'s
job. `=` and `!=` treat null as a value, so `{Notes} = null` asks whether the notes are empty.

### Safety

An expression is not a program. It can read the form's values and call the functions listed above, and nothing
else: no member access, no reflection, no assignment, no loops, so evaluating one from an untrusted document always
finishes and can read nothing but the answers. Parsing is bounded - 4,096 characters, 48 levels of nesting, 1,024
parts - and `matches` only takes its pattern as a literal in the expression, never one read from a field, with the
same 250 ms time limit every pattern gets. Evaluating never throws for the values it meets: text multiplied by a
number, a date that does not parse or a division by zero is simply no answer. A document whose expression does not
parse fails `TryImport`, saying which field and keyword it was in, rather than having the condition quietly dropped
- which would show a field meant to be hidden, or make one meant to be required optional.

### Functions of your own

```csharp
BormExpressionFunctions.Register("vatRate", args => args[0] is "DE" ? 0.19m : 0.20m, minArgs: 1, maxArgs: 1);
// "x-computed": "round({net} * (1 + vatRate({country})), 2)"
```

Registered functions are the trust boundary: an expression from any document can call them, so they must be safe
with any arguments (they are given evaluated values, a missing answer as null) and should return null rather than
throw. Register them at startup; a built-in function cannot be replaced. `today()` and `now()` read
`BormExpressionFunctions.TimeProvider`, which a test can replace with a fixed clock; it is the local time of the
process evaluating the expression - on Blazor Server, the server's.

Expressions are synchronous and pure. A rule that calls a service, a lookup, or anything asynchronous stays a
delegate (`MustAsync`, `OptionsFrom`), and keeps working alongside them.

## Combobox and tags

A `<select>` of two hundred countries is navigated by scrolling. A combobox is typed into:

```csharp
.Field(x => x.Country, f => f
    .Options(("fr", "France"), ("gb", "United Kingdom") /* … */)
    .AsCombobox()                      // closed: an answer on no list is reported
    .Clearable())

.Field(x => x.City, f => f
    .AsCombobox(allowCustom: true)     // the list proposes; anything is accepted
    .OptionsFrom(load: …, dependsOn: nameof(Model.Country)))
```

It is built on the browser's own `<input list>` + `<datalist>`, so filtering, keyboard navigation and screen-reader
support are the platform's. The library supplies the piece a bare datalist cannot: the label the user reads is
mapped onto the value the model stores, so an option can be `("fr", "France")` rather than being forced to show its
own key. Static options, `OptionsFromEnum` and async/cascading `OptionsFrom` all work exactly as they do on a
`<select>`.

A combobox always commits when the choice is made, whatever `UpdateOn` says: the box holds a label and the model
holds the value that label stands for, and half a label stands for nothing.

`AsCombobox()` adds a rule reporting an answer that is on no list - it is *reported*, never silently discarded. The
rule can only check options the schema itself declares; choices that arrive from an `OptionsFrom` provider live in the
form's runtime state rather than the schema, so validate those on the server where the same lookup lives.

### Lookups that search as you type

A customer out of a hundred thousand fits no list. `SearchFrom` makes the combobox ask as the user types:

```csharp
.Field(x => x.CustomerId, f => f.SearchFrom(
    search: ctx => customers.SearchAsync(ctx.SearchText, ctx.CurrentValue as string, ctx.CancellationToken),
    value:  c => c.Id,
    label:  c => c.Name,
    minLength: 2,                 // nothing is asked before two characters
    debounceMilliseconds: 250))   // and only once the user pauses
```

- The search runs once `minLength` characters are typed and the user pauses; a search typed past is cancelled
  (`ctx.CancellationToken`) and its answer never lands over a newer one. A `minLength` of 0 searches with empty
  text before anything is typed - "the top results".
- A field that opens holding a value - an edit form - is asked once with no `SearchText` and the value in
  `ctx.CurrentValue`, so it can return the one option the box should show instead of a bare id. The option the
  user picks is remembered, so its label stays in the box after a later search no longer returns it.
- Text committed before its results arrived (typed quickly, tabbed away) is searched for exactly, and matched if it
  is a result. Text that names no result is **refused** - left in the box with a message while the model keeps its
  last value - because a search field stands for a record; `allowCustom: true` stores it as typed instead. A submit
  or a wizard's Next pressed while that search is out waits for its answer rather than sending the value from
  before; `state.TrackEntryAsync(task)` does the same for a control of your own whose commit awaits something.
- Until enough is typed the box tells assistive technology how much is needed; then how many results there are.
  Nothing is written to the model per keystroke: only a choice, or leaving the box, commits.
- `state.SearchOptionsAsync(field, path, text)` runs the same search for a control of your own.

Search providers are code, so they are not exported to JSON Schema; validate the chosen id on the server, where the
same lookup lives.

### Tags

A list of short strings is a set of chips rather than a repeater:

```csharp
.Field(x => x.Skills, f => f.AsTags(max: 8))
```

Enter or a comma adds one (`Attr("tagSeparators", ";")` changes the set), backspace on an empty box takes the last
one back, and duplicates are refused case-insensitively. A list loaded from storage that already holds a duplicate
still renders, and × removes only the chip it is on. The entry box deliberately belongs to no form, so pressing Enter
adds a tag instead of posting the page.

## Change handlers

`Computed` derives a value the field owns. `OptionsFrom(dependsOn: …)` reloads a list. `ClearOnHide` empties an
abandoned branch. What none of them expresses is "when A changes, write B" where B is still the user's to edit - so
that is what `OnChange` is for:

```csharp
.Field(x => x.Plan, f => f
    .Options(("solo", "Solo"), ("team", "Team"))
    .OnChange(ctx => ctx.SetSibling(nameof(Model.Seats), ctx.Value is "team" ? 5 : 1)))

.Field(x => x.Country, f => f.OnChange(ctx => ctx.ClearSibling("City")))
```

Paths resolve relative to the object that owns the field, exactly as conditions and computed dependencies do, so a
handler on a repeater's item template means *that row*. What a handler writes does not mark the field touched - the
user has not been there, and a field the form filled in on their behalf should not open covered in errors. Handlers
do not run while the form is being constructed, and a pair that answer each other settles rather than recursing.
A handler runs after the form has settled the change it reacts to - computed values are up to date and any select
cascading off the field has already been emptied - so it reads current totals and may set the dependent field itself.

## Drafts and autosave

A long form owes its user "you were halfway through this - carry on?" when the tab closes or the session expires.
`BormDraft` writes the form's unfinished answers as a small JSON document and puts them back; where it is kept -
browser storage, the server, a database - is the application's choice:

```razor
<BormView State="_state" OnValidSubmit="Submit" />
<BormAutosave State="_state" OnSave="json => drafts.SaveAsync(DraftKey, json)" OnDiscard="() => drafts.DeleteAsync(DraftKey)" />
<BormUnsavedChangesGuard State="_state" />

@code {
    protected override async Task OnInitializedAsync()
    {
        _state = new BormState(_definition, new BormModelDataAccessor(_model), Services);
        if (await drafts.LoadAsync(DraftKey) is { } json)
            BormDraft.Restore(_state, json);    // back as unsaved changes, on the step it was saved on
    }

    private async Task Submit(BormState state)
    {
        await api.SaveAsync(_model);
        await drafts.DeleteAsync(DraftKey);      // a submitted form has no draft
        state.AcceptChanges();                   // and nothing left to warn about
    }
}
```

- **What a draft holds**: every value the schema binds to, one per path (`Lines[2].Quantity`), with each repeater's
  row count and the wizard step. Types come back as they went - enums, dates, a timestamp's offset, a multi-select's
  list. Computed values are not saved; they are computed again.
- **What it never holds**: files (a browser file cannot outlive the page that chose it) and sensitive values -
  passwords, and fields marked `Sensitive()` (`x-sensitive`) - unless `BormDraftOptions.IncludeSensitive` asks.
  `ExcludeHidden` also leaves out the answers of branches the user hid.
- **Restoring** writes the answers as unsaved changes on top of what the form opened with, so `IsFormDirty` and an
  unsaved-changes prompt stay meaningful and `Reset()` takes them away. Change handlers stay silent and a cascading
  select keeps the answer it is given. What the form has no field for, or can no longer hold, is listed in the
  result's `Skipped` rather than left on a field as an error the user never made - and so are rows past
  `BormDraft.MaxRows` (10,000, all the draft's repeaters together) and a row count for a row that is not there.
- **Versions**: a draft records `BormDefinition.Version` (`.Version("3")`, `x-version`). When it differs from the
  form's, `BormDraftOptions.Migrate` gets the draft to bring up to date first:
  `m => { if (m.FromVersion == "2") m.Rename("Phone", "Contact.Phone").Remove("Fax"); }`.

`BormAutosave` writes one a moment after the user stops changing the form (`DelayMilliseconds`, default 1000) -
only while there are unsaved changes, and once more as the page goes away if a save was still waiting. A draft
put back by `BormDraft.Restore` is not saved again, and `OnDiscard` runs once the form is back where it started -
`Reset()`, or every change undone - so the stored draft can go with it. Clear the
stored draft once the form is submitted. Autosave and the guard below hear what a field has committed, and a text box commits what is typed
in it when it is left: give the fields whose half-written answers a draft should keep `UpdateOnInput(300)`, and they
commit as the user types. `BormUnsavedChangesGuard` asks before the user leaves a form with unsaved
changes: Blazor's `NavigationLock` gives an external navigation the browser's own "leave site?" prompt, and an
in-app one asks `OnConfirm` (a dialog of yours) or the browser's `confirm()` with `BormMessageKeys.UnsavedChanges`.

`state.Snapshot(new BormSnapshotOptions { ExcludeHidden = true, ExcludeSensitive = true })` is the payload-shaped
counterpart: a hidden answer is *retained* while the user can still toggle back to it, *cleared* when the field is
`ClearOnHide`, and *excluded* from what is sent when the snapshot says so. A repeater comes row by row
(`Lines[0].Product`) rather than as its whole list, and everything inside a sensitive list or group counts as sensitive.

A draft is plain JSON holding personal data. Keep it where the user's other data is kept, and not in storage another
user of the same machine can read.

## Undo and redo

`ShowUndo` puts Undo and Redo buttons beside Submit; `state.UndoAsync()` and `state.RedoAsync()` do the same from
code, with `CanUndo`, `CanRedo` and `NextUndoPath` to drive buttons of your own.

```razor
<BormView Definition="_definition" Model="_model" ShowUndo="true" />
```

- **A change is everything one answer set off**: the value itself, a field a condition then hid and cleared, a
  sibling a change handler filled in, a city an abandoned country took with it. They are taken back together, and the
  handler is not run again on the way - what it did is part of the change. Computed values are not recorded; they
  follow the answers back.
- **Rows keep their identity.** Adding, removing, moving, swapping, copying and clearing rows are changes like any
  other, and undoing a removal puts back the same row object - with whatever it held - rather than a copy.
- Two writes to one field within a second are one change, so a field that writes as the user types is not undone a
  character at a time, and the tidying a field does as the user leaves it (`Trim()`, a mask) belongs to that answer.
  `ResetField` and `SetValues` are changes of their own.
- A field put back is judged again as a change would be, so an error the user has been shown comes back with the
  value that caused it.
- `HistoryLimit` is how many changes are kept - 0, the default, keeps none; `ShowUndo` makes it 100. `Reset()` and
  restoring a draft start the history again. Only changes made through the state are recorded: a model edited behind
  the form's back has nothing to undo.

The buttons keep the focus, so they can be pressed again; each is named for the field it acts on ("Undo the change to
Full name"), and what it did is announced ("Undone: Full name"). Ctrl+Z inside a text box still undoes the typing in
that box, as the browser always has - the form's Undo takes back answers already given.

## Layout

Set a column count on the form and let individual fields span more than one. The grid collapses to a single column
on narrow screens.

```csharp
BormBuilder.For<Profile>()
    .Columns(2)
    .Field(x => x.FirstName)
    .Field(x => x.LastName)
    .Field(x => x.Bio, f => f.AsTextArea(4).ColumnSpan(2))
    .Build();
```

### Long lists

A list of hundreds or thousands of rows is shown a page at a time:

```csharp
.Array(x => x.Lines, line => line.Field(l => l.Product), f => f.PageSize(25))
```

The pager offers Previous and Next, a page picker and the rows a page holds ("26–50 of 1,000"), and says how many rows
on other pages need attention, with a button to the first of them. Every way of reaching a row still reaches it: the
error summary's links and focus-the-first-error turn to the row's page before they move the focus - a list inside a
row of another paged list included - and adding, copying or moving a row turns to the page it lands on. Row numbers
and the rows' accessible names stay those of the whole list. `x-pageSize` carries it through JSON Schema.

It is paged rather than virtualised on purpose. A virtualised list leaves the rows out of view out of the page, where
neither the keyboard, nor the error summary, nor a screen reader can reach them; a page is always whole.

### Groups

A run of consecutive fields sharing a group name renders as one `<fieldset>` with a `<legend>`, so a screen reader
announces "Who you are" as focus enters the first control in it. `[Display(GroupName = …)]` on the model does the
same thing.

```csharp
BormBuilder.For<SupportTicket>()
    .Field(x => x.Name,  f => f.Group("Who you are"))
    .Field(x => x.Email, f => f.Group("Who you are"))
    .Field(x => x.Team,  f => f.Group("Where it belongs"))
    .Build();
```

Grouping is by runs rather than by collecting every field with the same name, so the declared order is never
rearranged behind your back.

### Owning the layout

`BormView` renders the whole schema. When the page wants the layout for itself, place fields one at a time instead -
they keep the schema's label, rules, conditions and ARIA wiring:

```razor
<div class="two-up">
    <BormField State="_state" Name="ReporterName" />
    <BormField State="_state" Name="ReporterEmail" />
</div>

<h3>Routing</h3>
<BormField State="_state" Name="Team" />
<BormField State="_state" Name="Lines[0].Product" />

<button @onclick="() => _state.SubmitAsync(Save)">Raise ticket</button>
```

Each field follows the state by itself - its message, a condition, a list of options arriving - so the page only
needs to subscribe to `StateChanged` for markup of its own that reads the state (an "unsaved changes" count, say). An
unknown path renders nothing, which is what a schema chosen at run time needs; set `ThrowIfMissing="true"` to catch a
typo in a fixed layout. Rendering the same schema twice on one page? Give each state its own `IdPrefix`.

## Field presentation

The details that separate a generated form from a designed one:

```csharp
BormBuilder.For<Listing>()
    // A section heading that belongs to the schema, not to the page around it.
    .Static("basics", "The basics", "What someone sees first in the search results.")

    .Field(x => x.Title, f => f
        .MaxLength(60)
        .CharacterCount()                       // a live "12 / 60"
        .UpdateOnInput())                       // write on every keystroke, not on blur

    .Field(x => x.Summary, f => f
        .AsTextArea(3)
        .UpdateOnInput(debounceMilliseconds: 150))  // one write per pause, not per character

    .Field(x => x.Price, f => f.Prefix("€").Suffix("/ month"))
    .Field(x => x.City, f => f
        .Suggest("Amsterdam", "Berlin", "Lisbon")   // proposes; does not restrict
        .Clearable())
    .Field(x => x.Passcode, f => f.AsPassword().Revealable())
    .Field(x => x.Published, f => f.AsSwitch())
    .Field(x => x.Query, f => f
        .HideLabel()                            // stays the accessible name
        .InputAttr("data-testid", "search"))    // any HTML attribute you like
    .Build();
```

`UpdateOnInput` matters more than it looks: the browser's `change` event fires on *blur* for a text box, so without
it the model does not see a keystroke until the user leaves the field. The `input` handler is only wired when a field
actually needs it, so nothing pays for a round-trip per keystroke it has no use for. A debounced value never waits for
its timer when it matters: leaving the box, pressing Enter (which submits, or moves a wizard on) and the control
going away all write it first.

`Clearable()` puts a × inside the box once it holds something, and puts the caret back in the box after clearing it.

### File upload

A property typed `IBrowserFile` (or a list of them) renders a file picker. `Accept(...)`, `MaxFileSize(...)` and
`[FileExtensions]` become both the picker's own `accept` list and rules that report a file the picker let through
anyway; a list of files counts against `Items(min, max)` like any other collection. Each takes a message of its own,
which is used only for its own failure, whichever order they are called in. Read the files in your submit
handler with `OpenReadStream`, exactly as you would with Blazor's `InputFile`.

Or send each file as soon as it is chosen, and keep what your storage returns for it instead:

```csharp
.Field(x => x.ResumeId, f => f.Accept(".pdf").MaxFileSize(5_000_000).UploadWith(async upload =>
{
    using var content = new StreamContent(upload.OpenReadStream());       // the bar follows these bytes
    var response = await http.PostAsync("api/files", content, upload.CancellationToken);
    response.EnsureSuccessStatusCode();
    return await response.Content.ReadAsStringAsync(upload.CancellationToken);   // what the field stores
}))
```

The field then holds a string - or a list of them for a field taking several files - and never the file itself, so it
is an answer like any other: it goes into a draft, survives a reload and reaches the server as the id it is. A field
taking several adds each new file to the ones it opened with, which are listed - each with its own Remove - beside
them; one taking a single file lets go of the one it had as soon as another is chosen, so a replacement that fails
leaves it empty rather than quietly still holding the file the user replaced. Each file
is judged by `Accept` and `MaxFileSize` before anything is sent, then listed with a progress bar and a Cancel button
while it goes, Remove once it is there, and Retry - with the reason - if the uploader throws. Completion and failure
are announced once each, not per percent. A submit pressed while a file is on its way waits for it, and Cancel,
clearing the field or leaving the form cancels `upload.CancellationToken`. `state.UploadsFor(path)` lists them for a
control of your own. Uploaders are code, so they are not exported to JSON Schema.

### Formatted text and signatures

```csharp
.Field(x => x.CoverLetter, f => f.AsMarkdown().MaxLength(4000))
.Field(x => x.Signature, f => f.AsSignature().Required())
```

`AsMarkdown()` is a text area for formatted text written in Markdown - paragraphs and line breaks, headings, bulleted
and numbered lists, quotes, code, rules, `**bold**`, `*italic*`, `~~struck~~`, `` `code` `` and `[links](https://…)` -
with a preview beneath it that follows the typing, a short guide to the syntax, and a review that shows it formatted.
The value is the Markdown text, so its length rules count what the user typed. `BormMarkdown.ToHtml` renders it
anywhere else, and `ToPlainText` takes the formatting out.

What it renders is safe to put on a page: everything the user wrote is HTML-encoded, only the elements listed above
are written, a link goes only to an `http`, `https` or `mailto` address - `javascript:` and every other scheme stay
text - and links carry `rel="nofollow noopener noreferrer"`. Rendering is linear in the length of the text, however
it is crafted. It is not a WYSIWYG editor: that needs a `contenteditable` element and script to read it, and Borm
ships no script.

`AsSignature()` is a signature pad: an SVG drawn on with a mouse, a finger or a pen (the page does not scroll while it
is being signed), stroke by stroke, with Clear beside it - and **Type it instead**, for anyone who cannot draw, which
writes the typed name in a handwriting face. Either way the value is an SVG image as a `data:` URL that an `<img>`
shows as it is; `BormSignature` builds, checks and decodes them, and a review shows the signature as an image. Only a
value in that shape is ever shown, and SVG shown through an `<img>` cannot run script. A signature is personal data:
mark it `Sensitive()` if it should not be kept in a draft. `x-widget: markdown` and `x-widget: signature` carry both
through JSON Schema.

## Checking a schema

`Validate()` reports the mistakes that are easy to make and hard to see - duplicate field names, arrays with no item
template, steps naming fields that do not exist, conditions and expressions pointing nowhere (past a `[]` too), a
comparison rule that never revalidates, computed values that feed each other in a cycle, and settings a control can
never honour. It is a development aid, never run automatically - a schema being edited at runtime has every right
to be incomplete:

```csharp
foreach (var problem in definition.Validate())
    logger.LogWarning("{Problem}", problem);   // [Error] Lines: An array field has no ItemTemplate…
```

## Custom renderers

`AddBorm` takes a callback that configures the field renderer registry - override a built-in control, or register a
component under a key a field asks for:

```csharp
builder.Services.AddBorm(registry =>
{
    registry.RegisterCustom<StarRatingInput>("rating");
});
```

```csharp
.Field(x => x.Rating, f => f.CustomRenderer("rating").Attr("max", 5))
```

Custom input components inherit from `BormInputBase`, which hands them the field (`Field`), its context (`Context`,
with `Value`, `StringValue` and `SetValueAsync`) and whether they are `Disabled`:

```razor
@inherits BormInputBase

<div role="radiogroup" aria-label="@Field.Label">
    @for (var n = 1; n <= 5; n++)
    {
        var star = n;
        <button type="button" disabled="@Disabled" @onclick="@(() => Context.SetValueAsync(star))">★</button>
    }
</div>
```

A field of type `BormFieldType.Custom` whose renderer key is not registered throws with a message naming the key,
rather than silently falling back to a text box. Calling `AddBorm` twice configures the registry that is already
there, so a module can add its renderers after the application has registered its own.

### Blazor's own inputs inside a renderer

`InputText`, `InputNumber<T>`, `ValidationMessage<T>`, `ValidationSummary` and the inputs of most component libraries
need a Blazor `EditContext` to exist at all. Set `ProvideEditContext` on the view and it cascades one that tells the
same story as the form, so such a control can sit in a custom renderer and show the field's errors as its own:

```razor
<BormView Definition="_definition" Model="_model" ProvideEditContext="true">
    <ValidationSummary />
</BormView>
```

```razor
@inherits BormInputBase

<InputText id="@Context.ElementId"
           Value="@Context.StringValue"
           ValueChanged="@((string? v) => Context.SetFromStringAsync(v))"
           ValueExpression="@(Context.ValueExpression<string?>())" />
```

- **The form stays the source of truth.** Values are written through the field context, never through the
  `EditContext`, so conditions, computed values and dirty tracking stay right. `Context.ValueExpression<T>()` names
  the property of the object that owns the field - the model, a nested object, a list's row - or the key of a
  dictionary-backed one, so the field identifier Blazor works out is the one the form's messages are filed under.
  Ask for the property's own type: `ValueExpression<string>()` over an `int` throws rather than read it wrongly.
- **A message reaches the context when the form would show it** - the field touched, or the form submitted - so a
  Blazor input reads `invalid` no sooner than Borm's own controls do. Only errors are copied: an `EditContext` has no
  severity, so warnings and notes stay Borm's.
- **`EditContext.Validate()` runs the form's validation**, which is what a hand-written submit button over the
  context does; the synchronous rules have answered before it returns.
- `Context.EditContext` and `Context.FieldIdentifier` are there for a component that wants them directly, and are
  null while the view provides no context. For a state rendered without a view, construct a `BormEditContextAdapter`
  over it and cascade its `EditContext`; give it your component's `InvokeAsync`, and dispose it with the state.

Off by default: a form that does not use these controls has no cascade in its tree.

## The BormView component

| Parameter | Description |
| --- | --- |
| `Definition` | The schema to render. Required unless `State` is supplied. |
| `Model` | Optional typed model to bind to. When omitted, a dictionary store is used. |
| `Data` | Optional `IDictionary<string, object?>` backing store (used when `Model` is null). A writable dictionary is edited **in place** at every level - so don't share one (a static "defaults" dictionary, say) between forms or circuits; a read-only one is copied. Keys match without regard to case, typed nested containers keep their types, and values from `JsonSerializer.Deserialize<Dictionary<string, object?>>` are converted to plain dictionaries, lists and primitives as they are reached. |
| `State` | Provide a pre-configured `BormState` (e.g. with FluentValidation wired up). |
| `ConfigureState` | Runs against the state the view builds from `Definition`, before it renders - set the validation triggers, wire up a validator, subscribe to an event. Saves constructing and disposing a state by hand just to change one setting. |
| `OnValidSubmit` | Raised with the state after a successful (valid) submit. |
| `OnInvalidSubmit` | Raised after a submit that failed validation. |
| `OnSubmitError` | Raised with the exception when `OnValidSubmit` throws. While it is set the failure is handled here, rather than escaping into the renderer - where, on Blazor Server, an unhandled exception ends the circuit. |
| `OnFieldChanged` | Raised with the path of a field whose value changed. |
| `ReadOnly` | Renders every field read-only (review mode) and hides the buttons. Pressing Enter in a read-only form does not submit it. |
| `Disabled` | Disables every field and button - a save in flight, a locked record. Unlike `ReadOnly` the fields cannot be tabbed through or read out, so reach for it only when the form is genuinely inoperable. |
| `ShowSubmitButton` / `SubmitText` / `SubmittingText` | The built-in submit button (default `true` / `"Submit"`). While the form is validating or saving the button says so, rather than only going grey. |
| `ShowResetButton` / `ResetText` | A button restoring the values the form started with. |
| `ShowUndo` | Undo and Redo buttons, and the history they need - see [Undo and redo](#undo-and-redo). |
| `ShowErrorSummary` | Lists every error above the form, each linking to its field - or to its step on a wizard. The links name the current page in full, so they work under any `<base href>`. |
| `FocusFirstError` | Moves focus to the first invalid field after a failed submit (default `true`; suppressed while `ShowErrorSummary` is on, since the summary takes focus itself). |
| `AllowStepNavigation` | Lets the user click back to a completed wizard step (default `true`). |
| `BackText` / `NextText` | Wizard navigation labels. |
| `Header` / `Footer` / `Actions` | Render fragments for extra content and a custom button row. |
| `Class` | Extra CSS classes for the `<form>` element. |
| `IdPrefix` | What every DOM id in the form starts with (default `borm_`). Give two forms on one page a prefix each. |
| `ProvideEditContext` | Cascades a Blazor `EditContext` mirroring the form's validation, for `InputBase`-derived controls, `ValidationSummary` and component-library inputs placed in the form (default `false`). See [Blazor's own inputs inside a renderer](#blazors-own-inputs-inside-a-renderer). |
| `FormName` / `PostedModelName` | Makes the form one the browser can post with no interactivity, its controls named for the page's `[SupplyParameterFromForm]` model (default `Model`). See [Forms that work with no interactivity](#forms-that-work-with-no-interactivity). |
| `ChildContent` | Extra content rendered inside the `<form>`. |

Any other attribute you set is splatted onto the `<form>` element.

When you let the view build its own state from `Definition`, capture the component to reach it:

```razor
<BormView @ref="_form" Definition="_definition" Model="_model" />

<button @onclick="() => _form.Form.Reset()" disabled="@(!_form.Form.IsFormDirty)">Undo</button>

@code { private BormView _form = default!; }
```

A state the page constructs itself is the page's to dispose: `BormState` is `IDisposable`, and disposing it cancels
an options load or an async rule still in flight instead of letting it finish against a form nobody is looking at.

## BormState at a glance

| Member | Purpose |
| --- | --- |
| `GetValue` / `SetValue` / `SetValueQuietly` | Read and write by path; the quiet variant does not mark the field touched. |
| `For<TModel>()` | The same state addressed by expression - `state.For<Order>().SetValue(x => x.Lines[i].Quantity, 3)`, `GetFieldState(x => x.Email)`, `FocusAsync`, `ResetField`, `SetServerError` - so a rename renames every read and write, and a typo does not compile. |
| `SetValues` / `Batch` | Write many values as one change - one re-render, not one per field. |
| `IsFormDirty`, `DirtyFields`, `TouchedFields` | Change tracking. Dirtiness is a comparison against the values the form opened with, so a field typed into and put back is clean again. |
| `ResetField(path)` | Puts one field - and anything nested beneath it - back to the value it started with. |
| `IsValidating`, `IsSubmitting`, `IsSubmitted`, `SubmitCount`, `IsValid`, `HasValidated` | Submission state. `IsValid` reports what validation has *found*, so check `HasValidated` before treating it as a verdict - a form nobody has validated has no errors. |
| `IsSubmitSuccessful` | Whether the last submit passed validation *and* its handler returned without throwing, being cancelled or recording errors - what a "Saved." banner binds to. |
| `SubmitOutcome`, `SubmitError` | How the last submit ended - `Invalid`, `Rejected`, `Failed`, `Cancelled`, `Succeeded` - and the exception behind a `Failed` one. |
| `CancelSubmit()`, `SubmitCancellationToken`, `ReadOnlyWhileSubmitting` | Stop a save in flight, the token its handler passes on, and holding the fields still while it runs. |
| `NotifyDefinitionChanged()` | The schema was changed in place (a field added, removed or reconfigured): new fields get defaults and baselines, computed values are evaluated again, and what was recorded against removed paths is dropped. |
| `IsValidatingField(path)`, `ValidatingFields` | Whether an async rule is in flight for one field, so a remote uniqueness check can put a spinner beside its own box rather than declaring the whole form busy. |
| `GetFieldState(path)` | Touched, dirty, invalid, validating, validated, the first error and every message for one field, in a single read - and `Status` (`Unvalidated`, `Pending`, `Invalid`, `Valid`) for a control that wants it in one word. |
| `RunAsyncRulesOnInvalidFields` | Run a field's async rules even when its sync rules already found an error (off by default). |
| `NormalizeField(field, path)` / `NormalizeAll()` | Run the schema's normalizers. The built-in controls call these for you on blur and on submit. |
| `SubmitAsync` | Normalizes, marks everything touched, validates, and dispatches - ignoring re-entrant calls. See [How a submit ends](#how-a-submit-ends). |
| `ValidateAsync` / `ValidateStepAsync` / `ValidateFieldAsync` | Validation at three scopes; newer runs supersede older ones, and a superseded run reports false rather than a verdict it never reached. `ValidateFieldAsync(path)` resolves the field from the schema for you. |
| `ValidateDependentsAsync(path)` | Revalidates every field that declared `RevalidateOn(path)`. The built-in controls call it for you; this is the hook for a custom one. |
| `SingleErrorPerField` | Show a field's first error rather than every rule it breaks. Every rule still runs. |
| `MessagesFor`, `AllMessages`, `OrderedMessages`, `SetServerError(s)` | Validation messages. |
| `Reset()` / `Reset(values)` / `AcceptChanges` | Restore the starting values, rebase onto new ones, or make the current ones the new baseline. |
| `Snapshot()` | Every bound path mapped onto its value - the shape `Reset(values)` takes back, so the two are a draft save and restore. |
| `AddArrayItem`, `InsertArrayItem`, `DuplicateArrayItem`, `RemoveArrayItem`, `MoveArrayItem`, `SwapArrayItems`, `ClearArrayItems` | Repeater operations; messages, touched and dirty state, rejected entries and loaded choices follow their items. A `T[]` property works too - it is replaced with a resized copy. |
| `FocusAsync(path)` | Moves focus to the control rendering a path; false when nothing is rendering it. |
| `Text(key, args)` | Resolves the library's own UI text through the registered message provider. |
| `NextStepAsync`, `PreviousStep`, `GoToStep`, `VisibleSteps`, `CurrentStepNumber`, `FurthestStepIndex`, `IsStepReachable` | Wizard navigation. |
| `ReadOnly` / `Disabled` | Makes the whole form read-only (still focusable and readable) or disabled (inoperable). |
| `StateChanged` | Raised after anything the page might render changes. A page that renders its own markup from the state subscribes to it (and unsubscribes when it is disposed). |

## Theming

### The custom properties

Every colour - plus the corner radius, the field gap and the form's maximum width - is a CSS custom property declared
on `.bit-borm-form`, so a host app restyles every form by overriding them in a rule that reaches the form itself,
without touching the stylesheet. Because the form declares every token on its own element, a value set on an ancestor
(`:root`, `body`, a page wrapper) is shadowed and has no effect; write `.bit-borm-form` or `.my-page .bit-borm-form`
instead:

| Property | Light | Dark | Used for |
| --- | --- | --- | --- |
| `--bit-borm-accent` | `#4f46e5` | `#4f46e5` | The primary button, focus rings, a checked switch, the range slider. |
| `--bit-borm-accent-contrast` | `#ffffff` | `#ffffff` | Text and marks drawn on the accent colour. |
| `--bit-borm-surface` | `#ffffff` | `#111827` | The background of controls, tags and buttons. |
| `--bit-borm-surface-muted` | `#fafafa` | `#1f2937` | Read-only controls, affixes and hovered buttons. |
| `--bit-borm-surface-disabled` | `#f3f4f6` | `#374151` | Disabled controls. |
| `--bit-borm-border` | `#d1d5db` | `#4b5563` | Control and card borders. |
| `--bit-borm-border-focus` | `var(--bit-borm-accent)` | `var(--bit-borm-accent)` | The border of a focused control. |
| `--bit-borm-error` | `#dc2626` | `#f87171` | Error text, invalid borders, the required marker. |
| `--bit-borm-error-contrast` | `#ffffff` | `#111827` | Text drawn on the error colour: a step's error count. |
| `--bit-borm-error-surface` | `#fef2f2` | `#2a1215` | The error summary's background and a tag's remove button on hover. |
| `--bit-borm-warning` | `#d97706` | `#fbbf24` | Warning messages. |
| `--bit-borm-text` | `#111827` | `#f9fafb` | Labels and values. |
| `--bit-borm-muted` | `#6b7280` | `#9ca3af` | Help text, counters, placeholders. |
| `--bit-borm-radius` | `8px` | `8px` | Corner radius of controls and cards. |
| `--bit-borm-gap` | `1rem` | `1rem` | The space between fields. |
| `--bit-borm-max-width` | `720px` | `720px` | The form's maximum width; `none` fills the container. |

```css
.bit-borm-form {
    --bit-borm-accent: #0d9488;
    --bit-borm-radius: 4px;
    --bit-borm-max-width: none;
}
```

That is enough for a token the two themes share - the accent, the radius, the gap, the width. A token that differs
between them (every surface, border and text colour) has to be overridden once per theme: a plain `.bit-borm-form`
rule loaded after the stylesheet would also replace the dark value when the OS is dark, and a `data-theme` rule outranks
it. The four rules below mirror the stylesheet's own:

```css
.bit-borm-form { --bit-borm-surface: #fffdf7; }
[data-theme="light"] .bit-borm-form, .bit-borm-form[data-theme="light"] { --bit-borm-surface: #fffdf7; }

@media (prefers-color-scheme: dark) {
    .bit-borm-form { --bit-borm-surface: #1c1917; }
}
[data-theme="dark"] .bit-borm-form, .bit-borm-form[data-theme="dark"] { --bit-borm-surface: #1c1917; }
```

### Colour modes

The default theme follows the OS light/dark preference. An app with its own theme toggle drives the same tokens by
putting `data-theme="dark"` or `data-theme="light"` on any ancestor (or on the form itself), which wins over the media
query - so "dark site, light OS" comes out dark. Each theme also sets `color-scheme`, so the parts of a control the
browser draws itself - date pickers, open dropdowns, the file button, scrollbars - follow it too.

### Styling hooks

The markup uses BEM-style classes under the `bit-borm-` prefix. The ones most worth targeting:

| Class | Element |
| --- | --- |
| `bit-borm-form` | The `<form>`; carries the custom properties. `__title`, `__description`, `__body`, `__actions` are its parts. |
| `bit-borm-field` | One field: its label, control, help and messages. `--invalid`, `--checkbox` and `--static` are its states. |
| `bit-borm-label`, `bit-borm-help`, `bit-borm-required` | A field's label, its help text and the required marker. |
| `bit-borm-input` | Every text-like control, select and textarea; `--invalid` when it holds an error. |
| `bit-borm-messages`, `bit-borm-message` | A field's message list; `--error`, `--warning` and `--info` per severity. |
| `bit-borm-group`, `bit-borm-fieldgroup` | A nested object, and a run of fields sharing a group name. |
| `bit-borm-array` | A repeater; `__item`, `__item-body`, `__item-actions`, `__header`, `__empty` are its parts. |
| `bit-borm-stepper` | A wizard's step list; `__step`, `__link`, `__num`, `__label` are its parts. |
| `bit-borm-tabs` | Steps shown as tabs; `__list`, `__tab`, `__label`, `__badge` (a count of errors) and `__panel` are its parts. |
| `bit-borm-section` | A step shown as a section, `--collapsible` in an accordion; `__title` and `__body` are its parts, `has-errors` its state. |
| `bit-borm-review` | A review of the answers; `__section`, `__header`, `__heading`, `__list`, `__entry`, `__label`, `__value`, `__rows` and `__empty` are its parts. |
| `bit-borm-summary` | The error summary. |
| `bit-borm-btn` | Every button; `--primary`, `--ghost`, `--danger`, `--add`, `--icon` and `--sm` are its variants. |
| `bit-borm-grid` | The form body when `Columns(...)` is set. |
| `bit-borm-sr-only` | Text that is read out but not shown. |

## Accessibility

Accessibility is the default rather than an option:

- Every control has a real `<label>` (a hidden one still names it when `HideLabel()` is set), and radio groups,
  multi-selects, groups and repeaters are `<fieldset>`s with a `<legend>`.
- `aria-required`, `aria-invalid` and `aria-describedby` are set on every control, the last pointing at the field's
  help text and messages, so a message is read out when focus reaches the field.
- After a failed submit, focus moves to the first invalid field - or to the error summary, which links to each field
  (or its step on a wizard). When there is no field to take focus, the outcome is announced instead.
- Changing a wizard step moves focus to the new step's content; adding a repeater row puts the caret in it, and
  removing one moves it to the row that took its place, or to the add button when the last one goes.
- Repeater, tag and character-count changes are announced through live regions.
- `ReadOnlyWhen` keeps a settled value focusable and readable, where a disabled control would leave the tab order.
- `Autofocus()` focuses a field when the form first renders.
- A message's severity is said in words as well as shown in colour: "Error:" is read before an error (which is also
  set in a heavier weight), and "Warning:" or "Note:" is shown and read before the others, so a warning is never
  told from an error by colour alone. The words come from the message provider and are no part of the message's
  own text.
- Tabs follow the WAI-ARIA tabs pattern (one tab stop, arrow keys, Home and End); an accordion is the browser's
  own `<details>`; a tab or section with errors carries a count that is read out too; a review's "Change" buttons
  are named for their step.
- Every target the stylesheet draws is at least 24 × 24 CSS pixels, WCAG 2.2's minimum, by its own size rather than
  by its spacing.
- The layout uses logical CSS properties only, so a form mirrors correctly inside `dir="rtl"`.

What the library cannot vouch for: a custom renderer's own markup, the browser's built-in pickers (the date, month
and week controls, `<datalist>` and `<select>` popups) and the colours a host app re-tints the tokens to - check
contrast against WCAG 1.4.3 when you do.

## Localization

Register an `IBormMessageProvider` to replace the built-in English text - typically one wrapping `IStringLocalizer`:

```csharp
builder.Services.AddBormMessages<MyMessageProvider>();
```

```csharp
public sealed class MyMessageProvider(IStringLocalizer<MyMessageProvider> localizer) : IBormMessageProvider
{
    public string Get(string key, params object?[] args)
    {
        var text = localizer[key, args];
        return text.ResourceNotFound ? BormDefaultMessageProvider.Instance.Get(key, args) : text.Value;
    }
}
```

It covers everything the library renders, not only the validation messages: the submit and reset buttons, a wizard's
Back/Next, the select placeholder and its loading state, a repeater's add/remove labels and empty state, the error
summary heading, the character counter. The keys are listed on `BormMessageKeys`; anything a provider does not
recognise should fall through to the default provider (or to the key itself).

Messages passed explicitly to a rule - and labels passed explicitly to `BormView` - always win over the provider.

### Built-in translations

The library's own words come translated, too: German, Spanish, French, Italian, Dutch, Portuguese, Russian, Turkish,
Persian, Arabic, Japanese and simplified Chinese.

```csharp
builder.Services.AddBormTranslations();
```

Every validation message, button, announcement and accessible name the library renders, and the designer, is then
said in the form's `state.Language` when it has one, else in `CurrentUICulture`, and in English for any other
language. Regional cultures read their language's table (`de-CH`, `pt-BR`); simplified Chinese answers to `zh`,
`zh-Hans`, `zh-CN` and `zh-SG`, and a traditional-Chinese culture is answered in English rather than in the wrong
script. Counts are phrased to read right for any number ("Anzahl: 3") instead of imposing English plural rules on
other languages, with a wording of its own for exactly one where a language has one. Changing `state.Language`
rewrites the form's words at once; a rule's message already on screen is rewritten when its field is next validated.

A provider of your own can answer in a form's language as well: implement `IBormLocalizedMessageProvider`, whose
`Get(language, key, args)` the form calls with its `Language`. `BormValidator.Language` does the same for a form
validated on its own, such as one posted to the server, and `BormDesigner` takes a `Language` parameter. The words a
schema supplies - labels, options, a repeater's item noun - are the schema's to translate, below.

### The schema's own text

The message provider translates the library's words; the schema's - labels, help, placeholders, the labels of its
own options, step titles, the form's title - go through an `IBormSchemaLocalizer`, typically one wrapping
`IStringLocalizer`:

```csharp
builder.Services.AddBormSchemaLocalizer<FormText>();

public sealed class FormText(IStringLocalizer<FormText> localizer) : IBormSchemaLocalizer
{
    public string? Localize(string key, string? text)
        => localizer[key] is { ResourceNotFound: false } found ? found.Value : null;   // null keeps the text
}
```

| Key | Text |
| --- | --- |
| `{field}.label`, `.help`, `.placeholder`, `.prefix`, `.suffix` | A field's texts. `{field}` is its `TextKey` (`x-i18n`) when set, else its path with the rows taken out - `Lines.Product` for every line's product. |
| `{field}.options.{value}` | The label of one of the field's own options (a provider's options are its own to translate). |
| `steps.{id}.title`, `steps.{id}.description` | A step's texts. |
| `form.title`, `form.description` | The form's. |

A form described in JSON can carry its translations itself, as `x-translations` on the root - culture to key to
text - consulted after any registered localizer, for the current UI culture and then its parent (`de-CH`, then
`de`):

```json
"x-translations": {
  "de": { "form.title": "Bestellung", "Lines.Product.label": "Artikel", "plan.options.team": "Team" },
  "fr": { "form.title": "Commande" }
}
```

The builders add the same tables in C#, and they are exported as `x-translations` with the rest of the schema:

```csharp
BormBuilder.For<Order>()
    .Title("Order")
    .Translate("de", new Dictionary<string, string> { ["form.title"] = "Bestellung", ["Plan.options.team"] = "Team" })
    .Translate("fr", new Dictionary<string, string> { ["form.title"] = "Commande" })
    .Build();
```

Both are asked each time the text renders, so a page that changes culture shows the new language on its next render.

`state.Language` gives one form a language of its own - a public form in the visitor's language, whatever the page's
culture. It needs no culture data, which matters on WebAssembly: under `InvariantGlobalization` the UI culture is
always the invariant one, and no translation would ever apply. The document's translations are looked up by the name
and then by its language (`de-CH`, then `de`), and `BormView` marks the form with it as its `lang`, so a screen
reader reads it in that language. Null, the default, follows `CurrentUICulture`.
A translated option label is what a combobox matches typed text against and what a review reads back. A form with
nothing to translate pays nothing for any of this. Dates and numbers a review reads back follow `CurrentCulture`;
the inputs themselves are the browser's, which format them for the user's own locale. The library sets no `dir`:
its layout uses logical CSS properties throughout, so a form inside `dir="rtl"` mirrors as the page does.

## Hosting models

Borm has no JavaScript and no browser-only API, so it runs the same way under every Blazor hosting model.

- **Blazor WebAssembly**: register `AddBorm()` in `Program.cs` and link the stylesheet from `index.html`.
- **Blazor Server**: the same, in the server's `Program.cs` and `App.razor`. Every field that updates on input sends
  its keystrokes over the circuit, so prefer the default update-on-change, or a debounce, on a busy form.
- **Blazor Web App**: register `AddBorm()` in both the server and the client project - the page is prerendered on the
  server and then runs on the client, and both containers must resolve the renderer registry. The form prerenders as
  plain HTML and becomes interactive once the page does. A page in static server rendering - no interactive render
  mode at all - can post a Borm form too; see below.

### Forms that work with no interactivity

Give the view a `FormName`, and bind the page's model from the post:

```razor
@page "/apply"
@attribute [ExcludeFromInteractiveRouting]      @* This page stays static in an interactive app *@

<BormView Definition="_definition" Model="Model" FormName="apply" OnValidSubmit="SaveAsync" />

@code {
    [SupplyParameterFromForm(FormName = "apply")] public Application? Model { get; set; }

    protected override void OnInitialized() => Model ??= new();
}
```

The form then posts like any HTML form - in a page with no interactive render mode, or in a browser with no script -
with an antiforgery token, each control under the name model binding reads (`Model.Address.City`,
`Model.Lines[0].Product`, `PostedModelName` to change `Model`). On the server the view judges the posted answers
exactly as an interactive one would and calls `OnValidSubmit`, or renders the page again with every message in place;
text the binder could not read - letters in a number box - is shown back as the user typed it, with the reason. A
combobox posts the label the user saw, and it is turned back into its value; tags post one entry each. A read-only
select, radio group, checkbox, multi-select or slider - drawn disabled, which a browser never posts - sends its answer
in a hidden input instead, and so does a signature the field holds; a disabled field posts nothing. Interactive, the
same form is a Blazor form as before.

With nothing to click, the form shows what a click would have: a wizard's steps, and tabs, as sections one after
another (an accordion is the browser's own and still opens); a repeater's rows as they stand, every row of a paged one
included, without add, remove or move buttons. Conditions are judged when the page is rendered, so a field that
depends on another appears after the post that answered it. A box left unticked posts nothing and binds as `false`, so
a `bool` whose default is `true` cannot be turned off without script; a file, a signature and a search lookup need an
interactive page.
- **Blazor Hybrid**: register `AddBorm()` in `MauiProgram.cs` and link the stylesheet from `wwwroot/index.html`.

### Trimming

A Blazor WebAssembly app is trimmed when it is published. `Bit.Borm` is not marked as trimmable, so the default
(`TrimMode` partial) keeps it whole, and keeps the app's own assembly, where its models live: the demo is published
this way, and its end-to-end suite passes against that build. A bound model is read, written and generated from by
reflection over its properties, all the way down through its objects and lists, which no annotation can describe;
marking the package trimmable would mean hiding that from the trimmer, or a warning on every `BormView` that binds a
model. With `TrimMode` full, keep Borm and the assemblies holding your models whole - the end-to-end suite passes
against the demo published this way too:

```xml
<ItemGroup>
    <TrimmerRootAssembly Include="Bit.Borm" />
    <TrimmerRootAssembly Include="$(AssemblyName)" />   <!-- and any other assembly holding models -->
</ItemGroup>
```

A control registered with `RegisterCustom<TComponent>()` keeps its constructor and parameters in a trimmed app: the
registry's component types carry the annotation Blazor gives its own.

## Key types

| Type | Purpose |
| --- | --- |
| `BormDefinition` | The schema: fields, wizard steps, form-level rules and metadata. `Clone()` for a shared schema you want to tailor, `Validate()` to check it. |
| `BormFieldDefinition` / `BormFieldType` | A single field and its logical type. |
| `BormBuilder` / `BormBuilder<TModel>` | Fluent schema builders. |
| `BormSchemaGenerator` | Reflection + DataAnnotations schema generation. |
| `BormExpression`, `BormExpressionCondition`, `BormExpressionRule`, `BormExpressionFunctions` | The [expression](#expressions) language: parse and evaluate one, use it as a condition or a rule, register functions of your own. |
| `BormJsonSchemaImporter` / `BormJsonSchemaExporter` | JSON Schema import and export. |
| `BormState` | Runtime state: data, validation, dirty/touched tracking, wizard position. |
| `BormView` / `BormField` | Render a whole schema, or one field of it inside your own layout. |
| `BormErrorSummary`, `BormValidationMessages` | The error summary and a field's message list, for a layout of your own. |
| `BormModelDataAccessor` / `BormDictionaryDataAccessor` | Data backing stores. |
| `IBormFieldRendererRegistry`, `BormInputBase` | Maps field types to renderer components, and the base class for your own. |
| `IBormMessageProvider`, `BormMessageKeys` | Supplies the text of every built-in message and label. |
| `BormTranslatedMessageProvider`, `IBormLocalizedMessageProvider` | The library's words in twelve more languages (`AddBormTranslations()`), and a provider that answers in a form's own `Language`. |
| `BormMask`, `BormMarkdown`, `BormSignature` | An input mask; the safe Markdown renderer; a signature's SVG data URL, drawn or typed. |
| `BormFileUploader`, `BormUpload`, `BormUploadContext` | Uploading a file as it is chosen (`UploadWith`), and following it. |
| `BormDesigner` | The visual form designer: `Value` in, a new definition out through `ValueChanged`. |
| `BormConditionOperator`, `BormValidationTrigger`, `BormUpdateTrigger` | The vocabulary of conditions, validation timing and write-back timing. |

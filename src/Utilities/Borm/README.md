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
  with `$ref`, `allOf`, `if`/`then`/`else` and `dependentRequired`) - and export any form back out again.
- **Rich field types**: text, search, password, email, URL, number, select, combobox, multi-select, radio,
  checkbox and switch, date and time, range, colour, file upload, tags, static headings, nested objects and
  repeating arrays.
- **Validation**: required, length, range (numbers, dates and times), pattern, email, URL, UUID, compare,
  multiple-of, unique items, collection size, file size and type, custom sync and async rules, form-level rules,
  conditional rules, DataAnnotations' `IValidatableObject`, and FluentValidation through
  `Bit.Borm.FluentValidation`.
- **When it runs**: on submit, on blur or on change, separately before and after the first submit; dependent
  fields revalidate when the field they read changes; async rules are debounced, superseded and cancelled.
- **Conditional behaviour**: show, hide, disable, lock (read-only) or require fields and wizard steps based on
  other values - per row inside a repeater - and clear the values of branches the user abandoned.
- **Data that follows the form**: computed values, async and cascading options, change handlers, value
  normalisation (`Trim()`, `Normalize(...)`), dirty and touched tracking, snapshots and resets.
- **Wizards**: ordered steps with per-step validation, conditional steps and a clickable stepper.
- **Accessible by default**: real labels, `aria-required` / `aria-invalid` / `aria-describedby`, grouped radio and
  multi-select controls, focus management after a failed submit, live announcements and an error summary that
  links to each field.
- **Yours to restyle and extend**: every colour is a `--bit-borm-*` custom property with light and dark themes,
  every built-in string goes through `IBormMessageProvider`, and any control can be replaced through the renderer
  registry.

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

- .NET 8, .NET 9 or .NET 10
- Blazor WebAssembly, Blazor Server, Blazor Web App or Blazor Hybrid
- `FluentValidation` 12 only when the optional `Bit.Borm.FluentValidation` package is used

## The demo

The demo site, [borm.bitplatform.dev](https://borm.bitplatform.dev), opens on an overview with a live form, and its
documentation - starting at `/getting-started` - is a set of working forms, one per topic of this guide:
`/quickstart` (a schema from a C# type), `/builder` (the fluent builder and conditions), `/array` (repeaters and
computed totals), `/wizard`, `/layout`, `/field-polish`, `/combobox-tags`, `/per-row-conditions`, `/settled-answers`,
`/composition`, `/state-api`, `/validation`, `/dependent-fields`, `/cascading`, `/files`, `/json-schema` (a live JSON
Schema editor with round-trip export), `/data-annotations`, `/fluent-validation` and `/custom-renderer`. Every page can
show its own source, and Ctrl+K (or `/`) searches them all. The site re-tints Borm's `--bit-borm-*` tokens to its own
light and dark theme, which is what [Theming](#theming) describes.

To run it locally:

```bash
dotnet run --project src/Utilities/Borm/Demo/Server
```

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
- [Combobox and tags](#combobox-and-tags)
- [Change handlers](#change-handlers)
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
with titles (with or without a `{"type":"null"}` branch, and with or without `$ref`s) becomes a labelled select. A
union of genuinely different *object* shapes describes a choice of subforms rather than a choice of values - it
needs a selector and a form that swaps with it - so it is left alone rather than bound to a branch the document
never committed to.

### Untrusted documents

Use `TryImport` when the schema comes from a user. Besides malformed JSON, it reports a document that is not a
schema, and one that goes past a budget no real form comes near: 10,000 fields, objects and arrays nested 32 levels
deep, `allOf` nested 64 levels deep, or 80,000 `allOf`/`$ref` merges. A few hundred bytes of `$ref`s can describe
millions of fields, and those documents are refused rather than built:

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
| `x-widget` | Force a control: `select`, `textarea`, `radio`, `combobox`, `multiselect`, `tags`, `range`, `switch`, `file`, `static`, `hidden`, `array`, … A document that names its control is taken at its word; one that does not gets the friendliest default for the shape it describes. |
| `x-allowCustom`, `x-enum` | A combobox that also accepts answers on no list. Its choices travel as `x-enum`, because `enum` means "one of these and nothing else" to every validator; a combobox with a plain `enum` is a closed choice and rejects anything else. |
| `x-renderer` | A custom renderer key registered with `RegisterCustom`. |
| `x-order`, `x-group`, `x-placeholder`, `x-autocomplete`, `x-inputMode`, `x-autofocus` | Field metadata. |
| `x-columns`, `x-colSpan` | Grid layout. |
| `x-prefix`, `x-suffix`, `x-showLabel`, `x-characterCount` | Presentation. |
| `x-updateOn`, `x-debounce` | When the value is written back. |
| `x-step` | The control's granularity, as distinct from a `multipleOf` constraint. |
| `x-accept`, `x-multiple`, `x-maxFileSize` | File upload constraints. |
| `x-visibleWhen`, `x-disabledWhen`, `x-readOnlyWhen`, `x-requiredWhen` | Conditions, as `{"field":…,"op":…,"value":…}` or `{"all":[…]}` / `{"any":[…]}`. `op` is an operator name (any case) and defaults to `Equals` when absent; a condition whose `op` names no operator, or a group with a member that cannot be read, is dropped as a whole rather than guessed at. |
| `x-revalidateOn` | Paths whose change revalidates this field - the other half of a cross-field rule. |
| `x-clearOnHide` | Clear the value when the field is hidden. |
| `x-attributes`, `x-inputAttributes` | Renderer hints and extra HTML attributes. |
| `x-steps` | Wizard steps, each with `id`, `title`, `fields` and an optional `visibleWhen`. |
| `enumNames`, `x-enumGroups`, `x-enumDisabled` | Labels matching the `enum` values, the `<optgroup>` each belongs to, and the values that may not be chosen. |
| `examples` | `<datalist>` suggestions. A tag list's go on `items` with its per-tag `minLength`, `maxLength` and `pattern`. |

Conditions, rules and renderer hints backed by code (`VisibleWhen(predicate)`, `Must`, `MustAsync`, `Computed`,
`OnChange`, `Normalize`, a delegate stashed in `Attributes`) have no JSON form and are omitted from the export
rather than approximated.

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
var draft = state.Snapshot();     // stash it in session storage
// … the user comes back …
state.Reset(draft);
```

`ValidateFieldAsync(path)` checks one answer without judging the whole form, and `GetFieldState(path)` answers
everything about one field at once:

```csharp
await state.ValidateFieldAsync("Customer");

var field = state.GetFieldState("Customer");
// field.IsTouched, field.IsDirty, field.IsInvalid, field.Error, field.Messages
```

Errors the server finds after a submit go back onto the fields they belong to with `SetServerError(path, message)`,
or `SetServerErrors(...)`, which replaces the form's messages with a whole batch from one failed request.

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
row's quantity. The untyped builder takes a context instead: `f.Computed(ctx => ctx.Sibling("width"), "width")`.

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

## Checking a schema

`Validate()` reports the mistakes that are easy to make and hard to see - duplicate field names, arrays with no item
template, steps naming fields that do not exist, conditions pointing nowhere, a comparison rule that never
revalidates, and settings a control can never honour. It is a development aid, never run automatically - a schema
being edited at runtime has every right to be incomplete:

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
| `OnFieldChanged` | Raised with the path of a field whose value changed. |
| `ReadOnly` | Renders every field read-only (review mode) and hides the buttons. Pressing Enter in a read-only form does not submit it. |
| `Disabled` | Disables every field and button - a save in flight, a locked record. Unlike `ReadOnly` the fields cannot be tabbed through or read out, so reach for it only when the form is genuinely inoperable. |
| `ShowSubmitButton` / `SubmitText` / `SubmittingText` | The built-in submit button (default `true` / `"Submit"`). While the form is validating or saving the button says so, rather than only going grey. |
| `ShowResetButton` / `ResetText` | A button restoring the values the form started with. |
| `ShowErrorSummary` | Lists every error above the form, each linking to its field - or to its step on a wizard. The links name the current page in full, so they work under any `<base href>`. |
| `FocusFirstError` | Moves focus to the first invalid field after a failed submit (default `true`; suppressed while `ShowErrorSummary` is on, since the summary takes focus itself). |
| `AllowStepNavigation` | Lets the user click back to a completed wizard step (default `true`). |
| `BackText` / `NextText` | Wizard navigation labels. |
| `Header` / `Footer` / `Actions` | Render fragments for extra content and a custom button row. |
| `Class` | Extra CSS classes for the `<form>` element. |
| `IdPrefix` | What every DOM id in the form starts with (default `borm_`). Give two forms on one page a prefix each. |
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
| `SetValues` / `Batch` | Write many values as one change - one re-render, not one per field. |
| `IsFormDirty`, `DirtyFields`, `TouchedFields` | Change tracking. Dirtiness is a comparison against the values the form opened with, so a field typed into and put back is clean again. |
| `ResetField(path)` | Puts one field - and anything nested beneath it - back to the value it started with. |
| `IsValidating`, `IsSubmitting`, `IsSubmitted`, `SubmitCount`, `IsValid`, `HasValidated` | Submission state. `IsValid` reports what validation has *found*, so check `HasValidated` before treating it as a verdict - a form nobody has validated has no errors. |
| `IsSubmitSuccessful` | Whether the last submit passed validation *and* its handler returned without throwing - what a "Saved." banner binds to. |
| `IsValidatingField(path)`, `ValidatingFields` | Whether an async rule is in flight for one field, so a remote uniqueness check can put a spinner beside its own box rather than declaring the whole form busy. |
| `GetFieldState(path)` | Touched, dirty, invalid, validating, the first error and every message for one field, in a single read. |
| `NormalizeField(field, path)` / `NormalizeAll()` | Run the schema's normalizers. The built-in controls call these for you on blur and on submit. |
| `SubmitAsync` | Marks everything touched, validates, and dispatches - ignoring re-entrant calls. |
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

## Hosting models

Borm has no JavaScript and no browser-only API, so it runs the same way under every Blazor hosting model.

- **Blazor WebAssembly**: register `AddBorm()` in `Program.cs` and link the stylesheet from `index.html`.
- **Blazor Server**: the same, in the server's `Program.cs` and `App.razor`. Every field that updates on input sends
  its keystrokes over the circuit, so prefer the default update-on-change, or a debounce, on a busy form.
- **Blazor Web App**: register `AddBorm()` in both the server and the client project - the page is prerendered on the
  server and then runs on the client, and both containers must resolve the renderer registry. The form prerenders as
  plain HTML and becomes interactive once the page does; give the page an interactive render mode, since a form in
  static server rendering cannot respond to input.
- **Blazor Hybrid**: register `AddBorm()` in `MauiProgram.cs` and link the stylesheet from `wwwroot/index.html`.

Generating a schema reads a model's properties by reflection, so `Bit.Borm` is not marked as trimmable: a trimmed
app keeps it whole, and the models it binds to are kept by the app that declares them.

## Key types

| Type | Purpose |
| --- | --- |
| `BormDefinition` | The schema: fields, wizard steps, form-level rules and metadata. `Clone()` for a shared schema you want to tailor, `Validate()` to check it. |
| `BormFieldDefinition` / `BormFieldType` | A single field and its logical type. |
| `BormBuilder` / `BormBuilder<TModel>` | Fluent schema builders. |
| `BormSchemaGenerator` | Reflection + DataAnnotations schema generation. |
| `BormJsonSchemaImporter` / `BormJsonSchemaExporter` | JSON Schema import and export. |
| `BormState` | Runtime state: data, validation, dirty/touched tracking, wizard position. |
| `BormView` / `BormField` | Render a whole schema, or one field of it inside your own layout. |
| `BormErrorSummary`, `BormValidationMessages` | The error summary and a field's message list, for a layout of your own. |
| `BormModelDataAccessor` / `BormDictionaryDataAccessor` | Data backing stores. |
| `IBormFieldRendererRegistry`, `BormInputBase` | Maps field types to renderer components, and the base class for your own. |
| `IBormMessageProvider`, `BormMessageKeys` | Supplies the text of every built-in message and label. |
| `BormConditionOperator`, `BormValidationTrigger`, `BormUpdateTrigger` | The vocabulary of conditions, validation timing and write-back timing. |

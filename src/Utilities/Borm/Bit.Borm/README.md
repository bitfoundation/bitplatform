# Bit.Borm

A schema-driven form engine for Blazor. Describe a form once - from a C# type, with a fluent builder or as a JSON
Schema document - and Borm renders it, validates it and keeps its state. Every decision is made in C#; the renderers
are plain, accessible HTML with a pluggable renderer registry, and there is no JavaScript at all.

## Quick start

```bash
dotnet add package Bit.Borm
```

```razor
@* _Imports.razor *@
@using Bit.Borm
```

Register the rendering services in every container that renders a form (both the server and the client of a Blazor
Web App), and link the stylesheet from your host page (`App.razor` or `index.html`); it is not injected for you.

```csharp
builder.Services.AddBorm();
```

```html
<link rel="stylesheet" href="_content/Bit.Borm/bit-borm.css" />
```

```razor
<BormView Definition="_definition" Model="_model" OnValidSubmit="Save" />

@code {
    private readonly Contact _model = new();

    // Or BormSchemaGenerator.Generate<Contact>() to read the model's DataAnnotations,
    // or BormJsonSchemaImporter.Import(json) to render a JSON Schema document.
    private readonly BormDefinition _definition = BormBuilder.For<Contact>()
        .Title("Contact us")
        .Field(x => x.Name, f => f.Required().Placeholder("Your name"))
        .Field(x => x.Email, f => f.AsEmail().Required().Trim())
        .Field(x => x.Company, f => f
            .VisibleWhen(nameof(Contact.IsBusiness), BormConditionOperator.IsTrue)
            .RequiredWhen(nameof(Contact.IsBusiness), BormConditionOperator.IsTrue)
            .ClearOnHide())
        .Build();

    private Task Save(BormState state) => Task.CompletedTask; // _model is populated and valid here
}
```

## Features

- Schemas from a C# type and its DataAnnotations, from a strongly typed fluent builder, or from a JSON Schema
  document (draft-07 to 2020-12, with `$ref`, `allOf`, `if`/`then`/`else`) - and export back to JSON Schema.
- Text, number, select, combobox, multi-select, radio, checkbox, switch, date and time, range, colour, file, tags,
  static headings, nested objects and repeating arrays.
- Built-in, custom, async, cross-field and form-level rules, `IValidatableObject`, and FluentValidation through
  the `Bit.Borm.FluentValidation` package.
- Validation on submit, blur or change; dependent revalidation; superseded and cancelled async rules.
- Conditional visibility, requiredness, disabled and read-only state - per row inside a repeater - for fields and
  wizard steps.
- Computed values, async and cascading options, change handlers and value normalisation.
- Multi-step wizards with per-step validation and a clickable stepper.
- Dirty and touched tracking, snapshots, resets and server errors.
- Labels, `aria-*` wiring, focus management, live announcements and an error summary out of the box.
- Theming through `--bit-borm-*` CSS custom properties, with light, dark and system colour modes, and every built-in
  string localizable through `IBormMessageProvider`.
- Blazor WebAssembly, Server, Web App (including prerendering) and Hybrid on .NET 8, 9 and 10.

## For AI agents

The demo site hosts an MCP server at `https://borm.bitplatform.dev/mcp`
that answers from the shipped library: its API, its guide, its theme properties and
the demo's working forms, and it can inspect a JSON Schema document you hand it.

## Documentation

The full guide - schemas from C# types, the fluent builder, JSON Schema, validation, conditions and wizards, options,
computed values, layout, custom renderers, theming, accessibility, localization and hosting models - is in the
[Borm README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/Borm). See it running at
[borm.bitplatform.dev](https://borm.bitplatform.dev).

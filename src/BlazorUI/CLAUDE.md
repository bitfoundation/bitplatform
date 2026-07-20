# bit BlazorUI

Guidance for working on the bit BlazorUI component library and its demo app.

Coding style comes from the `.editorconfig` at the root of the `src` folder, as described in
[../CLAUDE.md](../CLAUDE.md).

## Demo pages

Each component has a demo page under
`Demo/Client/Bit.BlazorUI.Demo.Client.Core/Pages/Components/<Category>/<Component>/`, built from a
`<Component>Demo.razor` page hosting one or more `DemoExample` sections. Multi-API components split
those sections across `BitPivotItem` tabs (`_..ItemDemo`, `_..CustomDemo`, `_..OptionDemo`), each with
its own `.razor`, `.razor.cs` and `.razor.samples.cs` files.

### Order of the demo sections

The generic, look-and-feel sections always come **last**, in exactly this order:

1. `Color`
2. `External Icons`
3. `Size`
4. `Style & Class`
5. `RTL`

Everything specific to the component comes before them, starting with `Basic`. The reason is that
these five sections are near-identical on every page and carry no information about what the
component actually does, so they must not sit between the sections that do.

When adding a new section, insert it after the last component-specific one and before `Color`, then
renumber the sections that follow.

### Numbering of the demo sections

The sections are numbered sequentially from 1, with no gaps, in the order they are rendered:

```razor
<DemoExample Title="Basic" RazorCode="@example1RazorCode" CsharpCode="@example1CsharpCode" Id="example1">
```

`Id="exampleN"` and the `exampleNRazorCode` / `exampleNCsharpCode` fields in the `.razor.samples.cs`
file must all carry the same `N`, and the fields must be declared in that same order. Reordering or
inserting a section means renumbering every section after it, in the `.razor` **and** the
`.razor.samples.cs`, and updating any `Href="#exampleN"` cross-references.

### Consistency across the tabs of a multi-API component

The tabs of a multi-API component are three views of the same feature set, so they must stay aligned:
the same sections, in the same order, with the same titles, demonstrating the same data (same labels,
same number of button groups per section). A reader comparing two tabs should see only the API
difference, never a difference in what is being demonstrated.

### The code samples must match what is rendered

`RazorCode` / `CsharpCode` are what a reader copies out of the page, so they have to reflect the
markup actually rendered in that section, including any parameter that was added or renamed.

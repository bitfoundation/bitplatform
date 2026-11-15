# Contributing to bit BlazorUI

The repository-wide guide, [CONTRIBUTING.md](../../CONTRIBUTING.md) at the root, covers issues, the workflow and the
coding style. This page covers what is particular to the component library: the conventions every component follows,
and where each one is written down.

## Where the rules live

The conventions are written down once, in [CLAUDE.md](CLAUDE.md) in this folder. The name says it is read by coding
agents, and it is; it is also the rule book for people, and nothing on this page restates it. When this page and
`CLAUDE.md` seem to disagree, `CLAUDE.md` wins, and the page is the one to fix.

Most rules are also pinned by a test in `Tests/Bit.BlazorUI.Tests`, so a change that breaks one fails CI with a
message that names the rule.

## The conventions by topic

| Topic | Rule in `CLAUDE.md` | Pinned by |
|---|---|---|
| The edit boundary: a change to the library stays inside `src/BlazorUI` | [Never edit outside this folder](CLAUDE.md#never-edit-outside-this-folder) | review |
| File encoding | [Every source file starts with a BOM](CLAUDE.md#every-source-file-starts-with-a-bom) | `SourceFilesBomTests` |
| Breaking the public API | [A public API break goes into CompatibilitySuppressions.xml](CLAUDE.md#a-public-api-break-goes-into-compatibilitysuppressionsxml) | the package validation of a Release build |
| Scripts and stylesheets | [The scripts and stylesheets are trimmed at publish](CLAUDE.md#the-scripts-and-stylesheets-are-trimmed-at-publish) | `Tests/Bit.BlazorUI.Tests.Trimming` |
| Razor comments | [Razor comments](CLAUDE.md#razor-comments) | review |
| Parameter names: one spelling per concept, no `Is` on a component's own state, the empty and no-results slots | [One name per concept](CLAUDE.md#one-name-per-concept) | `BitParameterNamingContractTests` (the `Is` rule) |
| CSS class names | [Every component has a root class of its own](CLAUDE.md#every-component-has-a-root-class-of-its-own) | `BitRootClassContractTests` |
| Composition | [Core components never render each other](CLAUDE.md#core-components-never-render-each-other) | review |
| The AI packages | [The AI packages](CLAUDE.md#the-ai-packages) | review |
| Escape | [One Escape does one thing](CLAUDE.md#one-escape-does-one-thing) | `BitEscapeClaimContractTests`, `BitSurfaceKeyPropagationContractTests` |
| Disabled | [A disabled component runs none of the page's activation handlers](CLAUDE.md#a-disabled-component-runs-none-of-the-pages-activation-handlers), [A disabled component says so the same way everywhere](CLAUDE.md#a-disabled-component-says-so-the-same-way-everywhere) | `BitDisabledActivationContractTests`, `BitDisabledMarkupContractTests` |
| Right to left | [Right-to-left rules work without `:dir()`](CLAUDE.md#right-to-left-rules-work-without-dir), [A direction Dir does not name is read, never assumed](CLAUDE.md#a-direction-dir-does-not-name-is-read-never-assumed) | `BitComponentDirFallbackTests` |
| Numbers in markup | [A number in markup reads the same in every culture](CLAUDE.md#a-number-in-markup-reads-the-same-in-every-culture) | `BitCultureInvariantMarkupContractTests` |
| Keys in bUnit tests | [A key pressed in a bUnit test is a bUnit `Key`](CLAUDE.md#a-key-pressed-in-a-bunit-test-is-a-bunit-key) | review |
| Demo pages | [Demo pages](CLAUDE.md#demo-pages), [The two page shells](CLAUDE.md#the-two-page-shells) | `DemoPageContractTests` |
| The MCP server | [The MCP server](CLAUDE.md#the-mcp-server) | `Tests/Bit.BlazorUI.Tests.Mcp` |
| Styling | [Theme tokens in component SCSS](CLAUDE.md#theme-tokens-in-component-scss) | the tests in `Tests/Bit.BlazorUI.Tests/Utils/Theme` |

Accessibility checks that a test can't make, by a person with a screen reader, are in
[docs/accessibility](docs/accessibility/README.md).

## Not written down yet

These have no rule of their own yet. Until they do, follow the closest existing component, and open an issue before
adding a public API that would set a new pattern:

- **Value binding:** the inputs bind through `BitInputBase`'s `Value` / `ValueChanged`; a component outside that base
  has its own bindable pairs.
- **Loading and busy states:** the parameter is spelled `Loading` (see [One name per concept](CLAUDE.md#one-name-per-concept)),
  but what a loading component shows, which interactions it blocks and what it announces is not set yet.
- **Event callbacks:** when a callback runs, whether it can cancel, and what it is passed.
- **Templates:** the empty and no-results slots are set (above); loading and error templates are not.

A rule for one of these is added to `CLAUDE.md`, with a test where one can check it, and then gets a row in the table
above.

## Before you open a pull request

- A behavior change comes with a test that fails without it: a bUnit test in `Tests/Bit.BlazorUI.Tests`, and an E2E
  test in `Tests/E2E` for what needs a browser (keyboard, focus, layout).
- A new or changed parameter is shown on the component's demo page and in its parameter table.
- A breaking change has an entry in [BreakingChanges.md](BreakingChanges.md), and an API break also a suppression (see
  above).

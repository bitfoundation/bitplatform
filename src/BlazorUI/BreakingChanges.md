# bit BlazorUI breaking changes

Breaking changes to the public API of the bit BlazorUI packages (`Bit.BlazorUI`, `Bit.BlazorUI.Extras`,
`Bit.BlazorUI.Assets`), newest version first, each with what to change in your code.

## vNext (after 10.6.2)

### Every anchor resolves a new tab's rel the same way ([#13475](https://github.com/bitfoundation/bitplatform/issues/13475))

The components that render an anchor from a `Target` (`BitLink`, `BitTag`, `BitButton`, `BitActionButton`,
`BitButtonGroup`, `BitBadge`, `BitPersona`, `BitCard`, and the items of `BitNav`, `BitNavBar`,
`BitBreadcrumb` and `BitMenuButton`) used to each have their own rule for the rel a new tab gets. They now
share one: a `Target` of `_blank`, matched case-insensitively as the browser does, adds `noopener` to
whatever `Rel` says, unless `Rel` already says what the opener relationship should be (`NoOpener`,
`NoReferrer` or `Opener`); no other target gets a rel, and `noreferrer` is never added on its own. The
items of the four navigation components gain the `Rel` they were missing (`BitNavItem.Rel`,
`BitNavOption.Rel`, `BitNavNameSelectors.Rel` and the same on the navbar, breadcrumb and menu button), and
the four components announce a new-tab item the way `BitLink` does, worded by their new `NewTabHint` and
taken off by `NoNewTabHint` (both also in their `Bit*Params`). Most links render as they did. These do
not:

| Component | Before | After |
|---|---|---|
| `BitButton`, `BitActionButton`, `BitCard` with `Rel="BitLinkRels.Opener"` and `Target="_blank"` | `rel="opener noopener"`: the opened page could not reach the opener | `rel="opener"`: **the opened page can reach `window.opener`** |
| `BitBadge`, `BitPersona`, `BitTag` with a `Rel` that does not mention the opener (`NoFollow`, `External`, ...) and `Target="_blank"` | `rel="nofollow"`: no `noopener` | `rel="nofollow noopener"` |
| `BitButton` with a `rel` passed through `@attributes` and `Target="_blank"` | `rel="noopener"`: the passed rel was dropped | the passed rel plus `noopener`, e.g. `rel="nofollow sponsored noopener"` |
| `BitNav`, `BitNavBar` item with an external url and `Target="_blank"` | `rel="noopener noreferrer"` | `rel="noopener"`: **the page the link opens now receives the Referer header** |
| `BitNav`, `BitNavBar` item with an in-app url and `Target="_blank"` | no rel | `rel="noopener"` |
| `BitNav`, `BitNavBar` item with an external url and any other target (`_self`, `_top`, a frame name) | `rel="noopener noreferrer"` | no rel: the referrer is sent, as from any other link |
| `BitBreadcrumb`, `BitMenuButton` item with `Target="_blank"` | `rel="noopener noreferrer"` | `rel="noopener"`: **the page the link opens now receives the Referer header** |
| `BitNav`, `BitNavBar`, `BitBreadcrumb`, `BitMenuButton` item with `Target="_blank"` | not announced | announced: "(opens in a new tab)" is read after its name |
| `Target="_BLANK"` (any casing) on `BitLink`, `BitButton`, `BitActionButton`, `BitBadge`, `BitPersona` and the `BitButtonGroup`, `BitMenuButton` and `BitBreadcrumb` items (`BitTag` and `BitCard` already matched it) | not treated as a new tab | treated as a new tab: hardened, and announced by `BitLink` |

`Opener` is the one way to ask for the opener back, so it is now honored everywhere. Modern browsers
already apply `noopener` to `_blank` on their own, so `Opener` exists only to give that up. **If an app
sets `Rel` to `Opener` on a `BitButton`, `BitActionButton` or `BitCard`, directly or through a `BitParams`
cascade, its new tabs can now reach and navigate the page that opened them.** Check that this is intended,
and remove `Opener` wherever it is not:

```razor
@* before: the Opener was overridden *@
<BitCard Href="https://example.com" Target="_blank" Rel="BitLinkRels.Opener | BitLinkRels.NoFollow" />

@* after: keep the hardening by not asking for the opener *@
<BitCard Href="https://example.com" Target="_blank" Rel="BitLinkRels.NoFollow" />
```

`noreferrer` is a privacy choice rather than a security one - it hides which page a link was followed from,
and with it the app from the analytics of the site it links to - so it is the app's to make. The
navigation components used to make it for every new-tab link they rendered; **an app that relied on
that has to ask for it now**, on the item:

```csharp
// before: noopener noreferrer came on its own
new BitNavItem { Text = "Partner", Url = "https://partner.example", Target = "_blank" }

// after: ask for noreferrer where the destination must not learn where the reader came from
new BitNavItem { Text = "Partner", Url = "https://partner.example", Target = "_blank", Rel = BitLinkRels.NoReferrer }
```

An app whose own text already says that an item opens a new tab sets `NoNewTabHint` on the component,
so the announcement is not made twice.

### The enabled state is a flag: `IsEnabled` is now `Disabled` / `IsDisabled` ([#5527](https://github.com/bitfoundation/bitplatform/issues/5527))

The enabled state of every component, item, option and name selector used to be `IsEnabled`, which
defaults to `true` and so had to be written as `IsEnabled="false"`. It is renamed to its opposite and its
meaning is inverted along with its name: it now defaults to `false`, so it is switched on by writing its
name alone (`<BitButton Disabled>`). Other boolean parameters are not affected by this change; those that
default to `true` (`AllowDisabledFocus`, `ShowValue`, `AutoClose`, ...) keep their names and defaults.

The renames follow one scheme:

| Kind of parameter | New name |
|---|---|
| The enabled state of a component | `Disabled` |
| The enabled state of an item, an option or a name selector | `IsDisabled`, beside the other `Is*` members (`IsHidden`, `IsExpanded`, ...) |

#### `IsEnabled` -> `Disabled` / `IsDisabled`

| Before | After |
|---|---|
| `BitComponentBase.IsEnabled` (every component) | `BitComponentBase.Disabled` |
| `BitComponentBaseParams.IsEnabled` (every `Bit*Params`) | `BitComponentBaseParams.Disabled` |
| `BitModalParameters.IsEnabled` | `BitModalParameters.Disabled` |
| `BitDataGridFilterContext.IsEnabled` | `BitDataGridFilterContext.Disabled` |
| `IsEnabled` of the items: `BitAccordionListItem`, `BitBreadcrumbItem`, `BitButtonGroupItem`, `BitChoiceGroupItem`, `BitDropdownItem`, `BitMenuButtonItem`, `BitNavItem`, `BitNavBarItem`, `BitTimelineItem`, `BitThemeSwitcherItem`, `BitDateRangePickerPreset` | `IsDisabled` |
| `IsEnabled` of the options: `BitAccordionListOption`, `BitBreadcrumbOption`, `BitButtonGroupOption`, `BitChoiceGroupOption`, `BitDropdownOption`, `BitMenuButtonOption`, `BitNavOption`, `BitNavBarOption`, `BitTimelineOption` | `IsDisabled` |
| `IsEnabled` of the name selectors: `BitAccordionListNameSelectors`, `BitBreadcrumbNameSelectors`, `BitButtonGroupNameSelectors`, `BitChoiceGroupNameSelectors`, `BitDropdownNameSelectors`, `BitMenuButtonNameSelectors`, `BitNavNameSelectors`, `BitNavBarNameSelectors`, `BitTimelineNameSelectors` | `IsDisabled` |

Markup:

```razor
@* before *@
<BitButton IsEnabled="false">Save</BitButton>
<BitButton IsEnabled="isValid">Save</BitButton>
<BitDropdownOption Text="Apple" Value="1" IsEnabled="false" />

@* after *@
<BitButton Disabled>Save</BitButton>
<BitButton Disabled="isValid is false">Save</BitButton>
<BitDropdownOption Text="Apple" Value="1" IsDisabled />
```

`IsEnabled="true"` is the default it always was; remove it.

An `IsEnabled` left on a component still compiles, since a component passes any attribute it does not
take through to its element, but it no longer disables anything: the component renders enabled. Search
the markup for `IsEnabled` before upgrading.

The option components (`BitDropdownOption`, `BitNavOption`, ...) take `IsDisabled`, like the items they
stand for, not the components' `Disabled`. They capture no unmatched attributes, so `Disabled` (or a
lowercase `disabled`) written on an option compiles but throws at runtime, as any unknown parameter would.

C#:

```csharp
// before
new BitDropdownItem<string> { Text = "Apple", Value = "1", IsEnabled = false };
new BitButtonParams { IsEnabled = false };
if (button.IsEnabled) { ... }

// after
new BitDropdownItem<string> { Text = "Apple", Value = "1", IsDisabled = true };
new BitButtonParams { Disabled = true };
if (button.Disabled is false) { ... }
```

**Custom item classes.** The name selector now reads a *disabled* flag, so its default property name is
`IsDisabled`, and a selector must return `true` for an item that is disabled:

```csharp
// before
NameSelectors = new() { IsEnabled = { Selector = c => c.Enabled } };
NameSelectors = new() { IsEnabled = { Name = nameof(MyItem.Enabled) } };

// after
NameSelectors = new() { IsDisabled = { Selector = c => c.Enabled is false } };
NameSelectors = new() { IsDisabled = { Name = nameof(MyItem.Disabled) } };   // a property that is true when disabled
```

A custom class with an `IsEnabled` property and no name selector was read through the default name
before; rename that property to `IsDisabled` and invert its values, or point a selector at it as above.
Its `IsEnabled` is no longer read, so left as it is, every item it marks off is selectable.

**A lowercase `disabled` attribute written in markup is now the parameter.** The Razor compiler matches
the attributes written on a component to its parameters regardless of case, so `disabled` written on a
bit component used to be passed through to the element as an HTML attribute and now sets `Disabled`,
disabling the component (class, `aria-disabled`, tab order and events). A `disabled` key that only
arrives at runtime - in an `@attributes` dictionary or a `DynamicComponent`'s `Parameters` - is matched
by its exact name, so it is still passed through to the element as a plain HTML attribute, without any of
that. To disable a component, use `Disabled` itself.

### Unified position, placement, shape, line-style and selection enums ([#13041](https://github.com/bitfoundation/bitplatform/issues/13041))

Branch `13041-blazorui-position-enums-unification` (#13041) retires the per-component enums that each
described "where", "what outline", "what stroke" or "how many selected" in its own words, and replaces
them with a small set of library-wide enums. Most parameters typed with one of the retired enums were
also renamed from `...Position` / `...Location` to `...Placement`, so both the type and the parameter
name change at every call site.

Everything listed here is a compile-time break unless it is marked **behavior** or **CSS**.

#### The new shared enums

| Enum | Values | Replaces |
|---|---|---|
| `BitPlacement` | `Top`, `Bottom`, `Start`, `End`, `Left`, `Right`, `Center`, `TopAndBottom`, `StartAndEnd` | every one-axis position/side/alignment enum below |
| `BitPosition` (existing, values unchanged) | the 3x3 grid: `TopLeft` ... `BottomEnd` | `BitDialogPosition`, `BitSnackBarPosition` |
| `BitShape` | `Rounded`, `Square`, `Pill`, `Circle` | `BitBadgeShape`, `BitPersonaShape`, `BitTagShape`, `BitShimmerShape` |
| `BitLineStyle` | `Solid`, `Dashed`, `Dotted`, `Double` | `BitSeparatorLineStyle`, `BitTimelineLineVariant` |
| `BitSelectionMode` | `None`, `Single`, `Multiple` | `BitButtonGroupSelectionMode`, `BitDataGridSelectionMode` |

Existing enums that now also absorb retired ones: `BitTextAlign` (DataGrid column and Markdown table
alignment), `BitScrollAlignment` (BitVirtualize), `BitScrollSnapAlign` (BitSwiper).

`Start` / `End` are logical (follow the reading direction); `Left` / `Right` are physical (stay put in
RTL). Not every parameter honours every `BitPlacement` / `BitShape` value; an unsupported value falls
back to that parameter's default.

**Numeric values changed.** The member order of the new enums differs from the retired ones (for
example `BitCarouselDotsPosition.Bottom` was `0`, `BitPlacement.Top` is `0`; `BitSwipeDirection.Right`
was `0`). Code that casts these enums to or from `int`, or persists them numerically, must be updated.

#### Removed types

##### Bit.BlazorUI

| Removed | Use instead |
|---|---|
| `BitIconPosition` | `BitPlacement` (`Start`, `End`) |
| `BitLabelPosition` | `BitPlacement` (`Top`, `Bottom`, `Start`, `End`) |
| `BitPanelPosition` | `BitPlacement` (`Start`, `End`, `Top`, `Bottom`; now also `Left`, `Right`) |
| `BitIconLocation` | `BitPlacement` (`Left` -> `Start`, `Right` -> `End`) |
| `BitButtonGroupSelectionMode` | `BitSelectionMode` |
| `BitCarouselDotsPosition` | `BitPlacement` |
| `BitSwiperSnap` | `BitScrollSnapAlign` |
| `BitTimelineDotAlignment` | `BitPlacement` |
| `BitTimelineLinePosition` | `BitPlacement` |
| `BitTimelineLineVariant` | `BitLineStyle` |
| `BitPivotPosition` | `BitPlacement` |
| `BitBadgeShape` | `BitShape` (`Circular` -> `Pill`) |
| `BitPersonaShape` | `BitShape` (`Circular` -> `Pill`) |
| `BitTagShape` | `BitShape` (`Circular` -> `Pill`) |
| `BitShimmerShape` | `BitShape` (same value names) |
| `BitSnackBarPosition` | `BitPosition` (same value names) |
| `BitProgressGapPosition` | `BitPlacement` |
| `BitCalloutAlignment` | `BitPlacement` |
| `BitCalloutSide` | `BitPlacement` |
| `BitDialogPosition` | `BitPosition` (same value names) |
| `BitTooltipPosition` | `BitPlacement` for `Placement` + `BitPlacement` for the new `Alignment` (see BitTooltip) |
| `BitSeparatorAlignContent` | `BitPlacement` |
| `BitSeparatorLineStyle` | `BitLineStyle` |
| `BitStickyPosition` | `BitPlacement` (same value names) |
| `BitSwipeDirection` | `BitPlacement` (`Top`, `Bottom`, `Left`, `Right`) |

##### Bit.BlazorUI.Extras

| Removed | Use instead |
|---|---|
| `BitChartAlign` | `BitPlacement` |
| `BitChartPosition` | `BitPlacement` (the reserved `Chart` value is gone) |
| `BitDataGridColumnAlign` | `BitTextAlign` (see BitDataGrid - `Right` maps to `End`) |
| `BitDataGridPagerPosition` | `BitPlacement` (`Bottom`, `Top`, `TopAndBottom`) |
| `BitDataGridSelectionMode` | `BitSelectionMode` |
| `BitMapTooltipDirection` | `BitPlacement?` (`Auto` -> `null`) |
| `BitNavPanelPosition` | `BitPlacement` |
| `BitVirtualizeScrollAlignment` | `BitScrollAlignment` (`Auto` -> `Nearest`) |
| `BitMarkdownColumnAlignment` | `BitTextAlign?` (`None` -> `null`) |

#### Renamed and retyped parameters

Each row applies to the component's `[Parameter]` **and** to its `Bit<Component>Params` cascading
counterpart (as nullable), unless noted.

##### Bit.BlazorUI

| Component | Before | After |
|---|---|---|
| BitAccordion | `ExpanderIconPosition` (`BitIconPosition?`) | `ExpanderIconPlacement` (`BitPlacement?`) |
| BitActionButton | `IconPosition` (`BitIconPosition?`) | `IconPlacement` (`BitPlacement?`) |
| BitButton | `IconPosition` (`BitIconPosition?`) | `IconPlacement` (`BitPlacement?`) |
| BitButton | `LoadingLabelPosition` (`BitLabelPosition`) | `LoadingLabelPlacement` (`BitPlacement`, default `End`) |
| BitToggleButton | `IconPosition` (`BitIconPosition?`) | `IconPlacement` (`BitPlacement?`) |
| BitToggleButton | `LoadingLabelPosition` (`BitLabelPosition`) | `LoadingLabelPlacement` (`BitPlacement`, default `End`) |
| BitButtonGroup | `SelectionMode` (`BitButtonGroupSelectionMode?`) | `SelectionMode` (`BitSelectionMode?`) - type only |
| BitCheckbox | `LabelPosition` (`BitLabelPosition?`) | `LabelPlacement` (`BitPlacement?`) |
| BitChoiceGroup | `LabelPosition` (`BitLabelPosition?`) | `LabelPlacement` (`BitPlacement?`) |
| BitNumberField | `LabelPosition` (`BitLabelPosition?`) | `LabelPlacement` (`BitPlacement?`) |
| BitRating | `LabelPosition` (`BitLabelPosition?`) | `LabelPlacement` (`BitPlacement?`) |
| BitTextField | `LabelPosition` (`BitLabelPosition?`) | `LabelPlacement` (`BitPlacement?`) |
| BitTextField | `IconPosition` (`BitIconPosition?`) | `IconPlacement` (`BitPlacement?`) |
| BitToggle | `LabelPosition` (`BitLabelPosition?`) | `LabelPlacement` (`BitPlacement?`) |
| BitLoading* (all loaders, via `BitLoadingBase` / `BitLoadingParams`) | `LabelPosition` (`BitLabelPosition?`) | `LabelPlacement` (`BitPlacement?`) |
| BitCircularTimePicker | `IconLocation` (`BitIconLocation`, default `Right`) | `IconPlacement` (`BitPlacement`, default `End`) |
| BitDatePicker | `IconLocation` (`BitIconLocation`, default `Right`) | `IconPlacement` (`BitPlacement`, default `End`) |
| BitDateRangePicker | `IconLocation` (`BitIconLocation`, default `Right`) | `IconPlacement` (`BitPlacement`, default `End`) |
| BitTimePicker | `IconLocation` (`BitIconLocation`, default `Right`) | `IconPlacement` (`BitPlacement`, default `End`) |
| BitFileUpload | `LabelIconPosition` (`BitIconPosition?`) | `LabelIconPlacement` (`BitPlacement?`) |
| BitCarousel | `DotsPosition` (`BitCarouselDotsPosition?`) | `DotsPlacement` (`BitPlacement?`) |
| BitSwiper | `Snap` (`BitSwiperSnap?`) | `SnapAlign` (`BitScrollSnapAlign?`) |
| BitTimeline | `DotAlignment` (`BitTimelineDotAlignment?`) | `DotAlignment` (`BitPlacement?`) - type only |
| BitTimeline | `LinePosition` (`BitTimelineLinePosition?`) | `LinePlacement` (`BitPlacement?`) |
| BitTimeline | `LineVariant` (`BitTimelineLineVariant?`) | `LineStyle` (`BitLineStyle?`) |
| BitTimelineItem / BitTimelineOption | `LineVariant` (`BitTimelineLineVariant?`) | `LineStyle` (`BitLineStyle?`) |
| BitTimelineNameSelectors | `LineVariant` | `LineStyle` (`BitNameSelectorPair<TItem, BitLineStyle?>`) |
| BitBreadcrumb | `IconPosition` (`BitIconPosition?`) | `IconPlacement` (`BitPlacement?`) |
| BitBreadcrumbItem / BitBreadcrumbOption | `IconPosition` (`BitIconPosition?`) | `IconPlacement` (`BitPlacement?`) |
| BitBreadcrumbNameSelectors | `IconPosition` | `IconPlacement` (`BitNameSelectorPair<TItem, BitPlacement?>`) |
| BitDropMenu | `Side` (`BitCalloutSide?`) | `Placement` (`BitPlacement?`) |
| BitDropMenu | `Alignment` (`BitCalloutAlignment?`) | `Alignment` (`BitPlacement?`) - type only |
| BitDropMenu | `PanelPosition` (`BitPanelPosition?`) | `PanelPlacement` (`BitPlacement?`) |
| BitCallout | `Side` (`BitCalloutSide?`) | `Placement` (`BitPlacement?`) |
| BitCallout | `Alignment` (`BitCalloutAlignment?`) | `Alignment` (`BitPlacement?`) - type only |
| BitCallout | `PanelPosition` (`BitPanelPosition?`) | `PanelPlacement` (`BitPlacement?`) |
| BitPivot | `Position` (`BitPivotPosition?`) | `Placement` (`BitPlacement?`) |
| BitBadge | `Shape` (`BitBadgeShape?`) | `Shape` (`BitShape?`) - type only |
| BitPersona | `Shape` (`BitPersonaShape?`) | `Shape` (`BitShape?`) - type only |
| BitTag | `Shape` (`BitTagShape?`) | `Shape` (`BitShape?`) - type only |
| BitShimmer | `Shape` (`BitShimmerShape?`) | `Shape` (`BitShape?`) - type only |
| BitSnackBar | `Position` (`BitSnackBarPosition?`) | `Position` (`BitPosition?`) - type only |
| BitProgress | `GapPosition` (`BitProgressGapPosition`) | `GapPlacement` (`BitPlacement`, default `Bottom`) |
| BitDialog | `Position` (`BitDialogPosition`) | `Position` (`BitPosition`, default `Center`) - type only |
| BitPanel | `Position` (`BitPanelPosition?`) | `Placement` (`BitPlacement?`) |
| BitTooltip | `Position` (`BitTooltipPosition`) | `Placement` (`BitPlacement`, default `Top`) + new `Alignment` (`BitPlacement`, default `Center`) |
| BitTooltip | `MirrorInRtl` (`bool`) | **removed** - use the logical `Start` / `End` values instead |
| BitLink | `IconPosition` (`BitIconPosition?`) | `IconPlacement` (`BitPlacement?`) |
| BitSeparator | `AlignContent` (`BitSeparatorAlignContent?`) | `AlignContent` (`BitPlacement?`) - type only |
| BitSeparator | `LineStyle` (`BitSeparatorLineStyle?`) | `LineStyle` (`BitLineStyle?`) - type only |
| BitSticky | `Position` (`BitStickyPosition?`) | `Placement` (`BitPlacement?`) |
| BitSwipeTrapTriggerArgs | `Direction` (`BitSwipeDirection`) | `Direction` (`BitPlacement`) - type only |

##### Bit.BlazorUI.Extras

| Component / model | Before | After |
|---|---|---|
| BitAccordionList | `ExpanderIconPosition` (`BitIconPosition?`) | `ExpanderIconPlacement` (`BitPlacement?`) |
| BitDataGrid | `SelectionMode` (`BitDataGridSelectionMode`) | `SelectionMode` (`BitSelectionMode`) - type only |
| BitDataGrid | `PagerPosition` (`BitDataGridPagerPosition`) | `PagerPlacement` (`BitPlacement`, default `Bottom`) |
| BitDataGridColumn | `Align` (`BitDataGridColumnAlign`, default `Left`) | `Align` (`BitTextAlign`, default `Start`) |
| BitMapMarker | `TooltipDirection` (`BitMapTooltipDirection`, default `Auto`) | `TooltipPlacement` (`BitPlacement?`, `null` = auto) |
| BitNavPanel | `Position` (`BitNavPanelPosition`, non-nullable) | `Placement` (`BitPlacement?`) |
| BitVirtualize | `ScrollToIndexAsync(int, BitVirtualizeScrollAlignment, bool)` | `ScrollToIndexAsync(int, BitScrollAlignment, bool)` |
| BitChartLegendOptions | `Position` (`BitChartPosition`), `Align` (`BitChartAlign`) | `Placement` (`BitPlacement`), `Align` (`BitPlacement`) |
| BitChartTitleOptions | `Position` (`BitChartPosition`), `Align` (`BitChartAlign`) | `Placement` (`BitPlacement`), `Align` (`BitPlacement`) |
| BitChartScaleOptions | `Position` (`BitChartPosition?`) | `Placement` (`BitPlacement?`) |
| BitChartTickOptions | `Align` (`BitChartAlign`) | `Align` (`BitPlacement`) - type only |
| BitChartDataLabelOptions | `Anchor`, `Align` (`BitChartAlign`) | `Anchor`, `Align` (`BitPlacement`) - type only |
| BitChartTooltipOptions | `TitleAlign`, `BodyAlign` (`BitChartAlign`) | `TitleAlign`, `BodyAlign` (`BitPlacement`) - type only |
| BitChartLegendModel / BitChartTitleModel | `Position`, `Align` | `Placement`, `Align` (`BitPlacement`) |
| BitMarkdownTableNode | `Alignments` (`List<BitMarkdownColumnAlignment>`) | `Alignments` (`List<BitTextAlign?>`) |

#### Value mappings that are not one-to-one

- **Shapes**: `Circular` (Badge, Persona, Tag) is now `BitShape.Pill`. BitTag also renders `Circle` as
  a pill. BitPersona's default without `Squared` is `Pill`, and `Squared` still means `Rounded`.
- **Date/time picker icons**: `BitIconLocation.Left` -> `BitPlacement.Start`,
  `BitIconLocation.Right` -> `BitPlacement.End`.
- **BitDataGridColumn.Align** (**behavior**): the old `Left` / `Right` were already drawn logically
  (`flex-end` / `text-align: end`), so they map to `BitTextAlign.Start` / `BitTextAlign.End`.
  `BitTextAlign.Left` / `Right` are now **physical** and stay put in RTL - a column migrated as
  `Right` -> `Right` instead of `Right` -> `End` will no longer flip in RTL.
- **BitMapMarker**: `BitMapTooltipDirection.Auto` -> leave `TooltipPlacement` `null`. Any
  `BitPlacement` value other than `Top`, `Bottom`, `Left`, `Right` or `Center` also means auto.
- **BitVirtualize**: `BitVirtualizeScrollAlignment.Auto` -> `BitScrollAlignment.Nearest`.
- **BitMarkdown**: `BitMarkdownColumnAlignment.None` -> `null`.
- **BitChart**: `BitChartPosition.Chart` (reserved, never placed anything) is gone. Titles and legends
  placed `Left` / `Right` are now physical and keep their side in an RTL chart (**behavior**).
- **BitTooltip** (**behavior**): the twelve corner values are now a side (`Placement`) plus an alignment
  along that side (`Alignment`). The new alignment lines the tooltip's edge up with the **same** edge of
  the anchor and lets it grow away from it, whereas the old corner values let the tooltip overhang the
  anchor past the named corner, so the closest equivalents are:

  | Old `Position` | `Placement` | `Alignment` |
  |---|---|---|
  | `Top` / `Bottom` / `Left` / `Right` | same name | `Center` (default) |
  | `TopLeft` / `BottomLeft` | `Top` / `Bottom` | `Right` |
  | `TopRight` / `BottomRight` | `Top` / `Bottom` | `Left` |
  | `RightTop` / `LeftTop` | `Right` / `Left` | `Bottom` |
  | `RightBottom` / `LeftBottom` | `Right` / `Left` | `Top` |

  `MirrorInRtl="true"` is replaced by using `Start` / `End` for the `Placement` and/or `Alignment`, which
  follow the reading direction.
- **BitSwipeTrap**: `BitSwipeTrapTriggerArgs.Direction` reports `BitPlacement.Top/Bottom/Left/Right`;
  code comparing against `BitSwipeDirection.*` or its numeric values must switch.

#### New values that are now accepted (not breaking, listed for completeness)

- BitPanel, BitCallout / BitDropMenu panel mode: `Left` / `Right` (physical) besides `Start` / `End`.
- BitSnackBar: the full `BitPosition` grid (`TopLeft`, `TopRight`, `Center*`, `BottomLeft`,
  `BottomRight`), not only the six `Top*` / `Bottom*` start/center/end values.
- BitTooltip: logical `Start` / `End` sides and alignments.
- BitDataGridColumn: physical `Left` / `Right` besides logical `Start` / `End`.

#### CSS (for apps that style the generated class names)

- **BitTooltip**: the corner classes `bit-ttp-tlf`, `bit-ttp-trg`, `bit-ttp-rtp`, `bit-ttp-rbm`,
  `bit-ttp-brg`, `bit-ttp-blf`, `bit-ttp-lbm`, `bit-ttp-ltp` are gone. The root now carries one side class
  (`bit-ttp-top`, `-btm`, `-lft`, `-rgt`, `-sta`, `-end`) plus one alignment class (`bit-ttp-ahc`, `-ast`,
  `-aen`, `-alf`, `-arg` for top/bottom; `bit-ttp-avc`, `-atp`, `-abm` for left/right/start/end).
- **BitDataGrid**: the end-aligned cell class `bit-dtg-right` was renamed `bit-dtg-end`; `bit-dtg-right`
  (and new `bit-dtg-left`) now mean physical alignment.
- New classes, no removals: BitPanel `bit-pnl-left` / `bit-pnl-right`, BitCallout `bit-clo-lft` /
  `bit-clo-rgt`, BitDropMenu `bit-drm-lft` / `bit-drm-rgt`, BitSnackBar `bit-snb-tlf`, `-trg`, `-blf`,
  `-brg`, `-cst`, `-cen`, `-clf`, `-crg`, `-ctr`, BitChart `bit-cht-ttl-l`.

#### Internal-but-public members

- `BitSwipeTrap._OnKeyTrigger` (a `[JSInvokable]` callback) now takes a `string` instead of
  `BitSwipeDirection`. It is called only by the library's own script.

#### Known consumers in this repo that break

The Boilerplate template (`src/Templates/Boilerplate`) is outside the BlazorUI edit boundary and was
**not** updated. These lines will fail to compile against this branch:

| File | Line | Use |
|---|---|---|
| `Boilerplate.Client.Core/Components/Layout/AppConsentBanner.razor` | 12 | `Position="BitPanelPosition.Bottom"` -> `Placement="BitPlacement.Bottom"` |
| `Boilerplate.Client.Core/Components/Layout/AppSnackBar.razor` | 7 | `BitSnackBarPosition.TopCenter` -> `BitPosition.TopCenter` |
| `Boilerplate.Client.Core/Components/Pages/Categories/CategoriesPage.razor` | 58 | `BitDataGridColumnAlign.Center` -> `BitTextAlign.Center` |
| `Boilerplate.Client.Core/Components/Pages/Dashboard/ProductsPercentageWidget.razor.cs` | 19 | `Position = BitChartPosition.Right` -> `Placement = BitPlacement.Right` |
| `Boilerplate.Client.Core/Components/Pages/Management/OperationsPage.razor` | 217 | `BitTagShape.Circular` -> `BitShape.Pill` |
| `Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor` | 60 | `BitDataGridColumnAlign.Right` -> `BitTextAlign.End` |
| `Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor` | 67 | `BitDataGridColumnAlign.Center` -> `BitTextAlign.Center` |
| `Boilerplate.Client.Core/Components/Pages/Tenants/ManageAllTenantsPage.razor` | 49 | `BitDataGridColumnAlign.Center` -> `BitTextAlign.Center` |

(Paths are relative to `src/Templates/Boilerplate/Bit.Boilerplate/src/Client/`.)

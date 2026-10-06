# Breaking changes - unified position, placement, shape, line-style and selection enums

Branch `13041-blazorui-position-enums-unification` (#13041) retires the per-component enums that each
described "where", "what outline", "what stroke" or "how many selected" in its own words, and replaces
them with a small set of library-wide enums. Most parameters typed with one of the retired enums were
also renamed from `...Position` / `...Location` to `...Placement`, so both the type and the parameter
name change at every call site.

Everything listed here is a compile-time break unless it is marked **behavior** or **CSS**.

## The new shared enums

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

## Removed types

### Bit.BlazorUI

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

### Bit.BlazorUI.Extras

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

## Renamed and retyped parameters

Each row applies to the component's `[Parameter]` **and** to its `Bit<Component>Params` cascading
counterpart (as nullable), unless noted.

### Bit.BlazorUI

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

### Bit.BlazorUI.Extras

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

## Value mappings that are not one-to-one

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

## New values that are now accepted (not breaking, listed for completeness)

- BitPanel, BitCallout / BitDropMenu panel mode: `Left` / `Right` (physical) besides `Start` / `End`.
- BitSnackBar: the full `BitPosition` grid (`TopLeft`, `TopRight`, `Center*`, `BottomLeft`,
  `BottomRight`), not only the six `Top*` / `Bottom*` start/center/end values.
- BitTooltip: logical `Start` / `End` sides and alignments.
- BitDataGridColumn: physical `Left` / `Right` besides logical `Start` / `End`.

## CSS (for apps that style the generated class names)

- **BitTooltip**: the corner classes `bit-ttp-tlf`, `bit-ttp-trg`, `bit-ttp-rtp`, `bit-ttp-rbm`,
  `bit-ttp-brg`, `bit-ttp-blf`, `bit-ttp-lbm`, `bit-ttp-ltp` are gone. The root now carries one side class
  (`bit-ttp-top`, `-btm`, `-lft`, `-rgt`, `-sta`, `-end`) plus one alignment class (`bit-ttp-ahc`, `-ast`,
  `-aen`, `-alf`, `-arg` for top/bottom; `bit-ttp-avc`, `-atp`, `-abm` for left/right/start/end).
- **BitDataGrid**: the end-aligned cell class `bit-dtg-right` was renamed `bit-dtg-end`; `bit-dtg-right`
  (and new `bit-dtg-left`) now mean physical alignment.
- New classes, no removals: BitPanel `bit-pnl-left` / `bit-pnl-right`, BitCallout `bit-clo-lft` /
  `bit-clo-rgt`, BitDropMenu `bit-drm-lft` / `bit-drm-rgt`, BitSnackBar `bit-snb-tlf`, `-trg`, `-blf`,
  `-brg`, `-cst`, `-cen`, `-clf`, `-crg`, `-ctr`, BitChart `bit-cht-ttl-l`.

## Internal-but-public members

- `BitSwipeTrap._OnKeyTrigger` (a `[JSInvokable]` callback) now takes a `string` instead of
  `BitSwipeDirection`. It is called only by the library's own script.

## Known consumers in this repo that break

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

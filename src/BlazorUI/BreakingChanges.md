# bit BlazorUI breaking changes

Breaking changes to the public API of the bit BlazorUI packages (`Bit.BlazorUI`, `Bit.BlazorUI.Extras`,
`Bit.BlazorUI.AI`, `Bit.BlazorUI.Assets`), newest version first, each with what to change in your code.

**A name removed or renamed since 10.5.0 fails your build** once you upgrade: the `Bit.BlazorUI` package ships an
analyzer that reports every one your code still uses - a parameter on a component (`BITBC001`), a component's tag or a
type (`BITBC002`) - at the line it is written on, with what to write instead. Without it most of them would build
silently: a component renders an attribute it does not take as an HTML attribute, and the setting does nothing. Set
`<BitBlazorUIBreakingChangeAnalyzer>false</BitBlazorUIBreakingChangeAnalyzer>` in the project file to turn it off, or
change one rule's severity in a `.globalconfig` beside the project file (`is_global = true`, then
`dotnet_diagnostic.BITBC001.severity = warning`), or silence it with `<NoWarn>$(NoWarn);BITBC001</NoWarn>`. An
`.editorconfig` cannot do it for the markup: those errors point into the `.razor` file, which no `.editorconfig` section
applies to. An AI agent connected
to the bit BlazorUI MCP server gets the same list from `GetBitBlazorUIBreakingChanges`, and the `upgrade-bit-blazorui`
prompt walks it through an upgrade. What changed without a name changing - markup, CSS classes, defaults, behaviour - is
only in this file.

## 11.0.0

### `BitRichTextEditor` keeps its own undo history ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

Undo and redo no longer go to the browser's native stack, which cannot follow the changes the editor makes to its
content itself. The editor keeps its own history, and `UndoAsync`, `RedoAsync`, Ctrl+Z, Ctrl+Y and Ctrl+Shift+Z, the
toolbar's buttons and the browser's own undo (its Edit menu, a gesture) all go to it. Nothing is renamed. What an app
can notice:

- **Typing is undone a word at a time.** A step ends at a space or punctuation, after a pause of over 600 ms, and where
  typing turns to deleting. Each command, paste, input rule or IME composition is a step of its own. An input rule
  (`**bold**`, `# `) is undone back to the text as it was typed.
- **The toolbar's Undo and Redo are disabled while there is nothing to undo or redo** (`disabled` and
  `aria-disabled="true"`, like every disabled control). A test that clicks Undo on an editor nobody has edited finds
  it disabled. `CanUndo` and `CanRedo` say the same in C#.
- **A value set from .NET before the first edit is where the history starts**, so it cannot be undone into an empty
  editor. A value set after an edit is a step that can be undone, whether or not the editor has the focus. Before,
  only a value set while the editor had the focus could be undone.
- **The history keeps 200 steps**, and drops the oldest past 25 million characters of content in all.

### `AddBitBlazorUIExtrasServices` takes a `commandPalette` argument ([#15465](https://github.com/bitfoundation/bitplatform/issues/15465))

`AddBitBlazorUIExtrasServices` registers the services of `BitCommandPalette` (its registry, open state and history) along
with the rest of the Extras package, and takes the palette's app-wide `BitCommandPaletteOptions` through a new optional
last parameter:

```csharp
builder.Services.AddBitBlazorUIExtrasServices(commandPalette: options => options.GroupOrder.AddRange(["Navigation", "Actions"]));
```

The source of an existing call compiles as it is, but the method's signature changed: a library compiled against the
previous `Bit.BlazorUI.Extras` that calls it fails with a `MissingMethodException` until it is recompiled.

### `BitResourceTimeline`'s bar texts are one `Texts` with eight positions, and its `Texts` is `Strings` ([#15441](https://github.com/bitfoundation/bitplatform/issues/15441))

`BitResourceTimelineAllocation` labelled a bar through four strings, one per side. They are now the properties of one
`Texts` (a `BitResourceTimelineBarTexts`), which adds a text at the start and the end edge above and below the bar
(`TopStart`, `TopEnd`, `BottomStart`, `BottomEnd`). Each position is a `BitResourceTimelineBarText` with its own
`Text`, `Color`, `Font`, `OffsetX` and `OffsetY`, and a string converts to one implicitly:

| Before | After |
|---|---|
| `TextAbove = "LH441"` | `Texts = new() { TopCenter = "LH441" }` |
| `TextBelow = "Gate 5"` | `Texts = new() { BottomCenter = "Gate 5" }` |
| `TextStart = "08:00"` | `Texts = new() { Start = "08:00" }` |
| `TextEnd = "16:00"` | `Texts = new() { End = "16:00" }` |
| `bar.TextAbove` (read) | `bar.Texts?.TopCenter?.Text` |

The four texts are drawn where they were. The label a bar's default tooltip and accessible name read is the first text
of `TopCenter`, `TopStart`, `TopEnd`, `Start`, `End`, `BottomCenter`, `BottomStart` and `BottomEnd`, which is the
order the old four were read in.

So the bar texts and the words of the timeline itself are not both called texts, the component's `Texts` parameter
(and its counterpart on `BitResourceTimelineParams`) is `Strings`, and its type `BitResourceTimelineTexts` is
`BitResourceTimelineStrings`, with the same members:

| Before | After |
|---|---|
| `<BitResourceTimeline Texts="texts" />` | `<BitResourceTimeline Strings="strings" />` |
| `new BitResourceTimelineTexts { Loading = "..." }` | `new BitResourceTimelineStrings { Loading = "..." }` |

The analyzer reports a `Texts` left on the component and the four removed allocation properties (`BITBC001`), and the
old type (`BITBC002`). With it turned off, a `Texts` left in markup builds and is rendered as an HTML attribute, so the
timeline speaks English again.

### The words a component writes for itself are `Strings` everywhere ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

The object holding every word a component writes for itself - the one that localizes it - is called `Strings`, as it is
on `BitDataGrid` and `BitResourceTimeline`. The components that called it `Texts` are renamed to match: the parameter
(and its counterpart on the component's `Bit*Params`) is `Strings`, and its type is `Bit*Strings`, with the same
members. The Markdown pipeline follows: `BitMarkdownPipelineBuilder.Texts`, `BitMarkdownPipeline.Texts` and
`BitMarkdownRenderer.Texts` are `Strings`, `UseTexts()` is `UseStrings()`, and `BitMarkdownTexts` is
`BitMarkdownStrings`.

| Before | After |
|---|---|
| `<BitChart Texts="texts" />`, `new BitChartTexts { ... }` | `<BitChart Strings="strings" />`, `new BitChartStrings { ... }` |
| `<BitColorPicker Texts="texts" />`, `new BitColorPickerTexts { ... }` | `<BitColorPicker Strings="strings" />`, `new BitColorPickerStrings { ... }` |
| `<BitFullCalendar Texts="texts" />`, `new BitFullCalendarTexts { ... }` | `<BitFullCalendar Strings="strings" />`, `new BitFullCalendarStrings { ... }` |
| `<BitImageViewer Texts="texts" />`, `new BitImageViewerTexts { ... }` | `<BitImageViewer Strings="strings" />`, `new BitImageViewerStrings { ... }` |
| `<BitMarkdownEditor Texts="texts" />`, `new BitMarkdownEditorTexts { ... }` | `<BitMarkdownEditor Strings="strings" />`, `new BitMarkdownEditorStrings { ... }` |
| `<BitPdfViewer Texts="texts" />`, `new BitPdfViewerTexts { ... }` | `<BitPdfViewer Strings="strings" />`, `new BitPdfViewerStrings { ... }` |
| `new BitChartParams { Texts = ... }` (and the other `Bit*Params`) | `new BitChartParams { Strings = ... }` |
| `builder.UseTexts(new BitMarkdownTexts { ... })` | `builder.UseStrings(new BitMarkdownStrings { ... })` |
| `builder.Texts.AlertNote = "..."`, `pipeline.Texts`, `renderer.Texts` | `builder.Strings.AlertNote = "..."`, `pipeline.Strings`, `renderer.Strings` |
| `new BitMarkdownRenderer(renderers, texts)` | `new BitMarkdownRenderer(renderers, strings)` (only the parameter's name changed) |

The analyzer reports a `Texts` left on any of these components, their `Bit*Params` and the Markdown pipeline types, and a
`UseTexts()` call (`BITBC001`), and the old types (`BITBC002`). With it turned off, a `Texts` left in markup builds and
is rendered as an HTML attribute, so the component speaks English again.

### `BitCard` draws on its root's `::after` ([#15380](https://github.com/bitfoundation/bitplatform/issues/15380))

The accent bar of the new `Accent` parameter and `--bit-Card-accent-color` variable is the border of a layer over the
card, the root's `::after` pseudo-element. Since the variable can draw a bar on any card, that layer is there on every
card, with or without an accent: `content: ""`, `position: absolute`, `inset: 0`, a negative `margin` the width of the
card's rule, `box-sizing: border-box`, `border-radius: inherit` and `pointer-events: none`.

**An app that decorates the `::after` of a card's root** (through `Class`, `Classes.Root` or a `.bit-crd::after` rule
of its own) now has those declarations merged into its pseudo-element, so a badge placed with `top` / `right` is
stretched over the whole card. Draw the decoration on an element of its own inside the card instead, or set each of
those properties back in the app's rule:

```css
.my-card::after {
    content: "NEW";
    position: absolute;
    inset: 4px 4px auto auto;
    margin: 0;
}
```

### `BitMenuButton` reads a `Description` and a `Match` off custom items ([#15375](https://github.com/bitfoundation/bitplatform/issues/15375))

`BitMenuButtonNameSelectors` has two new members, `Description` and `Match`. Like the other name selectors, they read
the property of the same name off a custom item class when no selector is given:

- **A `Description` property** is rendered as a second line under the item's text and announced as its description
  (`aria-describedby`). The item's accessible name then becomes its text and secondary text alone
  (`aria-label`), unless it has an `AriaLabel`.
- **A `Match` property** of type `BitNavMatch?` marks the item whose `Href` matches the current URL with
  `aria-current="page"` and the `Current` class-style part. A property named `Match` of any other type is
  ignored.

**An app whose custom item class has a `Description` property used for something else** turns the new reading off
with a selector that returns null:

```csharp
NameSelectors = new() { Description = { Selector = _ => null } };
```

### `BitFullCalendarState` is read-only, but for its filters ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

`BitFullCalendar.State` is what the calendar is showing, for an app to read. The 56 members that changed it are no
longer public. Each was the plumbing between the calendar and its parts, or a second way to do what a parameter, a
`Settings` property or a method of `BitFullCalendar` already does. Several of them were undone by the calendar's next
render, which applies its parameters again. Its color and attendee filters keep their public methods (`SetColorFilter`,
`SetAttendeeFilter`, `FilterByColor`, `ClearFilter`), since nothing else sets them. What to use instead:

| Was on `State` | Use |
| --- | --- |
| `SetSelectedDate(date)` | `BitFullCalendar.GoToDate(date)`, or `DefaultDate` |
| `GoToToday()`, `NavigateNext()`, `NavigatePrevious()` | The methods of the same names on `BitFullCalendar` |
| `SetView(view)`, `SetMode(mode)` | `BitFullCalendar.ChangeView(view)`, `ChangeMode(mode)`, or `DefaultView`, `DefaultMode` |
| `SetUse24HourFormat`, `SetBadgeVariant`, `SetStartOfDayHour`, `SetSlotDurationMinutes`, `SetHiddenDays`, `SetFirstDayOfWeek`, `SetShowWeekNumbers`, `SetShowCurrentTimeIndicator`, `SetMaxEventsPerDayCell`, `SetRequireEventDescription`, `SetAllowEventOverlap`, `SetAllowRangeSelection`, `SetBusinessDays`, `SetHighlightBusinessHours`, `SetRestrictToBusinessHours`, `SetFixedWeekCount`, `SetShowNonCurrentDates`, `SetNavLinks`, `SetAgendaModeGroupBy`, `SetEventLayout`, `SetShowDayViewCalendar`, `SetWeekDayCount` | The `Settings` property of the same name |
| `SetVisibleHours(start, end)`, `SetBusinessHours(start, end)` | `Settings.VisibleStartHour` / `VisibleEndHour`, `Settings.BusinessStartHour` / `BusinessEndHour` |
| `SetEditPermissions(add, edit, delete, drag, resize)` | `Settings.AllowAdd`, `AllowEdit`, `AllowDelete`, `AllowDrag`, `AllowResize` |
| `ToggleTimeFormat()`, `ToggleShowWeekNumbers()`, `ToggleShowCurrentTimeIndicator()`, `ToggleHighlightBusinessHours()`, `ToggleShowDayViewCalendar()`, `ToggleEventLayout()` | Set the `Settings` property to the opposite of what `State` reads |
| `SetReadOnly`, `SetDateBounds`, `SetCulture`, `SetDirection`, `SyncViews` | The `ReadOnly`, `MinDate` / `MaxDate`, `Culture` / `CultureName`, `Dir` and `Views` parameters |
| `Initialize`, `SyncEvents`, `AddEvent`, `UpdateEvent`, `RemoveEvent`, `SyncResources` | The `Events` and `Resources` parameters: change the list the calendar is given |
| The constructor, `StartDrag`, `EndDrag`, `HandleDrop`, `UpdateUI`, `MarkCurrentRangeReported`, `AgendaScrollToTodayNonce` | Nothing: the calendar's own plumbing, which a `State` an app constructs is never attached to. With no public constructor, `BitFullCalendarState` can no longer be constructed or derived from. |

### Five of `BitDataGrid`'s helper types are internal ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

`BitDataGridDataProcessor`, `BitDataGridQueryableProcessor`, `BitDataGridPropertyAccessor<TItem>`,
`BitDataGridColumnSlot<TItem>` and `BitDataGridExcelStyle` are the machinery `BitDataGrid` works with, and none of
its parameters, callbacks or templates hands one of them out, so they are no longer public:

- **The two processors** apply a search, filters, sorts, groups and aggregates through the grid's own column
  components, keyed by their ids, and read each column's field through a reader the column sets up only as it renders
  inside a grid. Columns built outside one are skipped, so the processors could not apply a
  `BitDataGridReadRequest` anywhere but in the grid. The grid still applies all of it to the rows it is given in
  `Items`, a list or an `IQueryable<TItem>` (which a provider such as EF Core runs at the source). An `OnRead`
  handler applies the request's `Search`, `Filters`, `Sorts`, `Skip` and `Take` to its own data source, as before.
- **`BitDataGridPropertyAccessor<TItem>`** is how a column reads and writes the field its `Field` or `Property` names.
- **`BitDataGridColumnSlot<TItem>`** is a cell position while `VirtualizeColumns` is on.
- **`BitDataGridExcelStyle`** carries the colors and fonts read off the rendered grid when `ExcelExportStyled` is set.

### `BitFullCalendarChangeNotifier`, `BitFullCalendarColorScheme` and `BitFullCalendarHelpers` are internal ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

These are the machinery `BitFullCalendar` and its parts work with: the change notifier that hands a drag, a resize or
an edit to `OnChanging` and `OnChange`, the color scheme the calendar builds from `EventColorOptions`, and the date,
layout and recurrence helpers its views are drawn with. None of `BitFullCalendar`'s parameters, callbacks or templates
hands one of them out, so they are no longer public.

- The calendar's changes reach an app through `OnChanging`, `OnChange` and `OnRefused`, as before.
- An event's color is still set by `BitFullCalendarEvent.Color`, an id of `EventColorOptions`; its default is
  `"blue"`, which `BitFullCalendarColorScheme.FallbackColorId` held.
- The occurrences of the recurring events in the shown range are in `BitFullCalendar.State.ExpandedEvents`, and the
  date math of a week or a month is in .NET's own `Calendar`, `CultureInfo` and `ISOWeek`.

### `BitFullCalendar`'s `BitFc*` parts are `_BitFc*` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

The 40 `BitFc*` components are the header, views, dialogs, event blocks and other parts `BitFullCalendar` draws
itself with. Each one runs on the calendar's own state, change notifier and settings, which the calendar cascades to
it (16 of them also on a cascade the library keeps internal), so they cannot be used on their own, and they were public
only because a Razor component cannot be anything else. They now carry the leading underscore of the library's other
parts (`_BitNavBarChild`, `_BitDataGridRow`) and are hidden from IntelliSense: `BitFcX` is `_BitFcX`, for every one of

`BitFcAddEditEventDialog`, `BitFcAgendaEvents`, `BitFcBackgroundEvents`, `BitFcBody`, `BitFcDateNavigator`,
`BitFcDateTimePicker`, `BitFcDayBackgroundLabels`, `BitFcDayCell`, `BitFcDayView`, `BitFcDayViewMultiDayEventsRow`,
`BitFcDraggableEvent`, `BitFcDroppableArea`, `BitFcEventBlock`, `BitFcEventBullet`, `BitFcEventDetailsDialog`,
`BitFcEventListDialog`, `BitFcEventTemplateHost`, `BitFcFilterEvents`, `BitFcHeader`, `BitFcMiniCalendar`,
`BitFcModeTabs`, `BitFcMonthEventBadge`, `BitFcMonthView`, `BitFcNowIndicator`, `BitFcRecurrenceScopeDialog`,
`BitFcRenderGroupedEvents`, `BitFcSettings`, `BitFcTimeColumn`, `BitFcTimelineDayView`, `BitFcTimelineEventBlock`,
`BitFcTimelineLayout`, `BitFcTimelineMonthView`, `BitFcTimelineNowIndicator`, `BitFcTimelineWeekView`, `BitFcToast`,
`BitFcTodayButton`, `BitFcViewTabs`, `BitFcWeekView`, `BitFcWeekViewMultiDayEventsRow` and `BitFcYearView`.

An app that only uses `BitFullCalendar` has nothing to change, and what the calendar renders is unchanged. The
calendar's models, settings, texts and state (`BitFullCalendarEvent`, `BitFullCalendarSettings`,
`BitFullCalendar.State`, ...) keep their names. The day cell's `LastColumn` and `TabStop`, below, are parameters of
`_BitFcDayCell`.

### `BitDataGridRow`, `BitDataGridCell` and `BitDataGridCellEditor` are `_BitDataGridRow`, `_BitDataGridCell` and `_BitDataGridCellEditor` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

These are the row, the cell and the built-in cell editor `BitDataGrid` renders for itself. They take the grid itself
as a parameter and work on its own row, focus and edit state, so they cannot be used on their own, and they were
public only because a Razor component cannot be anything else. They now carry the leading underscore of the library's
other parts (`_BitNavBarChild`, `_BitDropdownItem`) and are hidden from IntelliSense.

| Before | After |
|---|---|
| `BitDataGridRow<TItem>` | `_BitDataGridRow<TItem>` |
| `BitDataGridCell<TItem>` | `_BitDataGridCell<TItem>` |
| `BitDataGridCellEditor<TItem>` | `_BitDataGridCellEditor<TItem>` |

An app that only uses `BitDataGrid` has nothing to change, and what the grid renders is unchanged. A column's own
editor is still written with its `EditTemplate`. `BitDataGridCellEventArgs` and `BitDataGridRowReorderEventArgs`, the
payloads of the grid's callbacks, keep their names.

### `IsStreaming` is `Streaming` on `BitAiCodeBlockView` and `BitAiStreamingMarkdown`, and `BitAiChatMessage`'s `IsLatest` is `Latest` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own state is spelled without an `Is`, and three parameters of `Bit.BlazorUI.AI` were still spelled with
one:

| Before | After |
|---|---|
| `<BitAiCodeBlockView Code="@code" IsStreaming="streaming" />` | `<BitAiCodeBlockView Code="@code" Streaming="streaming" />` |
| `<BitAiStreamingMarkdown Text="@text" IsStreaming="streaming" />` | `<BitAiStreamingMarkdown Text="@text" Streaming="streaming" />` |
| `<BitAiChatMessage Message="message" IsLatest="false" />` | `<BitAiChatMessage Message="message" Latest="false" />` |

What they do and their defaults (`false`, `false` and `true`) are unchanged. The models keep their `Is`, as the models
of one item do: `BitAiMessage.IsStreaming`, the parts' `IsStreaming` (`BitAiTextPart`, `IBitAiStreamablePart`, ...)
and the render contexts' (`BitAiCodeBlockContext.IsStreaming`) are as they were, so
`<BitAiStreamingMarkdown Text="@part.Text" Streaming="@part.IsStreaming" />` is the new spelling of the usual binding.

An old name left on a tag still builds and is ignored: a streaming block is highlighted and a streaming reply drawn
without its caret as if they were complete, and an older `BitAiChatMessage` marked `IsLatest="false"` offers
regeneration and follow-ups again. Search for both names when upgrading.

### `BitFcDayCell`'s `IsLastColumn` and `IsRovingCell` are `LastColumn` and `TabStop` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

`BitFcDayCell` is the day cell `BitFullCalendar`'s month view renders for itself, public only because Razor needs it to
be. A component's own setting is spelled without an `Is`: `IsLastColumn` drops its `Is`, and `IsRovingCell`, set on
the one cell that holds the month grid's roving tab stop, is named for what it does:

| Before | After |
|---|---|
| `<BitFcDayCell IsLastColumn="@last" />` | `<BitFcDayCell LastColumn="@last" />` |
| `<BitFcDayCell IsRovingCell="@roving" />` | `<BitFcDayCell TabStop="@roving" />` |

`BitFullCalendar` sets both itself, so an app that only uses `BitFullCalendar` has nothing to change. What the calendar
renders is unchanged.

### `BitNumberField`'s `IsInputReadOnly` is `InputReadOnly` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own setting is spelled without an `Is`, and the number field's switch that stops the typing but
leaves the stepping was spelled `IsInputReadOnly`. It drops the `Is` and keeps the `Input` the field's `HideInput` and
`InputMode` start with, with its counterpart on `BitNumberFieldParams`:

| Before | After |
|---|---|
| `<BitNumberField IsInputReadOnly />` | `<BitNumberField InputReadOnly />` |
| `new BitNumberFieldParams { IsInputReadOnly = true }` | `new BitNumberFieldParams { InputReadOnly = true }` |

What it does and its default (`false`) are unchanged. An `IsInputReadOnly` left on a field still builds and is
ignored, so the field can be typed into again; search for the name when upgrading.

### `IsMonthPickerVisible` is `ShowMonthPicker` on `BitDatePicker` and `BitDateRangePicker` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own setting is spelled without an `Is`, and the switch that shows or hides a part of a component is
spelled `Show...` (`ShowGoToToday`, `ShowTimePicker`, `ShowMonthPickerAsOverlay`). The two pickers' month picker
switch was spelled `IsMonthPickerVisible`; it takes the name `BitCalendar` already gives the same setting, with its
counterpart on `BitDatePickerParams` and `BitDateRangePickerParams`:

| Before | After |
|---|---|
| `<BitDatePicker IsMonthPickerVisible="false" />` | `<BitDatePicker ShowMonthPicker="false" />` |
| `<BitDateRangePicker IsMonthPickerVisible="false" />` | `<BitDateRangePicker ShowMonthPicker="false" />` |
| `new BitDatePickerParams { IsMonthPickerVisible = false }` | `new BitDatePickerParams { ShowMonthPicker = false }` |
| `new BitDateRangePickerParams { IsMonthPickerVisible = false }` | `new BitDateRangePickerParams { ShowMonthPicker = false }` |

What it does and its default (`true`) are unchanged. An `IsMonthPickerVisible="false"` left on a picker still builds
and is ignored, so the month picker shows again; search for the name when upgrading.

### `BitDialog`'s `IsDraggable` is `Draggable`, and its `IsOkButtonEnabled` and `IsCancelButtonEnabled` are `OkButtonDisabled` and `CancelButtonDisabled` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own setting is spelled without an `Is` (`Disabled`, `Blocking`, `Alert`), and three of the dialog's
were still spelled with one. `IsDraggable` is renamed, with its counterpart on `BitDialogParams`; `Draggable` is the
name `BitModal` already gives the same setting. The two button flags turn around to say what they do, the way
`Disabled` does: they are off by default and disable their button when set, so **the value you pass is inverted**:

| Before | After |
|---|---|
| `<BitDialog IsDraggable />` | `<BitDialog Draggable />` |
| `<BitDialog IsOkButtonEnabled="agreed" />` | `<BitDialog OkButtonDisabled="agreed is false" />` |
| `<BitDialog IsCancelButtonEnabled="false" />` | `<BitDialog CancelButtonDisabled />` |
| `new BitDialogParams { IsDraggable = true }` | `new BitDialogParams { Draggable = true }` |

An `IsOkButtonEnabled` or `IsCancelButtonEnabled` left on a `BitDialog` still builds and is ignored, so the button it
was disabling is enabled again; search for both names when upgrading. An HTML `draggable` attribute written on a
`BitDialog` now sets `Draggable`, as it already does on `BitModal`, instead of being rendered on the dialog's element.

### `IsAlert` is `Alert` on `BitDialog`, `BitModal` and `BitPanel` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own setting is spelled without an `Is` (`Disabled`, `Blocking`, `Modeless`), and the flag that makes
the three surfaces an `alertdialog` was still spelled with one. The `[Parameter]` is renamed on the three components,
with its counterparts on `BitModalParams` and `BitPanelParams` and on the option classes their services take,
`BitModalParameters` and `BitPanelParameters`:

| Before | After |
|---|---|
| `<BitDialog IsAlert="true" />` | `<BitDialog Alert="true" />` |
| `<BitModal IsAlert="false" />` | `<BitModal Alert="false" />` |
| `<BitPanel IsAlert />` | `<BitPanel Alert />` |
| `new BitModalParameters { IsAlert = true }` | `new BitModalParameters { Alert = true }` |
| `new BitPanelParams { IsAlert = true }` | `new BitPanelParams { Alert = true }` |

The role each of them renders is unchanged (`alertdialog` or `dialog`).

### `BitMediaQuery`'s `IsMatched` is `Matched`, and its `Matched` and `NotMatched` are `MatchedTemplate` and `NotMatchedTemplate` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own state is spelled without an `Is` (`Disabled`, `Checked`, `Expanded`), but the media query's
`Matched` was already taken by the content it renders while the query matches. The two pieces of content take the
`...Template` name, which frees `Matched` for the state; the state's uncontrolled default was already `DefaultMatched`:

| Before | After |
|---|---|
| `<BitMediaQuery @bind-IsMatched="isMd" />` | `<BitMediaQuery @bind-Matched="isMd" />` |
| `<BitMediaQuery IsMatchedChanged="OnMatchedChanged" />` | `<BitMediaQuery MatchedChanged="OnMatchedChanged" />` |
| `<Matched>wide</Matched>` | `<MatchedTemplate>wide</MatchedTemplate>` |
| `<NotMatched>narrow</NotMatched>` | `<NotMatchedTemplate>narrow</NotMatchedTemplate>` |

Code that still uses the old names builds: a `<Matched>` or `<NotMatched>` tag left inside a `BitMediaQuery` only
draws the warning RZ10012 ("Found markup element with unexpected name") and renders as an unknown HTML element, so
the content of both states shows at once. Search for the warning after upgrading. What the media query renders with
the new names is unchanged, and so are `ChildContent`, `Template`, `DefaultMatched` and `OnChange`.

### `BitAccordion`'s `IsExpanded` is `Expanded` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own state is spelled without an `Is` (`Disabled`, `Checked`, `Shown`), and the accordion's expanded
state was still spelled with one. The `[Parameter]` is renamed with its two-way binding and its uncontrolled default:

| Before | After |
|---|---|
| `<BitAccordion IsExpanded="open" />` | `<BitAccordion Expanded="open" />` |
| `<BitAccordion @bind-IsExpanded="open" />` | `<BitAccordion @bind-Expanded="open" />` |
| `<BitAccordion IsExpandedChanged="OnExpandedChanged" />` | `<BitAccordion ExpandedChanged="OnExpandedChanged" />` |
| `<BitAccordion DefaultIsExpanded />` | `<BitAccordion DefaultExpanded />` |

What the accordion renders is unchanged (`bit-acd-exp`, `aria-expanded`), and so are its `Expand`, `Collapse` and
`Toggle` methods and its `OnChange`, `OnExpand`, `OnCollapse` and `OnToggling` callbacks. The models of one item keep
their `Is` (`BitAccordionListItem.IsExpanded`, `BitNavItem.IsExpanded` and the like): the flag of one item of a parent
is spelled with one.

### `BitTooltip`'s `IsShown` is `Shown` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own state is spelled without an `Is` (`Disabled`, `Checked`, `Toggled`), and the tooltip's shown state
was still spelled with one. The `[Parameter]` is renamed with its two-way binding and its uncontrolled default:

| Before | After |
|---|---|
| `<BitTooltip IsShown="visible" />` | `<BitTooltip Shown="visible" />` |
| `<BitTooltip @bind-IsShown="visible" />` | `<BitTooltip @bind-Shown="visible" />` |
| `<BitTooltip IsShownChanged="OnShownChanged" />` | `<BitTooltip ShownChanged="OnShownChanged" />` |
| `<BitTooltip DefaultIsShown="true" />` | `<BitTooltip DefaultShown="true" />` |

What the tooltip renders is unchanged (`bit-ttp-vis`), and so are its `Show`, `Hide` and `Toggle` methods and its
`OnShow`, `OnHide` and `OnToggle` events.

### `IsToggled` is `Toggled` on `BitMenuButton` and `BitNavPanel` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own state is spelled without an `Is` (`Disabled`, `Loading`, `Checked`), and the toggled state of the
split menu button's header and of the nav panel's rail were still spelled with one. Each `[Parameter]` is renamed
with its two-way binding, and on the menu button with its uncontrolled default and its `BitMenuButtonParams`
counterpart:

| Before | After |
|---|---|
| `<BitMenuButton Split Toggle IsToggled="bold" />` | `<BitMenuButton Split Toggle Toggled="bold" />` |
| `<BitMenuButton Split Toggle @bind-IsToggled="bold" />` | `<BitMenuButton Split Toggle @bind-Toggled="bold" />` |
| `<BitMenuButton Split Toggle DefaultIsToggled="true" />` | `<BitMenuButton Split Toggle DefaultToggled="true" />` |
| `new BitMenuButtonParams { DefaultIsToggled = true }` | `new BitMenuButtonParams { DefaultToggled = true }` |
| `<BitNavPanel @bind-IsToggled="rail" />` | `<BitNavPanel @bind-Toggled="rail" />` |
| `<BitNavPanel IsToggledChanged="OnRailChanged" />` | `<BitNavPanel ToggledChanged="OnRailChanged" />` |

What the two render is unchanged (`bit-mnb-tgl`, `bit-npn-tgl`, the `Toggled` class-style part). `OnToggleChange` and
`ToggledWidth` keep their names, and so do the button group's items (`BitButtonGroupItem.IsToggled`,
`BitButtonGroupOption.IsToggled`): the flag of one item of a parent is spelled with an `Is`.

### `BitToggleButton`'s `IsChecked` is `Checked` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own state is spelled without an `Is` (`Disabled`, `Vertical`, `Loading`), and the toggle button's
checked state was the one flag of the buttons still spelled with one. The `[Parameter]`, its two-way binding and its
uncontrolled default are renamed together, and so is `BitToggleButtonParams.DefaultIsChecked`:

| Before | After |
|---|---|
| `<BitToggleButton IsChecked="on" />` | `<BitToggleButton Checked="on" />` |
| `<BitToggleButton @bind-IsChecked="on" />` | `<BitToggleButton @bind-Checked="on" />` |
| `<BitToggleButton IsCheckedChanged="OnCheckedChanged" />` | `<BitToggleButton CheckedChanged="OnCheckedChanged" />` |
| `<BitToggleButton DefaultIsChecked="true" />` | `<BitToggleButton DefaultChecked="true" />` |
| `new BitToggleButtonParams { DefaultIsChecked = true }` | `new BitToggleButtonParams { DefaultChecked = true }` |

What the toggle button renders is unchanged (`bit-tgb-chk`, `aria-pressed`, `aria-checked`). The menu button's items
keep their `Is` (`BitMenuButtonItem.IsChecked`, `BitMenuButtonOption.IsChecked`): the flag of one item of a parent is
spelled with one.

### `IsLoading` is `Loading` on every component ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

The loading state had two names: `Loading` on `BitCard`, `BitCheckbox`, `BitDataGrid`, `BitBasicList`,
`BitNumberField`, `BitSearchBox`, `BitTextField` and `BitToggle`, and `IsLoading` on eleven others. A component's own
state is spelled without an `Is`, so those eleven now say `Loading` too: `BitActionButton`, `BitButton`,
`BitMenuButton`, `BitToggleButton`, `BitDropdown`, `BitOtpInput`, `BitTagsInput`, `BitDropMenu`, `BitChart`,
`BitFullCalendar` and `BitFcBody`. Their `*Params` counterparts are renamed with them, and on the four buttons the
two-way binding follows the value (`IsLoadingChanged` is `LoadingChanged`):

| Before | After |
|---|---|
| `<BitButton IsLoading="saving" />` | `<BitButton Loading="saving" />` |
| `<BitButton @bind-IsLoading="saving" AutoLoading />` | `<BitButton @bind-Loading="saving" AutoLoading />` |
| `<BitButton IsLoadingChanged="OnLoadingChanged" />` | `<BitButton LoadingChanged="OnLoadingChanged" />` |
| `new BitDropdownParams { IsLoading = true }` | `new BitDropdownParams { Loading = true }` |

What the components render is unchanged. The models of one item keep their `Is` (`BitButtonGroupItem.IsLoading`,
`BitButtonGroupOption.IsLoading`, `BitSnackBarItem.IsLoading`), and so do the read-only state that a reference exposes
(`BitInfiniteScrolling.IsLoading`, `BitAiReadAloud.IsLoading`) and `BitSearchBoxAnnouncementArgs.IsLoading`: none
of them is a parameter of a component.

### `BitSlider`'s `IsRanged` and `IsOriginFromZero` are `Ranged` and `OriginFromZero` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

The slider's last two flags spelled with an `Is` lose it, like its `Vertical` before them (a component's own state is
spelled without one). The `[Parameter]`s and their `BitSliderParams` counterparts are renamed:

| Before | After |
|---|---|
| `<BitSlider IsRanged />` | `<BitSlider Ranged />` |
| `<BitSlider IsOriginFromZero />` | `<BitSlider OriginFromZero />` |
| `new BitSliderParams { IsRanged = true, IsOriginFromZero = true }` | `new BitSliderParams { Ranged = true, OriginFromZero = true }` |

What the slider renders is unchanged (`bit-sld-rgd`, the `OriginFromZero` class-style part of the zero tick).

### `BitSlider`'s `IsVertical` is `Vertical` ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

A component's own state is spelled without an `Is` (`Disabled`, `Vertical`, `Loading`), and the eleven other
components that stand upright already call it `Vertical`; `BitSlider` was the one `IsVertical`. The `[Parameter]` and
its `BitSliderParams` counterpart are renamed:

| Before | After |
|---|---|
| `<BitSlider IsVertical />` | `<BitSlider Vertical />` |
| `new BitSliderParams { IsVertical = true }` | `new BitSliderParams { Vertical = true }` |

What the slider renders is unchanged (`bit-sld-vrt`, `aria-orientation="vertical"`).

### A grouped `BitDataGrid` with `Virtualize` is virtualized ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

`Virtualize` used to be turned off while the rows were grouped, so every row of every open group was in the DOM: 10,000
grouped rows took 130,000 elements and seconds to render. A grouped view is now virtualized like a flat one, as one
list of group rows and data rows, which changes what a grouped grid with `Virtualize` renders:

- **Only a window of the rows is in the DOM**, with the spacers `Virtualize` keeps around it. A script or a test that
  read every row of a grouped grid off the page reads the rows in view; scroll a row into view first, or call
  `RevealItemAsync`, which scrolls to its place.
- **A group row is exactly `RowHeight` tall**, like every other row of a virtualized grid, and keeps its toggle, title,
  count and aggregates on one line: a title or aggregates that used to wrap end in an ellipsis. A group row carries
  the class `bit-dtg-group-row-fix` there.
- **The body has no `rowgroup` role**, as the body of a flat virtualized grid has none: the spacers sit among its rows.

A grouped grid without `Virtualize` renders as it did.

### Three Extras surfaces stack on the layer scale rather than above it ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

Three surfaces of `Bit.BlazorUI.Extras` were stacked with a literal z-index far above the library's layer scale (the
`--bit-zin-*` tokens, 1000 to 1400 by default), so moving the scale with `BitThemeZIndices` left them where they were.
They now read the scale:

| Surface | Before | After |
|---|---|---|
| The bars over the device's insets of `BitAppShell` | `999999` | one above the snackbar's layer, `calc(var(--bit-zin-snackbar) + 1)` (1401 by default) |
| The pointer catcher of a `BitDataGrid` column resize | `9999` | one above the snackbar's layer, `calc(var(--bit-zin-snackbar) + 1)` (1401 by default) |
| The tooltip of `BitResourceTimeline` | `2147483647` | the callout layer, `var(--bit-zin-callout)` (1300 by default), like `BitTooltip` |

The inset bars and the resize catcher still sit above everything the library renders; the tooltip now shares its
layer with every other tooltip and popup. An element of an app stacked between the old and the new value (a z-index
of 2000, say) now paints over them: give it a value under the scale, or move the scale above it with
`BitThemeZIndices`.

### Every component has a root class of its own ([#15137](https://github.com/bitfoundation/bitplatform/issues/15137))

Two components that shared a root class shared every rule written for it, so each restyled the other wherever both
were on a page: a `BitNavPanel` lost its width and the top of its list to the `BitStepper` rules that matched the
`BitSwipeTrap` inside it. The newer component of each pair - or the Legacy twin, which keeps its own prefix beside the
component it predates - takes a class prefix of its own; no public API is renamed, but an app's stylesheets, scripts
and tests written against the old classes move to the new ones:

| Component | Before | After |
|---|---|---|
| `BitStepper` | `bit-stp`, `bit-stp-*` (shared with `BitSwipeTrap`) | `bit-str`, `bit-str-*`, and the `data-bit-str-opt` attribute |
| `BitMarkdownEditorLegacy` | `bit-mde`, `bit-mde-txa` (shared with `BitMarkdownEditor`) | `bit-mel`, `bit-mel-txa` |
| `BitMarkdownViewerLegacy` | `bit-mdv bit-mdv-lgc` (shared with `BitMarkdownViewer`) | `bit-mvl` |
| `BitRichTextEditorLegacy` | `bit-rte bit-rte-lgc`, `bit-rte-tlb`, `bit-rte-edt`, `bit-rte-rvs` (shared with `BitRichTextEditor`) | `bit-rel`, `bit-rel-tlb`, `bit-rel-edt`, `bit-rel-rvs` |
| The popups of `BitCallout`, `BitDropMenu`, `BitMenuButton` and `BitSearchBox` | `bit-cal-ent` for the frame their entry starts from (under `BitCalendar`'s prefix) | `bit--entry` |

The Legacy rules also weigh less than they did: `.bit-mdv.bit-mdv-lgc` (two classes) is now `.bit-mvl` (one), and
`.bit-rte.bit-rte-lgc ...` is now `.bit-rel ...`. A rule of an app with a single class, on a `BitMarkdownViewerLegacy`
or `BitRichTextEditorLegacy` or inside one, that used to lose to the component's rule now ties with it, and wins
when the app's stylesheet loads after the Legacy bundle (`.my-viewer { width: 50% }` on a `BitMarkdownViewerLegacy`
now applies). Remove or narrow such a rule where the component's own look is the one wanted.
### What axe-core found inside the components is fixed, and the markup it changed ([#14891](https://github.com/bitfoundation/bitplatform/issues/14891))

No public API is renamed or removed, but an app's stylesheets, scripts and tests written against the markup below
see a difference:

| Component | Before | After |
|---|---|---|
| `BitSwiper` | The root was the tab stop (`tabindex`, `TabIndex`, the focus ring) | The box the items scroll in (`.bit-swp-cnt`, `Classes.Container`) is: `TabIndex` lands there, and `NoKeyboard` / `Disabled` give it `tabindex="-1"`. The root keeps its region role and its keyboard handling |
| `BitInfiniteScrolling` | No tab index unless `TabIndex` was set | A list whose own content overflows it is given `tabindex="0"` by its script, and loses it once the content fits again - unless `TabIndex` or a plain `tabindex` attribute says otherwise (not in `Feed` mode, nor with a `ScrollerSelector`). A list of no height, which the page scrolls, gets none |
| `BitPhoneInput` | The country selector was named `Country: United States` | It ends with the dial code it shows: `Country: United States, +1` (unless `NoDialCode`); a `DropdownTemplate` makes it `aria-labelledby` the country (`Country: Portugal`, visually hidden) and the template's own content |
| `BitCalendar`, `BitDatePicker`, `BitDateRangePicker` | A month cell had `aria-label="January"` | It is named by the text it shows (`Jan`, or what a `MonthCellTemplate` shows), the full month staying its `title` |
| `BitCalendar`, `BitDatePicker`, `BitDateRangePicker` | The corner above the week numbers was empty | It shows `#`, still named by `WeekNumbersHeaderTitle` |
| `BitCalendar`, `BitDatePicker`, `BitDateRangePicker` | The days grid had `aria-label` | It is `aria-labelledby` the month title above it, when that title is not the month picker's toggle; the pickers' grid is also `aria-disabled="true"` while disabled, as the calendar's already was |
| `BitRating` | A read-only rating carried its value in an `<input type="number">` | In an `<input type="hidden">` |
| The callouts (`BitDropdown`, `BitDatePicker`, `BitMenuButton`, every popup moved to the body) | The copies of the ancestors a popup leaves behind kept the tag of a `main`, `dt`, `dd` or `caption` | They are `div`s, since none of those tags permits the `role="none"` a copy takes to stay out of the accessibility tree: a page selector naming one of them as an ancestor (`main .menu`, `dd > .x`) no longer reaches the popup - one naming a class of that ancestor still does |
| `BitMarkdownEditor` | The status bar was a plain `div` | It is a `role="group"`, `aria-disabled="true"` while the editor is disabled |
| `BitText`, `BitTextShimmer` | A disabled one carried `bit-dis` only | It writes `aria-disabled="true"` too, whatever its tag |
| `BitSwiper` | A disabled swiper's box still scrolled under a touch, a trackpad or a wheel | It stops scrolling (`overflow: hidden`) while disabled |
| `BitFullCalendar` | A week number link was named `Go to week 27`, a week view column header `Go to Monday, January 5, 2026` | Each is named by the text it shows first: `W27, Go to week 27`, `Mon 5, Go to Monday, January 5, 2026` (the link's text now has a space between the day and its number, and a day or week view event block one between its title and its time) |
| `BitFullCalendar` | An event drawn by a `MonthEventTemplate`, `WeekEventTemplate`, `DayEventTemplate` or `TimelineEventTemplate` was named `Planning, 09:00 - 10:00, 7/16/2025`, the template's content sitting straight in the badge or block | The event carries an `id` and is `aria-labelledby` itself (its own content, the template's) and then a `hidden` span of those details: `Pin: Planning Planning, 09:00 - 10:00, 7/16/2025` |
| `BitCalendar` | A `Required` calendar's days grid had `aria-required="true"` | The grid takes no `aria-required`; it is `aria-describedby` a `hidden` span reading `BitStrings.Current.Required` ("Required") instead, except while the calendar is disabled or read-only |
| `BitFullCalendar` | A month badge of a timed event spanning days was named `Title, 7/16/2025 22:00 - 7/17/2025 23:59` (`d` dates, an end at midnight read as 23:59 of the day before) | It is named the way the other views name it: `Title, Jul 16, 2025 22:00 - Jul 18, 2025 00:00`; a badge of a single day keeps `Title, 09:00 - 10:00, 7/16/2025` |

A test that located a month cell by the name `January`, a swiper by focusing its root, a phone selector, a FullCalendar
nav link or a templated FullCalendar event by its old name moves to the new one, as does one reading `aria-required`
off the calendar's grid (it reads its accessible description instead).

### The empty and no-results slots have one name each ([#15029](https://github.com/bitfoundation/bitplatform/issues/15029))

What a component shows when it has nothing to show was spelled five ways. It now has two names, one per meaning:
`EmptyTemplate` / `EmptyText` (and the `Empty` class-style part) for a component with **no items at all**, and
`NoResultsTemplate` / `NoResultsText` (and the `NoResults` part) for a search or filter that **matched none** of the
items it has. The outliers are renamed to them; each row applies to the `[Parameter]` and to its `BitXxxParams`
counterpart:

| Component | Before | After |
|---|---|---|
| `BitBasicList` | `EmptyContent` | `EmptyTemplate` |
| `BitAccordionList` | `EmptyContent` | `EmptyTemplate` |
| `BitChart` | `NoDataTemplate`, `NoDataText`, `BitChartClassStyles.NoData` | `EmptyTemplate`, `EmptyText`, `Empty` |
| `BitInfiniteScrolling` | `EmptyMessage` | `EmptyText` |
| `BitDropdown` | `BitDropdownClassStyles.EmptyContent` | `Empty` |
| `BitNavPanel` | `EmptyListTemplate`, `EmptyListMessage`, `BitNavPanelClassStyles.EmptyListMessage` | `NoResultsTemplate`, `NoResultsText`, `NoResults` |
| `BitCountryPicker`, `BitPhoneInput`, `BitTimeZonePicker` | `NoResultsMessage` | `NoResultsText` |

`BitNavPanel`'s are no-results names, not empty ones: its slot was always the message of a search that matched no item.

**Search the markup for the old names**: an old name written as an attribute (`NoDataText="…"`) still compiles,
without a warning, and is rendered as an attribute of the root rather than applied, so the component silently falls
back to its default content. An old template written as a child element fails to compile in a `BitBasicList`
(`RZ9996`), but in a `BitAccordionList`, which takes child content, it compiles with only a `RZ10012` warning and is
taken for markup of that content. Leave `EmptyContent` alone on the framework's own `Virtualize`, which is not one of
these.

### Every empty list and fruitless search says so, in the same words ([#15032](https://github.com/bitfoundation/bitplatform/issues/15032))

What a component shows when it has nothing to show now works the same way on all of them: the template wins, then
the text, and with neither the component says the default of `BitStrings.Current` - `NoItemsFound` ("No items
found") for no items at all and `NoResultsFound` ("No results found") for a search or filter that matched none.
Translate them once through a `BitStrings` subclass (`AddBitBlazorUIServices(o => o.Strings = …)`) instead of on
every component.

**Components that showed nothing now show the default.** Pass an `EmptyTemplate` that renders nothing to keep the
old look:

| Component | Before | Now |
|---|---|---|
| `BitBasicList`, `BitAccordionList`, `BitVirtualize` | an empty list without an `EmptyTemplate` rendered nothing | renders `EmptyText` (new), else "No items found", in a part of its own (`bit-bsl-emp`, `bit-acl-emp`, `bit-vir-emp`) styled through the `Empty` class-style part (new on the first two); `BitBasicList` and `BitAccordionList` also read the new `--bit-BasicList-empty-*` / `--bit-AccordionList-empty-*` variables |
| `BitSearchBox` | a search that found no suggestion kept the callout closed unless `NoResultsText` or `NoResultsTemplate` was given | opens the callout and says "No results found" (and announces it) |

**The default words changed** where a component had words of its own:

| Component | Before | Now |
|---|---|---|
| `BitChart.EmptyText` | "No data to display" | "No items found" |
| `BitInfiniteScrolling.EmptyText` | "There is no item" | "No items found" |
| `BitNotificationCenter.EmptyText` | "No notifications" | "No items found" |
| `BitDataGridStrings.EmptyText` | "No records to display." | "No items found" |
| `BitDataGridStrings.NoMatchesText` | "No records match the current filters." | "No results found" |
| `BitNavPanel.NoResultsText` | "Nothing found!" | "No results found" |
| `BitSearchBox`'s announcement of a fruitless search | "No suggestion found." | "No results found" |

`BitChart.EmptyText` and `BitInfiniteScrolling.EmptyText` are `null` by default now, as every other `EmptyText` is,
instead of holding their English text: code that reads them gets `null` until a text is set. The two texts of
`BitDataGridStrings` above read `BitStrings.Current` each time they are shown until a text of their own is set.

**`BitDropdown` keeps its two states apart.** A search that matches none of the items renders `NoResultsTemplate`,
else `NoResultsText`, else "No results found", in its own `bit-drp-nor` element styled through the new
`BitDropdownClassStyles.NoResults` part. `EmptyTemplate` and `EmptyText` are only for a dropdown with no items at
all, searched or not; before, they were shown for a fruitless search too when no `NoResultsText` was given. Move
a no-results message written in `EmptyText` or `EmptyTemplate` to `NoResultsText` or `NoResultsTemplate`.

**CSS names:**

| Before | After |
|---|---|
| `.bit-cht-nodata` | `.bit-cht-emp` |
| `.bit-srb-nrs` | `.bit-srb-nor` |
| `--bit-Chart-no-data-color` | `--bit-Chart-empty-color` |

`BitNotificationCenter` also takes an `EmptyTemplate` now, which wins over its `EmptyText` and cascades through
`BitNotificationCenterParams`.
### `BitAiEmptyState` is a `BitEmptyState` and takes its names ([#ISSUE](https://github.com/bitfoundation/bitplatform/issues/ISSUE))

The empty state of a conversation is now drawn by the core `BitEmptyState`, with the words of a conversation, a sparkle
for its icon and the suggestions for its actions. It takes the names of the core one, with no old name kept beside them:

| Before | After |
|---|---|
| `BitAiEmptyState.Body` (and `BitAiEmptyStateParams.Body`) | `Description` |
| `BitAiEmptyState.ChildContent` | `DescriptionTemplate` |
| `BitAiEmptyStateClassStyles.Body` | `Description` |
| `BitAiChat.EmptyBody` (and `BitAiChatParams.EmptyBody`) | `EmptyDescription` |
| `BitAiStrings.EmptyBody` | `EmptyDescription` |
| `--bit-AiEmptyState-color` | `--bit-AiEmptyState-description-color` |

Markup written as the child content of a `BitAiEmptyState` no longer compiles (`RZ9996`): wrap it in
`<DescriptionTemplate>`. A `Body="…"` or `EmptyBody="…"` attribute still compiles, without a warning, and lands on the
root as an HTML attribute, so the line falls back to the string table: search the markup for them. A `BitAiStrings`
subclass that overrides `EmptyBody` fails to compile until it overrides `EmptyDescription`.

**The markup changed.** The root keeps its `bit-aiems` class, and a `BitEmptyState` inside it holds the parts:

| Before | After |
|---|---|
| `.bit-aiems-ico` (a `BitIcon`, `.bit-ico`) | `.bit-ems-ico` (an `<i>`, or a `<div>` holding the `IconTemplate`) |
| `.bit-aiems-ttl` | `.bit-ems-ttl` |
| `.bit-aiems-bdy` | `.bit-ems-dsc` |
| `.bit-aiems-sgs` | `.bit-ems-con` |

The layout (gap, padding, centering) is on the inner `.bit-ems`, not on the root; the `--bit-AiEmptyState-*` variables
still restyle it, and the line now takes its color and size from `--bit-AiEmptyState-description-color` and the new
`--bit-AiEmptyState-description-font-size` (default `--bit-tpg-fs-md`) rather than from the color of the root. The
`Classes` and `Styles` parts reach the same elements as before.

It also gains the `TitleTemplate` and `HeadingLevel` of the core one.
### `BitNavPanel` renders the content of its drawer the first time it opens ([#14866](https://github.com/bitfoundation/bitplatform/issues/14866))

Below its `DrawerBreakpoint` the panel is an off-canvas drawer, and the content of a drawer that has never been opened
(the header, the search box, the `BitNav` and the footer) is now left out until it first opens. It keeps the content from
then on. Leaving it out takes knowing the shape of the panel before its first render, which only a WebAssembly app can ask
the browser synchronously (from the start with the bundled scripts, once the media query script is loaded with the lazy
ones): on Server, Hybrid and the first page of a lazy-scripts app the content is rendered as before. A prerender and a
static SSR page still render the content, so the links stay in the HTML. On a wide screen the panel is a column and
renders as before.

What reads the nav still finds it: an app listening to the selection the automatic `NavMode` takes from the URL
(`SelectedItemChanged`, `@bind-SelectedItem`, `OnSelectItem`) gets the content at once, and the methods that act on the
nav (`SelectItem`, `ExpandItem`, `CollapseItem`, `ToggleItem`, `ExpandAll`, `CollapseAll`, `FocusItem`,
`IsItemExpanded`) render it as they are called. **An app that reads something else of the nav before the drawer has been
opened**, such as a template's side effects, sets `NoLazyDrawer` (or `BitNavPanelParams.NoLazyDrawer`) to `true` to
render the content at once, as before.

`BitMediaQuery` asks the browser the same way before its own first render: in a WebAssembly app whose script is in the
page it renders the matching content right away, and `DefaultMatched` only applies where the browser cannot answer yet.

### `BitCountries`' countries and `All` are properties ([#14867](https://github.com/bitfoundation/bitplatform/issues/14867))

Every country of `BitCountries` (`BitCountries.UnitedStates`, ...) and `BitCountries.All` were `static readonly` fields and
are now static properties of the same names and types, handing out the same instances. The fields made naming a single
country (a `DefaultCountry` set up as a WebAssembly app starts) run a static constructor creating all of them, one very
large method the interpreter has to transform before it runs; the properties build the table the first time any of it
is read.

Code reading them compiles unchanged. **A library or app compiled against an earlier version** is recompiled, since the
fields it loads are gone (`MissingFieldException`). **Code passing one by `ref` or `out`**, or reading the fields
through reflection (`typeof(BitCountries).GetFields()`), reads it into a local or uses `GetProperties()` instead.

### Seekable PDF sources defer forms and tagged structure ([#13702](https://github.com/bitfoundation/bitplatform/issues/13702))

For a `BitPdfViewer` loaded with `BitPdfSource.FromStreamFactory`, `FormFields` and `StructureTree` now return empty
collections until `LoadDocumentDetailsAsync()` completes. **An app that reads them from `OnDocumentLoaded`** awaits
that method before using them. This avoids reading the whole document when opening a seekable stream. Buffered
sources, including the snapshot produced by `FromStreamAsync`, keep their eager behavior.

### The packages target .NET 10 and .NET 11 only

The packages no longer ship `net8.0` and `net9.0` builds. **An app on .NET 8 or .NET 9** moves to .NET 10 or later
before it updates, or stays on 10.x of the packages.

### `BitCarousel`'s go-left and go-right buttons are its next and previous buttons ([#14560](https://github.com/bitfoundation/bitplatform/issues/14560))

The buttons were named after the way the slides move, not the edge they sit at or what they do: the go-left button sat
at the right edge and went to the next slide of a left-to-right carousel, but to the previous one of a right-to-left
carousel. They are now the next and the previous button, each keeping its label, its icon and its parts in either
direction, while the direction moves it to the other edge (the next button sits at the left edge of a right-to-left
carousel). Each row applies to the `[Parameter]` and to its `BitCarouselParams` counterpart:

| Before | After |
|---|---|
| `GoLeftAriaLabel`, `GoLeftIcon`, `GoLeftIconName` | `NextAriaLabel`, `NextIcon`, `NextIconName` |
| `GoRightAriaLabel`, `GoRightIcon`, `GoRightIconName` | `PrevAriaLabel`, `PrevIcon`, `PrevIconName` |
| `BitCarouselClassStyles.GoLeftButton`, `GoLeftButtonIcon` | `NextButton`, `NextButtonIcon` |
| `BitCarouselClassStyles.GoRightButton`, `GoRightButtonIcon` | `PrevButton`, `PrevButtonIcon` |
| the `bit-csl-lbt` class (the right edge) | `bit-csl-nbt` (the next button, at either edge) |
| the `bit-csl-rbt` class (the left edge) | `bit-csl-pbt` (the previous button, at either edge) |

**Go-left is next, not previous**: rename by what the button does, never by the word. **An app with a right-to-left
carousel** that set `GoLeft*` was setting the previous button there, so those values go to `Prev*` (and `GoRight*` to
`Next*`). **Search the markup for `GoLeft` and `GoRight`**: an old name written on the component still compiles, and
is rendered as an attribute of its root rather than applied, so the button silently falls back to its default label
and icon. A custom next/prev icon is now given for a left-to-right carousel and mirrored with its button in a
right-to-left one, as the default chevrons are; **an app that picked the glyph for a right-to-left carousel itself**
gives it the left-to-right glyph. **A stylesheet or a test locator** that read the edge off `bit-csl-lbt` /
`bit-csl-rbt` reads the button off the new classes, and the direction off the `bit-csl-rtl` class of the root, which a
carousel laid out right to left carries whether its `Dir` says so or it inherits the direction.

### An error message template is announced in place of the message it hides ([#14563](https://github.com/bitfoundation/bitplatform/issues/14563))

`BitTextField`, `BitSearchBox`, `BitNumberField`, `BitDatePicker`, `BitTimePicker`, `BitDropdown` and `BitToggle`
announce their error message through a live region. Given both an `ErrorMessage` and an `ErrorMessageTemplate`, they
announced the plain `ErrorMessage`, which the template replaces on screen, and given a template alone all but
`BitNumberField` announced nothing, leaving a rejected value sounding accepted. The live region now announces what is
on screen: a template as a bare "Invalid input", or as the text of the new `ErrorMessageAriaLabel` parameter (also in
their `Bit*Params`), which the field is described by on every later focus too. `ErrorMessageAriaLabel` also makes the
forced `Invalid` state announce it. **An app that set the plain message next to a template to have it announced**,
as the docs of `BitToggle` advised, moves that text to `ErrorMessageAriaLabel`:

```razor
@* before: the hidden message was announced *@
<BitToggle Label="Public profile" ErrorMessage="This conflicts with your privacy settings.">
    <ErrorMessageTemplate>This conflicts with your <a href="/privacy">privacy settings</a>.</ErrorMessageTemplate>
</BitToggle>

@* after *@
<BitToggle Label="Public profile" ErrorMessageAriaLabel="This conflicts with your privacy settings.">
    <ErrorMessageTemplate>This conflicts with your <a href="/privacy">privacy settings</a>.</ErrorMessageTemplate>
</BitToggle>
```

### A `BitDropdown` or `BitChoiceGroup` of options of another `TValue` throws ([#14383](https://github.com/bitfoundation/bitplatform/issues/14383))

An option finds its `BitDropdown` or `BitChoiceGroup` through a cascade typed by its own `TValue`, so the options of
another one (the `BitDropdownOption<Guid>` children of a `BitDropdown<BitDropdownOption<Guid>, Guid?>`) never reached it
and the component rendered without a single one of them. It now throws an `InvalidOperationException` naming the type
to use as it initializes, whatever its `NameSelectors`. **An app with such a component** gives its options the
`TValue` of the component: `BitDropdownOption<Guid?>`. The built-in items are not affected: a
`BitDropdownItem<Guid>` in a dropdown of `Guid?` is read as the `BitDropdownItem<Guid?>` it stands for.

### `BitDataGrid` leaves the clicks on a control in a cell to the control ([#14316](https://github.com/bitfoundation/bitplatform/issues/14316))

A click on a control in a cell - a button, a link or a field of a cell's template, an ARIA widget, an element in the
tab sequence, the grid's own checkbox and toggles, anything in a dialog or in a grid rendered in a cell - also reached
the cell and the row, which selected the row, toggled its detail (`ExpandDetailOnRowClick`), opened the cell's editor
on a double-click and raised `OnRowClick`, `OnRowDoubleClick`, `OnCellClick`, `OnCellDoubleClick` and
`OnCellContextMenu` on top of what the control did. The grid now leaves such a click to the control: none of that
happens, and a right-click on the control keeps the browser's own menu where `OnCellContextMenu` suppressed it. The
double-clicks on the grid's own control cells are no longer the row's either. **An app that read the clicked row from
the selection or from one of these events** in the handler of a control of its template (an Edit button reading
`SelectedItems`) takes the item from the template's context instead. **A control whose click should still reach the
row** carries `data-bit-interactive="false"`, and an element of a template the grid does not take for a control (an
icon with an `@onclick`) carries `data-bit-interactive` to keep its clicks from the row.

The drag of a draggable `BitDialog` / `BitModal` goes by the same definition of a control: it no longer starts on an
element marked `data-bit-interactive`, an `a` without an `href`, an element in the tab sequence or an ARIA widget it
did not know (`menuitemcheckbox`, `menuitemradio`, `treeitem`, `searchbox`, a media player's controls), and now does
start on a `label` without a control and on a `summary` that is not the first of a `details` element.

### A `BitDataGrid` none of whose cells takes the focus is a table

The grid role promises the keyboard of the ARIA grid pattern: a cell takes the focus and the arrow keys move it. A
`BitDataGrid` that does not provide that keyboard is now announced as a table: its root is `role="table"` and every
cell that heads nothing is `role="cell"`, with no `aria-multiselectable`. That is a grid without `CellNavigation`,
without `Single` selection or `Cell` editing (which turn it on), that is not a tree. The rest is unchanged: rows,
column and row headers, the counts and
indexes, `aria-sort`, `aria-selected` and the accessible name. No C# API changes. **An app's test that finds such a grid
or its cells by role** (`GetByRole(AriaRole.Grid)`, `[role=grid]`, `[role=gridcell]`) looks for `table` and `cell`
instead, or turns `CellNavigation` on if the grid should be navigable cell by cell.

### An input in an `EditForm` carries the framework's field classes

The framework's inputs (`InputText`, `InputSelect`, ...) carry the classes their `EditContext` gives their field:
`valid` or `invalid`, and `modified` once the field has changed - or the names of the app's `FieldCssClassProvider`
(`EditContext.SetFieldCssClassProvider`) instead. The inputs built on `BitInputBase` now carry them too, on their root
beside `bit-inv`: `BitTextField`, `BitDropdown`, `BitDatePicker` and the rest of the core inputs, and the Extras'
`BitPhoneInput`, `BitCountryPicker`, `BitTimeZonePicker` and `BitMarkdownEditor`, whenever one is bound to a field of a
form and not `NoValidate`. No C# API changes. **An app whose stylesheet styles these classes** - the Blazor templates'
`app.css` has `.invalid { outline: 1px solid red; }` and `.valid.modified:not([type=checkbox]) { outline: 1px solid
#26b050; }` - now draws that around the whole input, its label and its messages included. Scope such a rule to the
native controls (`:is(input, select, textarea).invalid`) to keep it on the framework's inputs, which leaves the bit
inputs to their own invalid look (`bit-inv`), or remove it. **A test that compares the whole `class` attribute** of an
input's root in a form expects the new classes.

## 10.7.0 (first shipped in 10.7.0-pre-01)

### `BitFullCalendarChangeNotifier` passes on whether to show the refusal notice ([#13827](https://github.com/bitfoundation/bitplatform/issues/13827))

`BitFullCalendarChangeNotifier.RefusalReporter` is now an `Action<BitFullCalendarChangeRefusal, bool>`, whose second
argument says whether the calendar shows its own notice for the refusal: a dialog that already shows the reason
inline turns it off. `ReportRefusal` takes it as an optional `showNotice`, `true` by default, so a call to it compiles
as before, and an assembly compiled against the earlier one has to be compiled again. **An app that sets
`RefusalReporter` itself** takes the second argument: `(refusal, showNotice) => ...`.

### A published WebAssembly app gets the scripts and stylesheets of the components it uses ([#13876](https://github.com/bitfoundation/bitplatform/issues/13876))

The packages' bundles (`bit.blazorui.js`, `bit.blazorui.css` and its Fluent variants, the Extras' and the Legacy
package's) are put back together at publish from only the pieces an app can use - the components ILLink kept, the
scripts they call - and the vendored libraries nothing loads (pdf.js, quill, chart.js, marked, Leaflet, OpenLayers, the
flags) are left out of the publish. A standalone Blazor WebAssembly app gets this by default; a build, `dotnet run` and
`dotnet watch` are unchanged. **An app that renders a component the code never names** (by reflection, through a
`DynamicComponent` built from a type name), **uses a component's classes in markup of its own without the component**,
**or calls `BitBlazorUI.*` from JavaScript the publish cannot read** (loaded from a CDN, built outside the app's
projects - the `.js` and `.html` files of those are read) can lose what it needs in production only: name it with
`<BitBlazorUIComponent Include="..." />` or `<BitBlazorUIAsset Include="..." />` in the csproj, or turn the trimming off
with `<BitBlazorUITrimAssets>false</BitBlazorUITrimAssets>`. A Blazor Web App or Blazor Server app is unchanged until it
turns the trimming on - or publishes with `PublishTrimmed`, which turns it on the way it does in a WebAssembly app (a
server hosting a WebAssembly client excepted, whose trimmer knows the server alone).

The packages no longer ship source maps (`.css.map`): they pointed at stylesheet sources the packages do not carry.

### A trimmed app without the modules of the lazy scripts leaves out their map ([#14005](https://github.com/bitfoundation/bitplatform/issues/14005))

Each package's assembly embeds `bit-lazy-scripts.json`, the map the library imports the modules of the lazy scripts by.
A trimmed publish of an app that publishes none of the modules - an app in bundle mode, unless
`BitBlazorUIIncludeScriptModules` is `true` - now leaves it out, since nothing of the app can import them. **An app that
turns the lazy scripts on at runtime** (`BitBlazorUIScripts.UseLazyScripts`, `AddBitBlazorUIServices(o => o.LazyScripts =
true)`) without `BitBlazorUILazyScripts` - one importing the modules from a `ContentPath` elsewhere too - sets
`<BitBlazorUIIncludeScriptModules>true</BitBlazorUIIncludeScriptModules>` in the csproj: without the map, its calls into
the scripts find no module to import.

### The item type of the item-based components keeps its public properties in a trimmed app

`BitDropdown`, `BitChoiceGroup`, `BitButtonGroup`, `BitMenuButton`, `BitNav`, `BitNavBar`, `BitNavPanel`,
`BitBreadcrumb`, `BitTimeline`, `BitAccordionList` and `BitDataGrid` (with `BitDataGridColumn`) read an app's own item
type by the names of its properties - the `NameSelectors` names, a column's `Field`. A trimmed app could lose a
property it never read in code of its own, and the component then showed nothing for it. Their `TItem` now carries
`[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)]`, so the type's public properties are
kept wherever it is handed to one of them. Nothing changes for an app that names its item type (`BitDropdown<MyItem,
string>`). **A generic component or method of an app's own that passes its type parameter on as one of these
`TItem`s** gets the trim analysis warning IL2091 when it runs the trim analyzers; give its type parameter the same
attribute. A `BitDataGridColumn.Field` path that reaches a nested type ("Address.City") still depends on the app
reading that nested property somewhere itself; `Property="x => x.Address.City"` names every member and keeps them all.

### Rate limiters run their callback on the caller's context

`BitDebouncer.Do` and `BitThrottler.Do` used to run the callback through `Task.Run`, on the thread pool. They
now wait out the delay and run it on the context `Do` was called from - the component's renderer when a component
calls it - so a callback that calls `StateHasChanged` or changes the component's state works without
`InvokeAsync`. This matters to Blazor Server and Hybrid apps; on single-threaded WebAssembly the callback already
ran on the one thread there is. **A callback that does blocking or CPU-heavy work and relied on running off the
renderer** now holds the renderer (on Blazor Server, the circuit) for as long as it runs; it offloads that work
itself:

```csharp
await _debouncer.Do(300, async () =>
{
    _results = await Task.Run(() => Search(_term)); // the heavy part, off the renderer
    StateHasChanged();
});
```

`BitThrottler` also opens again after a callback that throws: it used to stay shut, dropping every later `Do`
call for the life of the instance. An exception the callback throws, other than a cancellation, reaches the
awaiter of the `Do` call that ran it, as before; a caller that does not await `Do` does not observe it.

### `BitRichTextEditor` waits for the sanitizer in its parameter pass

An updated `Value` is sanitized through the editor's script and pushed into the editor as part of the parameter
pass, which now awaits both; they used to be started there and finish after it, so the render that followed a
`Value` change could run before the sanitized value was in place. That render now sees the sanitized value, and
the cleaned HTML written back to `@bind-Value` is written in the same pass. An interop call that fails there (a
circuit that has gone away, a script that is not available) ends the sync and leaves the editor's content as it
was, rather than failing the parameter pass. Nothing changes in an app's code.

### One Escape does one thing: a component that acts on the key takes it from the surface around it ([#13545](https://github.com/bitfoundation/bitplatform/issues/13545))

An Escape pressed in a component inside a surface that closes on Escape - `BitDialog`, `BitModal`, `BitPanel`, a
callout (`BitCallout` and the popups of the dropdowns, pickers and menus), a shown `BitTooltip`, a `BitMessage`
that dismisses on it - used to reach both: a `BitTextField` with `ClearOnEscape` emptied itself and the dialog
around it closed on the same press. Now whoever acts on the key takes it by preventing its default, every surface
leaves an Escape whose default is prevented alone, and the surface closes on the next press:

- A field that clears on Escape takes the key only while it has something to clear (`BitTextField` with
  `ClearOnEscape`, `BitSearchBox`, `BitNumberField`, `BitTagsInput`), and a component whose popup is open (a
  dropdown's list, a picker's calendar, a nested callout) closes that popup alone. The components of
  `Bit.BlazorUI.Extras` that act on the key themselves (`BitDataGrid`, `BitRichTextEditor`, `BitMarkdownEditor`,
  `BitNavPanel`, `BitFullCalendar`'s dialogs, ...) take it the same way.
- An Escape with Shift, Ctrl, Alt or Meta held is left to the surface: a field no longer clears on it. A
  `BitTextField` bound one way (a `Value` with neither `ValueChanged` nor `OnChange`) no longer clears on
  Escape at all.
- An Escape pressed to cancel an IME composition reaches no .NET keydown handler any more, an app's own included:
  it is stopped on the document element ahead of Blazor's event delegation, in every browser.

**An app's own control inside one of these surfaces that acts on Escape in a Blazor `@onkeydown`** - and should
keep the surface open while it does - says so in its markup, with `data-bit-esc="claim"` on the element its
handler listens on, rendered while the press does something (or `"text"`: only while that input holds text). A
script of the app's own calls `preventDefault()` on the keydown instead. An app's own listener that closes
something on Escape checks `event.defaultPrevented` to follow the same rule; and one that prevents the default of
an Escape for a reason of its own now keeps the library's surfaces open on that press. A test or a flow that
pressed Escape once to clear a field and close the dialog around it presses it twice.

```razor
@* OnFilterKeyDown empties the filter on Escape; "text" takes the key only while the input holds text *@
<input @bind="_filter" @onkeydown="OnFilterKeyDown" data-bit-esc="text" />
```

The date, date range, time and circular time pickers and `BitPhoneInput` also close their popup when the focus
moves on from both the field and the popup (a Tab out of the field), rather than leaving it open behind its
overlay.

### Dialogs, modals, panels and callouts no longer stop the keys pressed inside them ([#13565](https://github.com/bitfoundation/bitplatform/issues/13565))

`BitDialog`, `BitModal`, `BitPanel` (one that closed on Escape) and `BitCallout` stopped the keydown events
pressed inside them at their root (`@onkeydown:stopPropagation`), so a Blazor `@onkeydown` an app wrote on an
element around one never heard a key pressed inside it. They now let every key through, so the page's own
keyboard shortcuts around them (Ctrl+S) keep working while one is open; a nested surface's Escape is told from
the outer one's own in script instead, so one press still closes only the innermost surface. **An app's handler
around one of these components that relied on not hearing the keys typed inside it** - a page shortcut on a
letter, Enter or Delete that would now fire while the user types into a field of the dialog - checks where the
key came from, or whether the surface is open, itself. A `@onkeydown:stopPropagation` of the app's own around the
surface's content is not the way: `BitModal`, `BitPanel` and `BitCallout` listen on their root, above that
content, for the Escape they close on.

### Components no longer run past the side of a narrow container ([#13834](https://github.com/bitfoundation/bitplatform/issues/13834))

A component in a horizontally scrolling box, or on a phone, widened the page around it: the visually hidden text it
keeps for screen readers is absolutely positioned, and with nothing of the component positioned it was placed
against whatever positioned ancestor the page had, outside the box that scrolls. The element each such text is
rendered in now declares `position: relative`:

- the root of `BitButton`, `BitChart`, `BitCheckbox`, `BitCountryPicker`, `BitFileInput`, `BitFileUpload`,
  `BitLink`, `BitLoading`, `BitMarkdownViewer`, `BitNumberField`, `BitOtpInput`, `BitPhoneInput`, `BitRating`,
  `BitSlider`, `BitTag`, `BitTimeZonePicker` and `BitToggle`;
- a part of `BitBreadcrumb` (an item), `BitDropdown` (a chip, the text of the field), `BitFullCalendar` (a week
  number cell), `BitMap` (a column header of the marker list), `BitMessage` (the content wrapper), `BitNav` (the
  suffix of an item), `BitNavPanel` (its content), `BitPdfViewer` (a toolbar group), `BitPhoneInput` (the country
  button) and `BitTimeZonePicker` (an option's text).

An element of your own that is absolutely positioned inside the content of one of them (a corner marker in a
button's child content, in a tag, in a message) is now placed against that element rather than against an ancestor
further out. To keep it where it was, position the element you meant it to follow.

A few components also give up width they used to hold, so they fit the container they are given:

- **`BitOtpInput`**: the root is never wider than its container, and its boxes narrow evenly when the row would be.
- **`BitNumberField`**: a label longer than its line wraps, as `BitTextField`'s does, rather than holding one line.
- **`BitSearchBox`**: the input gives up the width of its `size` attribute, so the field shrinks in a flex row.
- **`BitPhoneInput`**: the root is never wider than its container.
- **`BitTimeline`**: a `--bit-Timeline-content-gap` wider than half the dot no longer pushes the items past the side.
- **`BitMap`**: the on-screen marker list scrolls sideways in its own box.

### `BitColorPicker`, `BitFileInput` and `BitFileUpload` are form inputs ([#1530](https://github.com/bitfoundation/bitplatform/issues/1530))

The three were the last inputs that did not derive from `BitInputBase<TValue>`, so none of them took part in an
`EditForm`: a `[Required]` on the property they were bound to was only checked on submit, they never turned red,
and a `ValidationMessage` beside them never updated as the user picked. All three now derive from
`BitInputBase<string?>`, which gives them `Value` / `ValueChanged` / `ValueExpression` (`@bind-Value`),
`DefaultValue`, `DisplayName`, `Name`, `Required`, `ReadOnly`, `NoValidate`, `InputHtmlAttributes` and the base
`OnChange`, exactly as on every other input. Their `Bit*Params` derive from `BitInputBaseParams<string?>`, so
`ReadOnly` and `Required` cascade through `BitParams` too.

What a form reads from each:

- **`BitColorPicker`**: the color, as the CSS string it has always been written in. A bound null stays null (the
  picker shows white) until a color is picked, so a required picker starts invalid.
- **`BitFileInput`**: the names of the valid files of the list joined with commas (`report.pdf,photo.png`), or
  null while there is none. A file the component's own validations rejected is not part of it.
- **`BitFileUpload`**: the names of the files that are part of the selection, joined the same way: a preloaded
  file and one still waiting to upload count; one that was rejected, failed, was canceled or removed does not.

A browser cannot be handed files from code, so the value of the two file components is theirs to write: setting
it to null or empty from outside clears the selection (a form reset, a model cleared after a save - the preloaded
files of a `BitFileUpload` stay, and its value names them again), and any other value is left until the selection
next changes.

The `EditContext` hears of what the user did - a pick, a removal, an upload outcome - and not of what only brings
the value in step with the app: preloaded files, an emptied value, `Reset()` (and so an `AutoReset` browse) or a
picker's `Format` changed at runtime write the value without marking the field modified or validating it. The base
`OnChange` of `BitFileUpload` is not invoked for a failed upload, which is the server's answer rather than an edit.

#### Renamed and removed parameters

| Before | After |
|---|---|
| `BitColorPicker.Color` / `ColorChanged` (`@bind-Color`) | `Value` / `ValueChanged` (`@bind-Value`) |
| `BitColorPicker.OnChange` (`EventCallback<BitColorChangeEventArgs>`) | `OnColorChange`; the base `OnChange` is now an `EventCallback<string?>` carrying the value |
| `BitColorPicker.ReadOnly` | inherited from `BitInputBase`, same behaviour |
| `BitColorPickerParams.ReadOnly` | inherited from `BitInputBaseParams`, same behaviour |
| `BitFileInput.OnChange` (`EventCallback<BitFileInputInfo[]>`) | `OnFilesChange`; the base `OnChange` carries the value |
| `BitFileUpload.OnChange` (`EventCallback<BitFileInfo[]>`) | `OnFilesChange`; the base `OnChange` carries the value |

`BitColorPicker.OnChangeEnd` keeps its name and its `BitColorChangeEventArgs`. `BitColorPicker.FocusAsync` is now
an override of the base one; `BitFileInput` and `BitFileUpload` gain `FocusAsync`, which focuses the browse button.

```razor
@* before *@
<BitColorPicker @bind-Color="model.Color" OnChange="HandleColorChange" />
<BitFileInput OnChange="HandleFilesChange" />
<BitFileUpload OnChange="HandleFilesChange" UploadUrl="/upload" />

@* after *@
<BitColorPicker @bind-Value="model.Color" OnColorChange="HandleColorChange" />
<BitFileInput OnFilesChange="HandleFilesChange" />
<BitFileUpload OnFilesChange="HandleFilesChange" UploadUrl="/upload" />
```

An `OnChange` handler left in place either fails to compile (its parameter is not a `string`) or, for a handler
of no parameters, silently moves to the base callback - which for the picker fires on the value only, and for the
file components only when the value moves rather than on every status change. Rename it.

#### Behaviour that changes without a rename

- `BitFileInput` and `BitFileUpload` now have a `ReadOnly`: a read-only one opens no file dialog, takes no drop or
  paste, and refuses its remove button (and, for the upload, its upload, pause, cancel, retry and batch buttons),
  which stay focusable and carry `aria-disabled="true"`. `Browse()` does nothing on it either.
- A read-only `BitFileInput` or `BitFileUpload` also renders its native file input `disabled`, so a `<label
  for="@fileInput.InputId">` of the app's own opens no dialog on it.
- `BitFileInput.Reset()` and `BitFileUpload.Reset()` still invoke none of the change callbacks, but a bound
  `Value` follows the cleared list through `ValueChanged`, without the `EditContext` being told.
- Each of the three renders one more element, a visually hidden `<input class="bit-input-hidden">` at the end of
  its root that carries the value in a form: `Name` and `InputHtmlAttributes` land on it. `Required` does not: the
  native attribute on a control nobody can see would make the browser block the submit of the `EditForm` over a
  message it cannot show, so a required value is left to the model's validation, as on `BitToggle`. A selector or
  test locator that took "the last input" of the component picks it up now.
- A failed validation adds `bit-inv` to the root, as on every input: the picker's borders and focus ring and the
  browse button of the file components turn to the error color (`--bit-ColorPicker-invalid-color`,
  `--bit-FileInput-invalid-color`, `--bit-FileUpload-invalid-color`), and the browse button carries
  `aria-invalid="true"`. A required picker with a label draws the asterisk after it
  (`--bit-ColorPicker-required-color`).

#### Known consumers in this repo that break

The Boilerplate template (`src/Templates/Boilerplate` then, now `src/Boilerplate` in [bitfoundation/bitfabric](https://github.com/bitfoundation/bitfabric)) is outside the BlazorUI edit boundary and was
**not** updated:

| File | Line | Use |
|---|---|---|
| `Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor` | 69 | `@bind-Color="category.Color"` -> `@bind-Value="category.Color"` (the `ValidationMessage` beside it then updates as the user picks) |
| `Boilerplate.Client.Core/Components/Layout/AppAiChatPanel.razor` | 235 | `OnChange="HandleOnAttachmentChange"` -> `OnFilesChange="HandleOnAttachmentChange"` |

(Paths are relative to `src/Templates/Boilerplate/Bit.Boilerplate/src/Client/`, now `src/Boilerplate/Bit.Boilerplate/src/Client/` in bitfoundation/bitfabric.)

### A modal container of an app's own reads its modals through `ModalReferences` ([#13558](https://github.com/bitfoundation/bitplatform/issues/13558))

The list of modals `BitModalContainerBase<TReference, TParameters>` kept is no longer a `protected` field:
`_modalRefs` is now the read-only `ModalReferences`, since a container that changed the list itself left the
container's own record of each modal (its exit animation, its merged parameters) out of step with it.
**A container that derived from the base and read `_modalRefs`** reads `ModalReferences` instead. One that
rendered its modals by cascading the reference and the parameters itself renders them through
`RenderModals()` - or `RenderModals(template)` to put markup of its own around each modal - which is the only
way a closed modal plays its exit animation; a container for a `BitModalService` is simplest derived from
`BitModalContainer`, which already names the service, merges the parameters and honours `CloseOnNavigation`:

```razor
@inherits BitModalContainer

@(RenderModals(item => @<div class="modal-frame">@item.Modal</div>))
```

The modals a container renders also render only when something of their own changes - their parameters
(`Update`, `Refresh`, the container's `ModalParameters`) or their closing - rather than whenever any modal is
shown or closed. Content that reads its `BitModalReference` is still handed it again on each of those changes,
so it follows the reference's `Parameters` and `IsClosed`; content that relied on being rendered again because
another modal came or went calls `StateHasChanged` itself.

### A disabled native control now carries `aria-disabled="true"` beside `disabled` ([#13549](https://github.com/bitfoundation/bitplatform/issues/13549))

Components marked a disabled control in different ways: some wrote the native `disabled` attribute alone, some
`aria-disabled="true"` alone, some both - the date picker's calendar buttons wrote both while the identical ones
of the date range picker wrote `disabled` only - so no one selector or test locator found a disabled control. Every
native control a component renders disabled (`button`, `input`, `select`, `textarea`, `option`, ...) now writes
both, so `:disabled` and `[aria-disabled="true"]` each find it, and a control kept focusable while unavailable (a
loading button, `AllowDisabledFocus`, a link) goes on writing `aria-disabled="true"` alone. The root of every
disabled component carries the `bit-dis` class, as it already did, which is the hook for a disabled component as a
whole.

What changes in the markup: about three hundred controls across the inputs, pickers, navs, lists, surfaces and the
`Bit.BlazorUI.Extras` components (`BitDataGrid`, `BitRichTextEditor`, `BitPdfViewer`, `BitMarkdownEditor`,
`BitPhoneInput`, `BitFullCalendar`, ...) and the paginator of the `Bit.BlazorUI.Legacy` data grid gain
`aria-disabled="true"` while disabled; and a few that wrote `aria-disabled` for another state only (loading,
read-only, nothing to act on) now also write it while disabled - the Ok button of `BitDialog`, the batch actions of
`BitFileUpload`, the time buttons of `BitCalendar`, the eye dropper and presets of `BitColorPicker`, the button of
`BitDropMenu` and of `BitTag`. **A rule or locator meant for the controls
that are only `aria-disabled`** - the focusable ones - now matches the natively disabled ones too: add
`:not(:disabled)` to it. One that expected no `aria-disabled` on a disabled control checks `disabled` instead.

A boolean written into an input's `InputHtmlAttributes` for an attribute the input also writes itself
(`aria-disabled`, `aria-invalid`, `aria-label`, `spellcheck`, ...) is now rendered the way Blazor renders any
splatted boolean - with no value while it is `true`, left out while it is `false` - rather than as the text `True`
or `False`. Write the string (`["aria-disabled"] = "true"`) for a value of its own.

### A duration an app sets on a component follows reduced motion like the theme's own ([#13552](https://github.com/bitfoundation/bitplatform/issues/13552))

Under `prefers-reduced-motion: reduce` the theme's `--bit-mot-duration*` tokens collapse, and the app restores them
by withdrawing that at `:root` or by `ForceAnimation` (the `bit-fam` class) on a component or an element around it.
A duration the page set on a component through a parameter did not follow them: `BitCarousel`'s and `BitSwiper`'s
`AnimationDuration`, `BitCollapse`'s `Duration` / `Delay` / `ExpandDuration` / `CollapseDuration`,
`BitAccordion`'s and `BitSnackBar`'s `TransitionDuration` each collapsed through a reduced-motion query of the
component's own that only `ForceAnimation` undid, and `BitChart` took its entry animations out the same way. Every
duration the page sets - those parameters, the chart's animation options and the public
`--bit-<Component>-*-duration` variables alike - is now scaled by the new `--bit-mot-duration-factor` token, 1
normally and 0 under the preference, so it collapses (to 0.01ms, and a delay to nothing) exactly where the theme's
durations do and comes back wherever they are restored. **An app that restores the motion tokens at `:root`, to
animate regardless of the preference, now gets these animations back as well**; one that wants them to stay
collapsed there sets `--bit-mot-duration-factor: 0` itself. To scale every duration the page sets on components,
an app sets `--bit-mot-duration-factor-full` in CSS, which the preference still collapses;
`BitThemeMotion.DurationFactor` writes the effective token, which outranks the preference, so it is left unset
unless the app answers the preference itself.

Two reduced-motion rules that ignored `ForceAnimation` now honour it: `BitPivot` keeps the smooth scrolling of its
headers, and a `BitDialog` whose dismissal is refused shakes rather than showing the ring drawn in place of the
shake.

### A disabled component no longer runs the activation handlers written on it ([#13547](https://github.com/bitfoundation/bitplatform/issues/13547))

An HTML event handler written straight on a component (`<BitToggle Disabled="true" @onclick="...">`) lands on
its root element, and it used to run while the component was disabled: a screen reader's click ignores the
`pointer-events: none` of the disabled state, a click or key of an enabled control inside the component bubbles
up to its root, and several components do not turn the pointer off at all. A disabled component now drops the
handlers of the events an element is activated through - `onclick`, `ondblclick`, `onauxclick`,
`oncontextmenu`, `onmousedown` / `onmouseup`, `onpointerdown` / `onpointerup`, `ontouchstart` / `ontouchend`,
`onkeydown` / `onkeyup` / `onkeypress` - the way `BitElement` already did and a disabled form control does.
Hover and focus handlers are kept, and every handler is back as soon as the component is enabled again.

This applies to the buttons (`BitActionButton`, `BitButton`, `BitButtonGroup`, `BitMenuButton`,
`BitToggleButton`), every input, `BitTimeline`, the navs (`BitBreadcrumb`, `BitDropMenu`, `BitNav`,
`BitNavBar`, `BitPagination`, and `BitPivotItem` - disabled on its own or by its `BitPivot`), `BitPersona`,
`BitTag`, `BitCard`, `BitIcon`, `BitImage`, `BitLink`, and in `Bit.BlazorUI.Extras` to `BitFlag`,
`BitPhoneInput`, `BitThemeSwitcher`, `BitAccentColorSwitcher`, `BitRichTextEditor`, `BitMarkdownEditor`,
`BitDataGrid`, `BitChart` and `BitPdfViewer`. A component whose `Disabled` only switches one behaviour off
while the content it hosts stays live keeps running them: `BitBadge`, `BitTooltip`, `BitCallout`,
`BitAccordion` and `BitAccordionList` (their panels), `BitCarousel` and `BitSwiper` (their slides), `BitPivot`
(the panel of its open tab), `BitMap` (its overlay), `BitMessageBox`, `BitNavPanel`, and the other panels,
dialogs and layouts. **An app that relied on a handler of a disabled component running** - to explain why it is
disabled, say - puts it on an element of its own around the component.

`BitButtonGroup` also turns an item's click away for the group's own `Disabled`, not only for the item's
`IsDisabled`: with `DisabledInteractive`, a disabled group used to still run `OnItemClick`, the item's
`OnClick` and its toggle on a key press. The link items of a disabled group are out of the tab order.

### A nested component no longer inherits what an outer one's parameters set ([#13535](https://github.com/bitfoundation/bitplatform/issues/13535))

A component hands what its parameters ask for to its stylesheet as private custom properties
(`--bit-<prefix>-*`), set by a class or an inline style on its root, and custom properties inherit: an instance
rendered in the content of another one of its kind read the outer one's values wherever it set none of its own -
a `BitDialog` in a dialog given a `Width`, a `BitGrid` in a grid given a `MinItemWidth`, a `BitStack` in a stack
given a direction or a gap, a `BitPanel` in a panel. Every component's root now resets every private property its
parameters publish (to `initial`), so an inner instance goes by its own parameters and its defaults alone;
`BitCard` also registers the ones only its root reads (its shadows and border width) as non-inheriting, so an
ancestor cannot reach them at all. The private properties were never public API: **an app that set one on an
ancestor to restyle the components under it** sets the component's public `--bit-<Component>-*` variable instead,
which inherits as before - one set on an instance's `Style` reaches the instances nested in it too; to restyle one
instance alone, set its parameter, or the property itself through `Style` / `Styles` / `Classes`.

### An app's class keeps painting a part while it is hovered, pressed or focused ([#13542](https://github.com/bitfoundation/bitplatform/issues/13542))

The hover, pressed and focus states of the components used to paint their parts' `color`, `background`,
`border-color`, `fill` and `stroke` themselves, in rules more specific than the single class an app hands a
part through `Class` or `Classes`. So a `Class="brand-btn"` with `.brand-btn { background: purple }` painted the
button at rest and lost to the library's hover color as soon as the pointer was over it. Those states now
change a private variable that the rule at rest paints from, so a part an app paints keeps that paint in
every state it keeps it at rest. This applies to every component in `Bit.BlazorUI` and `Bit.BlazorUI.Extras`.

Nothing changes for an app that restyles through the public `--bit-<Component>-*-hover-*` / `-active-*`
variables, or one that does not paint a part through its own class. **An app that paints a part through its
own class and relied on the library's hover, pressed or focus color showing over it now keeps its own color in
those states.** To bring back state feedback, style the state in the app's own rule:

```css
.brand-btn { background: purple; }
.brand-btn:hover { background: rebeccapurple; }
```

Disabled states, `forced-colors` mode, the browser's scrollbar parts and the Leaflet / OpenLayers controls
of `BitMap` still paint directly, as before. One side effect in `forced-colors` mode: a part that is drawn
in system colors at rest (today or the selected day of a calendar, a checked or toggled button, an active
toolbar button) now keeps those colors under the pointer, where a library hover rule used to paint theme
colors over them.

### A parameter set on a component wins over the public CSS variable that restyles it ([#13483](https://github.com/bitfoundation/bitplatform/issues/13483))

The public `--bit-<Component>-*` variables were read ahead of the value a parameter published
(`color: var(--bit-Button-color, var(--bit-btn-clr-txt))`), so a variable set on `:root` or an ancestor to restyle
the buttons of an app also repainted, resized or reshaped every button that asked for something else with its
`Color`, `Size` or `Rounded`. The variables now restyle the default only: every read ranks the parameter first, then
the variable, then the default the unset parameter stands for, so `:root { --bit-Badge-background: gray }` leaves
`<BitBadge Color="BitColor.Error">` red while it repaints every badge that left `Color` unset. This holds for the
`Color`, `Size`, `Shape` / `Rounded` and the value parameters (`Height`, `Gap`, ...) of every component of
`Bit.BlazorUI` and `Bit.BlazorUI.Extras` that has public variables for what they set; a value cascaded from a
`BitParams` ancestor counts as set. What a parameter does not paint stays the variable's: an Outline badge's
transparent background is its `Variant`'s, so `--bit-Badge-background` still fills it under an explicit `Color`.
Each component's demo page says which parameter wins over which variable. **An app that sets these parameters
explicitly and also restyles the components through their variables** loses the variable on those instances:
drop the parameter where the variable should decide, or restyle the role itself through the theme's color tokens
(`--bit-clr-pri`, ...).

What changes in the markup: a `Color`, `Size` or `Shape` left unset no longer renders the class of its default - a
`<BitButton>` used to carry `bit-btn-pri bit-btn-md` and now carries neither, a `<BitTag>` drops
`bit-tag-pri bit-tag-md bit-tag-rnd`, and the same goes for the default classes of the other buttons, the inputs,
the navs, `BitBadge`, `BitCard`, `BitIcon`, `BitLink`, `BitMessage`, `BitPersona`, `BitProgress`, `BitSnackBar`,
`BitTimeline` and the rest. **A rule or test locator written against a default class** (`.bit-btn-pri`) now matches
only the components that set the parameter: target the root class (`.bit-btn`), or set the parameter.

`BitSnackBarService.Show` takes its `color` as `BitColor?`, defaulting to none rather than `BitColor.Info`: a snack
bar shown without a color is still drawn as Info, unless the `--bit-SnackBar-*` color variables say otherwise, while
`Info(...)` and a color passed in win over them. A call compiled against the earlier signature keeps working. The
buttons of `BitMessageBox` no longer pass their neutral default as `Color`, so an app's `--bit-Button-*` variables
now restyle them too.

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

### Core components draw their own spinner and skeleton instead of rendering other components ([#13476](https://github.com/bitfoundation/bitplatform/issues/13476))

A component of `Bit.BlazorUI` no longer renders another public component of the library inside it, so a restyle
of one - its `--bit-<Component>-*` variables, a `BitParams` cascade, an app's rule for its classes - no longer
carries into another. Two components did, and now draw the part themselves:

| Component | Before | After |
|---|---|---|
| `BitTextField` while `Loading`, with no `LoadingTemplate` | a `BitSpinnerLoading` | `<div class="bit-tfl-spn" aria-hidden="true">`, restyled through `Classes.Spinner` / `Styles.Spinner` and `--bit-TextField-spinner-size` / `-color` / `-track-color` |
| `BitCard` while loading, with no `LoadingTemplate` | three `BitShimmer`s in `.bit-crd-skl` | three `<div class="bit-crd-skb">` bars in `.bit-crd-skl` (now `aria-hidden="true"`), restyled through `Classes.Skeleton` / `Styles.Skeleton`, `Classes.SkeletonBar` / `Styles.SkeletonBar` and `--bit-Card-skeleton-background` / `-color` |

**An app that restyled these parts through the inner component** - a `BitSpinnerLoading` or `BitShimmer` rule, its
variables or its `BitParams` reaching into the field or the card - restyles them through the members and variables
above instead. A test that found the inner component in the rendered markup looks for the new elements.

Every inline spinner of the library (the buttons, `BitCheckbox`, `BitToggle`, `BitDropdown`, `BitSearchBox`,
`BitTagsInput`, `BitFileUpload`, `BitDropMenu`, `BitAccordion`, `BitDialog`, `BitSnackBar`, `BitBasicList` and, in
`Bit.BlazorUI.Extras`, `BitChart`, `BitDataGrid`, `BitInfiniteScrolling` and `BitMap`) now turns on the shared
`bit-spin` keyframes: each component's own (`bit-btn-spinner-animation`, `bit-srb-spin`, ...) is gone, so an app's
CSS that named one of them names `bit-spin`.

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

### `BitComponentBase.OnVisibilityChanged` is removed

The protected virtual `OnVisibilityChanged(BitVisibility)` hook of `BitComponentBase` was never called: an override
of it never ran, whatever `Visibility` was set to. It is removed. **A component of an app's own deriving from a bit
component that overrides it** no longer compiles (CS0115); act on the change where the parameter arrives instead,
comparing `Visibility` with the value it had in `OnParametersSet`, or read `Visibility` while rendering.

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
  placed `Left` / `Right` are now physical and keep their side in an RTL chart (**behavior**); the new
  `Start` / `End` placements put them beside the plot on the reading start / end, which is what the old
  `Left` / `Right` did, so migrate those as `Left` -> `Start` and `Right` -> `End` to keep a chart
  following the reading direction ([#13714](https://github.com/bitfoundation/bitplatform/issues/13714)).
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
  `-brg`, `-cst`, `-cen`, `-clf`, `-crg`, `-ctr`, and BitChart's side classes of a title, subtitle or legend
  beside the plot, `bit-cht-lft` / `bit-cht-rgt` / `bit-cht-sta` / `bit-cht-end` (the 11.0 previews' `bit-cht-ttl-l`
  is gone again: `bit-cht-lft` says the same).

#### Internal-but-public members

- `BitSwipeTrap._OnKeyTrigger` (a `[JSInvokable]` callback) now takes a `string` instead of
  `BitSwipeDirection`. It is called only by the library's own script.

#### Known consumers in this repo that break

The Boilerplate template (`src/Templates/Boilerplate` then, now `src/Boilerplate` in [bitfoundation/bitfabric](https://github.com/bitfoundation/bitfabric)) is outside the BlazorUI edit boundary and was
**not** updated. These lines will fail to compile against this branch:

| File | Line | Use |
|---|---|---|
| `Boilerplate.Client.Core/Components/Layout/AppConsentBanner.razor` | 12 | `Position="BitPanelPosition.Bottom"` -> `Placement="BitPlacement.Bottom"` |
| `Boilerplate.Client.Core/Components/Layout/AppSnackBar.razor` | 7 | `BitSnackBarPosition.TopCenter` -> `BitPosition.TopCenter` |
| `Boilerplate.Client.Core/Components/Pages/Categories/CategoriesPage.razor` | 58 | `BitDataGridColumnAlign.Center` -> `BitTextAlign.Center` |
| `Boilerplate.Client.Core/Components/Pages/Dashboard/ProductsPercentageWidget.razor.cs` | 19 | `Position = BitChartPosition.Right` -> `Placement = BitPlacement.End` (keeps the pie legend on the reading end in Persian and Arabic) |
| `Boilerplate.Client.Core/Components/Pages/Management/OperationsPage.razor` | 217 | `BitTagShape.Circular` -> `BitShape.Pill` |
| `Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor` | 60 | `BitDataGridColumnAlign.Right` -> `BitTextAlign.End` |
| `Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor` | 67 | `BitDataGridColumnAlign.Center` -> `BitTextAlign.Center` |
| `Boilerplate.Client.Core/Components/Pages/Tenants/ManageAllTenantsPage.razor` | 49 | `BitDataGridColumnAlign.Center` -> `BitTextAlign.Center` |

(Paths are relative to `src/Templates/Boilerplate/Bit.Boilerplate/src/Client/`, now `src/Boilerplate/Bit.Boilerplate/src/Client/` in bitfoundation/bitfabric.)

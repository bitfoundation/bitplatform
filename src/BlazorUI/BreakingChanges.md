# bit BlazorUI breaking changes

Breaking changes to the public API of the bit BlazorUI packages (`Bit.BlazorUI`, `Bit.BlazorUI.Extras`,
`Bit.BlazorUI.Assets`), newest version first, each with what to change in your code.

## 10.7.0 (first shipped in 10.7.0-pre-01)

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

The Boilerplate template (`src/Templates/Boilerplate`) is outside the BlazorUI edit boundary and was
**not** updated:

| File | Line | Use |
|---|---|---|
| `Boilerplate.Client.Core/Components/Pages/Categories/AddOrEditCategoryModal.razor` | 69 | `@bind-Color="category.Color"` -> `@bind-Value="category.Color"` (the `ValidationMessage` beside it then updates as the user picks) |
| `Boilerplate.Client.Core/Components/Layout/AppAiChatPanel.razor` | 235 | `OnChange="HandleOnAttachmentChange"` -> `OnFilesChange="HandleOnAttachmentChange"` |

(Paths are relative to `src/Templates/Boilerplate/Bit.Boilerplate/src/Client/`.)

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

The Boilerplate template (`src/Templates/Boilerplate`) is outside the BlazorUI edit boundary and was
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

(Paths are relative to `src/Templates/Boilerplate/Bit.Boilerplate/src/Client/`.)

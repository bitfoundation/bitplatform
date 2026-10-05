# bit BlazorUI breaking changes

Breaking changes to the public API of the bit BlazorUI packages (`Bit.BlazorUI`, `Bit.BlazorUI.Extras`,
`Bit.BlazorUI.Assets`), newest version first, each with what to change in your code.

## vNext (after 10.6.2)

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
take through to its element, but it would no longer disable anything. So it throws an
`InvalidOperationException` naming the component when it renders, rather than leaving a control meant to
be gated quietly enabled. Search the markup for `IsEnabled` before upgrading.

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
Read through the new default name, such a class would have every item it marks off selectable, so a
component reading the default `IsDisabled` off a type that has an `IsEnabled` property but no
`IsDisabled` one throws an `InvalidOperationException` that says which type to change.

**A lowercase `disabled` attribute written in markup is now the parameter.** The Razor compiler matches
the attributes written on a component to its parameters regardless of case, so `disabled` written on a
bit component used to be passed through to the element as an HTML attribute and now sets `Disabled`,
disabling the component (class, `aria-disabled`, tab order and events). A `disabled` key that only
arrives at runtime - in an `@attributes` dictionary or a `DynamicComponent`'s `Parameters` - is matched
by its exact name, so it is still passed through to the element as a plain HTML attribute, without any of
that. To disable a component, use `Disabled` itself.

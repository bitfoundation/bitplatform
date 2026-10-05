# bit BlazorUI breaking changes

Breaking changes to the public API of the bit BlazorUI packages (`Bit.BlazorUI`, `Bit.BlazorUI.Extras`,
`Bit.BlazorUI.Assets`), newest version first, each with what to change in your code.

## vNext (after 10.6.2)

### Boolean parameters are flags: none of them defaults to `true` ([#5527](https://github.com/bitfoundation/bitplatform/issues/5527))

Every boolean parameter now defaults to `false`, so it is switched on by writing its name alone
(`<BitButton Disabled>`) and never has to be written as `="false"`. A parameter that used to default to
`true` is renamed to its opposite, and its meaning is inverted along with its name.

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

**A lowercase `disabled` attribute is now the parameter.** Blazor matches component parameters regardless
of case, so `disabled` written on a bit component used to be passed through to the element as an HTML
attribute and now sets `Disabled`, disabling the component (class, `aria-disabled`, tab order and
events). To disable a component, use `Disabled` itself.

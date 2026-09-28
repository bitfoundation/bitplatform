namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Tag;

public partial class BitTagDemo
{
    private readonly string example1RazorCode = @"
<BitTag Text=""Basic tag"" />
<BitTag Text=""Design"" Color=""BitColor.Info"" />
<BitTag Text=""Archived"" Color=""BitColor.Tertiary"" />";

    private readonly string example2RazorCode = @"
<BitTag Text=""Fill"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Outline"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Text"" Variant=""BitVariant.Text"" />

<BitTag Text=""Rounded"" Shape=""BitTagShape.Rounded"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Circular"" Shape=""BitTagShape.Circular"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Square"" Shape=""BitTagShape.Square"" Variant=""BitVariant.Outline"" />

<BitTag Text=""Fill"" Variant=""BitVariant.Fill"" IsEnabled=""false"" />
<BitTag Text=""Outline"" Variant=""BitVariant.Outline"" Shape=""BitTagShape.Circular"" IsEnabled=""false"" />
<BitTag Text=""Text"" Variant=""BitVariant.Text"" IsEnabled=""false"" />";

    private readonly string example3RazorCode = @"
<BitTag Text=""Calendar"" IconName=""@BitIconName.Calendar"" />
<BitTag Text=""Status"" SecondaryIconName=""@BitIconName.ChevronDown"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Both ends"" IconName=""@BitIconName.Tag"" SecondaryIconName=""@BitIconName.ChevronDown"" Color=""BitColor.Info"" />
<BitTag Text=""Reversed"" IconName=""@BitIconName.Tag"" SecondaryIconName=""@BitIconName.ChevronDown"" Color=""BitColor.Success"" Reversed />";

    private readonly string example4RazorCode = @"
<BitTag Text=""Annie Lindqvist"" IconUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" Variant=""BitVariant.Outline"" />

<BitTag Text=""Annie Lindqvist"" SecondaryText=""Software engineer"" IconUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png""
        Color=""BitColor.Tertiary"" />

<BitTag Text=""Awaiting review"" IconUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png""
        IconAlt=""Assigned to Annie Lindqvist"" Color=""BitColor.Warning"" />

<BitTag Text=""Storage"" SecondaryText=""12.4 GB used"" IconName=""@BitIconName.Cloud"" Color=""BitColor.Info"" />";

    private readonly string example5RazorCode = @"
@foreach (var tag in dismissibleTags)
{
    <BitTag @key=""tag""
            @ref=""dismissibleTagRefs[tag]""
            Text=""@tag""
            IconName=""@BitIconName.Tag""
            Variant=""BitVariant.Outline""
            OnDismiss=""() => DismissTag(tag)"" />
}
<BitButton Variant=""BitVariant.Text"" IsEnabled=""@(dismissibleTags.Count < 3)"" OnClick=""ResetDismissibleTags"">Reset</BitButton>


<BitTag Text=""Custom glyph"" Color=""BitColor.Error"" DismissIconName=""@BitIconName.ChromeClose""
        DismissLabel=""Clear the custom glyph tag"" OnDismiss=""() => { }"" />
<BitTag Text=""Formatted label"" Color=""BitColor.Info"" DismissLabelFormat=""Take {0} off the list"" OnDismiss=""() => { }"" />
<BitTag Text=""Disabled"" IsEnabled=""false"" OnDismiss=""() => { }"" />";
    private readonly string example5CsharpCode = @"
private List<string> dismissibleTags = [""Design"", ""Research"", ""Docs""];
private readonly Dictionary<string, BitTag> dismissibleTagRefs = [];

private async Task DismissTag(string tag)
{
    var index = dismissibleTags.IndexOf(tag);

    dismissibleTags.Remove(tag);
    dismissibleTagRefs.Remove(tag);

    if (dismissibleTags.Count == 0) return;

    // the tag that took its place, or the last one when the end of the list was removed
    var next = dismissibleTags[Math.Min(index, dismissibleTags.Count - 1)];

    if (dismissibleTagRefs.TryGetValue(next, out var nextRef))
    {
        await nextRef.FocusAsync();
    }
}

private void ResetDismissibleTags()
{
    dismissibleTags = [""Design"", ""Research"", ""Docs""];
}";

    private readonly string example6RazorCode = @"
<BitTag Text=""Add to filters"" IconName=""@BitIconName.Add"" OnClick=""() => clickCount++"" />
<BitTag Text=""Click or dismiss"" Variant=""BitVariant.Outline"" Color=""BitColor.Info""
        OnClick=""() => clickCount++"" OnDismiss=""() => dismissCount++"" />
<BitTag Text=""Disabled"" IsEnabled=""false"" OnClick=""() => clickCount++"" />

<div class=""example-card"" @onclick=""() => cardClickCount++"">
    <div>A clickable card:</div>
    <BitTag Text=""Bubbles"" Variant=""BitVariant.Outline"" OnClick=""() => clickCount++"" />
    <BitTag Text=""Stops"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" StopPropagation OnClick=""() => clickCount++"" />
</div>

<div>Tag clicks: <b>@clickCount</b>, dismissals: <b>@dismissCount</b>, card clicks: <b>@cardClickCount</b></div>";
    private readonly string example6CsharpCode = @"
private int clickCount;
private int dismissCount;
private int cardClickCount;";

    private readonly string example7RazorCode = @"
@foreach (var filter in filters)
{
    <BitTag Text=""@filter""
            Variant=""BitVariant.Outline""
            Color=""BitColor.Info""
            Selected=""selectedFilters.Contains(filter)""
            SelectedChanged=""v => ToggleFilter(filter, v)"" />
}
<div>Selected: <b>@(selectedFilters.Count == 0 ? ""none"" : string.Join("", "", selectedFilters))</b></div>

<BitTag Text=""No checkmark"" @bind-Selected=""isPinned"" HideSelectedIcon />
<BitTag Text=""Starred"" IconName=""@BitIconName.FavoriteStar"" SelectedIconName=""@BitIconName.FavoriteStarFill""
        Color=""BitColor.Warning"" Variant=""BitVariant.Text"" @bind-Selected=""isStarred"" />


<BitTag Text=""Keeps its own state"" DefaultSelected=""false"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" />
<BitTag Text=""Asks first"" DefaultSelected=""false"" Color=""BitColor.Warning""
        OnChanging=""args => args.Cancel = allowSelectionChange is false"" />
<BitToggle @bind-Value=""allowSelectionChange"" Label=""Allow the change"" Inline />

<BitTag Text=""Static selection"" Selected Color=""BitColor.Success"" />";
    private readonly string example7CsharpCode = @"
private bool isPinned;
private bool isStarred = true;
private bool allowSelectionChange;

private readonly string[] filters = [""Open"", ""In progress"", ""Done""];
private readonly List<string> selectedFilters = [""In progress""];

private void ToggleFilter(string filter, bool selected)
{
    if (selected)
    {
        if (selectedFilters.Contains(filter) is false)
        {
            selectedFilters.Add(filter);
        }
    }
    else
    {
        selectedFilters.Remove(filter);
    }
}";

    private readonly string example8RazorCode = @"
<BitTag Text=""Iconography"" IconName=""@BitIconName.Ribbon"" Href=""/iconography"" Variant=""BitVariant.Outline"" />

<BitTag Text=""Docs"" Color=""BitColor.Info"" Href=""https://blazorui.bitplatform.dev"" Target=""_blank""
        SecondaryIconName=""@BitIconName.OpenInNewWindow"" />

<BitTag Text=""Source"" Color=""BitColor.Secondary"" Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank""
        Rel=""BitLinkRels.NoFollow | BitLinkRels.NoReferrer"" NewTabHint=""(opens GitHub in a new tab)"" />

<BitTag Text=""Logo"" IconName=""@BitIconName.Download"" Color=""BitColor.Success"" Variant=""BitVariant.Outline""
        Href=""/_content/Bit.BlazorUI.Demo.Client.Core/images/bit-logo-blue.png"" Download=""bit-logo.png"" />

<BitTag Text=""Disabled"" Href=""https://bitplatform.dev"" IsEnabled=""false"" />

<BitTag Text=""This page"" Href=""#example8"" Selected AriaCurrent=""BitNavAriaCurrent.Page""
        Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />";

    private readonly string example9RazorCode = @"
<style>
    .example-initials {
        width: 2em;
        height: 2em;
        display: flex;
        flex-shrink: 0;
        font-size: 0.75em;
        border-radius: 50%;
        align-items: center;
        justify-content: center;
        background-color: var(--bit-clr-bg-ter);
    }

    .example-count {
        flex-shrink: 0;
        font-size: 0.85em;
        padding: 0.1em 0.5em;
        border-radius: 999px;
        background-color: var(--bit-clr-bg-ter);
    }
</style>


<BitTag IconName=""@BitIconName.Contact"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"">
    <b>Alex</b>&nbsp;<span style=""opacity:0.7"">(owner)</span>
</BitTag>

<BitTag IconName=""@BitIconName.Filter"" Color=""BitColor.Info"" OnClick=""() => { }"">
    Status<BitIcon IconName=""@BitIconName.ChevronDown"" />
</BitTag>


<BitTag Text=""Alex Parker"" SecondaryText=""Product designer"" Variant=""BitVariant.Outline"">
    <PrefixTemplate>
        <span class=""example-initials"">AP</span>
    </PrefixTemplate>
</BitTag>

<BitTag Text=""Open issues"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"">
    <SuffixTemplate>
        <span class=""example-count"">24</span>
    </SuffixTemplate>
</BitTag>

<BitTag Text=""Deploying"" Color=""BitColor.Success"" SecondaryIconName=""@BitIconName.ChevronRight"" OnClick=""() => { }"">
    <PrefixTemplate>
        <BitRollerLoading CustomSize=""16"" Color=""BitColor.TertiaryBackground"" />
    </PrefixTemplate>
</BitTag>";

    private readonly string example10RazorCode = @"
<div style=""max-width: 22rem"">
    <BitTag Text=""A tag with a label long enough to wrap onto a second line"" Variant=""BitVariant.Outline"" />

    <BitTag NoWrap
            IconName=""@BitIconName.Tag""
            Variant=""BitVariant.Outline""
            Text=""A tag with a label long enough to wrap onto a second line""
            Title=""A tag with a label long enough to wrap onto a second line"" />

    <BitTag FullWidth Text=""Full width and dismissible"" IconName=""@BitIconName.Tag"" Color=""BitColor.Info"" OnDismiss=""() => { }"" />

    <BitTag FullWidth Text=""Full width with a trailing glyph"" IconName=""@BitIconName.Tag"" Color=""BitColor.Success""
            Variant=""BitVariant.Outline"" SecondaryIconName=""@BitIconName.ChevronRight"" OnClick=""() => { }"" />
</div>";

    private readonly string example11RazorCode = @"
<BitTag IconName=""@BitIconName.Filter"" AriaLabel=""Show the filters"" OnClick=""() => { }"" />
<BitTag IconName=""@BitIconName.Pinned"" AriaLabel=""Pinned to the top"" Variant=""BitVariant.Outline"" />
<BitTag Text=""3"" IconName=""@BitIconName.Mail"" AriaLabel=""3 unread messages"" Color=""BitColor.Info"" />
<BitTag Text=""Only mine"" @bind-Selected=""isOnlyMine"" Variant=""BitVariant.Outline"" AriaDescription=""Shows only the items you own"" />
<BitTag Text=""Offline"" Color=""BitColor.Warning"" AriaDescription=""Dismissing hides the notice, not the problem"" OnDismiss=""() => { }"" />


<BitTag @ref=""plainFocusTag"" Text=""A plain tag with a TabIndex"" TabIndex=""0"" Variant=""BitVariant.Outline"" />
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => plainFocusTag?.FocusAsync()"">Focus it</BitButton>
<BitTag Text=""Shared with me"" DefaultSelected=""false"" Variant=""BitVariant.Outline"" IsEnabled=""false"" AllowDisabledFocus
        AriaDescription=""Unavailable while offline"" />";
    private readonly string example11CsharpCode = @"
private bool isOnlyMine;
private BitTag? plainFocusTag;";

    private readonly string example12RazorCode = @"
<BitParams Parameters=""@tagParams"">
    <BitTag Text=""Design"" OnDismiss=""() => { }"" />
    <BitTag Text=""Research"" OnDismiss=""() => { }"" />
    <BitTag Text=""Urgent"" Color=""BitColor.Error"" OnDismiss=""() => { }"" />
</BitParams>

<BitTag Text=""Outside the cascade"" OnDismiss=""() => { }"" />";
    private readonly string example12CsharpCode = @"
private readonly BitTagParams[] tagParams =
[
    new()
    {
        Color = BitColor.Info,
        Variant = BitVariant.Outline,
        Shape = BitTagShape.Circular,
        IconName = BitIconName.Filter,
        DismissLabelFormat = ""Remove the {0} filter"",
    }
];";

    private readonly string example13RazorCode = @"
<BitTag Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" Variant=""BitVariant.Text"" />

<BitTag Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" />

<BitTag Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" />

<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" Variant=""BitVariant.Text"" />

<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Variant=""BitVariant.Text"" />

<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" Variant=""BitVariant.Text"" />

<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Fill"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" />

<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Variant=""BitVariant.Fill"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Variant=""BitVariant.Outline"" />
<BitTag Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" Variant=""BitVariant.Text"" />

<BitTag Text=""PrimaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""PrimaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""PrimaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"" />

<BitTag Text=""SecondaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""SecondaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SecondaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"" />

<BitTag Text=""TertiaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""TertiaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""TertiaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"" />

<BitTag Text=""PrimaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""PrimaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""PrimaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"" />

<BitTag Text=""SecondaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""SecondaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SecondaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"" />

<BitTag Text=""TertiaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Fill"" />
<BitTag Text=""TertiaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"" />
<BitTag Text=""TertiaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"" />

<BitTag Text=""PrimaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Fill"" />
<BitTag Text=""PrimaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"" />
<BitTag Text=""PrimaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"" />

<BitTag Text=""SecondaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Fill"" />
<BitTag Text=""SecondaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"" />
<BitTag Text=""SecondaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"" />

<BitTag Text=""TertiaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Fill"" />
<BitTag Text=""TertiaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"" />
<BitTag Text=""TertiaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"" />


<div><b>Disabled</b>:</div>

<BitTag IsEnabled=""false"" Text=""Primary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Primary"" />
<BitTag IsEnabled=""false"" Text=""Secondary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Secondary"" />
<BitTag IsEnabled=""false"" Text=""Tertiary"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Tertiary"" />
<BitTag IsEnabled=""false"" Text=""Info"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Info"" />
<BitTag IsEnabled=""false"" Text=""Success"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" />
<BitTag IsEnabled=""false"" Text=""Warning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Warning"" />
<BitTag IsEnabled=""false"" Text=""SevereWarning"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SevereWarning"" />
<BitTag IsEnabled=""false"" Text=""Error"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Error"" />

<BitTag IsEnabled=""false"" Text=""PrimaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBackground"" />
<BitTag IsEnabled=""false"" Text=""SecondaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBackground"" />
<BitTag IsEnabled=""false"" Text=""TertiaryBackground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBackground"" />

<BitTag IsEnabled=""false"" Text=""PrimaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryForeground"" />
<BitTag IsEnabled=""false"" Text=""SecondaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryForeground"" />
<BitTag IsEnabled=""false"" Text=""TertiaryForeground"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryForeground"" />

<BitTag IsEnabled=""false"" Text=""PrimaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.PrimaryBorder"" />
<BitTag IsEnabled=""false"" Text=""SecondaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.SecondaryBorder"" />
<BitTag IsEnabled=""false"" Text=""TertiaryBorder"" IconName=""@BitIconName.Calendar"" Color=""BitColor.TertiaryBorder"" />";

    private readonly string example14RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitTag Text=""House"" Icon=""@(""fa-solid fa-house"")"" />
<BitTag Text=""Heart"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" />
<BitTag Text=""GitHub"" Icon=""@BitIconInfo.Fa(""fa-brands fa-github"")"" />
<BitTag Text=""Dismiss"" Icon=""@BitIconInfo.Fa(""solid tag"")"" DismissIcon=""@BitIconInfo.Fa(""solid xmark"")"" OnDismiss=""() => { }"" />
<BitTag Text=""Selected"" Icon=""@BitIconInfo.Fa(""solid star"")"" SelectedIcon=""@BitIconInfo.Fa(""solid check"")"" Selected />
<BitTag Text=""Trailing"" Icon=""@BitIconInfo.Fa(""solid filter"")"" SecondaryIcon=""@BitIconInfo.Fa(""solid chevron-down"")"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitTag Text=""House"" Icon=""@(""bi bi-house-fill"")"" />
<BitTag Text=""Heart"" Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" />
<BitTag Text=""GitHub"" Icon=""@BitIconInfo.Bi(""github"")"" />
<BitTag Text=""Dismiss"" Icon=""@BitIconInfo.Bi(""tag-fill"")"" DismissIcon=""@BitIconInfo.Bi(""x-lg"")"" OnDismiss=""() => { }"" />
<BitTag Text=""Selected"" Icon=""@BitIconInfo.Bi(""star-fill"")"" SelectedIcon=""@BitIconInfo.Bi(""check-lg"")"" Selected />
<BitTag Text=""Trailing"" Icon=""@BitIconInfo.Bi(""funnel-fill"")"" SecondaryIcon=""@BitIconInfo.Bi(""chevron-down"")"" />";

    private readonly string example15RazorCode = @"
<BitTag Text=""Small"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Small"" />
<BitTag Text=""Small"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Small"" Variant=""BitVariant.Outline"" OnDismiss=""() => { }"" />

<BitTag Text=""Medium"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Medium"" />
<BitTag Text=""Medium"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" OnDismiss=""() => { }"" />

<BitTag Text=""Large"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Large"" />
<BitTag Text=""Large"" IconName=""@BitIconName.Calendar"" Size=""BitSize.Large"" Variant=""BitVariant.Outline"" OnDismiss=""() => { }"" />";

    private readonly string example16RazorCode = @"
<style>
    .custom-class {
        border-radius: 0.25rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root {
        color: mediumpurple;
        border-radius: 0.5rem;
        border-color: mediumpurple;
        background-color: transparent;
        box-shadow: mediumpurple 0 0 0.5rem;
    }

    .custom-icon {
        font-size: 1.25rem;
        font-weight: bolder;
    }

    .custom-selected {
        border-color: deeppink;
        background-color: deeppink;
    }
</style>


<BitTag Text=""Styled Tag"" IconName=""@BitIconName.People"" Style=""font-style: italic; letter-spacing: 0.05em;"" />
<BitTag Text=""Classed Tag"" IconName=""@BitIconName.People"" Class=""custom-class"" Variant=""BitVariant.Outline"" />


<BitTag Text=""Styles""
        SecondaryText=""with a second line""
        IconName=""@BitIconName.People""
        Styles=""@(new() { Text = ""font-style: italic;"",
                          SecondaryText = ""text-decoration: underline;"",
                          Icon = ""transform: rotate(-15deg);"" })"" />

<BitTag Text=""Classes""
        IconName=""@BitIconName.People""
        Classes=""@(new() { Root = ""custom-root"",
                           Icon = ""custom-icon"" })"" />

<BitTag Text=""Selected""
        @bind-Selected=""isStyledSelected""
        Classes=""@(new() { Selected = ""custom-selected"" })"" />


<BitTag Text=""Custom palette"" IconName=""@BitIconName.Tag"" OnClick=""() => { }""
        Style=""--bit-Tag-color: #5b21b6; --bit-Tag-background: #ede9fe; --bit-Tag-border-color: #c4b5fd; --bit-Tag-icon-color: #db2777; --bit-Tag-hover-background: #ddd6fe;"" />
<BitTag Text=""Bold pill"" Style=""--bit-Tag-radius: 999px; --bit-Tag-font-weight: 700; --bit-Tag-padding-x: 1rem;"" />
<BitTag Text=""Elevated"" Variant=""BitVariant.Outline"" Style=""--bit-Tag-shadow: var(--bit-shd-card); --bit-Tag-border-color: transparent;"" />

<div style=""--bit-Tag-selected-background: var(--bit-clr-suc); --bit-Tag-selected-border-color: var(--bit-clr-suc); --bit-Tag-selected-color: var(--bit-clr-suc-text); --bit-Tag-min-height: 2rem; --bit-Tag-max-width: 9rem;"">
    <BitTag Text=""Open"" DefaultSelected=""true"" Variant=""BitVariant.Outline"" />
    <BitTag Text=""In progress"" DefaultSelected=""false"" Variant=""BitVariant.Outline"" />
    <BitTag Text=""A long label cut by the max width"" NoWrap Title=""A long label cut by the max width"" Variant=""BitVariant.Outline"" />
</div>";
    private readonly string example16CsharpCode = @"
private bool isStyledSelected = true;";

    private readonly string example17RazorCode = @"
<div dir=""rtl"">
    <BitTag Dir=""BitDir.Rtl"" Text=""برچسب"" IconName=""@BitIconName.Calendar"" />
    <BitTag Dir=""BitDir.Rtl"" Text=""طراحی"" IconName=""@BitIconName.Tag"" Color=""BitColor.Info"" Variant=""BitVariant.Outline"" />
    <BitTag Dir=""BitDir.Rtl"" Text=""پژوهش"" IconName=""@BitIconName.Tag"" Color=""BitColor.Error"" DismissLabelFormat=""حذف {0}"" OnDismiss=""() => { }"" />
    <BitTag Dir=""BitDir.Rtl"" Text=""معکوس"" IconName=""@BitIconName.Calendar"" Color=""BitColor.Success"" Reversed OnDismiss=""() => { }"" />
</div>";
}

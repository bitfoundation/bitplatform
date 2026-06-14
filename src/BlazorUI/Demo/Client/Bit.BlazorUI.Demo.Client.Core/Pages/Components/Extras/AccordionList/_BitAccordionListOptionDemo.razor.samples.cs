namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListOptionDemo
{
    private readonly string example1RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example3RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"" DefaultExpandedKey=""users"">
    <BitAccordionListOption Key=""general"" Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Multiple TItem=""BitAccordionListOption"" DefaultExpandedKeys=""@([""general"", ""advanced""])"">
    <BitAccordionListOption Key=""general"" Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example4RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption""
                  OnExpand=""(BitAccordionListOption item) => expandedTitle = item.Title""
                  OnCollapse=""(BitAccordionListOption item) => collapsedTitle = item.Title""
                  OnToggle=""(BitAccordionListOption item) => toggledTitle = item.Title"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>

<div>Last expanded: <b>@expandedTitle</b></div>
<div>Last collapsed: <b>@collapsedTitle</b></div>
<div>Last toggled: <b>@toggledTitle</b></div>

<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"" OnClick=""_ => { clickCounter++; StateHasChanged(); }"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"" OnClick=""_ => { clickCounter++; StateHasChanged(); }"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"" OnClick=""_ => { clickCounter++; StateHasChanged(); }"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>
<div>Item click count: <b>@clickCounter</b></div>";
    private readonly string example4CsharpCode = @"
private int clickCounter;
private string? expandedTitle;
private string? collapsedTitle;
private string? toggledTitle;";

    private readonly string example5RazorCode = @"
<BitButton OnClick=""() => accordionListRef.ExpandAll()"">Expand all</BitButton>
<BitButton OnClick=""() => accordionListRef.CollapseAll()"">Collapse all</BitButton>

<BitAccordionList @ref=""accordionListRef"" Multiple TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";
    private readonly string example5CsharpCode = @"
private BitAccordionList<BitAccordionListOption> accordionListRef = default!;";

    private readonly string example6RazorCode = @"
<BitButtonGroup Toggle Items=""bindingButtons"" TItem=""BitButtonGroupItem"" @bind-ToggleKey=""boundExpandedKey"" />

<div>Bound expanded key: <b>@boundExpandedKey</b></div>

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Key=""general"" Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";
    private readonly string example6CsharpCode = @"
private string? boundExpandedKey = ""users"";

private List<BitButtonGroupItem> bindingButtons =>
[
    new() { Key = ""general"", Text = ""General"" },
    new() { Key = ""users"", Text = ""Users"" },
    new() { Key = ""advanced"", Text = ""Advanced"" },
];";

    private readonly string example7RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"" ExpanderIconName=""@BitIconName.Add"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"" ExpanderIconName=""@BitIconName.Settings"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"" ExpanderIconName=""@BitIconName.Contact"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"" ExpanderIconName=""@BitIconName.Ringer"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example8RazorCode = @"
<BitAccordionList NoBorder TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Background=""BitColorKind.Secondary"" Border=""BitColorKind.Tertiary"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example9RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">
        <HeaderTemplate Context=""option"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" Color=""BitColor.Warning"" />
            <b>@option.Title</b>
        </HeaderTemplate>
        <Body Context=""option"">
            <BitText Typography=""BitTypography.Caption1"">The general settings of the application</BitText>
        </Body>
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">
        <HeaderTemplate Context=""option"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" Color=""BitColor.Warning"" />
            <b>@option.Title</b>
        </HeaderTemplate>
        <Body Context=""option"">
            <BitText Typography=""BitTypography.Caption1"">You are currently not an owner</BitText>
        </Body>
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"">
        <HeaderTemplate Context=""option"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" Color=""BitColor.Warning"" />
            <b>@option.Title</b>
        </HeaderTemplate>
        <Body Context=""option"">
            <BitText Typography=""BitTypography.Caption1"">Filtering has been entirely disabled</BitText>
        </Body>
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example10RazorCode = @"
<BitAccordionList Gap=""8"" Style=""border: 1px solid var(--bit-clr-pri); border-radius: 0.5rem; padding: 0.5rem;"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Gap=""8"" Class=""custom-item"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Styles=""@(new() { ItemTitle = ""color: tomato;"", ItemHeader = ""background-color: var(--bit-clr-bg-sec);"" })"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Classes=""@(new() { ItemTitle = ""custom-title"" })"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example11RazorCode = @"
<BitAccordionList Dir=""BitDir.Rtl"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""تنظیمات عمومی"" Description=""تنظیمات کلی برنامه"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""کاربران"" Description=""شما در حال حاضر مالک نیستید"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.
    </BitAccordionListOption>
</BitAccordionList>";
}

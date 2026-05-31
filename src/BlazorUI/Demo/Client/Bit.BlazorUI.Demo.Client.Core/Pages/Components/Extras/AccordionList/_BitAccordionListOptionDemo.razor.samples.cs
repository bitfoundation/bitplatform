namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListOptionDemo
{
    private readonly string example1RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""Accordion 1"" Description=""The first item"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Accordion 2"" Description=""The second item"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Accordion 3"" Description=""The third item"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""Accordion 1"" Description=""The first item"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Accordion 2"" Description=""The second item"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Accordion 3"" Description=""The third item"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example3RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"" DefaultExpandedKey=""users"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Multiple TItem=""BitAccordionListOption"" DefaultExpandedKeys=""@([""general"", ""advanced""])"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams.
    </BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"">
        In the beginning, there is silence a blank canvas yearning to be filled, a quiet space where creativity waits to awaken.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example4RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption""
                  OnExpand=""(BitAccordionListOption item) => expandedTitle = item.Title""
                  OnCollapse=""(BitAccordionListOption item) => collapsedTitle = item.Title""
                  OnToggle=""(BitAccordionListOption item) => toggledTitle = item.Title"">
    <BitAccordionListOption Title=""Accordion 1"">Body of the first item.</BitAccordionListOption>
    <BitAccordionListOption Title=""Accordion 2"">Body of the second item.</BitAccordionListOption>
    <BitAccordionListOption Title=""Accordion 3"">Body of the third item.</BitAccordionListOption>
</BitAccordionList>

<div>Last expanded: <b>@expandedTitle</b></div>
<div>Last collapsed: <b>@collapsedTitle</b></div>
<div>Last toggled: <b>@toggledTitle</b></div>

<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""Accordion 1"" OnClick=""_ => { clickCounter++; StateHasChanged(); }"">Click my header.</BitAccordionListOption>
    <BitAccordionListOption Title=""Accordion 2"" OnClick=""_ => { clickCounter++; StateHasChanged(); }"">Click my header.</BitAccordionListOption>
</BitAccordionList>
<div>Item click count: <b>@clickCounter</b></div>";
    private readonly string example4CsharpCode = @"
private int clickCounter;
private string? expandedTitle;
private string? collapsedTitle;
private string? toggledTitle;";

    private readonly string example5RazorCode = @"
<BitButtonGroup Items=""bindingButtons"" TItem=""BitButtonGroupItem"" />

<div>Bound expanded key: <b>@boundExpandedKey</b></div>

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">The general settings of the application.</BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">You are currently not an owner.</BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"">Filtering has been entirely disabled.</BitAccordionListOption>
</BitAccordionList>";
    private readonly string example5CsharpCode = @"
private string? boundExpandedKey = ""users"";

private List<BitButtonGroupItem> bindingButtons =>
[
    new() { Text = ""General"", OnClick = _ => boundExpandedKey = ""general"" },
    new() { Text = ""Users"", OnClick = _ => boundExpandedKey = ""users"" },
    new() { Text = ""Advanced"", OnClick = _ => boundExpandedKey = ""advanced"" },
    new() { Text = ""None"", OnClick = _ => boundExpandedKey = null },
];";

    private readonly string example6RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""Profile"" ExpanderIconName=""@BitIconName.Contact"">Your profile information.</BitAccordionListOption>
    <BitAccordionListOption Title=""Settings"" ExpanderIconName=""@BitIconName.Settings"">The application settings.</BitAccordionListOption>
    <BitAccordionListOption Title=""Notifications"" ExpanderIconName=""@BitIconName.Ringer"">Your notification preferences.</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example7RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""Custom"">
        <HeaderTemplate Context=""option"">
            <BitIcon IconName=""@BitIconName.FavoriteStarFill"" Color=""BitColor.Warning"" />
            <b>@option.Title</b>
        </HeaderTemplate>
        <Body Context=""option"">
            The body of the option with a custom header.
        </Body>
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Accordion 2"">
        The body of a regular option.
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example8RazorCode = @"
<BitAccordionList Dir=""BitDir.Rtl"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""تنظیمات عمومی"" Description=""تنظیمات کلی برنامه"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""کاربران"" Description=""شما در حال حاضر مالک نیستید"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.
    </BitAccordionListOption>
</BitAccordionList>";
}

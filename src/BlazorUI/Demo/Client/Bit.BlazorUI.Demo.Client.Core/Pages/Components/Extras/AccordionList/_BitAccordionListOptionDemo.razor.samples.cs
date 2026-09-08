namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AccordionList;

public partial class _BitAccordionListOptionDemo
{
    private readonly string example1RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, ...
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, ...
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence, ...
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example2RazorCode = @"
<BitAccordionList Multiple TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, ...
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, ...
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence, ...
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Multiple MaxExpanded=""2"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        Once upon a time, ...
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Description=""You are currently not an owner"">
        Every story starts with a blank canvas, ...
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" Description=""Filtering has been entirely disabled"">
        In the beginning, there is silence, ...
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example3RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"" DefaultExpandedKey=""users"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"">In the beginning, there is silence, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Multiple TItem=""BitAccordionListOption"" DefaultExpandedKeys=""@([""general"", ""advanced""])"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"">In the beginning, there is silence, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example4RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"" Collapsible=""false"" DefaultExpandedKey=""general"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"">In the beginning, there is silence, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example5RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" IconName=""@BitIconName.Settings"" ExpanderIconName=""@BitIconName.ChevronDownSmall"">
        Once upon a time, ...
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" IconName=""@BitIconName.Contact"" ExpanderIconName=""@BitIconName.ChevronDownSmall"">
        Every story starts with a blank canvas, ...
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"" IconName=""@BitIconName.Ringer"">
        In the beginning, there is silence, ...
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList TItem=""BitAccordionListOption"" ExpanderIconName=""@BitIconName.Add"" ExpandedExpanderIconName=""@BitIconName.Remove"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList TItem=""BitAccordionListOption"" ExpanderIconPosition=""BitIconPosition.Start"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList HideExpanderIcon TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example6RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        <Actions Context=""option"">
            <BitButton IconOnly
                       Variant=""BitVariant.Text""
                       IconName=""@BitIconName.MoreVertical""
                       Title=""@($""More about {option.Title}"")""
                       OnClick=""() => actionedTitle = option.Title"" />
        </Actions>
        <Body Context=""option"">
            Once upon a time, ...
        </Body>
    </BitAccordionListOption>
</BitAccordionList>

<div>Last action: <b>@actionedTitle</b></div>";
    private readonly string example6CsharpCode = @"
private string? actionedTitle;";

    private readonly string example7RazorCode = @"
<BitAccordionList Multiple
                  TItem=""BitAccordionListOption""
                  DefaultExpandedKeys=""@([""locked""])""
                  OnItemClick=""(BitAccordionListOption option) => { if (option.ReadOnly is true) readOnlyClickCount++; }"">
    <BitAccordionListOption Key=""normal"" Title=""General settings"" Description=""A live option"">
        Once upon a time, ...
    </BitAccordionListOption>
    <BitAccordionListOption Key=""disabled"" Title=""Users"" Description=""Turned off altogether"" IsEnabled=""false"">
        Every story starts with a blank canvas, ...
    </BitAccordionListOption>
    <BitAccordionListOption Key=""locked"" Title=""Advanced settings"" Description=""Open on purpose and staying that way"" ReadOnly=""true"">
        In the beginning, there is silence, ...
    </BitAccordionListOption>
</BitAccordionList>

<div>Clicks on the read-only header: <b>@readOnlyClickCount</b></div>";
    private readonly string example7CsharpCode = @"
private int readOnlyClickCount;";

    private readonly string example8RazorCode = @"
<BitAccordionList TItem=""BitAccordionListOption""
                  OnExpand=""(BitAccordionListOption option) => expandedTitle = option.Title""
                  OnCollapse=""(BitAccordionListOption option) => collapsedTitle = option.Title""
                  OnToggle=""(BitAccordionListOption option) => toggledTitle = option.Title"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"">In the beginning, there is silence, ...</BitAccordionListOption>
</BitAccordionList>

<div>Last expanded: <b>@expandedTitle</b></div>
<div>Last collapsed: <b>@collapsedTitle</b></div>
<div>Last toggled: <b>@toggledTitle</b></div>

<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" OnClick=""_ => { clickCounter++; StateHasChanged(); }"">
        Once upon a time, ...
    </BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" OnClick=""_ => { clickCounter++; StateHasChanged(); }"">
        Every story starts with a blank canvas, ...
    </BitAccordionListOption>
</BitAccordionList>

<div>Item click count: <b>@clickCounter</b></div>";
    private readonly string example8CsharpCode = @"
private int clickCounter;
private string? expandedTitle;
private string? collapsedTitle;
private string? toggledTitle;";

    private readonly string example9RazorCode = @"
<BitCheckbox @bind-Value=""lockToggling"" Label=""Refuse every toggle"" />
<BitCheckbox @bind-Value=""slowToggling"" Label=""Take a second to decide"" />

<BitAccordionList TItem=""BitAccordionListOption"" OnToggling=""HandleOnToggling"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<div>Last request: <b>@togglingReport</b></div>";
    private readonly string example9CsharpCode = @"
private bool lockToggling;
private bool slowToggling;
private string? togglingReport;

private async Task HandleOnToggling(BitAccordionListToggleArgs<BitAccordionListOption> args)
{
    togglingReport = $""{args.Item.Title} is {(args.IsExpanding ? ""expanding"" : ""collapsing"")} ({args.Reason})"";

    // The header of this option reports itself as aria-busy for as long as the callback is awaited.
    if (slowToggling)
    {
        await Task.Delay(1000);
    }

    args.Cancel = lockToggling;
}";

    private readonly string example10RazorCode = @"
<BitButtonGroup Toggle Items=""bindingButtons"" TItem=""BitButtonGroupItem"" @bind-ToggleKey=""boundExpandedKey"" />

<div>Bound expanded key: <b>@boundExpandedKey</b></div>

<BitAccordionList @bind-ExpandedKey=""boundExpandedKey"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"">In the beginning, there is silence, ...</BitAccordionListOption>
</BitAccordionList>

<div>Bound expanded keys: <b>@string.Join("", "", boundExpandedKeys)</b></div>

<BitAccordionList Multiple @bind-ExpandedKeys=""boundExpandedKeys"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"">In the beginning, there is silence, ...</BitAccordionListOption>
</BitAccordionList>";
    private readonly string example10CsharpCode = @"
private string? boundExpandedKey = ""users"";
private IEnumerable<string> boundExpandedKeys = [""general""];

private List<BitButtonGroupItem> bindingButtons =>
[
    new() { Key = ""general"", Text = ""General"" },
    new() { Key = ""users"", Text = ""Users"" },
    new() { Key = ""advanced"", Text = ""Advanced"" },
];";

    private readonly string example11RazorCode = @"
<BitButton OnClick=""@(() => accordionListRef!.ExpandAll())"">Expand all</BitButton>
<BitButton OnClick=""@(() => accordionListRef!.CollapseAll())"">Collapse all</BitButton>
<BitButton OnClick=""@(() => accordionListRef!.Toggle(""users""))"">Toggle Users</BitButton>
<BitButton OnClick=""@(() => accordionListRef!.FocusItem(""advanced""))"">Focus Advanced</BitButton>

<BitAccordionList @ref=""accordionListRef"" Multiple @bind-ExpandedKeys=""programmaticKeys"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Key=""general"" Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""users"" Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
    <BitAccordionListOption Key=""advanced"" Title=""Advanced settings"">In the beginning, there is silence, ...</BitAccordionListOption>
</BitAccordionList>

<div>Expanded keys: <b>@string.Join("", "", programmaticKeys)</b></div>";
    private readonly string example11CsharpCode = @"
private IEnumerable<string> programmaticKeys = [];
private BitAccordionList<BitAccordionListOption>? accordionListRef;

// The same state can also be read back without a binding:
// accordionListRef.IsExpanded(""users""); accordionListRef.GetExpandedKeys();";

    private readonly string example12RazorCode = @"
<BitAccordionList Multiple LazyContent TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""Lazy panel"" Description=""Rendered the first time it is opened, and kept afterwards"">
        This panel was rendered at @DateTime.Now.ToString(""HH:mm:ss.fff"")
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Multiple UnmountOnCollapse TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""Unmounted panel"" Description=""Rendered again on every open"">
        This panel was rendered at @DateTime.Now.ToString(""HH:mm:ss.fff"")
    </BitAccordionListOption>
</BitAccordionList>

<BitAccordionList TItem=""BitAccordionListOption"" MaxHeight=""100px"" DefaultExpandedKey=""long-1"">
    <BitAccordionListOption Key=""long-1"" Title=""A long panel"">a very long text ...</BitAccordionListOption>
    <BitAccordionListOption Key=""long-2"" Title=""Another long panel"">a very long text ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example13RazorCode = @"
<BitAccordionList TransitionDuration=""0"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList TransitionDuration=""1500"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example14RazorCode = @"
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
</BitAccordionList>

<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"" Description=""The general settings of the application"">
        <TitleTemplate Context=""option"">
            <BitTag Text=""@option.Title"" Color=""BitColor.SecondaryBackground"" />
        </TitleTemplate>
        <ExpanderTemplate Context=""option"">
            <BitIcon IconName=""@BitIconName.ChevronDownSmall"" />
        </ExpanderTemplate>
        <Body Context=""option"">
            Once upon a time, ...
        </Body>
    </BitAccordionListOption>
</BitAccordionList>";

    private readonly string example15RazorCode = @"
<BitAccordionList Multiple
                  TItem=""BitAccordionListOption""
                  HeadingLevel=""2""
                  NoContentRegion
                  NoNavigationLoop
                  AriaLabel=""Application settings"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Advanced settings"">In the beginning, there is silence, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example16RazorCode = @"
<BitAccordionList ExpandOnPrint TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example17RazorCode = @"
<BitAccordionList Gap=""0"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList NoBorder TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example18RazorCode = @"
<BitCheckbox @bind-Value=""showEmptyItems"" Label=""Show the options"" />

<BitAccordionList TItem=""BitAccordionListOption"">
    <EmptyContent>
        <BitText Typography=""BitTypography.Body2"">There is nothing to show here yet.</BitText>
    </EmptyContent>
    <Options>
        @if (showEmptyItems)
        {
            <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
            <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
        }
    </Options>
</BitAccordionList>";
    private readonly string example18CsharpCode = @"
private bool showEmptyItems;";

    private readonly string example19RazorCode = @"
<div class=""scroll-box"">
    <BitAccordionList ScrollIntoViewOnExpand TItem=""BitAccordionListOption"">
        <BitAccordionListOption Title=""First section"">Once upon a time, ...</BitAccordionListOption>
        <BitAccordionListOption Title=""Second section"">Every story starts with a blank canvas, ...</BitAccordionListOption>
        <BitAccordionListOption Title=""Third section"">In the beginning, there is silence, ...</BitAccordionListOption>
        <BitAccordionListOption Title=""Fourth section"">Once upon a time, ...</BitAccordionListOption>
    </BitAccordionList>
</div>";

    private readonly string example20RazorCode = @"
<BitAccordionList Background=""BitColorKind.Secondary"" Border=""BitColorKind.Tertiary"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Background=""BitColorKind.Tertiary"" Border=""BitColorKind.Transparent"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example21RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitAccordionList TItem=""BitAccordionListOption"" ExpanderIcon=""@BitIconInfo.Fa(""solid angle-down"")"">
    <BitAccordionListOption Title=""General settings"" Icon=""@BitIconInfo.Fa(""solid gear"")"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Icon=""@BitIconInfo.Fa(""solid user"")"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList TItem=""BitAccordionListOption"" ExpanderIcon=""@BitIconInfo.Bi(""chevron-down"")"">
    <BitAccordionListOption Title=""General settings"" Icon=""@BitIconInfo.Bi(""gear"")"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"" Icon=""@BitIconInfo.Bi(""person"")"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example22RazorCode = @"
<BitAccordionList Size=""BitSize.Small"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Size=""BitSize.Medium"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Size=""BitSize.Large"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example23RazorCode = @"
<style>
    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }

    .custom-title {
        color: tomato;
        font-style: italic;
    }

    .custom-expanded {
        border-color: tomato;
    }
</style>

<BitAccordionList Gap=""8"" Style=""border: 1px solid var(--bit-clr-pri); border-radius: 0.5rem; padding: 0.5rem;"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Gap=""8"" Class=""custom-item"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Styles=""@(new() { ItemTitle = ""color: tomato;"", ItemHeader = ""background-color: var(--bit-clr-bg-sec);"" })"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>

<BitAccordionList Classes=""@(new() { ItemTitle = ""custom-title"", ItemExpanded = ""custom-expanded"" })"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""General settings"">Once upon a time, ...</BitAccordionListOption>
    <BitAccordionListOption Title=""Users"">Every story starts with a blank canvas, ...</BitAccordionListOption>
</BitAccordionList>";

    private readonly string example24RazorCode = @"
<BitAccordionList Dir=""BitDir.Rtl"" TItem=""BitAccordionListOption"">
    <BitAccordionListOption Title=""تنظیمات عمومی"" Description=""تنظیمات کلی برنامه"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.
    </BitAccordionListOption>
    <BitAccordionListOption Title=""کاربران"" Description=""شما در حال حاضر مالک نیستید"">
        لورم ایپسوم متن ساختگی با تولید سادگی نامفهوم از صنعت چاپ است.
    </BitAccordionListOption>
</BitAccordionList>";
}

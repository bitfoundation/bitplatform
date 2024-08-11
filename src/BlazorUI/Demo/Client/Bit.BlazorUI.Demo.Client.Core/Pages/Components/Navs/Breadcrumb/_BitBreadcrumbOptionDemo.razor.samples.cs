namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbOptionDemo
{
    private readonly string example1RazorCode = @"
<div>
    <BitLabel>Basic</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>

<div>
    <BitLabel>Group Disabled</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" IsEnabled=""false"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>

<div>
    <BitLabel>Option Disabled</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" IsEnabled=""false"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" IsEnabled=""false"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
";

    private readonly string example2RazorCode = @"
<div>
    <BitLabel>MaxDisplayedOptions (1)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""1"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (2)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""2"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3), OverflowIndex (0)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""0"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3), OverflowIndex (1)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""1"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>MaxDisplayedOptions (3), OverflowIndex (2)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
";

    private readonly string example3RazorCode = @"
<div>
    <BitLabel>BitIconName (ChevronDown)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""@BitIconName.ChevronDown"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>

<div>
    <BitLabel>BitIconName (CollapseMenu)</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""@BitIconName.CollapseMenu"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
";

    private readonly string example4RazorCode = @"
<style>
    .custom-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: limegreen;

        &:hover {
            background: greenyellow;
        }
    }

    .custom-selected-item {
        color: red;
        margin: 2px 5px;
        border-radius: 2px;
        background: mediumspringgreen;

        &:hover {
            background: greenyellow;
        }
    }
</style>

<div>
    <BitLabel>Options Class</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" Class=""custom-item"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" Class=""custom-item"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" Class=""custom-item"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" Class=""custom-item"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>Options Style</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" Style=""color:red;background:greenyellow"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" Style=""color:red;background:greenyellow"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" Style=""color:red;background:greenyellow"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" Style=""color:red;background:greenyellow"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>Selected Option Class</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" SelectedItemClass=""custom-selected-item"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
<div>
    <BitLabel>Selected Option Style</BitLabel>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" SelectedItemStyle=""color:red; background:lightgreen;"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
    </BitBreadcrumb>
</div>
";

    private readonly string example5RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" SelectedItemStyle=""color:red; background:lightgreen;"">
    <BitBreadcrumbOption Text=""Option 1"" IsSelected=""@(SelectedOptionNumber == 1)"" OnClick=""() => SelectedOptionNumber = 1"" />
    <BitBreadcrumbOption Text=""Option 2"" IsSelected=""@(SelectedOptionNumber == 2)"" OnClick=""() => SelectedOptionNumber = 2"" />
    <BitBreadcrumbOption Text=""Option 3"" IsSelected=""@(SelectedOptionNumber == 3)"" OnClick=""() => SelectedOptionNumber = 3"" />
    <BitBreadcrumbOption Text=""Option 4"" IsSelected=""@(SelectedOptionNumber == 4)"" OnClick=""() => SelectedOptionNumber = 4"" />
    <BitBreadcrumbOption Text=""Option 5"" IsSelected=""@(SelectedOptionNumber == 5)"" OnClick=""() => SelectedOptionNumber = 5"" />
    <BitBreadcrumbOption Text=""Option 6"" IsSelected=""@(SelectedOptionNumber == 6)"" OnClick=""() => SelectedOptionNumber = 6"" />
</BitBreadcrumb>
";
    private readonly string example5CsharpCode = @"
private int SelectedOptionNumber = 6;
";

    private readonly string example6RazorCode = @"
<div>
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""@MaxDisplayedItems"" OverflowIndex=""@OverflowIndex"">
        @for (int i = 0; i < ItemsCount; i++)
        {
            int index = i + 1;
            <BitBreadcrumbOption Text=""@($""Option {index}"")"" IsSelected=""@(CustomizedSelectedOptionNumber == index)"" OnClick=""() => CustomizedSelectedOptionNumber = index"" />
        }
    </BitBreadcrumb>
</div>
<div class=""operators"">
    <div>
        <BitButton OnClick=""() => ItemsCount++"">Add Option</BitButton>
        <BitButton OnClick=""() => ItemsCount--"">Remove Option</BitButton>
    </div>
    <div>
        <BitNumberField @bind-Value=""MaxDisplayedItems"" Label=""MaxDisplayedOption"" ShowArrows=""true"" />
        <BitNumberField @bind-Value=""OverflowIndex"" Label=""OverflowIndex"" ShowArrows=""true"" />
    </div>
</div>
";
    private readonly string example6CsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private int CustomizedSelectedOptionNumber = 4;
";

    private readonly string example7RazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl"" TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <BitBreadcrumbOption Text=""پوشه اول"" />
    <BitBreadcrumbOption Text=""پوشه دوم"" IsSelected=""true"" />
    <BitBreadcrumbOption Text=""پوشه سوم"" />
    <BitBreadcrumbOption Text=""پوشه چهارم"" />
    <BitBreadcrumbOption Text=""پوشه پنجم"" />
    <BitBreadcrumbOption Text=""پوشه ششم"" />
</BitBreadcrumb>";
}

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbOptionDemo
{
    private readonly string example1RazorCode = @"
<BitLabel>Basic</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>Disabled</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" IsEnabled=""false"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>Option Disabled</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" IsEnabled=""false"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" IsEnabled=""false"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>";

    private readonly string example2RazorCode = @"
<BitLabel>Max displayed options (1)</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""1"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>Max displayed options (2)</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""2"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>Max displayed options (3)</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>Max displayed options (3), OverflowIndex (0)</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""0"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>Max displayed options (3), OverflowIndex (1)</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""1"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>Max displayed options (3), OverflowIndex (2)</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>";

    private readonly string example3RazorCode = @"
<BitLabel>BitIconName (ChevronDown)</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""@BitIconName.ChevronDown"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>BitIconName (CollapseMenu)</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""@BitIconName.CollapseMenu"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>";

    private readonly string example4RazorCode = @"
<style>
    .custom-item {
        color: #ffcece;
    }

    .custom-item:hover {
        color: #ff6868;
        background: transparent;
    }


    .custom-selected-item {
        color: blueviolet;
    }

    .custom-selected-item:hover {
        color: blueviolet;
        background: transparent;
        text-shadow: blueviolet 0 0 1rem;
    }
</style>


<BitLabel>Options Class</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" Class=""custom-item"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" Class=""custom-item"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" Class=""custom-item"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" Class=""custom-item"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>Options Style</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" IsSelected=""true"" />
</BitBreadcrumb>


<BitLabel>Selected Option Class</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" SelectedItemClass=""custom-selected-item"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>

<BitLabel>Selected Option Style</BitLabel>
<BitBreadcrumb TItem=""BitBreadcrumbOption"" SelectedItemStyle=""color: lightseagreen; text-shadow: lightseagreen 0 0 1rem;"">
    <BitBreadcrumbOption Text=""Folder 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Folder 4"" Href=""/components/breadcrumb"" IsSelected=""true"" />
</BitBreadcrumb>";

    private readonly string example5RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" SelectedItemStyle=""color: dodgerblue;"">
    <BitBreadcrumbOption Text=""Folder 1"" IsSelected=""@(SelectedOptionNumber == 1)"" OnClick=""() => SelectedOptionNumber = 1"" />
    <BitBreadcrumbOption Text=""Folder 2"" IsSelected=""@(SelectedOptionNumber == 2)"" OnClick=""() => SelectedOptionNumber = 2"" />
    <BitBreadcrumbOption Text=""Folder 3"" IsSelected=""@(SelectedOptionNumber == 3)"" OnClick=""() => SelectedOptionNumber = 3"" />
    <BitBreadcrumbOption Text=""Folder 4"" IsSelected=""@(SelectedOptionNumber == 4)"" OnClick=""() => SelectedOptionNumber = 4"" />
    <BitBreadcrumbOption Text=""Folder 5"" IsSelected=""@(SelectedOptionNumber == 5)"" OnClick=""() => SelectedOptionNumber = 5"" />
    <BitBreadcrumbOption Text=""Folder 6"" IsSelected=""@(SelectedOptionNumber == 6)"" OnClick=""() => SelectedOptionNumber = 6"" />
</BitBreadcrumb>";
    private readonly string example5CsharpCode = @"
private int SelectedOptionNumber = 6;";

    private readonly string example6RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""@MaxDisplayedItems"" OverflowIndex=""@OverflowIndex"">
    @for (int i = 0; i < ItemsCount; i++)
    {
        int index = i + 1;
        <BitBreadcrumbOption Text=""@($""Folder {index}"")""
                             OnClick=""() => CustomizedSelectedOptionNumber = index""
                             IsSelected=""@(CustomizedSelectedOptionNumber == index)"" />
    }
</BitBreadcrumb>

<BitButton OnClick=""() => ItemsCount++"">Add Option</BitButton>
<BitButton OnClick=""() => ItemsCount--"">Remove Option</BitButton>

<BitNumberField @bind-Value=""MaxDisplayedItems"" Label=""Max displayed options"" ShowArrows=""true"" />
<BitNumberField @bind-Value=""OverflowIndex"" Label=""Overflow index"" ShowArrows=""true"" />";
    private readonly string example6CsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private int CustomizedSelectedOptionNumber = 4;";

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

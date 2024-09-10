namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Navs.Breadcrumb;

public partial class _BitBreadcrumbOptionDemo
{
    private readonly string example1RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" IsEnabled=""false"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" IsEnabled=""false"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" IsEnabled=""false"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>";

    private readonly string example2RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""1"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""2"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""0"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""1"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>";

    private readonly string example3RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""@BitIconName.ChevronDown"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIcon=""@BitIconName.CollapseMenu"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
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


<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" Class=""custom-item"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" Class=""custom-item"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" Class=""custom-item"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" Class=""custom-item"" IsSelected />
</BitBreadcrumb>


<BitBreadcrumb TItem=""BitBreadcrumbOption"" SelectedItemStyle=""color: lightseagreen; text-shadow: lightseagreen 0 0 1rem;"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" SelectedItemClass=""custom-selected-item"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>";

    private readonly string example5RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" SelectedItemStyle=""color: dodgerblue;"">
    <BitBreadcrumbOption Text=""Option 1"" IsSelected=""@(SelectedOptionNumber == 1)"" OnClick=""() => SelectedOptionNumber = 1"" />
    <BitBreadcrumbOption Text=""Option 2"" IsSelected=""@(SelectedOptionNumber == 2)"" OnClick=""() => SelectedOptionNumber = 2"" />
    <BitBreadcrumbOption Text=""Option 3"" IsSelected=""@(SelectedOptionNumber == 3)"" OnClick=""() => SelectedOptionNumber = 3"" />
    <BitBreadcrumbOption Text=""Option 4"" IsSelected=""@(SelectedOptionNumber == 4)"" OnClick=""() => SelectedOptionNumber = 4"" />
    <BitBreadcrumbOption Text=""Option 5"" IsSelected=""@(SelectedOptionNumber == 5)"" OnClick=""() => SelectedOptionNumber = 5"" />
    <BitBreadcrumbOption Text=""Option 6"" IsSelected=""@(SelectedOptionNumber == 6)"" OnClick=""() => SelectedOptionNumber = 6"" />
</BitBreadcrumb>";
    private readonly string example5CsharpCode = @"
private int SelectedOptionNumber = 6;";

    private readonly string example6RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""@MaxDisplayedItems"" OverflowIndex=""@OverflowIndex"">
    @for (int i = 0; i < ItemsCount; i++)
    {
        int index = i + 1;
        <BitBreadcrumbOption Text=""@($""Option {index}"")""
                             OnClick=""() => CustomizedSelectedOptionNumber = index""
                             IsSelected=""@(CustomizedSelectedOptionNumber == index)"" />
    }
</BitBreadcrumb>

<BitButton OnClick=""() => ItemsCount++"">Add Option</BitButton>
<BitButton OnClick=""() => ItemsCount--"">Remove Option</BitButton>

<BitNumberField @bind-Value=""MaxDisplayedItems"" Label=""Max displayed options"" ShowArrows />
<BitNumberField @bind-Value=""OverflowIndex"" Label=""Overflow index"" ShowArrows />";
    private readonly string example6CsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private int CustomizedSelectedOptionNumber = 4;";

    private readonly string example7RazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl"" TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <BitBreadcrumbOption Text=""پوشه اول"" />
    <BitBreadcrumbOption Text=""پوشه دوم"" IsSelected />
    <BitBreadcrumbOption Text=""پوشه سوم"" />
    <BitBreadcrumbOption Text=""پوشه چهارم"" />
    <BitBreadcrumbOption Text=""پوشه پنجم"" />
    <BitBreadcrumbOption Text=""پوشه ششم"" />
</BitBreadcrumb>";
}

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

<BitBreadcrumb TItem=""BitBreadcrumbOption"" SelectedItemAsText>
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
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""bit BlazorUI"" Href=""https://blazorui.bitplatform.dev"" Target=""_blank""
                         Title=""Opens the bit BlazorUI website in a new tab"" />
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
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" DividerIconName=""@BitIconName.CaretRightSolid8"" OverflowIconName=""@BitIconName.ChevronDown"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" IconName=""@BitIconName.AdminELogoInverse32"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" IconName=""@BitIconName.AppsContent"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" IconName=""@BitIconName.AzureIcon"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IconName=""@BitIconName.ClassNotebookLogo16"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" OverflowIconName=""@BitIconName.CollapseMenu"" ReversedIcon>
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" IconName=""@BitIconName.AdminELogoInverse32"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" IconName=""@BitIconName.AppsContent"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" IconName=""@BitIconName.AzureIcon"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IconName=""@BitIconName.ClassNotebookLogo16"" IsSelected />
</BitBreadcrumb>";

    private readonly string example4RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" DividerIconName=""@BitIconName.CaretRightSolid8"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" DividerText=""/"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" DividerText=""›"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>";

    private readonly string example5RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <DividerIconTemplate>
        <BitIcon IconName=""@BitIconName.CaretRightSolid8"" Color=""BitColor.Warning"" />
    </DividerIconTemplate>
    <Options>
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
    </Options>
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <ItemTemplate Context=""item"">
        <div style=""font-weight: bold; color: #d13438; font-style:italic;"">
            @item.Text
        </div>
    </ItemTemplate>
    <OverflowTemplate Context=""item"">
        <div style=""font-weight: bold; color: blueviolet; font-style:italic;"">
            @item.Text
        </div>
    </OverflowTemplate>
    <Options>
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
    </Options>
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <Options>
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"">
            <Template Context=""item""><div style=""color:green"">@item.Text</div></Template>
            <OverflowTemplate Context=""item""><div style=""color:green;text-decoration:underline;"">@item.Text</div></OverflowTemplate>
        </BitBreadcrumbOption>
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"">
            <Template Context=""item""><div style=""color:yellow"">@item.Text</div></Template>
            <OverflowTemplate Context=""item""><div style=""color:yellow;text-decoration:underline;"">@item.Text</div></OverflowTemplate>
        </BitBreadcrumbOption>
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"">
            <Template Context=""item""><div style=""color:red"">@item.Text</div></Template>
            <OverflowTemplate Context=""item""><div style=""color:red;text-decoration:underline;"">@item.Text</div></OverflowTemplate>
        </BitBreadcrumbOption>
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected>
            <Template Context=""item""><div style=""color:blue"">@item.Text</div></Template>
            <OverflowTemplate Context=""item""><div style=""color:blue;text-decoration:underline;"">@item.Text</div></OverflowTemplate>
        </BitBreadcrumbOption>
    </Options>
</BitBreadcrumb>";

    private readonly string example6RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"" Styles=""@(new() { SelectedItem = ""color: dodgerblue;"", OverflowSelectedItem = ""color: red;"" })"">
    <BitBreadcrumbOption Text=""Option 1"" IsSelected=""@(SelectedOptionNumber == 1)"" OnClick=""() => SelectedOptionNumber = 1"" />
    <BitBreadcrumbOption Text=""Option 2"" IsSelected=""@(SelectedOptionNumber == 2)"" OnClick=""() => SelectedOptionNumber = 2"" />
    <BitBreadcrumbOption Text=""Option 3"" IsSelected=""@(SelectedOptionNumber == 3)"" OnClick=""() => SelectedOptionNumber = 3"" />
    <BitBreadcrumbOption Text=""Option 4"" IsSelected=""@(SelectedOptionNumber == 4)"" OnClick=""() => SelectedOptionNumber = 4"" />
    <BitBreadcrumbOption Text=""Option 5"" IsSelected=""@(SelectedOptionNumber == 5)"" OnClick=""() => SelectedOptionNumber = 5"" />
    <BitBreadcrumbOption Text=""Option 6"" IsSelected=""@(SelectedOptionNumber == 6)"" OnClick=""() => SelectedOptionNumber = 6"" />
</BitBreadcrumb>";
    private readonly string example6CsharpCode = @"
private int SelectedOptionNumber = 6;";

    private readonly string example7RazorCode = @"
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

<BitNumberField @bind-Value=""MaxDisplayedItems"" Label=""Max displayed options"" ShowButtons />
<BitNumberField @bind-Value=""OverflowIndex"" Label=""Overflow index"" ShowButtons />";
    private readonly string example7CsharpCode = @"
private int ItemsCount = 4;
private uint OverflowIndex = 2;
private uint MaxDisplayedItems = 3;
private int CustomizedSelectedOptionNumber = 4;";

    private readonly string example8RazorCode = @"
<style>
    .narrow-box {
        padding: 4px;
        overflow: hidden;
        max-width: 480px;
        border: 1px dashed gray;
    }
</style>

<div class=""narrow-box"">
    <BitBreadcrumb TItem=""BitBreadcrumbOption"">
        <BitBreadcrumbOption Text=""Very long option name 1"" Href=""/components/breadcrumb"" Title=""Very long option name 1"" />
        <BitBreadcrumbOption Text=""Very long option name 2"" Href=""/components/breadcrumb"" Title=""Very long option name 2"" />
        <BitBreadcrumbOption Text=""Very long option name 3"" Href=""/components/breadcrumb"" Title=""Very long option name 3"" IsSelected />
    </BitBreadcrumb>
</div>

<div class=""narrow-box"">
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxItemWidth=""5rem"">
        <BitBreadcrumbOption Text=""Very long option name 1"" Href=""/components/breadcrumb"" Title=""Very long option name 1"" />
        <BitBreadcrumbOption Text=""Very long option name 2"" Href=""/components/breadcrumb"" Title=""Very long option name 2"" />
        <BitBreadcrumbOption Text=""Very long option name 3"" Href=""/components/breadcrumb"" Title=""Very long option name 3"" IsSelected />
    </BitBreadcrumb>
</div>

<div class=""narrow-box"">
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" Wrap>
        <BitBreadcrumbOption Text=""Very long option name 1"" Href=""/components/breadcrumb"" Title=""Very long option name 1"" />
        <BitBreadcrumbOption Text=""Very long option name 2"" Href=""/components/breadcrumb"" Title=""Very long option name 2"" />
        <BitBreadcrumbOption Text=""Very long option name 3"" Href=""/components/breadcrumb"" Title=""Very long option name 3"" IsSelected />
    </BitBreadcrumb>
</div>";

    private readonly string example9RazorCode = @"
<style>
    .resizable-box {
        width: 320px;
        padding: 4px;
        overflow: auto;
        max-width: 100%;
        resize: horizontal;
        border: 1px dashed gray;
    }
</style>

<div class=""resizable-box"">
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" AutoCollapse>
        <BitBreadcrumbOption Text=""Very long option name 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Very long option name 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Very long option name 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Very long option name 4"" Href=""/components/breadcrumb"" IsSelected />
    </BitBreadcrumb>
</div>

<div class=""resizable-box"">
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" AutoCollapse OverflowIndex=""1"">
        <BitBreadcrumbOption Text=""Very long option name 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Very long option name 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Very long option name 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Very long option name 4"" Href=""/components/breadcrumb"" IsSelected />
    </BitBreadcrumb>
</div>";

    private readonly string example10RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" StructuredData MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>";

    private readonly string example11RazorCode = @"
<BitBreadcrumb TItem=""BitBreadcrumbOption"" ExpandOverflow MaxDisplayedItems=""3"" OverflowIndex=""1"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 5"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 6"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" ExpandOverflow Wrap MaxDisplayedItems=""2"" OverflowIndex=""1"">
    <BitBreadcrumbOption Text=""Very long option name 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Very long option name 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Very long option name 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Very long option name 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>";

    private readonly string example12RazorCode = @"
@foreach (var color in colors)
{
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" Color=""color"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
    </BitBreadcrumb>
}

@code {
    private readonly BitColor[] colors =
    [
        BitColor.Primary,
        BitColor.Secondary,
        BitColor.Tertiary,
        BitColor.Info,
        BitColor.Success,
        BitColor.Warning,
        BitColor.SevereWarning,
        BitColor.Error
    ];
}";

    private readonly string example13RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitBreadcrumb TItem=""BitBreadcrumbOption""
               MaxDisplayedItems=""3"" OverflowIndex=""2""
               Styles=""@(new() { ItemIcon = ""line-height:unset"" })"">
    <BitBreadcrumbOption Text=""Home"" Icon=""@(""fa-solid fa-house"")"" />
    <BitBreadcrumbOption Text=""Products"" Icon=""@(""fa-solid fa-box"")"" />
    <BitBreadcrumbOption Text=""Electronics"" Icon=""@(""fa-solid fa-microchip"")"" />
    <BitBreadcrumbOption Text=""Laptops"" Icon=""@(""fa-solid fa-laptop"")"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption""
               MaxDisplayedItems=""3"" OverflowIndex=""2""
               Styles=""@(new() { ItemIcon = ""line-height:unset"" })"">
    <BitBreadcrumbOption Text=""Home"" Icon=""@BitIconInfo.Css(""fa-solid fa-house"")"" />
    <BitBreadcrumbOption Text=""Products"" Icon=""@BitIconInfo.Css(""fa-solid fa-box"")"" />
    <BitBreadcrumbOption Text=""Electronics"" Icon=""@BitIconInfo.Css(""fa-solid fa-microchip"")"" />
    <BitBreadcrumbOption Text=""Laptops"" Icon=""@BitIconInfo.Css(""fa-solid fa-laptop"")"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption""
               MaxDisplayedItems=""3"" OverflowIndex=""2""
               Styles=""@(new() { ItemIcon = ""line-height:unset"" })"">
    <BitBreadcrumbOption Text=""Home"" Icon=""@BitIconInfo.Fa(""solid house"")"" />
    <BitBreadcrumbOption Text=""Products"" Icon=""@BitIconInfo.Fa(""solid box"")"" />
    <BitBreadcrumbOption Text=""Electronics"" Icon=""@BitIconInfo.Fa(""solid microchip"")"" />
    <BitBreadcrumbOption Text=""Laptops"" Icon=""@BitIconInfo.Fa(""solid laptop"")"" IsSelected />
</BitBreadcrumb>

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitBreadcrumb TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <BitBreadcrumbOption Text=""Home"" Icon=""@BitIconInfo.Bi(""house-fill"")"" />
    <BitBreadcrumbOption Text=""Products"" Icon=""@BitIconInfo.Bi(""box-seam-fill"")"" />
    <BitBreadcrumbOption Text=""Electronics"" Icon=""@BitIconInfo.Bi(""cpu-fill"")"" />
    <BitBreadcrumbOption Text=""Laptops"" Icon=""@BitIconInfo.Bi(""laptop-fill"")"" IsSelected />
</BitBreadcrumb>";

    private readonly string example14RazorCode = @"
@foreach (var size in sizes)
{
    <BitBreadcrumb TItem=""BitBreadcrumbOption"" Size=""size"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
        <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
        <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
    </BitBreadcrumb>
}

@code {
    private readonly BitSize[] sizes = [BitSize.Small, BitSize.Medium, BitSize.Large];
}";

    private readonly string example15RazorCode = @"
<style>
    .custom-class {
        font-style: italic;
        text-shadow: dodgerblue 0 0 0.5rem;
        border-bottom: 1px solid dodgerblue;
    }

    .custom-item {
        color: #ffcece;

        &:hover {
            color: #ff6868;
            background: transparent;
        }
    }

    .custom-item-1 {
        color: #b6ff00;

        &:hover {
            color: #2aff00;
            background: transparent;
        }
    }

    .custom-item-2 {
        color: #ffd800;

        &:hover {
            color: #ff6a00;
            background: transparent;
        }
    }

    .custom-selected-item {
        color: blueviolet;

        &:hover {
            color: blueviolet;
            background: transparent;
            text-shadow: blueviolet 0 0 1rem;
        }
    }
</style>


<BitBreadcrumb TItem=""BitBreadcrumbOption"" Class=""custom-class"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" Style=""font-style: italic;text-shadow: aqua 0 0 0.5rem;border-bottom: 1px solid aqua;"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" Class=""custom-item-1"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" Class=""custom-item-2"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" Class=""custom-item-1"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" Class=""custom-item-2"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" Style=""color: aqua; text-shadow: aqua 0 0 1rem;"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" Style=""color: dodgerblue; text-shadow: dodgerblue 0 0 1rem;"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" Style=""color: aqua; text-shadow: aqua 0 0 1rem;"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" Classes=""@(new() { Item = ""custom-item"", SelectedItem = ""custom-selected-item"" })"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>

<BitBreadcrumb TItem=""BitBreadcrumbOption"" Styles=""@(new() { Item = ""color: green;"", SelectedItem = ""color: lightseagreen; text-shadow: lightseagreen 0 0 1rem;"" })"">
    <BitBreadcrumbOption Text=""Option 1"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 2"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 3"" Href=""/components/breadcrumb"" />
    <BitBreadcrumbOption Text=""Option 4"" Href=""/components/breadcrumb"" IsSelected />
</BitBreadcrumb>";

    private readonly string example16RazorCode = @"
<BitBreadcrumb Dir=""BitDir.Rtl"" TItem=""BitBreadcrumbOption"" MaxDisplayedItems=""3"" OverflowIndex=""2"">
    <BitBreadcrumbOption Text=""پوشه اول"" />
    <BitBreadcrumbOption Text=""پوشه دوم"" IsSelected />
    <BitBreadcrumbOption Text=""پوشه سوم"" />
    <BitBreadcrumbOption Text=""پوشه چهارم"" />
    <BitBreadcrumbOption Text=""پوشه پنجم"" />
    <BitBreadcrumbOption Text=""پوشه ششم"" />
</BitBreadcrumb>";
}

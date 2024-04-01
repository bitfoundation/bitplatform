namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonOptionDemo
{
    private string? exampleSelectedOption;

    private BitMenuButtonOption? changedSelectedOption;
    private BitMenuButtonOption twoWaySelectedOption = default!;



    private readonly string example1RazorCode = @"
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Standard""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Text""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Text"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Options"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Options"" IsEnabled=""false"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example3RazorCode = @"
<BitMenuButton Text=""Options""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Options""
               IsEnabled=""false""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example4RazorCode = @"
<BitMenuButton Text=""Options""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Text"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Options""
               IsEnabled=""false""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Text"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example5RazorCode = @"
<BitMenuButton Split Text=""Primary"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Split
               Text=""Standard""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Split
               Text=""Text""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Text"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example6RazorCode = @"
<BitMenuButton Sticky TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Split Sticky
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example7RazorCode = @"
<BitMenuButton Text=""IconName""
               IconName=""@BitIconName.Edit"" 
               TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Split
               Text=""ChevronDownIcon""
               IconName=""@BitIconName.Add""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";

    private readonly string example8RazorCode = @"
<BitMenuButton Text=""Options"" OnClick=""(BitMenuButtonOption item) => exampleSelectedOption = item?.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Split
               Text=""Options""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard""
               OnClick=""@((BitMenuButtonOption item) => exampleSelectedOption = ""Main button clicked"")"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => exampleSelectedOption = $""Option A - OnClick"")"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => exampleSelectedOption = $""Option C - OnClick"")"" />
</BitMenuButton>


<BitMenuButton Sticky OnClick=""(BitMenuButtonOption item) => exampleSelectedOption = item?.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Split Sticky TItem=""BitMenuButtonOption"" ButtonStyle=""BitButtonStyle.Standard"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => exampleSelectedOption = $""Option A - OnClick"")"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => exampleSelectedOption = $""Option C - OnClick"")"" />
</BitMenuButton>";
    private readonly string example8CsharpCode = @"
private string? exampleSelectedOption;";

    private readonly string example9RazorCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>


<BitMenuButton TItem=""BitMenuButtonOption"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
    <Options>
        <BitMenuButtonOption Text=""Option A"" Key=""A"" />
        <BitMenuButtonOption Text=""Option B"" Key=""B"" />
        <BitMenuButtonOption Text=""Option C"" Key=""C"" />
        <BitMenuButtonOption Text=""Option D"" Key=""D"" />
    </Options>
</BitMenuButton>

<BitMenuButton Split
               Text=""Options""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
    <Options>
        <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </Options>
</BitMenuButton>

<BitMenuButton Text=""Options"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"">
        <Template Context=""item""><div class=""item-template-box"" style=""color:green"">@item.Text (@item.Key)</div></Template>
    </BitMenuButtonOption>
    <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"">
        <Template Context=""item""><div class=""item-template-box"" style=""color:yellow"">@item.Text (@item.Key)</div></Template>
    </BitMenuButtonOption>
    <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"">
        <Template Context=""item""><div class=""item-template-box"" style=""color:red"">@item.Text (@item.Key)</div></Template>
    </BitMenuButtonOption>
</BitMenuButton>";

    private readonly string example10RazorCode = @"
<style>
    .custom-class {
        color: aqua;
        overflow: hidden;
        border-radius: 1rem;
    }

    .custom-item {
        color: aqua;
        background-color: darkgoldenrod;
    }

    .custom-icon {
        color: red;
    }

    .custom-text {
        color: aqua;
    }
</style>


<BitMenuButton Text=""Styled Button""
               TItem=""BitMenuButtonOption""
               Style=""width: 200px; height: 40px;"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Classed Button""
               Class=""custom-class""
               TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Option Styled & Classed Button"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" Style=""color:red"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" Class=""custom-item"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" Style=""background:blue"" />
</BitMenuButton>


<BitMenuButton Text=""Styles""
               TItem=""BitMenuButtonOption""
               IconName=""@BitIconName.ExpandMenu""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown""
               Styles=""@(new() { Icon = ""color: red;"",
                                 Text = ""color: aqua;"",
                                 ItemText = ""color: dodgerblue; font-size: 11px;"",
                                 Overlay = ""background-color: var(--bit-clr-bg-overlay);"" })"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Classes""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard""
               Classes=""@(new() { Icon = ""custom-icon"", Text = ""custom-text"" })"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example11RazorCode = @"
<BitMenuButton Sticky @bind-SelectedItem=""twoWaySelectedOption""
               TItem=""BitMenuButtonOption""
               ButtonStyle=""BitButtonStyle.Standard"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<div>Selected item: <b>@twoWaySelectedOption?.Text</b></div>


<BitMenuButton Split Sticky OnChange=""(BitMenuButtonOption item) => changedSelectedOption = item"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<div>Changed item: <b>@changedSelectedOption?.Text</b></div>


<BitMenuButton Sticky TItem=""BitMenuButtonOption""ButtonStyle=""BitButtonStyle.Standard"" >
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" IsSelected=""true"" />
</BitMenuButton>";
    private readonly string example11CsharpCode = @"
private BitMenuButtonOption? changedSelectedOption;
private BitMenuButtonOption twoWaySelectedOption = default!;";

    private readonly string example12RazorCode = @"
<BitMenuButton Text=""گزینه ها""
               Dir=""BitDir.Rtl""
               TItem=""BitMenuButtonOption""
               IconName=""@BitIconName.Edit"">
    <BitMenuButtonOption Text=""گزینه الف"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ب"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ج"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Split
               Text=""گزینه ها""
               Dir=""BitDir.Rtl""
               TItem=""BitMenuButtonOption""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown"">
    <BitMenuButtonOption Text=""گزینه الف"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ب"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ج"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";
}

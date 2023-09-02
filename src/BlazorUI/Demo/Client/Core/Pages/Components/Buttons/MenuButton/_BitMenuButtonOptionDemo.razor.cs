namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonOptionDemo
{
    private string? example1SelectedItem;
    private string? example2SelectedItem;
    private string? example3SelectedItem;
    private string? example41SelectedItem;
    private string? example42SelectedItem;
    private string? example43SelectedItem;
    private string? example5SelectedItem;
    private string? example6SelectedItem;
    private string? example71SelectedItem;
    private string? example72SelectedItem;



    private readonly string example1HtmlCode = @"
<BitMenuButton Text=""Primary"" OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Standard""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Disabled"" IsEnabled=""false"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1CsharpCode = @"
private string example1SelectedItem;";

    private readonly string example2HtmlCode = @"
<BitMenuButton Text=""Option Disabled"" OnItemClick=""(BitMenuButtonOption item) => example2SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Option OnClick"" TItem=""BitMenuButtonOption"" ButtonStyle=""BitButtonStyle.Standard"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => example2SelectedItem = $""Option A - OnClick"")"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => example2SelectedItem = $""Option C - OnClick"")"" />
</BitMenuButton>

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2CsharpCode = @"
private string example2SelectedItem;";

    private readonly string example3HtmlCode = @"
<BitMenuButton Text=""IconName""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example3SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""ChevronDownIcon""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown""
               OnItemClick=""(BitMenuButtonOption item) => example3SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example3SelectedItem</div>";
    private readonly string example3CsharpCode = @"
private string example3SelectedItem;";

    private readonly string example4HtmlCode = @"
<style>
    .custom-class {
        color: aqua;
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
               Style=""width:200px;height:40px;background-color:#888;""
               OnItemClick=""(BitMenuButtonOption item) => example41SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Classed Button""
               Class=""custom-class""
               OnItemClick=""(BitMenuButtonOption item) => example41SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example41SelectedItem</div>


<BitMenuButton Text=""Option Styled & Classed Button"" OnItemClick=""(BitMenuButtonOption item) => example42SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" Style=""color:red"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" Class=""custom-item"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" Style=""background:blue"" />
</BitMenuButton>

<div>Clicked Item: @example42SelectedItem</div>


<BitMenuButton Text=""Styles""
               IconName=""@BitIconName.ExpandMenu""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown""
               OnItemClick=""(BitMenuButtonOption item) => example43SelectedItem = item.Key""
               Styles=""@(new() { Icon = ""color:red"" ,
                                 Text = ""color:aqua"",
                                 ItemText = ""color:dodgerblue;font-size:11px"",
                                 Overlay = ""background-color: var(--bit-clr-bg-overlay)"" })"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Classes""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonItem item) => example43SelectedItem = item.Key""
               Classes=""@(new() { Icon = ""custom-icon"" , Text = ""custom-text"" })"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example43SelectedItem</div>";
    private readonly string example4CsharpCode = @"
private string example41SelectedItem;
private string example42SelectedItem;
private string example43SelectedItem;";

    private readonly string example5HtmlCode = @"
Visible: [
<BitMenuButton Visibility=""BitVisibility.Visible"" Text=""Visible menu button"" OnItemClick=""(BitMenuButtonOption item) => example5SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton> ]

Hidden: [
<BitMenuButton Visibility=""BitVisibility.Hidden"" Text=""Hidden menu button"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton> ]

Collapsed: [
<BitMenuButton Visibility=""BitVisibility.Collapsed"" Text=""Collapsed menu button"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton> ]";
    private readonly string example5CsharpCode = @"
private string example5SelectedItem;";

    private readonly string example6HtmlCode = @"
<BitMenuButton OnItemClick=""(BitMenuButtonOption item) => example6SelectedItem = item.Key"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Warning"" />
        <div style=""font-weight: 600; color: white;"">
            Custom Header!
        </div>
        <BitIcon IconName=""@BitIconName.Warning"" />
    </HeaderTemplate>
    <ChildContent>
        <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
        <BitMenuButtonOption Text=""Option D"" Key=""D"" IconName=""@BitIconName.Emoji"" />
    </ChildContent>
</BitMenuButton>

<BitMenuButton ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonOption item) => example6SelectedItem = item.Key"">
    <HeaderTemplate>
        <div style=""font-weight: bold; color: #d13438;"">
            Custom Header!
        </div>
    </HeaderTemplate>
    <ChildContent>
        <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
        <BitMenuButtonOption Text=""Option D"" Key=""D"" IconName=""@BitIconName.Emoji"" />
    </ChildContent>
</BitMenuButton>

<div>Clicked Item: @example5SelectedItem</div>";
    private readonly string example6CsharpCode = @"
private string example6SelectedItem;";

    private readonly string example7HtmlCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example71SelectedItem = item.Key"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </ChildContent>
</BitMenuButton>

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Edit""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonOption item) => example71SelectedItem = item.Key"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </ChildContent>
</BitMenuButton>

<div>Clicked Item: @example71SelectedItem</div>


<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example72SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"">
        <Template Context=""item""><div class=""item-template-box"" style=""color:green"">@item.Text (@item.Key)</div></Template>
    </BitMenuButtonOption>
    <BitMenuButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"">
        <Template Context=""item""><div class=""item-template-box"" style=""color:yellow"">@item.Text (@item.Key)</div></Template>
    </BitMenuButtonOption>
    <BitMenuButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"">
        <Template Context=""item""><div class=""item-template-box"" style=""color:red"">@item.Text (@item.Key)</div></Template>
    </BitMenuButtonOption>
</BitMenuButton>

<div>Clicked Item: @example72SelectedItem</div>";
    private readonly string example7CsharpCode = @"
private string example71SelectedItem;
private string example72SelectedItem;";
}

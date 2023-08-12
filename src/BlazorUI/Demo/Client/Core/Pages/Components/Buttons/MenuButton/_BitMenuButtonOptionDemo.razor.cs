namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonOptionDemo
{
    private readonly string example1HTMLCode = @"
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

<BitMenuButton Text=""Option Disabled"" OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Option OnClick"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => example1SelectedItem = $""Option A - OnClick"")"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => example1SelectedItem = $""Option C - OnClick"")"" />
</BitMenuButton>

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1CSharpCode = @"
private string example1SelectedItem;";

    private readonly string example2HTMLCode = @"
<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example2SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonOption item) => example2SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2CSharpCode = @"
private string example2SelectedItem;";

    private readonly string example3HTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        border-radius: 1rem;
    }

    .custom-item {
        color: aqua;
        background-color: darkgoldenrod;
    }
</style>

<BitMenuButton Text=""Styled Button""
               OnItemClick=""(BitMenuButtonOption item) => example31SelectedItem = item.Key""
               Style=""width: 200px; height: 40px; background-color: #8A8886; border-color: black;"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Classed Button""
               OnItemClick=""(BitMenuButtonOption item) => example31SelectedItem = item.Key""
               Class=""custom-class"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example31SelectedItem</div>


<BitMenuButton Text=""Option Styled & Classed Button"" OnItemClick=""(BitMenuButtonOption item) => example32SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" Style=""color:red"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" Class=""custom-item"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" Style=""background:blue"" />
</BitMenuButton>

<div>Clicked Item: @example32SelectedItem</div>";
    private readonly string example3CSharpCode = @"
private string example31SelectedItem;
private string example32SelectedItem;";

    private readonly string example4HTMLCode = @"
<BitMenuButton OnItemClick=""(BitMenuButtonOption item) => example4SelectedItem = item.Key"">
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
               OnItemClick=""(BitMenuButtonOption item) => example4SelectedItem = item.Key"">
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

<div>Clicked Item: @example4SelectedItem</div>";
    private readonly string example4CSharpCode = @"
private string example4SelectedItem;";

    private readonly string example5HTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example51SelectedItem = item.Key"">
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
               OnItemClick=""(BitMenuButtonOption item) => example51SelectedItem = item.Key"">
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

<div>Clicked Item: @example51SelectedItem</div>


<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example52SelectedItem = item.Key"">
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

<div>Clicked Item: @example52SelectedItem</div>";
    private readonly string example5CSharpCode = @"
private string example51SelectedItem;
private string example52SelectedItem;";

    private readonly string example6HTMLCode = @"
<BitMenuButton Text=""ClassStyles""
               IconName=""@BitIconName.ExpandMenu""
               ChevronDownIcon=""@BitIconName.DoubleChevronDown""
               OnItemClick=""(BitMenuButtonOption item) => example6SelectedItem = item.Key""
               ClassStyles=""@(new() { Icon = { Style = ""color:red"" },
                                      Text = { Style = ""color:aqua"" },
                                      ChevronDown = { Style = ""color:darkred;font-size:10px"" }})"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example6SelectedItem</div>";
    private readonly string example6CSharpCode = @"
private string? example6SelectedItem;";

}

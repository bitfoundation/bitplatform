namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonOptionDemo
{
    private readonly string example1BitMenuButtonOptionHTMLCode = @"
<BitMenuButton Text=""Primary""
               ButtonStyle=""BitButtonStyle.Primary""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Standard""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Disabled""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key""
               IsEnabled=""false"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Item Disabled""
               OnItemClick=""(BitMenuButtonOption item) => example1SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example1SelectedItem</div>";
    private readonly string example1BitMenuButtonOptionCSharpCode = @"
private string example1SelectedItem;
";

    private readonly string example2BitMenuButtonOptionHTMLCode = @"
<BitMenuButton Text=""Primary Button""
               IconName=""@BitIconName.Edit""
               ButtonStyle=""BitButtonStyle.Primary""
               OnItemClick=""(BitMenuButtonOption item) => example2SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Standard Button""
               IconName=""@BitIconName.Add""
               ButtonStyle=""BitButtonStyle.Standard""
               OnItemClick=""(BitMenuButtonOption item) => example2SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example2SelectedItem</div>";
    private readonly string example2BitMenuButtonOptionCSharpCode = @"
private string example2SelectedItem;";

    private readonly string example3BitMenuButtonOptionHTMLCode = @"
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
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Classed Button""
               OnItemClick=""(BitMenuButtonOption item) => example31SelectedItem = item.Key""
               Class=""custom-class"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<div>Clicked Item: @example31SelectedItem</div>


<BitMenuButton Text=""Item Styled & Classed Button"" OnItemClick=""(BitMenuButtonOption item) => example32SelectedItem = item.Key"">
    <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" Style=""color:red"" />
    <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" Class=""custom-item"" />
    <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" Style=""background:blue"" />
</BitMenuButton>

<div>Clicked Item: @example32SelectedItem</div>";
    private readonly string example3BitMenuButtonOptionCSharpCode = @"
private string example31SelectedItem;
private string example32SelectedItem;";

    private readonly string example4BitMenuButtonOptionHTMLCode = @"
<BitMenuButton ButtonStyle=""BitButtonStyle.Primary""
               OnItemClick=""(BitMenuButtonOption item) => example4SelectedItem = item.Key"">
    <HeaderTemplate>
        <BitIcon IconName=""@BitIconName.Warning"" />
        <div style=""font-weight: 600; color: white;"">
            Custom Header!
        </div>
        <BitIcon IconName=""@BitIconName.Warning"" />
    </HeaderTemplate>
    <ChildContent>
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
        <BitMenuButtonOption Text=""Item D"" Key=""D"" IconName=""@BitIconName.Emoji"" />
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
        <BitMenuButtonOption Text=""Item A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
        <BitMenuButtonOption Text=""Item C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
        <BitMenuButtonOption Text=""Item D"" Key=""D"" IconName=""@BitIconName.Emoji"" />
    </ChildContent>
</BitMenuButton>

<div>Clicked Item: @example4SelectedItem</div>";
    private readonly string example4BitMenuButtonOptionCSharpCode = @"
private string example4SelectedItem;
";

    private readonly string example5BitMenuButtonOptionHTMLCode = @"
<style>
    .item-template-box {
        display: flex;
        width: 100%;
    }
</style>

<BitMenuButton Text=""Primary Button""
               ButtonStyle=""BitButtonStyle.Primary""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example5SelectedItem = item.Key"">
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
               ButtonStyle=""BitButtonStyle.Standard""
               IconName=""@BitIconName.Edit""
               OnItemClick=""(BitMenuButtonOption item) => example5SelectedItem = item.Key"">
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

<div>Clicked Item: @example5SelectedItem</div>";
    private readonly string example5BitMenuButtonOptionCSharpCode = @"
private string example5SelectedItem;
";
}

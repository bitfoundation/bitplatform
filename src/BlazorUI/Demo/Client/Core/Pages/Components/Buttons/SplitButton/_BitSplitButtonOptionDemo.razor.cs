namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.SplitButton;

public partial class _BitSplitButtonOptionDemo
{
    private string? example1SelectedItem;
    private string? example2SelectedItem;
    private string? example3SelectedItem;
    private string? example4SelectedItem;
    private string? example51SelectedItem;
    private string? example52SelectedItem;
    private string? example53SelectedItem;
    private string? example61SelectedItem;
    private string? example62SelectedItem;

    private BitSplitButtonOption? twoWaySelectedItem;
    private BitSplitButtonOption? changedSelectedItem;


    private readonly string example1HTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton OnClick=""(BitSplitButtonOption item) => example1SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>
        
<BitLabel>Standard</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => example1SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<BitLabel>Disabled</BitLabel>
<BitSplitButton IsEnabled=""false"" TItem=""BitSplitButtonOption"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<div>Clicked item: @example1SelectedItem</div>";
    private readonly string example1CSharpCode = @"
private string example1SelectedItem;
";

    private readonly string example2HTMLCode = @"
<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => example2SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>
        
<BitLabel>Standard</BitLabel>
<BitSplitButton IsSticky=""true""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => example2SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>

<div>Clicked item: @example2SelectedItem</div>";
    private readonly string example2CSharpCode = @"
private string example2SelectedItem;
";

    private readonly string example3HTMLCode = @"
<BitLabel>Sticky Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => example3SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji2"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option D"" Key=""D"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>
        
<BitLabel>Basic Standard</BitLabel>
<BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => example3SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji2"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option D"" Key=""D"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<div>Clicked item: @example3SelectedItem</div>";
    private readonly string example3CSharpCode = @"
private string example3SelectedItem;
";

    private readonly string example4HTMLCode = @"
<BitSplitButton TItem=""BitSplitButtonOption"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => example4SelectedItem = $""Option A - OnClick"")"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => example4SelectedItem = $""Option B - OnClick"")"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => example4SelectedItem = $""Option C - OnClick"")"" />
</BitSplitButton>

<BitSplitButton IsSticky=""true""
                TItem=""BitSplitButtonOption""
                ButtonStyle=""BitButtonStyle.Standard"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => example4SelectedItem = $""Option A - OnClick - Sticky"")"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => example4SelectedItem = $""Option B - OnClick - Sticky"")"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => example4SelectedItem = $""Option C - OnClick - Sticky"")"" />
</BitSplitButton>

<div>Clicked item: @example4SelectedItem</div>";
    private readonly string example4CSharpCode = @"
private string? example4SelectedItem;";

    private readonly string example5HTMLCode = @"
<style>
    .custom-class {
        color: aqua;
        font-size: 18px;
    }

    .custom-item {
        color: aqua;
        background-color: darkgoldenrod;
    }

    .custom-chevron {
        background-color: aquamarine;
    }

    .custom-button {
        background-color: brown;
    }
</style>

<BitSplitButton Style=""width:200px;height:40px;""
                ChevronDownIcon=""@BitIconName.DoubleChevronDown8""
                OnClick=""(BitSplitButtonOption item) => example51SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<BitSplitButton Class=""custom-class""
                OnClick=""(BitSplitButtonOption item) => example51SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<div>Clicked item: @example51SelectedItem</div>


<BitSplitButton IsSticky=""true"" OnClick=""(BitSplitButtonOption item) => example52SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" Style=""color:red"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" Class=""custom-item"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" Style=""background:blue"" />
</BitSplitButton>

<div>Clicked Item: @example52SelectedItem</div>


<BitSplitButton OnClick=""(BitSplitButtonOption item) => example53SelectedItem = item.Text""
                Styles=""@(new() { ChevronDownButton=""background-color:red"",
                                  ChevronDownIcon=""color:darkblue"",
                                  ItemButton=""background:darkgoldenrod"" })"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>

<BitSplitButton ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => example53SelectedItem = item.Text""
                Classes=""@(new() { ChevronDownButton=""custom-chevron"",
                                   ItemButton=""custom-button"" })"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>
        
<div>Clicked Item: @example53SelectedItem</div>";
    private readonly string example5CSharpCode = @"
private string example51SelectedItem;
private string example52SelectedItem;
private string example53SelectedItem;";

    private readonly string example6HTMLCode = @"
<style>
    .item-template-box {
        gap: 6px;
        width: 100%;
        display: flex;
        align-items: center;
    }
</style>

<BitLabel>Primary</BitLabel>
<BitSplitButton IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => example61SelectedItem = item.Text"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </ChildContent>
</BitSplitButton>
        
<BitLabel>Standard</BitLabel>
<BitSplitButton IsSticky=""true""
                ButtonStyle=""BitButtonStyle.Standard""
                OnClick=""(BitSplitButtonOption item) => example61SelectedItem = item.Text"">
    <ItemTemplate Context=""item"">
        @if (item.Key == ""add-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Add"" />
                <span style=""color: green;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
        else if (item.Key == ""edit-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Edit"" />
                <span style=""color: yellow;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
        else if (item.Key == ""delete-key"")
        {
            <div class=""item-template-box"">
                <BitIcon IconName=""@BitIconName.Delete"" />
                <span style=""color: red;"">
                    @item.Text (@item.Key)
                </span>
            </div>
        }
    </ItemTemplate>
    <ChildContent>
        <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
        <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
        <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
    </ChildContent>
</BitSplitButton>

<div>Clicked item: @example61SelectedItem</div>


<BitSplitButton IsSticky=""true""
                OnClick=""(BitSplitButtonOption item) => example62SelectedItem = item.Text"">
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color: @(item.Key == ""add-key"" ? ""green"" : item.Key == ""edit-key"" ? ""yellow"" : ""red"");"">
                @item.Text (@item.Key)
            </span>
        </div>
    </ItemTemplate>
    <ChildContent>
        <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"">
            <Template Context=""item""><div class=""item-template-box"" style=""color:green"">@item.Text (@item.Key)</div></Template>
        </BitSplitButtonOption>
        <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"">
            <Template Context=""item""><div class=""item-template-box"" style=""color:yellow"">@item.Text (@item.Key)</div></Template>
        </BitSplitButtonOption>
        <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"">
            <Template Context=""item""><div class=""item-template-box"" style=""color:red"">@item.Text (@item.Key)</div></Template>
        </BitSplitButtonOption>
    </ChildContent>
</BitSplitButton>

<div>Clicked Item: @example62SelectedItem</div>";
    private readonly string example6CSharpCode = @"
private string example61SelectedItem;
private string example62SelectedItem;";

    private readonly string example7HTMLCode = @"
<BitSplitButton @bind-SelectedItem=""twoWaySelectedItem""
                IsSticky=""true""
                TItem=""BitSplitButtonOption""
                ButtonStyle=""BitButtonStyle.Standard"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>
<div>Selected item: <b>@twoWaySelectedItem?.Text</b></div>

<BitSplitButton IsSticky=""true"" OnChange=""(BitSplitButtonOption item) => changedSelectedItem = item"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton>
<div>Changed item: <b>@changedSelectedItem?.Text</b></div>";
    private readonly string example7CSharpCode = @"
private BitSplitButtonOption? twoWaySelectedItem;
private BitSplitButtonOption? changedSelectedItem;";

}

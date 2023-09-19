namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.SplitButton;

public partial class _BitSplitButtonOptionDemo
{
    private string? example41SelectedItem;
    private string? example42SelectedItem;

    private BitSplitButtonOption? twoWaySelectedItem;
    private BitSplitButtonOption? changedSelectedItem;



    private readonly string example1RazorCode = @"
<BitSplitButton TItem=""BitSplitButtonOption"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>
        
<BitSplitButton TItem=""BitSplitButtonOption"" ButtonStyle=""BitButtonStyle.Standard"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>

<BitSplitButton TItem=""BitSplitButtonOption"" IsEnabled=""false"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>";
    private readonly string example1CsharpCode = @"";

    private readonly string example2RazorCode = @"
<BitSplitButton TItem=""BitSplitButtonOption"" IsSticky=""true"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>
        
<BitSplitButton TItem=""BitSplitButtonOption"" ButtonStyle=""BitButtonStyle.Standard"" IsSticky=""true"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>";
    private readonly string example2CsharpCode = @"";

    private readonly string example3RazorCode = @"
<BitSplitButton TItem=""BitSplitButtonOption"" IsSticky=""true"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" IsEnabled=""false"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>

<BitSplitButton TItem=""BitSplitButtonOption"" ButtonStyle=""BitButtonStyle.Standard"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" IsEnabled=""false"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>";
    private readonly string example3CsharpCode = @"";

    private readonly string example4RazorCode = @"
<BitSplitButton IsSticky=""true"" OnClick=""(BitSplitButtonOption item) => example41SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>

<BitSplitButton ButtonStyle=""BitButtonStyle.Standard"" OnClick=""(BitSplitButtonOption item) => example41SelectedItem = item.Text"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>

<div>Clicked item: <b>@example41SelectedItem</b></div>



<BitSplitButton IsSticky=""true"" TItem=""BitSplitButtonOption"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" OnClick=""@(_ => example42SelectedItem = ""Add - Sticky"")"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" OnClick=""@(_ => example42SelectedItem = ""Edit - Sticky"")"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" OnClick=""@(_ => example42SelectedItem = ""Delete - Sticky"")"" />
</BitSplitButton>

<BitSplitButton TItem=""BitSplitButtonOption"" ButtonStyle=""BitButtonStyle.Standard"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" OnClick=""@(_ => example42SelectedItem = ""Add"")"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" OnClick=""@(_ => example42SelectedItem = ""Edit"")"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" OnClick=""@(_ => example42SelectedItem = ""Delete"")"" />
</BitSplitButton>

<div>Clicked item: <b>@example42SelectedItem</b></div>";
    private readonly string example4CsharpCode = @"
private string? example41SelectedItem;
private string? example42SelectedItem;";

    private readonly string example5RazorCode = @"
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

<BitSplitButton TItem=""BitSplitButtonOption""
                Style=""width:200px;height:40px;""
                ChevronDownIcon=""@BitIconName.DoubleChevronDown8"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>

<BitSplitButton TItem=""BitSplitButtonOption"" Class=""custom-class"" ButtonStyle=""BitButtonStyle.Standard"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>


<BitSplitButton TItem=""BitSplitButtonOption"" IsSticky=""true"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" Style=""color:red"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" Class=""custom-item"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" Style=""background:blue"" />
</BitSplitButton>


<BitSplitButton TItem=""BitSplitButtonOption""
                Styles=""@(new() { ChevronDownButton=""background-color:red"",
                                  ChevronDownIcon=""color:darkblue"",
                                  ItemButton=""background:darkgoldenrod"" })"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>

<BitSplitButton TItem=""BitSplitButtonOption""
                ButtonStyle=""BitButtonStyle.Standard""
                Classes=""@(new() { ChevronDownButton=""custom-chevron"", ItemButton=""custom-button"" })"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>";
    private readonly string example5CsharpCode = @"";

    private readonly string example6RazorCode = @"
Visible: [
<BitSplitButton Visibility=""BitVisibility.Visible"" TItem=""BitSplitButtonOption"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton> ]

Hidden: [
<BitSplitButton Visibility=""BitVisibility.Hidden"" TItem=""BitSplitButtonOption"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton> ]

Collapsed: [
<BitSplitButton Visibility=""BitVisibility.Collapsed"" TItem=""BitSplitButtonOption"">
    <BitSplitButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitSplitButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitSplitButton> ]";
    private readonly string example6CsharpCode = @"";

    private readonly string example7RazorCode = @"
<style>
    .item-template-box {
        gap: 6px;
        width: 100%;
        display: flex;
        align-items: center;
    }
</style>

<BitSplitButton TItem=""BitSplitButtonOption"" IsSticky=""true"">
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

        
<BitSplitButton TItem=""BitSplitButtonOption"" ButtonStyle=""BitButtonStyle.Standard"" IsSticky=""true"">
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
        <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""BitIconName.Delete"" />
    </ChildContent>
</BitSplitButton>



<BitSplitButton TItem=""BitSplitButtonOption"" IsSticky=""true"">
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
</BitSplitButton>";
    private readonly string example7CsharpCode = @"";

    private readonly string example8RazorCode = @"
<BitSplitButton @bind-SelectedItem=""twoWaySelectedItem""
                IsSticky=""true""
                TItem=""BitSplitButtonOption""
                ButtonStyle=""BitButtonStyle.Standard"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>
<div>Selected item: <b>@twoWaySelectedItem?.Text</b></div>


<BitSplitButton IsSticky=""true"" OnChange=""(BitSplitButtonOption item) => changedSelectedItem = item"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" />
</BitSplitButton>
<div>Changed item: <b>@changedSelectedItem?.Text</b></div>


<BitSplitButton IsSticky=""true"" TItem=""BitSplitButtonOption"">
    <BitSplitButtonOption Text=""Add"" Key=""add-key"" IconName=""@BitIconName.Add"" />
    <BitSplitButtonOption Text=""Edit"" Key=""edit-key"" IconName=""@BitIconName.Edit"" />
    <BitSplitButtonOption Text=""Delete"" Key=""delete-key"" IconName=""@BitIconName.Delete"" IsSelected=""true"" />
</BitSplitButton>";
    private readonly string example8CsharpCode = @"
private BitSplitButtonOption? twoWaySelectedItem;
private BitSplitButtonOption? changedSelectedItem;";
}

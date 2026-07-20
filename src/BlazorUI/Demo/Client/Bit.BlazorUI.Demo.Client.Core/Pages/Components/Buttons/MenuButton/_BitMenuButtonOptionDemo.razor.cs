namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.MenuButton;

public partial class _BitMenuButtonOptionDemo
{
    private string? exampleChangedOption;
    private string? exampleClickedOption;

    private BitMenuButtonOption twoWaySelectedOption = default!;

    private bool oneWayIsOpen;
    private bool twoWayIsOpen;

    private bool optionIsToggled;
    private bool optionToggledValue;

    private BitMenuButtonOption optionA = default!;
    private BitMenuButtonOption optionB = default!;
    private BitMenuButtonOption optionC = default!;

    private bool optionIsLoading;
    private bool optionAutoIsLoading;

    private async Task HandleOnLoadingClick()
    {
        optionAutoIsLoading = true;
        await Task.Delay(2000);
        optionAutoIsLoading = false;
    }



    private readonly string example1RazorCode = @"
<BitMenuButton Text=""MenuButton"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example2RazorCode = @"
<BitMenuButton Text=""Split"" TItem=""BitMenuButtonOption"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example3RazorCode = @"
<BitMenuButton Text=""Fill"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Outline"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Text"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Fill"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" IsEnabled=""false"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Outline"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Text"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Fill"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Outline"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Text"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Fill"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" IsEnabled=""false"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Outline"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" IsEnabled=""false"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Text"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" IsEnabled=""false"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example4RazorCode = @"
<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Split Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Split Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Split Sticky>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example5RazorCode = @"
<BitMenuButton Text=""IconName"" IconName=""@BitIconName.Edit"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""ChevronDownIconName"" TItem=""BitMenuButtonOption"" ChevronDownIconName=""@BitIconName.DoubleChevronDown"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";

    private readonly string example6RazorCode = @"
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Background=""BitColorKind.Primary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Background=""BitColorKind.Secondary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Background=""BitColorKind.Tertiary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Transparent"" TItem=""BitMenuButtonOption"" Background=""BitColorKind.Transparent"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example7RazorCode = @"
<BitMenuButton Text=""Toggle"" TItem=""BitMenuButtonOption"" Split Toggle>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""DefaultIsToggled"" TItem=""BitMenuButtonOption"" Split Toggle DefaultIsToggled=""true"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Two-way"" TItem=""BitMenuButtonOption"" Split Toggle @bind-IsToggled=""optionIsToggled"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitCheckbox Label=""IsToggled"" @bind-Value=""optionIsToggled"" />


<BitMenuButton Text=""OnToggleChange"" TItem=""BitMenuButtonOption"" Split Toggle OnToggleChange=""v => optionToggledValue = v"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<div>OnToggleChange: @optionToggledValue</div>";

    private readonly string example8RazorCode = @"
<BitMenuButton Text=""With icon"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.Edit"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""No icon"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.Edit"" NoIcon>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>


<BitMenuButton Sticky TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Sticky TItem=""BitMenuButtonOption"" NoIcon>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";

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
        <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
        <BitMenuButtonOption Text=""Option C"" Key=""C"" />
    </Options>
</BitMenuButton>


<BitMenuButton Text=""Options"" TItem=""BitMenuButtonOption"" Split>
    <ItemTemplate Context=""item"">
        <div class=""item-template-box"">
            <span style=""color:brown"">@item.Text (@item.Key)</span>
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
<BitMenuButton Text=""Options""
               OnChange=""(BitMenuButtonOption item) => exampleChangedOption = item?.Key""
               OnClick=""(BitMenuButtonOption item) => exampleClickedOption = item?.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Split Text=""Options"" TItem=""BitMenuButtonOption""
               OnChange=""(BitMenuButtonOption item) => exampleChangedOption = item?.Key""
               OnClick=""@((BitMenuButtonOption item) => exampleClickedOption = ""Main button clicked"")"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => exampleClickedOption = $""Option A"")"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => exampleClickedOption = $""Option C"")"" />
</BitMenuButton>

<BitMenuButton Sticky
               OnChange=""(BitMenuButtonOption item) => exampleChangedOption = item?.Key""
               OnClick=""(BitMenuButtonOption item) => exampleClickedOption = item?.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Split Sticky TItem=""BitMenuButtonOption""
               OnChange=""(BitMenuButtonOption item) => exampleChangedOption = item?.Key""
               OnClick=""(BitMenuButtonOption item) => exampleClickedOption = item?.Key"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" OnClick=""@(_ => exampleClickedOption = $""Option A"")"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" OnClick=""@(_ => exampleClickedOption = $""Option C"")"" />
</BitMenuButton>


<div>Changed option: @exampleChangedOption</div>
<div>Clicked option: @exampleClickedOption</div>";

    private readonly string example10CsharpCode = @"
private string? exampleChangedOption;
private string? exampleClickedOption;";

    private readonly string example11RazorCode = @"
<BitMenuButton Text=""Coming soon..."" TItem=""BitMenuButtonOption"" IsEnabled=""false"" />


<BitMenuButton Sticky @bind-SelectedItem=""twoWaySelectedOption"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" @ref=""optionA"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" @ref=""optionB"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" @ref=""optionC"" />
</BitMenuButton>
<BitChoiceGroup Horizontal TItem=""BitChoiceGroupOption<BitMenuButtonOption>"" TValue=""BitMenuButtonOption"" @bind-Value=""@twoWaySelectedOption"">
    <BitChoiceGroupOption Text=""Option A"" Id=""A"" Value=""optionA"" />
    <BitChoiceGroupOption Text=""Option B"" Id=""B"" Value=""optionB"" IsEnabled=""false"" />
    <BitChoiceGroupOption Text=""Option C"" Id=""C"" Value=""optionC"" />
</BitChoiceGroup>


<BitMenuButton Sticky TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" IsSelected=""true"" />
</BitMenuButton>


<BitMenuButton Sticky IsOpen=""oneWayIsOpen"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" @ref=""optionA"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" @ref=""optionB"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" @ref=""optionC"" />
</BitMenuButton>
<BitCheckbox Label=""One-way IsOpen"" @bind-Value=""oneWayIsOpen"" OnChange=""async _ => { await Task.Delay(2000); oneWayIsOpen = false; }"" />


<BitMenuButton Sticky @bind-IsOpen=""twoWayIsOpen"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" @ref=""optionA"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" @ref=""optionB"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" @ref=""optionC"" />
</BitMenuButton>
<BitCheckbox Label=""Two-way IsOpen"" @bind-Value=""twoWayIsOpen"" />";

    private readonly string example11CsharpCode = @"
private BitMenuButtonOption twoWaySelectedOption = default!;
private bool oneWayIsOpen;
private bool twoWayIsOpen;";

    private readonly string example12RazorCode = @"
<BitMenuButton Text=""File"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.OpenFile"">
    <BitMenuButtonOption Text=""New"" Key=""new"" IconName=""@BitIconName.Add"" />
    <BitMenuButtonOption Text=""Open"" Key=""open"" IconName=""@BitIconName.OpenFile"" />
    <BitMenuButtonOption IsSeparator />
    <BitMenuButtonOption Text=""Save"" Key=""save"" IconName=""@BitIconName.Save"" />
    <BitMenuButtonOption Text=""Save as"" Key=""save-as"" IconName=""@BitIconName.SaveAs"" />
    <BitMenuButtonOption IsSeparator />
    <BitMenuButtonOption Text=""Delete"" Key=""delete"" IconName=""@BitIconName.Delete"" />
</BitMenuButton>

<BitMenuButton Split Sticky TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""New"" Key=""new"" IconName=""@BitIconName.Add"" />
    <BitMenuButtonOption Text=""Open"" Key=""open"" IconName=""@BitIconName.OpenFile"" />
    <BitMenuButtonOption IsSeparator />
    <BitMenuButtonOption Text=""Save"" Key=""save"" IconName=""@BitIconName.Save"" />
    <BitMenuButtonOption Text=""Save as"" Key=""save-as"" IconName=""@BitIconName.SaveAs"" />
    <BitMenuButtonOption IsSeparator />
    <BitMenuButtonOption Text=""Delete"" Key=""delete"" IconName=""@BitIconName.Delete"" />
</BitMenuButton>";

    private readonly string example13RazorCode = @"
<BitMenuButton Text=""Links"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.Globe"">
    <BitMenuButtonOption Text=""bit platform"" Key=""bit"" IconName=""@BitIconName.Globe"" Href=""https://bitplatform.dev"" Target=""_blank"" Title=""The bit platform website"" />
    <BitMenuButtonOption Text=""GitHub repo"" Key=""github"" IconName=""@BitIconName.Link"" Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" Title=""The bit platform GitHub repository"" />
    <BitMenuButtonOption IsSeparator />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" Title=""A regular option"" />
</BitMenuButton>";

    private readonly string example14RazorCode = @"
<BitMenuButton Text=""Full width"" TItem=""BitMenuButtonOption"" FullWidth>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Full width split"" TItem=""BitMenuButtonOption"" FullWidth Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";

    private readonly string example15RazorCode = @"
<BitMenuButton Split Text=""Save"" IconName=""@BitIconName.Save"" IsLoading=""optionAutoIsLoading"" OnClick=""(BitMenuButtonOption option) => HandleOnLoadingClick()"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""Loading"" IsLoading=""optionIsLoading"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>
<BitCheckbox Label=""IsLoading"" @bind-Value=""optionIsLoading"" />";

    private readonly string example15CsharpCode = @"
private bool optionIsLoading;
private bool optionAutoIsLoading;

private async Task HandleOnLoadingClick()
{
    optionAutoIsLoading = true;
    await Task.Delay(2000);
    optionAutoIsLoading = false;
}";

    private readonly string example16RazorCode = @"
<BitMenuButton Text=""Hover over me"" Title=""The menu button tooltip"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Split Text=""Split"" Title=""The main button tooltip"" ChevronDownAriaLabel=""Open the menu"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IconName=""@BitIconName.Emoji"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";

    private readonly string example17RazorCode = @"
<BitMenuButton Text=""TopAndBottom"" DropDirection=""BitDropDirection.TopAndBottom"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
    <BitMenuButtonOption Text=""Option D"" Key=""D"" />
    <BitMenuButtonOption Text=""Option E"" Key=""E"" />
    <BitMenuButtonOption Text=""Option F"" Key=""F"" />
</BitMenuButton>

<BitMenuButton Text=""All"" DropDirection=""BitDropDirection.All"" TItem=""BitMenuButtonOption"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
    <BitMenuButtonOption Text=""Option D"" Key=""D"" />
    <BitMenuButtonOption Text=""Option E"" Key=""E"" />
    <BitMenuButtonOption Text=""Option F"" Key=""F"" />
</BitMenuButton>";

    private readonly string example18RazorCode = @"
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Primary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Primary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Primary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Primary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Secondary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Secondary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Secondary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Tertiary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Tertiary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Tertiary"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Info"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Info"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Info"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Info"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Info"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Success"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Success"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Success"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Success"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Success"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Warning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Warning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Warning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Warning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SevereWarning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SevereWarning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""SevereWarning"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SevereWarning"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Error"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.Error"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Error"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.Error"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""PrimaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBackground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBackground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBackground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""SecondaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBackground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBackground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBackground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""TertiaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBackground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBackground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBackground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBackground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""PrimaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryForeground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryForeground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryForeground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""SecondaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryForeground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryForeground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryForeground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""TertiaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryForeground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryForeground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryForeground"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryForeground"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""PrimaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.PrimaryBorder"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.PrimaryBorder"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""PrimaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.PrimaryBorder"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""SecondaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.SecondaryBorder"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.SecondaryBorder"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""SecondaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.SecondaryBorder"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""TertiaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Color=""BitColor.TertiaryBorder"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Color=""BitColor.TertiaryBorder"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""TertiaryBorder"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Color=""BitColor.TertiaryBorder"" Split>
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example19RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitMenuButton Text=""Actions"" 
               TItem=""BitMenuButtonOption""
               Icon=""@(""fa-solid fa-house"")"">
    <BitMenuButtonOption Text=""Add"" Icon=""@(""fa-solid fa-plus"")"" />
    <BitMenuButtonOption Text=""Edit"" Icon=""@(""fa-solid fa-pen"")"" />
    <BitMenuButtonOption Text=""Delete"" Icon=""@(""fa-solid fa-trash"")"" />
</BitMenuButton>

<BitMenuButton Split 
               Text=""Actions"" 
               TItem=""BitMenuButtonOption""
               Icon=""@(""fa-brands fa-github"")"">
    <BitMenuButtonOption Text=""Add"" Icon=""@(""fa-solid fa-plus"")"" />
    <BitMenuButtonOption Text=""Edit"" Icon=""@(""fa-solid fa-pen"")"" />
    <BitMenuButtonOption Text=""Delete"" Icon=""@(""fa-solid fa-trash"")"" />
</BitMenuButton>



<BitMenuButton Text=""Actions"" 
               Color=""BitColor.Secondary""
               TItem=""BitMenuButtonOption"" 
               Variant=""BitVariant.Outline""
               Icon=""@BitIconInfo.Css(""fa-solid fa-house"")"">
    <BitMenuButtonOption Text=""Add"" Icon=""@BitIconInfo.Css(""fa-solid fa-plus"")"" />
    <BitMenuButtonOption Text=""Edit"" Icon=""@BitIconInfo.Css(""fa-solid fa-pen"")"" />
    <BitMenuButtonOption Text=""Delete"" Icon=""@BitIconInfo.Css(""fa-solid fa-trash"")"" />
</BitMenuButton>

<BitMenuButton Split 
               Text=""Actions"" 
               Color=""BitColor.Secondary""
               TItem=""BitMenuButtonOption"" 
               Variant=""BitVariant.Outline""
               Icon=""@BitIconInfo.Css(""fa-brands fa-github"")"">
    <BitMenuButtonOption Text=""Add"" Icon=""@BitIconInfo.Css(""fa-solid fa-plus"")"" />
    <BitMenuButtonOption Text=""Edit"" Icon=""@BitIconInfo.Css(""fa-solid fa-pen"")"" />
    <BitMenuButtonOption Text=""Delete"" Icon=""@BitIconInfo.Css(""fa-solid fa-trash"")"" />
</BitMenuButton>



<BitMenuButton Text=""Actions"" 
               Variant=""BitVariant.Text""
               Color=""BitColor.Tertiary""
               TItem=""BitMenuButtonOption"" 
               Icon=""@BitIconInfo.Fa(""solid house"")"">
    <BitMenuButtonOption Text=""Add"" Icon=""@BitIconInfo.Fa(""solid plus"")"" />
    <BitMenuButtonOption Text=""Edit"" Icon=""@BitIconInfo.Fa(""solid pen"")"" />
    <BitMenuButtonOption Text=""Delete"" Icon=""@BitIconInfo.Fa(""solid trash"")"" />
</BitMenuButton>

<BitMenuButton Split
               Text=""Actions""
               Color=""BitColor.Tertiary""
               Variant=""BitVariant.Text""
               TItem=""BitMenuButtonOption""
               Icon=""@BitIconInfo.Fa(""brands github"")""
               ChevronDownIcon=""@BitIconInfo.Fa(""solid angles-down"")"">
    <BitMenuButtonOption Text=""Add"" Icon=""@BitIconInfo.Fa(""solid plus"")"" />
    <BitMenuButtonOption Text=""Edit"" Icon=""@BitIconInfo.Fa(""solid pen"")"" />
    <BitMenuButtonOption Text=""Delete"" Icon=""@BitIconInfo.Fa(""solid trash"")"" />
</BitMenuButton>";

    private readonly string example20RazorCode = @"
<BitMenuButton Text=""Small"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Size=""BitSize.Small"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Small"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Size=""BitSize.Small"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Small"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Size=""BitSize.Small"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Medium"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Size=""BitSize.Medium"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Medium"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Size=""BitSize.Medium"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Medium"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Size=""BitSize.Medium"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Large"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Fill"" Size=""BitSize.Large"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Large"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Outline"" Size=""BitSize.Large"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>
<BitMenuButton Text=""Large"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"" Size=""BitSize.Large"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example21RazorCode = @"
<style>
    .custom-class {
        margin-inline: 1rem;
        border-radius: 1rem;
        border-color: tomato;
        border-width: 0.25rem;
    }

    .custom-class > button {
        color: tomato;
        border-color: tomato;
        background: transparent;
    }

    .custom-class > button:hover {
        background-color: #ff63473b;
    }


    .custom-item {
        color: peachpuff;
        background-color: tomato;
    }


    .custom-button {
        color: deepskyblue;
        background: transparent;
    }

    .custom-opened .custom-button {
        color: cornflowerblue;
    }

    .custom-callout {
        border-radius: 1rem;
        border-color: lightgray;
        backdrop-filter: blur(20px);
        background-color: transparent;
        box-shadow: darkgray 0 0 0.5rem;
    }

    .custom-item-button {
        border-bottom: 1px solid gray;
    }

    .custom-item-button:hover {
        background-color: rgba(255, 255, 255, 0.2);
    }

    .custom-callout li:last-child .custom-item-button {
        border-bottom: none;
    }
</style>


<BitMenuButton Text=""Styled Button"" TItem=""BitMenuButtonOption"" Style=""border-radius: 1rem; margin: 1rem; box-shadow: aqua 0 0 1rem; overflow: hidden;"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Classed Button"" TItem=""BitMenuButtonOption"" Class=""custom-class"" Variant=""BitVariant.Outline"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>


<BitMenuButton Text=""Non-Sticky"" TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"">
    <BitMenuButtonOption Text=""Option A (Default)"" Key=""A"" IconName=""@BitIconName.Emoji"" Style=""color: brown"" />
    <BitMenuButtonOption Text=""Option B (Styled)"" Key=""B"" IconName=""@BitIconName.Emoji"" Style=""color: tomato; border-color: brown; background-color: peachpuff;"" />
    <BitMenuButtonOption Text=""Option C (Classed)"" Key=""C"" IconName=""@BitIconName.Emoji2"" Class=""custom-item"" />
</BitMenuButton>
<BitMenuButton Text=""Sticky"" Sticky TItem=""BitMenuButtonOption"" Variant=""BitVariant.Text"">
    <BitMenuButtonOption Text=""Option A (Default)"" Key=""A"" IconName=""@BitIconName.Emoji"" Style=""color: brown"" />
    <BitMenuButtonOption Text=""Option B (Styled)"" Key=""B"" IconName=""@BitIconName.Emoji"" Style=""color: tomato; border-color: brown; background-color: peachpuff;"" />
    <BitMenuButtonOption Text=""Option C (Classed)"" Key=""C"" IconName=""@BitIconName.Emoji2"" Class=""custom-item"" />
</BitMenuButton>


<BitMenuButton Text=""Classes"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.FormatPainter"" Variant=""BitVariant.Text""
               Classes=""@(new() { OperatorButton = ""custom-button"",
                                  Opened = ""custom-opened"",
                                  Callout = ""custom-callout"",
                                  ItemButton = ""custom-item-button"" })"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>

<BitMenuButton Text=""Styles"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.Brush""
               Styles=""@(new() { Root = ""--button-background: tomato; background: var(--button-background); border-color: var(--button-background); border-radius: 0.25rem;"",
                                 Opened = ""--button-background: orangered;"",
                                 OperatorButton = ""background: var(--button-background);"",
                                 ItemButton = ""background: lightcoral;"",
                                 Callout = ""border-radius: 0.25rem; box-shadow: lightgray 0 0 0.5rem;"" })"">
    <BitMenuButtonOption Text=""Option A"" Key=""A"" />
    <BitMenuButtonOption Text=""Option B"" Key=""B"" IsEnabled=""false"" />
    <BitMenuButtonOption Text=""Option C"" Key=""C"" />
</BitMenuButton>";

    private readonly string example22RazorCode = @"
<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" TItem=""BitMenuButtonOption"" IconName=""@BitIconName.Edit"">
    <BitMenuButtonOption Text=""گزینه الف"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ب"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ج"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>

<BitMenuButton Text=""گزینه ها"" Dir=""BitDir.Rtl"" TItem=""BitMenuButtonOption"" ChevronDownIconName=""@BitIconName.DoubleChevronDown"" Split>
    <BitMenuButtonOption Text=""گزینه الف"" Key=""A"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ب"" Key=""B"" IconName=""@BitIconName.Emoji"" />
    <BitMenuButtonOption Text=""گزینه ج"" Key=""C"" IconName=""@BitIconName.Emoji2"" />
</BitMenuButton>";
}

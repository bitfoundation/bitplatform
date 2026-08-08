namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TagsInput;

public partial class BitTagsInputDemo
{
    private readonly string example1RazorCode = @"
<BitTagsInput Placeholder=""Add tag..."" />

<BitTagsInput Placeholder=""ReadOnly"" ReadOnly DefaultValue=""@(new List<string> { ""Tag 1"", ""Tag 2"" })"" />

<BitTagsInput Placeholder=""Disabled"" IsEnabled=""false"" DefaultValue=""@(new List<string> { ""Tag 1"", ""Tag 2"" })"" />";

    private readonly string example2RazorCode = @"
<BitTagsInput Label=""Tags"" Placeholder=""Add tag..."" />

<BitTagsInput Label=""Tags"" Required Placeholder=""Required"" />

<BitTagsInput Placeholder=""Add tag..."">
    <LabelTemplate>
        <BitStack Horizontal Gap=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
            <BitIcon IconName=""@BitIconName.Tag"" />
            <BitText Typography=""BitTypography.Body1"">Custom label</BitText>
        </BitStack>
    </LabelTemplate>
</BitTagsInput>";

    private readonly string example3RazorCode = @"
<BitTagsInput Label=""Skills""
              Placeholder=""Add a skill...""
              Description=""Press Enter after each skill. Up to 5 of them."" />

<BitTagsInput Label=""Recipients"" Placeholder=""Add an address..."">
    <DescriptionTemplate>
        <BitStack Horizontal Gap=""0.25rem"" VerticalAlign=""BitAlignment.Center"">
            <BitIcon IconName=""@BitIconName.Info"" Style=""font-size:0.75rem;"" />
            <span>Separate the addresses with a comma or a semicolon.</span>
        </BitStack>
    </DescriptionTemplate>
</BitTagsInput>";

    private readonly string example4RazorCode = @"
<BitTagsInput Label=""Placeholder only""
              Placeholder=""Type a tag and press Enter"" />

<BitTagsInput Label=""Placeholder & TagsPlaceholder""
              Placeholder=""Type a tag and press Enter""
              TagsPlaceholder=""add another...""
              DefaultValue=""@(new List<string> { ""blazor"" })"" />";

    private readonly string example5RazorCode = @"
<BitTagsInput Label=""Comma or semicolon""
              Separators=""@(["","", "";""])""
              Placeholder=""a, b; c""
              Description=""Try pasting: red,green;blue"" />

<BitTagsInput Label=""Space separated""
              Separators=""@(["" ""])""
              Placeholder=""Type words separated by spaces"" />";

    private readonly string example6RazorCode = @"
<BitTagsInput Label=""Free text with suggestions""
              Suggestions=""frameworkSuggestions""
              Placeholder=""Start typing: bl, re, vu...""
              Description=""Type a letter or two to see the list."" />

<BitTagsInput Label=""Suggestions as the only accepted values""
              Suggestions=""frameworkSuggestions""
              Validator=""ValidateFramework""
              Placeholder=""Only a suggested value is accepted""
              Description=""Anything that is not one of the suggestions is refused."" />";
    private readonly string example6CsharpCode = @"
private readonly string[] frameworkSuggestions = [""blazor"", ""react"", ""vue"", ""angular"", ""svelte""];

private static bool ValidateFramework(string tag)
{
    return tag is ""blazor"" or ""react"" or ""vue"" or ""angular"";
}";

    private readonly string example7RazorCode = @"
<BitTagsInput Label=""Up to 3 tags""
              MaxTags=""3""
              Separators=""@(["",""])""
              Placeholder=""Add up to 3 tags""
              @bind-Value=""maxTagsValue""
              OnInvalid=""HandleMaxTagsInvalid"" />

<div>Tags: @(maxTagsValue is null ? 0 : maxTagsValue.Count) / 3</div>
@if (maxTagsMessage.HasValue())
{
    <div class=""invalid-message"">@maxTagsMessage</div>
}";
    private readonly string example7CsharpCode = @"
private ICollection<string>? maxTagsValue = [""blazor""];
private string? maxTagsMessage;

private void HandleMaxTagsInvalid(BitTagsInputInvalidArgs args)
{
    maxTagsMessage = args.Reason == BitTagsInputInvalidReason.MaxTags
        ? $""'{args.Tag}' was refused: no more than 3 tags.""
        : $""'{args.Tag}' was refused ({args.Reason})."";
}";

    private readonly string example8RazorCode = @"
<BitTagsInput Label=""MaxLength = 10""
              MaxLength=""10""
              Placeholder=""Max 10 characters per tag""
              Description=""Typing beyond the tenth character does nothing."" />

<BitTagsInput Label=""MinLength = 3""
              MinLength=""3""
              Placeholder=""At least 3 characters per tag""
              Description=""Shorter entries are refused."" />";

    private readonly string example9RazorCode = @"
<BitTagsInput Label=""Email addresses""
              Placeholder=""name@example.com""
              Separators=""@(["","", "";"", "" ""])""
              Pattern=""@emailPattern""
              Description=""Only well formed addresses are accepted.""
              OnInvalid=""HandlePatternInvalid"" />
@if (patternMessage.HasValue())
{
    <div class=""invalid-message"">@patternMessage</div>
}

<BitTagsInput Label=""Known frameworks""
              Placeholder=""blazor, react, vue, angular""
              Validator=""ValidateFramework""
              Description=""Only blazor, react, vue and angular are allowed.""
              OnInvalid=""HandleValidatorInvalid"" />
@if (validatorMessage.HasValue())
{
    <div class=""invalid-message"">@validatorMessage</div>
}";
    private readonly string example9CsharpCode = @"
private const string emailPattern = @""^[^@\s]+@[^@\s]+\.[^@\s]+$"";
private string? patternMessage;
private string? validatorMessage;

private void HandlePatternInvalid(BitTagsInputInvalidArgs args)
{
    patternMessage = $""'{args.Tag}' is not a valid email address."";
}

private static bool ValidateFramework(string tag)
{
    return tag is ""blazor"" or ""react"" or ""vue"" or ""angular"";
}

private void HandleValidatorInvalid(BitTagsInputInvalidArgs args)
{
    validatorMessage = $""'{args.Tag}' is not one of the known frameworks."";
}";

    private readonly string example10RazorCode = @"
<BitTagsInput Label=""Hashtags""
              Transformer=""NormalizeHashtag""
              Placeholder=""#Blazor, #WEB, # dot net""
              Separators=""@(["",""])""
              Description=""Lower cased, stripped of a leading # and of the whitespace inside."" />";
    private readonly string example10CsharpCode = @"
private static string NormalizeHashtag(string tag)
{
    return string.Concat(tag.TrimStart('#').Where(c => char.IsWhiteSpace(c) is false)).ToLowerInvariant();
}";

    private readonly string example11RazorCode = @"
<BitTagsInput Label=""Duplicates refused (default)""
              Placeholder=""Try adding the same tag twice""
              DefaultValue=""@(new List<string> { ""blazor"" })""
              OnTagExists=""HandleTagExists"" />
@if (duplicateMessage.HasValue())
{
    <div class=""invalid-message"">@duplicateMessage</div>
}

<BitTagsInput Label=""Case insensitive""
              Comparison=""StringComparison.OrdinalIgnoreCase""
              Placeholder=""Try 'BLAZOR'""
              DefaultValue=""@(new List<string> { ""blazor"" })"" />

<BitTagsInput Label=""Duplicates allowed""
              Duplicates
              Placeholder=""Add the same tag as often as you like"" />";
    private readonly string example11CsharpCode = @"
private string? duplicateMessage;

private void HandleTagExists(string tag)
{
    duplicateMessage = $""'{tag}' is already in the list."";
}";

    private readonly string example12RazorCode = @"
<BitTagsInput Label=""Double click a tag to correct it""
              EditableTags
              MinLength=""2""
              Placeholder=""Add tag...""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"", ""web"" })""
              OnEdit=""HandleEdit"" />
@if (editMessage.HasValue())
{
    <div>@editMessage</div>
}";
    private readonly string example12CsharpCode = @"
private string? editMessage;

private void HandleEdit(BitTagsInputEditArgs args)
{
    editMessage = $""'{args.Tag}' became '{args.NewTag}'."";
}";

    private readonly string example13RazorCode = @"
<BitTagsInput Label=""Clearable""
              ShowClearButton
              Placeholder=""Add a few tags, then clear them""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"", ""web"" })""
              OnClear=""HandleClear"" />
@if (clearedMessage.HasValue())
{
    <div>@clearedMessage</div>
}

@code {
    private string? clearedMessage;

    private void HandleClear(IReadOnlyList<string> tags)
    {
        clearedMessage = $""Cleared {tags.Count} tag(s)."";
    }
}";

    private readonly string example14RazorCode = @"
<BitTagsInput Label=""Plain count""
              ShowCounter
              Placeholder=""Add tag...""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Count against a ceiling""
              ShowCounter
              MaxTags=""5""
              Placeholder=""Add up to 5 tags""
              Description=""Press Enter after each tag.""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />";

    private readonly string example15RazorCode = @"
<BitTagsInput Label=""Full keyboard support""
              Placeholder=""Add a few tags, then walk them with the arrow keys""
              DefaultValue=""@(new List<string> { ""one"", ""two"", ""three"" })"" />

<BitTagsInput Label=""NoAddOnBlur & NoAddOnTab & NoBackspaceRemove""
              NoAddOnBlur
              NoAddOnTab
              NoBackspaceRemove
              Placeholder=""Only Enter adds a tag here""
              DefaultValue=""@(new List<string> { ""one"", ""two"" })"" />";

    private readonly string example16RazorCode = @"
<BitTagsInput Label=""Alt + arrows move the focused tag""
              AllowReorder
              Placeholder=""Add tag...""
              Description=""Focus a tag with Tab, then hold Alt and press the arrow keys.""
              DefaultValue=""@(new List<string> { ""first"", ""second"", ""third"", ""fourth"" })"" />";

    private readonly string example17RazorCode = @"
<BitTagsInput Placeholder=""Add tag..."" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"">
    <TagTemplate Context=""tag"">
        <BitIcon IconName=""@BitIconName.Tag"" Style=""font-size: 0.75rem;"" />
        <span style=""font-weight: 600;"">@tag</span>
    </TagTemplate>
</BitTagsInput>";

    private readonly string example18RazorCode = @"
<BitTagsInput Label=""Two-way bound"" Placeholder=""Add tag..."" @bind-Value=""boundTags"" />
<div>Tags: @(boundTags is not null ? string.Join("", "", boundTags) : ""null"")</div>

<BitTagsInput Label=""Uncontrolled (DefaultValue + OnChange)""
              Placeholder=""Add tag...""
              DefaultValue=""@(new List<string> { ""blazor"" })""
              OnChange=""v => changedTags = v"" />
<div>Tags: @(changedTags is not null ? string.Join("", "", changedTags) : ""null"")</div>";
    private readonly string example18CsharpCode = @"
private ICollection<string>? boundTags;
private ICollection<string>? changedTags;";

    private readonly string example19RazorCode = @"
<BitTagsInput Label=""Try adding 'block'""
              Placeholder=""Type 'block' to see OnBeforeAdd cancel the add""
              OnBeforeAdd=""HandleBeforeAdd""
              OnBeforeRemove=""HandleBeforeRemove""
              OnAdd=""HandleAdd""
              OnRemove=""HandleRemove""
              OnInvalid=""HandleInvalid""
              OnInput=""text => typedText = text"" />

<div>Typing: @typedText</div>
<div>Last event: @eventsLog</div>";
    private readonly string example19CsharpCode = @"
private string? typedText;
private string? eventsLog;

private void HandleBeforeAdd(BitTagsInputBeforeArgs args)
{
    if (args.Tag.Equals(""block"", StringComparison.OrdinalIgnoreCase))
    {
        args.Cancel = true;
        eventsLog = $""Adding '{args.Tag}' was cancelled by OnBeforeAdd."";
    }
}

private void HandleBeforeRemove(BitTagsInputBeforeArgs args)
{
    eventsLog = $""Removing '{args.Tag}'..."";
}

private void HandleAdd(IReadOnlyList<string> tags)
{
    eventsLog = $""Added: {string.Join("", "", tags)}"";
}

private void HandleRemove(string tag)
{
    eventsLog = $""Removed: {tag}"";
}

private void HandleInvalid(BitTagsInputInvalidArgs args)
{
    eventsLog = $""Rejected '{args.Tag}' ({args.Reason})"";
}";

    private readonly string example20RazorCode = @"
<BitTagsInput @ref=""apiTagsInput""
              Label=""Driven from the outside""
              MaxTags=""6""
              Placeholder=""Add tag...""
              DefaultValue=""@(new List<string> { ""blazor"" })"" />

<BitStack Horizontal Wrap Gap=""0.5rem"">
    <BitButton OnClick=""ApiAddTag"">Add ""dotnet""</BitButton>
    <BitButton OnClick=""ApiAddTags"">Add ""web"" & ""ui""</BitButton>
    <BitButton OnClick=""ApiRemoveTag"">Remove ""dotnet""</BitButton>
    <BitButton OnClick=""ApiRemoveFirst"">Remove first</BitButton>
    <BitButton OnClick=""ApiClear"">Clear</BitButton>
    <BitButton OnClick=""ApiFocus"">Focus</BitButton>
</BitStack>";
    private readonly string example20CsharpCode = @"
private BitTagsInput apiTagsInput = default!;

private Task ApiAddTag() => apiTagsInput.AddTagAsync(""dotnet"");

private Task ApiAddTags() => apiTagsInput.AddTagsAsync([""web"", ""ui""]);

private Task ApiRemoveTag() => apiTagsInput.RemoveTagAsync(""dotnet"");

private Task ApiRemoveFirst() => apiTagsInput.RemoveTagAtAsync(0);

private Task ApiClear() => apiTagsInput.Clear();

private async Task ApiFocus() => await apiTagsInput.FocusAsync();";

    private readonly string example21RazorCode = @"
<EditForm Model=""cancelModel"" OnValidSubmit=""() => cancelFormSubmitted = true"">
    <DataAnnotationsValidator />
    <BitTagsInput Label=""Tags""
                  Name=""tags""
                  Placeholder=""Add tags, then press Enter on the empty input""
                  @bind-Value=""cancelModel.Tags""
                  CancelConfirmKeysOnEmpty />
    <br />
    <div>Form submitted: @cancelFormSubmitted</div>
</EditForm>";
    private readonly string example21CsharpCode = @"
private bool cancelFormSubmitted;
private readonly ValidationTagsInputModel cancelModel = new();";

    private readonly string example22RazorCode = @"
<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />
    <BitTagsInput Label=""Tags""
                  Required
                  Placeholder=""Add at least one tag...""
                  Description=""At least one tag is required.""
                  @bind-Value=""validationModel.Tags"" />
    <ValidationMessage For=""() => validationModel.Tags"" />
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example22CsharpCode = @"
private readonly ValidationTagsInputModel validationModel = new();

private void HandleValidSubmit() { }

public class ValidationTagsInputModel
{
    [Required(ErrorMessage = ""At least one tag is required."")]
    [MinLength(1, ErrorMessage = ""At least one tag is required."")]
    public ICollection<string>? Tags { get; set; }
}";

    private readonly string example23RazorCode = @"
<BitTagsInput Label=""Fill"" Variant=""BitVariant.Fill"" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Outline"" Variant=""BitVariant.Outline"" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Text"" Variant=""BitVariant.Text"" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""NoBorder"" NoBorder DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />";

    private readonly string example24RazorCode = @"
<BitTagsInput Label=""Primary"" Accent=""BitColor.Primary"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Secondary"" Accent=""BitColor.Secondary"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Tertiary"" Accent=""BitColor.Tertiary"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Info"" Accent=""BitColor.Info"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Success"" Accent=""BitColor.Success"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Warning"" Accent=""BitColor.Warning"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""SevereWarning"" Accent=""BitColor.SevereWarning"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Error"" Accent=""BitColor.Error"" DefaultValue=""@(new List<string> { ""tag"" })"" />";

    private readonly string example25RazorCode = @"
<BitTagsInput Label=""Built-in icon names""
              ShowClearButton
              DismissIconName=""@BitIconName.ChromeClose""
              ClearButtonIconName=""@BitIconName.Delete""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""FontAwesome""
              ShowClearButton
              DismissIcon=""@BitIconInfo.Fa(""solid xmark"")""
              ClearButtonIcon=""@BitIconInfo.Fa(""solid trash"")""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Bootstrap Icons""
              ShowClearButton
              DismissIcon=""@BitIconInfo.Bi(""x-lg"")""
              ClearButtonIcon=""@BitIconInfo.Bi(""trash"")""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />";

    private readonly string example26RazorCode = @"
<BitTagsInput Label=""Small"" Size=""BitSize.Small"" ShowClearButton DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Medium"" Size=""BitSize.Medium"" ShowClearButton DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Large"" Size=""BitSize.Large"" ShowClearButton DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />";

    private readonly string example27RazorCode = @"
<BitTagsInput Style=""box-shadow: aqua 0 0 0.5rem;"" DefaultValue=""@(new List<string> { ""blazor"" })"" />

<BitTagsInput Class=""custom-class"" DefaultValue=""@(new List<string> { ""blazor"" })"" />

<BitTagsInput Label=""Styles""
              ShowClearButton
              Description=""Every part of the component has a slot of its own.""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })""
              Styles=""@(new() { Label = ""color: blueviolet; letter-spacing: 0.1rem;"",
                                Description = ""color: blueviolet;"",
                                InputContainer = ""border-color: blueviolet;"",
                                Tag = ""background: blueviolet; color: white; border-radius: 1rem;"",
                                FocusedTag = ""background: darkviolet;"",
                                Input = ""color: blueviolet;"",
                                ClearButton = ""color: blueviolet;"" })"" />

<BitTagsInput Label=""Classes""
              ShowClearButton
              Description=""Every part of the component has a slot of its own.""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })""
              Classes=""@(new() { Root = ""custom-root"",
                                 Label = ""custom-label"",
                                 Description = ""custom-description"",
                                 InputContainer = ""custom-container"",
                                 Tag = ""custom-tag"",
                                 DismissButton = ""custom-dismiss"",
                                 Input = ""custom-input"",
                                 ClearButton = ""custom-clear"" })"" />";

    private readonly string example28RazorCode = @"
<div dir=""rtl"">
    <BitTagsInput Dir=""BitDir.Rtl""
                  Label=""برچسب‌ها""
                  Placeholder=""برچسب جدید...""
                  Description=""با کلید Enter برچسب را ثبت کنید.""
                  DefaultValue=""@(new List<string> { ""بلیزر"", ""دات‌نت"" })"" />

    <BitTagsInput Dir=""BitDir.Rtl""
                  Label=""با دکمه پاک کردن""
                  ShowClearButton
                  Separators=""@([""،""])""
                  Placeholder=""برچسب‌ها را با ویرگول جدا کنید""
                  DefaultValue=""@(new List<string> { ""بلیزر"", ""دات‌نت"" })"" />
</div>";
}

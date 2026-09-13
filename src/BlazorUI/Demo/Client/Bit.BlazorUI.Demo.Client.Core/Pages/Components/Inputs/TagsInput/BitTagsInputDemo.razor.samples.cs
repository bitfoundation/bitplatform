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
<BitTagsInput Label=""Fill"" Variant=""BitVariant.Fill"" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Outline"" Variant=""BitVariant.Outline"" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Text"" Variant=""BitVariant.Text"" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""NoBorder"" NoBorder DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />";

    private readonly string example6RazorCode = @"
<BitTagsInput Label=""Fill"" TagVariant=""BitVariant.Fill"" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />
<BitTagsInput Label=""Outline"" TagVariant=""BitVariant.Outline"" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />
<BitTagsInput Label=""Text"" TagVariant=""BitVariant.Text"" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />";

    private readonly string example7RazorCode = @"
<BitTagsInput Label=""Comma or semicolon""
              Separators=""@(["","", "";""])""
              Placeholder=""a, b; c""
              Description=""Try pasting: red,green;blue - or a column copied out of a spreadsheet."" />

<BitTagsInput Label=""Space separated""
              Separators=""@(["" ""])""
              Placeholder=""Type words separated by spaces"" />";

    private readonly string example8RazorCode = @"
<BitTagsInput Label=""Free text with suggestions""
              Suggestions=""frameworkSuggestions""
              Placeholder=""Start typing: bl, re, vu...""
              Description=""Type a letter or two to see the list."" />

<BitTagsInput Label=""Suggestions as the only accepted values""
              Suggestions=""frameworkSuggestions""
              RestrictToSuggestions
              Placeholder=""Only a suggested value is accepted""
              Description=""Anything that is not one of the suggestions is refused.""
              OnInvalid=""HandleSuggestionInvalid"" />
@if (suggestionMessage.HasValue())
{
    <div class=""invalid-message"">@suggestionMessage</div>
}

<BitTagsInput Label=""A long catalogue, 5 offers at a time""
              Suggestions=""countrySuggestions""
              MaxSuggestions=""5""
              Placeholder=""Start typing a country...""
              Description=""Only the five best matches are ever written into the page."" />";
    private readonly string example8CsharpCode = @"
private readonly string[] frameworkSuggestions = [""blazor"", ""react"", ""vue"", ""angular"", ""svelte""];
private readonly string[] countrySuggestions = [""Argentina"", ""Australia"", ""Austria"", ""Belgium"", ""Brazil"",
                                                ""Canada"", ""Chile"", ""China"", ""Denmark"", ""Egypt"", ""Finland"",
                                                ""France"", ""Germany"", ""Greece"", ""India"", ""Indonesia"",
                                                ""Iran"", ""Ireland"", ""Italy"", ""Japan"", ""Mexico"", ""Morocco"",
                                                ""Netherlands"", ""New Zealand"", ""Norway"", ""Poland"", ""Portugal"",
                                                ""Spain"", ""Sweden"", ""Switzerland"", ""Turkey"", ""Ukraine""];
private string? suggestionMessage;

private void HandleSuggestionInvalid(BitTagsInputInvalidArgs args)
{
    suggestionMessage = args.Reason == BitTagsInputInvalidReason.NotSuggested
        ? $""'{args.Tag}' is not one of the suggested values.""
        : $""'{args.Tag}' was refused ({args.Reason})."";
}";

    private readonly string example9RazorCode = @"
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
    private readonly string example9CsharpCode = @"
private ICollection<string>? maxTagsValue = [""blazor""];
private string? maxTagsMessage;

private void HandleMaxTagsInvalid(BitTagsInputInvalidArgs args)
{
    maxTagsMessage = args.Reason == BitTagsInputInvalidReason.MaxTags
        ? $""'{args.Tag}' was refused: no more than 3 tags.""
        : $""'{args.Tag}' was refused ({args.Reason})."";
}";

    private readonly string example10RazorCode = @"
<BitTagsInput Label=""MaxLength = 10""
              MaxLength=""10""
              Placeholder=""Max 10 characters per tag""
              Description=""Typing beyond the tenth character does nothing."" />

<BitTagsInput Label=""MinLength = 3""
              MinLength=""3""
              Placeholder=""At least 3 characters per tag""
              Description=""Shorter entries are refused."" />";

    private readonly string example11RazorCode = @"
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
    private readonly string example11CsharpCode = @"
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

    private readonly string example12RazorCode = @"
<BitTagsInput Label=""Hashtags""
              Transformer=""NormalizeHashtag""
              Placeholder=""#Blazor, #WEB, # dot net""
              Separators=""@(["",""])""
              Description=""Lower cased, stripped of a leading # and of the whitespace inside."" />";
    private readonly string example12CsharpCode = @"
private static string NormalizeHashtag(string tag)
{
    return string.Concat(tag.TrimStart('#').Where(c => char.IsWhiteSpace(c) is false)).ToLowerInvariant();
}";

    private readonly string example13RazorCode = @"
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
    private readonly string example13CsharpCode = @"
private string? duplicateMessage;

private void HandleTagExists(string tag)
{
    duplicateMessage = $""'{tag}' is already in the list."";
}";

    private readonly string example14RazorCode = @"
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
    private readonly string example14CsharpCode = @"
private string? editMessage;

private void HandleEdit(BitTagsInputEditArgs args)
{
    editMessage = $""'{args.Tag}' became '{args.NewTag}'."";
}";

    private readonly string example15RazorCode = @"
<BitTagsInput Label=""Clearable""
              ShowClearButton
              Placeholder=""Add a few tags, then clear them""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"", ""web"" })""
              OnClear=""HandleClear"" />
@if (clearedMessage.HasValue())
{
    <div>@clearedMessage</div>
}

<BitTagsInput Label=""Guarded by OnBeforeClear""
              ShowClearButton
              Placeholder=""Add tag...""
              Description=""Clearing is refused while there are more than two tags.""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"", ""web"" })""
              OnBeforeClear=""HandleBeforeClear"" />
@if (beforeClearMessage.HasValue())
{
    <div>@beforeClearMessage</div>
}";
    private readonly string example15CsharpCode = @"
private string? clearedMessage;
private string? beforeClearMessage;

private void HandleClear(IReadOnlyList<string> tags)
{
    clearedMessage = $""Cleared {tags.Count} tag(s)."";
}

private void HandleBeforeClear(BitTagsInputClearArgs args)
{
    if (args.Tags.Count > 2)
    {
        args.Cancel = true;
        beforeClearMessage = $""Clearing {args.Tags.Count} tags was refused. Remove a few of them first."";
    }
    else
    {
        beforeClearMessage = $""Cleared {args.Tags.Count} tag(s)."";
    }
}";

    private readonly string example16RazorCode = @"
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

    private readonly string example17RazorCode = @"
<BitTagsInput Label=""Full keyboard support""
              Placeholder=""Add a few tags, then walk them with the arrow keys""
              DefaultValue=""@(new List<string> { ""one"", ""two"", ""three"" })"" />

<BitTagsInput Label=""NoAddOnBlur & NoAddOnTab & NoBackspaceRemove""
              NoAddOnBlur
              NoAddOnTab
              NoBackspaceRemove
              Placeholder=""Only Enter adds a tag here""
              DefaultValue=""@(new List<string> { ""one"", ""two"" })"" />

<BitTagsInput Label=""BackspaceEditsLastTag & ClearOnBlur""
              BackspaceEditsLastTag
              ClearOnBlur
              NoAddOnBlur
              Placeholder=""Add tag...""
              Description=""Backspace on the empty input takes the last tag back for correction; leaving the field throws away what was still being typed.""
              DefaultValue=""@(new List<string> { ""one"", ""two"" })"" />";

    private readonly string example18RazorCode = @"
<BitTagsInput Label=""Drag a tag, or move it with Alt + arrows""
              AllowReorder
              Placeholder=""Add tag...""
              Description=""Drag a chip onto another one, or focus one with Tab and hold Alt while pressing the arrow keys.""
              DefaultValue=""@(new List<string> { ""first"", ""second"", ""third"", ""fourth"" })""
              OnReorder=""HandleReorder"" />

@if (reorderMessage.HasValue())
{
    <div>@reorderMessage</div>
}";
    private readonly string example18CsharpCode = @"
private string? reorderMessage;

private void HandleReorder(BitTagsInputReorderArgs args)
{
    reorderMessage = $""'{args.Tag}' moved from position {args.OldIndex + 1} to {args.NewIndex + 1}."";
}";

    private readonly string example19RazorCode = @"
<BitTagsInput Placeholder=""Add tag..."" DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"">
    <TagTemplate Context=""tag"">
        <BitIcon IconName=""@BitIconName.Tag"" Style=""font-size: 0.75rem;"" />
        <span style=""font-weight: 600;"">@tag</span>
    </TagTemplate>
</BitTagsInput>";

    private readonly string example20RazorCode = @"
<BitTagsInput Label=""Two-way bound"" Placeholder=""Add tag..."" @bind-Value=""boundTags"" />
<div>Tags: @(boundTags is not null ? string.Join("", "", boundTags) : ""null"")</div>

<BitTagsInput Label=""Uncontrolled (DefaultValue + OnChange)""
              Placeholder=""Add tag...""
              DefaultValue=""@(new List<string> { ""blazor"" })""
              OnChange=""v => changedTags = v"" />
<div>Tags: @(changedTags is not null ? string.Join("", "", changedTags) : ""null"")</div>";
    private readonly string example20CsharpCode = @"
private ICollection<string>? boundTags;
private ICollection<string>? changedTags;";

    private readonly string example21RazorCode = @"
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
    private readonly string example21CsharpCode = @"
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

    private readonly string example22RazorCode = @"
<BitTagsInput @ref=""apiTagsInput""
              Label=""Driven from the outside""
              MaxTags=""6""
              EditableTags
              Placeholder=""Add tag...""
              DefaultValue=""@(new List<string> { ""blazor"" })"" />

<BitStack Horizontal Wrap Gap=""0.5rem"">
    <BitButton OnClick=""ApiAddTag"">Add ""dotnet""</BitButton>
    <BitButton OnClick=""ApiAddTags"">Add ""web"" & ""ui""</BitButton>
    <BitButton OnClick=""ApiRemoveTag"">Remove ""dotnet""</BitButton>
    <BitButton OnClick=""ApiRemoveFirst"">Remove first</BitButton>
    <BitButton OnClick=""ApiMoveFirstToEnd"">Move first to the end</BitButton>
    <BitButton OnClick=""ApiEditFirst"">Edit first</BitButton>
    <BitButton OnClick=""ApiSetInputText"">Type ""razor"" into the input</BitButton>
    <BitButton OnClick=""ApiClear"">Clear</BitButton>
    <BitButton OnClick=""ApiFocus"">Focus</BitButton>
</BitStack>";
    private readonly string example22CsharpCode = @"
private BitTagsInput apiTagsInput = default!;

private Task ApiAddTag() => apiTagsInput.AddTagAsync(""dotnet"");

private Task ApiAddTags() => apiTagsInput.AddTagsAsync([""web"", ""ui""]);

private Task ApiRemoveTag() => apiTagsInput.RemoveTagAsync(""dotnet"");

private Task ApiRemoveFirst() => apiTagsInput.RemoveTagAtAsync(0);

private Task ApiMoveFirstToEnd() => apiTagsInput.MoveTagAsync(0, (apiTagsInput.Value?.Count ?? 1) - 1);

private Task ApiEditFirst() => apiTagsInput.EditTagAsync(0);

private Task ApiSetInputText() => apiTagsInput.SetInputTextAsync(""razor"");

private Task ApiClear() => apiTagsInput.Clear();

private async Task ApiFocus() => await apiTagsInput.FocusAsync();";

    private readonly string example23RazorCode = @"
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
    private readonly string example23CsharpCode = @"
private bool cancelFormSubmitted;
private readonly ValidationTagsInputModel cancelModel = new();";

    private readonly string example24RazorCode = @"
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
    private readonly string example24CsharpCode = @"
private readonly ValidationTagsInputModel validationModel = new();

private void HandleValidSubmit() { }

public class ValidationTagsInputModel
{
    [Required(ErrorMessage = ""At least one tag is required."")]
    [MinLength(1, ErrorMessage = ""At least one tag is required."")]
    public ICollection<string>? Tags { get; set; }
}";

    private readonly string example25RazorCode = @"
<BitTagsInput Label=""Three at a time""
              MaxDisplayedTags=""3""
              ShowCounter
              Placeholder=""Add tag...""
              Description=""The rest of the tags are one click away.""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"", ""web"", ""ui"", ""wasm"", ""razor"" })"" />

<BitTagsInput Label=""Custom labels""
              MaxDisplayedTags=""2""
              MoreTagsFormat=""{0} more...""
              LessTagsText=""Fold back""
              Placeholder=""Add tag...""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"", ""web"", ""ui"" })"" />";

    private readonly string example26RazorCode = @"
<BitTagsInput Label=""Announcements of your own""
              EditableTags
              AllowReorder
              ShowClearButton
              Placeholder=""Add tag...""
              AddedAnnouncementFormat=""{0} was added to the list.""
              AddedManyAnnouncementFormat=""{0} tags were added to the list.""
              RemovedAnnouncementFormat=""{0} was taken off the list.""
              ClearedAnnouncementFormat=""The list of {0} tags was emptied.""
              EditedAnnouncementFormat=""{0} is the new text of the tag.""
              MovedAnnouncementFormat=""{0} is now number {1} out of {2}.""
              InvalidAnnouncementFormat=""{0} was refused.""
              MinLength=""3""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Names of your own""
              EditableTags
              ShowClearButton
              Placeholder=""Add tag...""
              TagsAriaLabel=""Selected skills""
              TagAriaDescription=""Press Enter to rename this skill, or Delete to drop it.""
              DismissAriaLabelFormat=""Drop {0}""
              DismissTitle=""Drop""
              EditAriaLabelFormat=""Rename {0}""
              ClearButtonAriaLabel=""Drop every skill""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />";

    private readonly string example27RazorCode = @"
<BitTagsInput Label=""Recipients""
              Prefix=""To:""
              Separators=""@(["","", "";""])""
              Placeholder=""Add an address...""
              DefaultValue=""@(new List<string> { ""ada@example.com"" })"" />

<BitTagsInput Label=""Lengths in centimetres""
              Suffix=""cm""
              Pattern=""^[0-9]+$""
              Placeholder=""Add a number...""
              DefaultValue=""@(new List<string> { ""12"", ""34"" })"" />

<BitTagsInput Label=""Templates""
              ShowClearButton
              Placeholder=""Add tag...""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"">
    <PrefixTemplate>
        <BitIcon IconName=""@BitIconName.Tag"" />
    </PrefixTemplate>
    <SuffixTemplate>
        <BitIcon IconName=""@BitIconName.Filter"" />
    </SuffixTemplate>
</BitTagsInput>";

    private readonly string example28RazorCode = @"
<BitTagsInput Label=""Recipients""
              Separators=""@(["","", "";"", "" ""])""
              Placeholder=""Add an address...""
              Description=""An address that is not well formed is drawn in red.""
              GetTagStyle=""GetRecipientStyle""
              DefaultValue=""@(new List<string> { ""ada@example.com"", ""not-an-address"" })"" />

<BitTagsInput Label=""Priorities""
              Placeholder=""Add: low, medium or high...""
              Description=""Each level carries a color of its own.""
              GetTagClass=""GetPriorityClass""
              DefaultValue=""@(new List<string> { ""low"", ""medium"", ""high"" })"" />";

    private readonly string example28CsharpCode = @"
private static string? GetRecipientStyle(string tag)
{
    return Regex.IsMatch(tag, @""^[^@\s]+@[^@\s]+\.[^@\s]+$"")
            ? null
            : ""background: #fde7e9; color: #a4262c; border-color: #a4262c;"";
}

private static string? GetPriorityClass(string tag) => tag.ToLowerInvariant() switch
{
    ""high"" => ""priority-high"",
    ""medium"" => ""priority-medium"",
    ""low"" => ""priority-low"",
    _ => null
};";

    private readonly string example29RazorCode = @"
<BitTagsInput Label=""Primary"" Color=""BitColor.Primary"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Secondary"" Color=""BitColor.Secondary"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Tertiary"" Color=""BitColor.Tertiary"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Info"" Color=""BitColor.Info"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Success"" Color=""BitColor.Success"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Warning"" Color=""BitColor.Warning"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""SevereWarning"" Color=""BitColor.SevereWarning"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""Error"" Color=""BitColor.Error"" DefaultValue=""@(new List<string> { ""tag"" })"" />

<BitTagsInput Label=""PrimaryBackground"" Color=""BitColor.PrimaryBackground"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""SecondaryBackground"" Color=""BitColor.SecondaryBackground"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""TertiaryBackground"" Color=""BitColor.TertiaryBackground"" DefaultValue=""@(new List<string> { ""tag"" })"" />

<BitTagsInput Label=""PrimaryForeground"" Color=""BitColor.PrimaryForeground"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""SecondaryForeground"" Color=""BitColor.SecondaryForeground"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""TertiaryForeground"" Color=""BitColor.TertiaryForeground"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""PrimaryBorder"" Color=""BitColor.PrimaryBorder"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""SecondaryBorder"" Color=""BitColor.SecondaryBorder"" DefaultValue=""@(new List<string> { ""tag"" })"" />
<BitTagsInput Label=""TertiaryBorder"" Color=""BitColor.TertiaryBorder"" DefaultValue=""@(new List<string> { ""tag"" })"" />";

    private readonly string example30RazorCode = @"
<BitTagsInput Label=""Built-in icon names""
              ShowClearButton
              DismissIconName=""@BitIconName.ChromeClose""
              ClearButtonIconName=""@BitIconName.Delete""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />
<BitTagsInput Label=""FontAwesome""
              ShowClearButton
              DismissIcon=""@BitIconInfo.Fa(""solid xmark"")""
              ClearButtonIcon=""@BitIconInfo.Fa(""solid trash"")""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />
<BitTagsInput Label=""Bootstrap Icons""
              ShowClearButton
              DismissIcon=""@BitIconInfo.Bi(""x-lg"")""
              ClearButtonIcon=""@BitIconInfo.Bi(""trash"")""
              DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />";

    private readonly string example31RazorCode = @"
<BitTagsInput Label=""Small"" Size=""BitSize.Small"" ShowClearButton DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Medium"" Size=""BitSize.Medium"" ShowClearButton DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />

<BitTagsInput Label=""Large"" Size=""BitSize.Large"" ShowClearButton DefaultValue=""@(new List<string> { ""blazor"", ""dotnet"" })"" />";

    private readonly string example32RazorCode = @"
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

    private readonly string example33RazorCode = @"
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

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.TagsInput;

public partial class BitTagsInputDemo
{
    private readonly string example1RazorCode = @"
<BitTagsInput Placeholder=""Add tag..."" />
<BitTagsInput Placeholder=""ReadOnly"" ReadOnly Value=""@(new List<string> { ""Tag 1"", ""Tag 2"" })"" />
<BitTagsInput Placeholder=""Disabled"" IsEnabled=""false"" Value=""@(new List<string> { ""Tag 1"", ""Tag 2"" })"" />";

    private readonly string example2RazorCode = @"
<BitTagsInput Label=""Tags"" Placeholder=""Add tag..."" />

<BitTagsInput Placeholder=""Add tag..."">
    <LabelTemplate>
        <BitStack Horizontal Gap=""0.5rem"">
            <BitIcon IconName=""@BitIconName.Tag"" />
            <BitText Typography=""BitTypography.Body1"">Custom label</BitText>
        </BitStack>
    </LabelTemplate>
</BitTagsInput>";

    private readonly string example3RazorCode = @"
<BitTagsInput Placeholder=""NoBorder"" NoBorder />";

    private readonly string example4RazorCode = @"
<BitTagsInput Placeholder=""Add tag..."" @bind-Value=""boundTags"" />
<div>Tags: @(boundTags is not null ? string.Join("", "", boundTags) : ""null"")</div>

<BitTagsInput Placeholder=""Add tag...""
              @bind-Value=""eventTags""
              OnAdd=""tags => addedTag = tags.LastOrDefault()""
              OnRemove=""tag => removedTag = tag"" />
<div>Added: @addedTag</div>
<div>Removed: @removedTag</div>";
    private readonly string example4CsharpCode = @"
private ICollection<string>? boundTags;
private ICollection<string>? eventTags;
private string? addedTag;
private string? removedTag;";

    private readonly string example5RazorCode = @"
<BitTagsInput Placeholder=""Duplicates allowed"" Duplicates />";

    private readonly string example6RazorCode = @"
<BitTagsInput Placeholder=""Max 3 tags"" MaxTags=""3"" />";

    private readonly string example7RazorCode = @"
<BitTagsInput Placeholder=""Use comma or semicolon"" Separators=""@(new[] { "","", "";"" })"" />";

    private readonly string example8RazorCode = @"
<BitTagsInput Placeholder=""Add tag..."">
    <TagTemplate Context=""tag"">
        <BitIcon IconName=""@BitIconName.Tag"" Style=""font-size: 0.75rem;"" />
        <span style=""font-weight: 600;"">@tag</span>
    </TagTemplate>
</BitTagsInput>";

    private readonly string example9RazorCode = @"
<BitTagsInput Placeholder=""Max 10 chars per tag"" MaxLength=""10"" />";

    private readonly string example10RazorCode = @"
<BitTagsInput Placeholder=""Paste comma-separated text"" Separators=""@(new[] { "","" })"" />";

    private readonly string example11RazorCode = @"
<BitTagsInput Placeholder=""Type 'block' to test OnBeforeAdd""
              OnBeforeAdd=""HandleBeforeAdd""
              OnBeforeRemove=""HandleBeforeRemove""
              OnTagExists=""HandleTagExists"" />
<div>Status: @eventsStatus</div>
@if (tagExistsMsg is not null)
{
    <div style=""color: red;"">@tagExistsMsg</div>
}";
    private readonly string example11CsharpCode = @"
private string? eventsStatus;
private string? tagExistsMsg;

private void HandleTagExists(string tag)
{
    tagExistsMsg = $""Tag '{tag}' already exists!"";
}

private void HandleBeforeAdd(BitTagsInputBeforeArgs args)
{
    if (args.Tag.Equals(""block"", StringComparison.OrdinalIgnoreCase))
    {
        args.Cancel = true;
        eventsStatus = $""Adding '{args.Tag}' was blocked by OnBeforeAdd."";
    }
    else
    {
        eventsStatus = $""Tag '{args.Tag}' added."";
    }
    tagExistsMsg = null;
}

private void HandleBeforeRemove(BitTagsInputBeforeArgs args)
{
    eventsStatus = $""Removing '{args.Tag}'."";
    tagExistsMsg = null;
}";

    private readonly string example12RazorCode = @"
<EditForm Model=""cancelModel"" OnValidSubmit=""() => cancelFormSubmitted = true"">
    <DataAnnotationsValidator />
    <BitTagsInput Label=""Tags"" Placeholder=""Add tags, then submit with Enter when empty""
                  @bind-Value=""cancelModel.Tags""
                  CancelConfirmKeysOnEmpty />
    <br />
    <div>Form submitted: @cancelFormSubmitted</div>
</EditForm>";

    private readonly string example13RazorCode = @"
<EditForm Model=""validationModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />
    <BitTagsInput Label=""Tags (required)"" Placeholder=""Add tag..."" @bind-Value=""validationModel.Tags"" />
    <ValidationMessage For=""() => validationModel.Tags"" />
    <br />
    <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
</EditForm>";
    private readonly string example13CsharpCode = @"
private readonly ValidationTagsInputModel validationModel = new();

private void HandleValidSubmit() { }

public class ValidationTagsInputModel
{
    [Required(ErrorMessage = ""At least one tag is required."")]
    [MinLength(1, ErrorMessage = ""At least one tag is required."")]
    public ICollection<string>? Tags { get; set; }
}";

    private readonly string example14RazorCode = @"
<BitTagsInput Placeholder=""Custom styles""
              Styles=""@(new() { Tag = ""background: #0078d4; color: white; border-radius: 12px; padding: 2px 8px;"",
                                DismissButton = ""color: white;"" })"" />

<BitTagsInput Placeholder=""Custom styles""
              Styles=""@(new() { InputContainer = ""border-color: #0078d4;"",
                                Input = ""color: #0078d4;"" })"" />";
}

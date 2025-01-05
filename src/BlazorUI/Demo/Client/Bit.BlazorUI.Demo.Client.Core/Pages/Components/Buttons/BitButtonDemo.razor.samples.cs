namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons;

public partial class BitButtonDemo
{
    private readonly string example1RazorCode = @"
<BitButton>Button</BitButton>";

    private readonly string example2RazorCode = @"
<BitButton>Primary text</BitButton>
<BitButton SecondaryText=""secondary text"" />

<BitButton SecondaryText=""this is the secondary text"">Primary text</BitButton>";

    private readonly string example3RazorCode = @"
<BitButton Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"">Text</BitButton>

<BitButton Variant=""BitVariant.Fill"" SecondaryText=""this is the secondary text"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"" SecondaryText=""this is the secondary text"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"" SecondaryText=""this is the secondary text"">Text</BitButton>


<BitButton Variant=""BitVariant.Fill"" IsEnabled=""false"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"" IsEnabled=""false"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"" IsEnabled=""false"">Text</BitButton>

<BitButton Variant=""BitVariant.Fill"" SecondaryText=""this is the secondary text"" IsEnabled=""false"">Fill</BitButton>
<BitButton Variant=""BitVariant.Outline"" SecondaryText=""this is the secondary text"" IsEnabled=""false"">Outline</BitButton>
<BitButton Variant=""BitVariant.Text"" SecondaryText=""this is the secondary text"" IsEnabled=""false"">Text</BitButton>";

    private readonly string example4RazorCode = @"
<BitButton IconName=""@BitIconName.EmojiNeutral"" Variant=""BitVariant.Fill"" />
<BitButton IconName=""@BitIconName.EmojiNeutral"" Variant=""BitVariant.Outline"" />
<BitButton IconName=""@BitIconName.EmojiNeutral"" Variant=""BitVariant.Text"" />

<BitButton IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Fill"">Start</BitButton>
<BitButton IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Outline"">Start</BitButton>
<BitButton IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Text"">Start</BitButton>

<BitButton IconName=""@BitIconName.Emoji"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Fill"">Start</BitButton>
<BitButton IconName=""@BitIconName.Emoji"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Outline"">Start</BitButton>
<BitButton IconName=""@BitIconName.Emoji"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Text"">Start</BitButton>

<BitButton IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Fill"" ReversedIcon>End</BitButton>
<BitButton IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"" ReversedIcon>End</BitButton>
<BitButton IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"" ReversedIcon>End</BitButton>

<BitButton IconName=""@BitIconName.Emoji2"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Fill"" ReversedIcon>End</BitButton>
<BitButton IconName=""@BitIconName.Emoji2"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Outline"" ReversedIcon>End</BitButton>
<BitButton IconName=""@BitIconName.Emoji2"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Text"" ReversedIcon>End</BitButton>";

    private readonly string example5RazorCode = @"
<BitButton IsLoading=""fillIsLoading"" Variant=""BitVariant.Fill"" Style=""min-width: 11rem"" OnClick=""LoadingFillClick"">
    Click me
</BitButton>

<BitButton IsLoading=""outlineIsLoading"" Variant=""BitVariant.Outline"" Style=""min-width: 11rem"" OnClick=""LoadingOutlineClick"">
    Click me
</BitButton>

<BitButton IsLoading=""textIsLoading"" Variant=""BitVariant.Text"" Style=""min-width: 11rem"" OnClick=""LoadingTextClick"">
    Click me
</BitButton>


<BitButton IsLoading=""fillIsLoading"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Fill"" Style=""min-width: 11rem"" OnClick=""LoadingFillClick"">
    Click me
</BitButton>

<BitButton IsLoading=""outlineIsLoading"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Outline"" Style=""min-width: 11rem"" OnClick=""LoadingOutlineClick"">
    Click me
</BitButton>

<BitButton IsLoading=""textIsLoading"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Text"" Style=""min-width: 11rem"" OnClick=""LoadingTextClick"">
    Click me
</BitButton>


<BitButton OnClick=""AutoLoadingClick"" AutoLoading>Click me</BitButton>
<div>AutoLoading click count: @autoLoadCount</div>

<BitButton OnClick=""AutoLoadingReclick"" AutoLoading Reclickable>Reclickable</BitButton>
<div>Reclickable AutoLoading click count: @reclickableAutoLoadCount</div>";
    private readonly string example5CsharpCode = @"
private bool fillIsLoading;
private bool outlineIsLoading;
private bool textIsLoading;

private async Task LoadingFillClick()
{
    fillIsLoading = true;
    await Task.Delay(3000);
    fillIsLoading = false;
}

private async Task LoadingOutlineClick()
{
    outlineIsLoading = true;
    await Task.Delay(3000);
    outlineIsLoading = false;
}

private async Task LoadingTextClick()
{
    textIsLoading = true;
    await Task.Delay(3000);
    textIsLoading = false;
}

private int autoLoadCount;
private async Task AutoLoadingClick()
{
    autoLoadCount++;
    await Task.Delay(3000);
}

private int reclickableAutoLoadCount;
private TaskCompletionSource clickTsc = new();
private CancellationTokenSource delayCts = new();
private Task AutoLoadingReclick(bool isLoading)
{
    if (isLoading)
    {
        clickTsc.TrySetException(new TaskCanceledException());
        delayCts.Cancel();
    }

    delayCts = new();
    clickTsc = new();

    reclickableAutoLoadCount++;

    _ = Task.Delay(3000, delayCts.Token).ContinueWith(async delayTask =>
    {
        await delayTask;
        clickTsc.TrySetResult();
    });

    return clickTsc.Task;
}";

    private readonly string example6RazorCode = @"
<BitButton IsLoading=""true""
           LoadingLabel=""End...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.End"">
    End
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Start...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.Start"">
    Start
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Bottom...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.Bottom"">
    Bottom
</BitButton>

<BitButton IsLoading=""true""
           LoadingLabel=""Top...""
           Style=""min-width: 6.5rem;""
           Variant=""BitVariant.Outline""
           LoadingLabelPosition=""BitLabelPosition.Top"">
    Top
</BitButton>";

    private readonly string example7RazorCode = @"
<BitButton Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"" Variant=""BitVariant.Outline"">
    Open bitplatform.dev
</BitButton>

<BitButton Href=""https://github.com/bitfoundation/bitplatform"" IconName=""@BitIconName.Globe"" Variant=""BitVariant.Outline"">
    Go to bitplatform GitHub
</BitButton>";

    private readonly string example8RazorCode = @"
<BitButton Rel=""BitLinkRel.NoFollow"" Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"" Variant=""BitVariant.Outline"">
    Open bitplatform.dev with a rel attribute (nofollow)
</BitButton>

<BitButton Rel=""BitLinkRel.NoFollow | BitLinkRel.NoReferrer"" Href=""https://bitplatform.dev"" Target=""_blank"" IconName=""@BitIconName.Globe"" Variant=""BitVariant.Outline"">
    Open bitplatform.dev with a rel attribute (nofollow & noreferrer)
</BitButton>";

    private readonly string example9RazorCode = @"
<BitDropdown Label=""FloatPosition"" Items=""floatPositionList"" @bind-Value=""floatPosition"" FitWidth />
<BitTextField Label=""FloatOffset"" @bind-Value=""floatOffset"" Immediate />

<BitButton Float FloatPosition=""floatPosition"" FloatOffset=""@floatOffset"" IconName=""@BitIconName.Add"" Size=""BitSize.Large"" IconOnly />

<div style=""position: relative; border: 1px gray solid"">
    <BitButton FloatAbsolute FloatPosition=""floatPosition"" FloatOffset=""@floatOffset"" IconName=""@BitIconName.Edit"" IconOnly />
    <div style=""height: 300px; overflow: auto"">
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum. Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan, nibh elit rhoncus mauris, eu semper tellus risus et nisi. Duis felis ipsum, luctus eget ultrices sit amet, scelerisque quis metus.<br />Suspendisse blandit erat ac lobortis pulvinar. Donec nunc leo, tempus sit amet accumsan in, sagittis sed odio. Pellentesque tristique felis sed purus pellentesque, ac dictum ex fringilla. Integer a tincidunt eros, non porttitor turpis. Sed gravida felis massa, in viverra massa aliquam sit amet. Etiam vitae dolor in velit sodales tristique id nec turpis. Proin sit amet urna sollicitudin, malesuada enim et, lacinia mi. Fusce nisl massa, efficitur sit amet elementum convallis, porttitor vel turpis. Fusce congue dui sit amet mollis pulvinar. Suspendisse vulputate leo quis nunc tincidunt, nec dictum risus congue.</p>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum. Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan, nibh elit rhoncus mauris, eu semper tellus risus et nisi. Duis felis ipsum, luctus eget ultrices sit amet, scelerisque quis metus.<br />Suspendisse blandit erat ac lobortis pulvinar. Donec nunc leo, tempus sit amet accumsan in, sagittis sed odio. Pellentesque tristique felis sed purus pellentesque, ac dictum ex fringilla. Integer a tincidunt eros, non porttitor turpis. Sed gravida felis massa, in viverra massa aliquam sit amet. Etiam vitae dolor in velit sodales tristique id nec turpis. Proin sit amet urna sollicitudin, malesuada enim et, lacinia mi. Fusce nisl massa, efficitur sit amet elementum convallis, porttitor vel turpis. Fusce congue dui sit amet mollis pulvinar. Suspendisse vulputate leo quis nunc tincidunt, nec dictum risus congue.</p>
        <p>Lorem ipsum dolor sit amet, consectetur adipiscing elit. Cras eu ligula quis orci accumsan pharetra. Fusce mattis sit amet enim vitae imperdiet. Maecenas hendrerit sapien nisl, quis consectetur mi bibendum vel. Pellentesque vel rhoncus quam, non bibendum arcu. Vivamus euismod tellus non felis finibus, dictum finibus eros elementum. Vivamus a massa sit amet leo volutpat blandit at vel tortor. Praesent posuere, nulla eu tempus accumsan, nibh elit rhoncus mauris, eu semper tellus risus et nisi. Duis felis ipsum, luctus eget ultrices sit amet, scelerisque quis metus.<br />Suspendisse blandit erat ac lobortis pulvinar. Donec nunc leo, tempus sit amet accumsan in, sagittis sed odio. Pellentesque tristique felis sed purus pellentesque, ac dictum ex fringilla. Integer a tincidunt eros, non porttitor turpis. Sed gravida felis massa, in viverra massa aliquam sit amet. Etiam vitae dolor in velit sodales tristique id nec turpis. Proin sit amet urna sollicitudin, malesuada enim et, lacinia mi. Fusce nisl massa, efficitur sit amet elementum convallis, porttitor vel turpis. Fusce congue dui sit amet mollis pulvinar. Suspendisse vulputate leo quis nunc tincidunt, nec dictum risus congue.</p>
    </div>
</div>";
    private readonly string example9CsharpCode = @"
private string? floatOffset;
private BitPosition floatPosition = BitPosition.BottomRight;

private readonly List<BitDropdownItem<BitPosition>> floatPositionList = Enum.GetValues<BitPosition>()
                                                                            .Cast<BitPosition>()
                                                                            .Select(enumValue => new BitDropdownItem<BitPosition>
                                                                            {
                                                                                Value = enumValue,
                                                                                Text = enumValue.ToString()
                                                                            })
                                                                            .ToList();";

    private readonly string example10RazorCode = @"
<EditForm Model=""buttonValidationModel"" OnValidSubmit=""HandleValidSubmit"">
    <DataAnnotationsValidator />
    <BitTextField Label=""Required"" Required @bind-Value=""buttonValidationModel.RequiredText"" />
    <ValidationMessage For=""() => buttonValidationModel.RequiredText"" style=""color:red"" />
    <BitTextField Label=""Nonrequired"" @bind-Value=""buttonValidationModel.NonRequiredText"" />
    <div>
        <BitButton ButtonType=""BitButtonType.Submit"">Submit</BitButton>
        <BitButton ButtonType=""BitButtonType.Reset"">Reset</BitButton>
        <BitButton ButtonType=""BitButtonType.Button"">Button</BitButton>
    </div>
</EditForm>";
    private readonly string example10CsharpCode = @"
public class ButtonValidationModel
{
    [Required]
    public string RequiredText { get; set; } = string.Empty;
    public string? NonRequiredText { get; set; }
}

private ButtonValidationModel buttonValidationModel = new();

private async Task HandleValidSubmit()
{
    await Task.Delay(2000);

    buttonValidationModel = new();

    StateHasChanged();
}";

    private readonly string example11RazorCode = @"
<style>
    .custom-content {
        gap: 0.5rem;
        display: flex;
        align-items: center;
    }
</style>


<BitButton Class=""custom-content"">
    <BitIcon IconName=""@BitIconName.Airplane"" Color=""BitColor.Tertiary"" />
    <span>A primary template</span>
    <BitRippleLoading CustomSize=""20"" Color=""BitColor.Tertiary"" />
</BitButton>

<BitButton Class=""custom-content"" Variant=""BitVariant.Outline"">
    <PrimaryTemplate>Primary text</PrimaryTemplate>
    <SecondaryTemplate>
        <BitIcon IconName=""@BitIconName.Accept"" />
        <span>A secondary template</span>
        <BitRollerLoading CustomSize=""20"" />
    </SecondaryTemplate>
</BitButton>

<BitButton Class=""custom-content"" Variant=""BitVariant.Text"">
    <PrimaryTemplate>
        <BitIcon IconName=""@BitIconName.Airplane"" />
        <span>A primary template</span>
        <BitRippleLoading Size=""20"" />
    </PrimaryTemplate>
    <SecondaryTemplate>
        <BitIcon IconName=""@BitIconName.Accept"" />
        <span>A secondary template</span>
        <BitRollerLoading CustomSize=""20"" />
    </SecondaryTemplate>
</BitButton>

<BitButton IsLoading=""!templateIsLoading"" OnClick=""LoadingTemplateClick"">
    <PrimaryTemplate>Click me</PrimaryTemplate>
    <LoadingTemplate>
        <div style=""display:flex;align-items:center;"">
            <BitEllipsisLoading CustomSize=""32"" Color=""BitColor.Tertiary"" />
            <span>Wait...</span>
        </div>
    </LoadingTemplate>
</BitButton>";
    private readonly string example11CsharpCode = @"
private bool templateIsLoading;

private async Task LoadingTemplateClick()
{
    templateIsLoading = true;
    await Task.Delay(3000);
    templateIsLoading = false;
}";

    private readonly string example12RazorCode = @"
<BitButton OnClick=""() => clickCounter++"">Click me (@clickCounter)</BitButton>";
    private readonly string example12CsharpCode = @"
private int clickCounter;";

    private readonly string example13RazorCode = @"
<BitButton Size=""BitSize.Small"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Size=""BitSize.Small"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Small"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"">Text</BitButton>

<BitButton Size=""BitSize.Small"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Size=""BitSize.Small"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Small"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"">Text</BitButton>


<BitButton Size=""BitSize.Medium"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Size=""BitSize.Medium"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Medium"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"">Text</BitButton>

<BitButton Size=""BitSize.Medium"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Size=""BitSize.Medium"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Medium"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"">Text</BitButton>


<BitButton Size=""BitSize.Large"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Size=""BitSize.Large"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Large"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"">Text</BitButton>

<BitButton Size=""BitSize.Large"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Fill"">Fill</BitButton>
<BitButton Size=""BitSize.Large"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Outline"">Outline</BitButton>
<BitButton Size=""BitSize.Large"" SecondaryText=""this is the secondary text"" IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Text"">Text</BitButton>";

    private readonly string example14RazorCode = @"
<BitButton FullWidth IconName=""@BitIconName.Emoji2"" Variant=""BitVariant.Fill"">Full Width Button</BitButton>";

    private readonly string example15RazorCode = @"
<BitButton FixedColor IconOnly
           Size=""BitSize.Large""
           Variant=""BitVariant.Outline""
           IconName=""@BitIconName.Emoji2""
           Color=""BitColor.TertiaryBackground"" />

<BitButton FixedColor IconOnly
           Size=""BitSize.Large""
           Variant=""BitVariant.Text""
           IconName=""@BitIconName.Emoji2""
           Color=""BitColor.TertiaryBackground"" />";

    private readonly string example16RazorCode = @"
<BitButton Color=""BitColor.Primary"">Primary</BitButton>
<BitButton Color=""BitColor.Primary"" Variant=""BitVariant.Outline"">Primary</BitButton>
<BitButton Color=""BitColor.Primary"" Variant=""BitVariant.Text"">Primary</BitButton>

<BitButton Color=""BitColor.Secondary"">Secondary</BitButton>
<BitButton Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"">Secondary</BitButton>
<BitButton Color=""BitColor.Secondary"" Variant=""BitVariant.Text"">Secondary</BitButton>

<BitButton Color=""BitColor.Tertiary"">Tertiary</BitButton>
<BitButton Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"">Tertiary</BitButton>
<BitButton Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"">Tertiary</BitButton>

<BitButton Color=""BitColor.Info"">Info</BitButton>
<BitButton Color=""BitColor.Info"" Variant=""BitVariant.Outline"">Info</BitButton>
<BitButton Color=""BitColor.Info"" Variant=""BitVariant.Text"">Info</BitButton>

<BitButton Color=""BitColor.Success"">Success</BitButton>
<BitButton Color=""BitColor.Success"" Variant=""BitVariant.Outline"">Success</BitButton>
<BitButton Color=""BitColor.Success"" Variant=""BitVariant.Text"">Success</BitButton>

<BitButton Color=""BitColor.Warning"">Warning</BitButton>
<BitButton Color=""BitColor.Warning"" Variant=""BitVariant.Outline"">Warning</BitButton>
<BitButton Color=""BitColor.Warning"" Variant=""BitVariant.Text"">Warning</BitButton>

<BitButton Color=""BitColor.SevereWarning"">SevereWarning</BitButton>
<BitButton Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"">SevereWarning</BitButton>
<BitButton Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"">SevereWarning</BitButton>

<BitButton Color=""BitColor.Error"">Error</BitButton>
<BitButton Color=""BitColor.Error"" Variant=""BitVariant.Outline"">Error</BitButton>
<BitButton Color=""BitColor.Error"" Variant=""BitVariant.Text"">Error</BitButton>


<BitButton Color=""BitColor.PrimaryBackground"">PrimaryBackground</BitButton>
<BitButton Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Outline"">PrimaryBackground</BitButton>
<BitButton Color=""BitColor.PrimaryBackground"" Variant=""BitVariant.Text"">PrimaryBackground</BitButton>

<BitButton Color=""BitColor.SecondaryBackground"">SecondaryBackground</BitButton>
<BitButton Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Outline"">SecondaryBackground</BitButton>
<BitButton Color=""BitColor.SecondaryBackground"" Variant=""BitVariant.Text"">SecondaryBackground</BitButton>

<BitButton Color=""BitColor.TertiaryBackground"">TertiaryBackground</BitButton>
<BitButton Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Outline"">TertiaryBackground</BitButton>
<BitButton Color=""BitColor.TertiaryBackground"" Variant=""BitVariant.Text"">TertiaryBackground</BitButton>


<BitButton Color=""BitColor.PrimaryForeground"">PrimaryForeground</BitButton>
<BitButton Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Outline"">PrimaryForeground</BitButton>
<BitButton Color=""BitColor.PrimaryForeground"" Variant=""BitVariant.Text"">PrimaryForeground</BitButton>

<BitButton Color=""BitColor.SecondaryForeground"">SecondaryForeground</BitButton>
<BitButton Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Outline"">SecondaryForeground</BitButton>
<BitButton Color=""BitColor.SecondaryForeground"" Variant=""BitVariant.Text"">SecondaryForeground</BitButton>

<BitButton Color=""BitColor.TertiaryForeground"">TertiaryForeground</BitButton>
<BitButton Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Outline"">TertiaryForeground</BitButton>
<BitButton Color=""BitColor.TertiaryForeground"" Variant=""BitVariant.Text"">TertiaryForeground</BitButton>


<BitButton Color=""BitColor.PrimaryBorder"">PrimaryBorder</BitButton>
<BitButton Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"">PrimaryBorder</BitButton>
<BitButton Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Text"">PrimaryBorder</BitButton>

<BitButton Color=""BitColor.SecondaryBorder"">SecondaryBorder</BitButton>
<BitButton Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"">SecondaryBorder</BitButton>
<BitButton Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Text"">SecondaryBorder</BitButton>

<BitButton Color=""BitColor.TertiaryBorder"">TertiaryBorder</BitButton>
<BitButton Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"">TertiaryBorder</BitButton>
<BitButton Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Text"">TertiaryBorder</BitButton>";

    private readonly string example17RazorCode = @"
<style>
    .custom-class {
        border-radius: 1rem;
        border-color: blueviolet;
        transition: background-color 1s;
        background: linear-gradient(90deg, magenta, transparent) blue;
    }

    .custom-class:hover {
        border-color: magenta;
        background-color: magenta;
    }

    .custom-content {
        gap: 0.5rem;
        display: flex;
        align-items: center;
    }

    .custom-root {
        color: aqua;
        min-width: 7.2rem;
        font-weight: bold;
        border-radius: 1rem;
        border-color: aqua;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-root:hover {
        color: black;
        background-color: aqua;
    }

    .custom-container {
        text-shadow: tomato 0 0 0.5rem;
    }

    .custom-spinner {
        border-color: red;
        border-top-color: goldenrod;
    }
</style>


<BitButton Style=""background-color: transparent; border-color: blueviolet; color: blueviolet;""
           SecondaryText=""this is the secondary text""
           Variant=""BitVariant.Outline"">
    Styled Button
</BitButton>

<BitButton Class=""custom-class"">
    Classed Button
</BitButton>


<BitButton IsLoading=""stylesIsLoading""
           LoadingLabel=""Wait...""
           OnClick=""LoadingStylesClick""
           Styles=""@(new() { Root = ""background-color: peachpuff; border-color: peachpuff; min-width: 6rem;"",
                             LoadingLabel = ""color: tomato; font-weight: bold;"",
                             Spinner = ""border-color: tomato; border-top-color: goldenrod;"" })"">
    Click me
</BitButton>

<BitButton IsLoading=""classesIsLoading""
           LoadingLabel=""Sending...""
           OnClick=""LoadingClassesClick""
           Variant=""BitVariant.Outline""
           Classes=""@(new() { Root = ""custom-root"",
                              LoadingContainer = ""custom-container"",
                              Spinner = ""custom-spinner"" })"">
    Click me
</BitButton>";
    private readonly string example17CsharpCode = @"
private bool stylesIsLoading;
private bool classesIsLoading;

private async Task LoadingStylesClick()
{
    stylesIsLoading = true;
    await Task.Delay(3000);
    stylesIsLoading = false;
}

private async Task LoadingClassesClick()
{
    classesIsLoading = true;
    await Task.Delay(3000);
    classesIsLoading = false;
}";

    private readonly string example18RazorCode = @"
<BitButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Fill"">
    دکمه با آیکن
</BitButton>
<BitButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Outline"">
    دکمه با آیکن
</BitButton>
<BitButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Text"">
    دکمه با آیکن
</BitButton>

<BitButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Fill"" SecondaryText=""این متن ثانویه است"">
    دکمه با آیکن
</BitButton>
<BitButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Outline"" SecondaryText=""این متن ثانویه است"">
    دکمه با آیکن
</BitButton>
<BitButton Dir=""BitDir.Rtl"" IconName=""@BitIconName.Emoji"" Variant=""BitVariant.Text"" SecondaryText=""این متن ثانویه است"">
    دکمه با آیکن
</BitButton>

<BitButton IsLoading Dir=""BitDir.Rtl"" LoadingLabel=""در حال بارگذاری"" Variant=""BitVariant.Fill"" />
<BitButton IsLoading Dir=""BitDir.Rtl"" LoadingLabel=""در حال بارگذاری"" Variant=""BitVariant.Outline"" />
<BitButton IsLoading Dir=""BitDir.Rtl"" LoadingLabel=""در حال بارگذاری"" Variant=""BitVariant.Text"" />";
}

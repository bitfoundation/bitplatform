namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Buttons.Button;

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
<BitButton IconName=""@BitIconName.Emoji2"" SecondaryText=""this is the secondary text"" Variant=""BitVariant.Text"" ReversedIcon>End</BitButton>

<BitButton IconUrl=""/images/bit-logo.svg"" SecondaryText=""IconUrl"" Variant=""BitVariant.Fill"">Start</BitButton>
<BitButton IconUrl=""/images/bit-logo.svg"" SecondaryText=""IconUrl"" Variant=""BitVariant.Outline"">Start</BitButton>
<BitButton IconUrl=""/images/bit-logo.svg"" SecondaryText=""IconUrl"" Variant=""BitVariant.Text"">Start</BitButton>

<BitButton IconUrl=""/images/bit-logo.svg"" SecondaryText=""IconUrl"" Variant=""BitVariant.Fill"" ReversedIcon>End</BitButton>
<BitButton IconUrl=""/images/bit-logo.svg"" SecondaryText=""IconUrl"" Variant=""BitVariant.Outline"" ReversedIcon>End</BitButton>
<BitButton IconUrl=""/images/bit-logo.svg"" SecondaryText=""IconUrl"" Variant=""BitVariant.Text"" ReversedIcon>End</BitButton>";

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

<BitButton IconOnly 
           Size=""BitSize.Large"" 
           IconName=""@BitIconName.Add"" 
           OnClick=""ScrollToFloat"" 
           Float 
           FloatPosition=""floatPosition"" 
           FloatOffset=""@floatOffset"" />


<div style=""position: relative; border: 1px gray solid"">
    <BitButton IconOnly 
               IconName=""@BitIconName.Edit"" 
               Draggable
               FloatAbsolute
               FloatPosition=""floatPosition"" 
               FloatOffset=""@floatOffset"" />
    <div style=""height:300px;overflow:auto;padding:0.5rem"">
        Once upon a time, stories wove connections between people, a symphony of voices crafting shared dreams. 
        Each word carried meaning, each pause brought understanding. Placeholder text reminds us of that moment 
        when possibilities are limitless, waiting for content to emerge. The spaces here are open for growth, 
        for ideas that change minds and spark emotions. This is where the journey begins—your words will lead the way.
        <br />
        Every story starts with a blank canvas, a quiet space waiting to be filled with ideas, emotions, and dreams. 
        These placeholder words symbolize the beginning—a moment of possibility where creativity has yet to take shape. 
        Imagine this text as the scaffolding of something remarkable, a foundation upon which connections and 
        inspirations will be built. Soon, these lines will transform into narratives that provoke thought, 
        spark emotion, and resonate with those who encounter them. Until then, they remind us of the beauty 
        in potential—the quiet magic of beginnings, where everything is still to come, and the possibilities 
        are boundless. This space is yours to craft, yours to shape, yours to bring to life.
        <br />
        In the beginning, there is silence—a blank canvas yearning to be filled, a quiet space where creativity waits 
        to awaken. These words are temporary, standing in place of ideas yet to come, a glimpse into the infinite 
        possibilities that lie ahead. Think of this text as a bridge, connecting the empty spaces of now with the 
        vibrant narratives of tomorrow. It whispers of the stories waiting to be told, of the thoughts yet to be 
        shaped into meaning, and the emotions ready to resonate with every reader.
        <br />
        In this space, potential reigns supreme. It is a moment suspended in time, where imagination dances freely and 
        each word has the power to transform into something extraordinary. Here lies the start of something new—an 
        opportunity to craft, inspire, and create. Whether it's a tale of adventure, a reflection of truth, or an 
        idea that sparks change, these lines are yours to fill, to shape, and to make uniquely yours. The journey 
        begins here, in this quiet moment where everything is possible.
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

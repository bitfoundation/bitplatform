namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Progress.Loading;

public partial class BitLoadingDemo
{
    private bool _isPaused;
    private bool _isWorking;

    private async Task StartWork()
    {
        _isWorking = true;
        await Task.Delay(1500);
        _isWorking = false;
    }



    private readonly List<ComponentParameter> componentParameters =
    [
        new()
        {
            Name = "AriaLive",
            Type = "string?",
            DefaultValue = "null",
            Description = "How insistently the live region of the loading component announces itself, rendered as the aria-live attribute of the root element. Falls back to \"polite\".",
        },
        new()
        {
            Name = "Classes",
            Type = "BitLoadingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS classes for different parts of the loading component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Color",
            Type = "BitColor?",
            DefaultValue = "null",
            Description = "The general color of the loading component.",
            LinkType = LinkType.Link,
            Href = "#color-enum",
        },
        new()
        {
            Name = "CustomColor",
            Type = "string?",
            DefaultValue = "null",
            Description = "The custom css color of the loading component. Only applies while Color is left unset.",
        },
        new()
        {
            Name = "CustomSize",
            Type = "int?",
            DefaultValue = "null",
            Description = "The custom size of the loading component in px. Only applies while Size is left unset.",
        },
        new()
        {
            Name = "Delay",
            Type = "int",
            DefaultValue = "0",
            Description = "How long, in milliseconds, the loading component waits before it renders anything at all, so that a quick task never makes it flash up and vanish again. Changing the value opens the window again from the new length.",
        },
        new()
        {
            Name = "Inline",
            Type = "bool",
            DefaultValue = "false",
            Description = "Lays the loading component out as an inline box aligned to the middle of the current line, so it can sit inside a sentence, a button or a table cell.",
        },
        new()
        {
            Name = "Label",
            Type = "string?",
            DefaultValue = "null",
            Description = "The text content of the label of the loading component, which is also what assistive technology announces.",
        },
        new()
        {
            Name = "LabelPosition",
            Type = "BitLabelPosition?",
            DefaultValue = "null",
            Description = "The position of the label of the loading component.",
            LinkType = LinkType.Link,
            Href = "#label-position-enum",
        },
        new()
        {
            Name = "LabelTemplate",
            Type = "RenderFragment?",
            DefaultValue = "null",
            Description = "The custom content of the label of the loading component.",
        },
        new()
        {
            Name = "Paused",
            Type = "bool",
            DefaultValue = "false",
            Description = "Holds the animation of the loading component at the frame it had reached instead of running it. The drawing keeps its shape and its place in the layout, so only the movement stops.",
        },
        new()
        {
            Name = "Role",
            Type = "string?",
            DefaultValue = "null",
            Description = "The ARIA role of the root element of the loading component. Falls back to \"status\", which makes the root a live region.",
        },
        new()
        {
            Name = "Size",
            Type = "BitSize?",
            DefaultValue = "null",
            Description = "The Size of the loading component.",
            LinkType = LinkType.Link,
            Href = "#size-enum",
        },
        new()
        {
            Name = "Speed",
            Type = "double?",
            DefaultValue = "null",
            Description = "How fast the animation runs, as a multiplier of its normal speed: 2 is twice as fast, 0.5 half as fast. Zero and negative values are ignored.",
        },
        new()
        {
            Name = "Styles",
            Type = "BitLoadingClassStyles?",
            DefaultValue = "null",
            Description = "Custom CSS styles for different parts of the loading component.",
            LinkType = LinkType.Link,
            Href = "#class-styles",
        },
        new()
        {
            Name = "Thickness",
            Type = "int?",
            DefaultValue = "null",
            Description = "The thickness, in px, of the stroke the loading component is drawn with. Only the loaders drawn with a stroke read it - BitRingLoading, BitDualRingLoading, BitRippleLoading, BitXboxLoading and BitSpinnerLoading - and it does not scale with Size. Zero and negative values are ignored.",
        }
    ];

    private readonly List<ComponentSubClass> componentSubClasses =
    [
        new()
        {
            Id = "class-styles",
            Title = "BitLoadingClassStyles",
            Parameters =
            [
                new()
                {
                    Name = "Root",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the root element of the BitLoading components."
                },
                new()
                {
                    Name = "Container",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the child container of the BitLoading components."
                },
                new()
                {
                    Name = "Child",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the child element(s) of the BitLoading components."
                },
                new()
                {
                    Name = "Label",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the label of the BitLoading components."
                },
                new()
                {
                    Name = "ScreenReaderText",
                    Type = "string?",
                    DefaultValue = "null",
                    Description = "Custom CSS classes/styles for the visually hidden text a labelless BitLoading component announces to assistive technology."
                }
            ]
        }
    ];

    private readonly List<ComponentSubEnum> componentSubEnums =
    [
        new()
        {
            Id = "color-enum",
            Name = "BitColor",
            Description = "Defines the general colors available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Primary",
                    Description="Info Primary general color.",
                    Value="0",
                },
                new()
                {
                    Name= "Secondary",
                    Description="Secondary general color.",
                    Value="1",
                },
                new()
                {
                    Name= "Tertiary",
                    Description="Tertiary general color.",
                    Value="2",
                },
                new()
                {
                    Name= "Info",
                    Description="Info general color.",
                    Value="3",
                },
                new()
                {
                    Name= "Success",
                    Description="Success general color.",
                    Value="4",
                },
                new()
                {
                    Name= "Warning",
                    Description="Warning general color.",
                    Value="5",
                },
                new()
                {
                    Name= "SevereWarning",
                    Description="SevereWarning general color.",
                    Value="6",
                },
                new()
                {
                    Name= "Error",
                    Description="Error general color.",
                    Value="7",
                },
                new()
                {
                    Name= "PrimaryBackground",
                    Description="Primary background color.",
                    Value="8",
                },
                new()
                {
                    Name= "SecondaryBackground",
                    Description="Secondary background color.",
                    Value="9",
                },
                new()
                {
                    Name= "TertiaryBackground",
                    Description="Tertiary background color.",
                    Value="10",
                },
                new()
                {
                    Name= "PrimaryForeground",
                    Description="Primary foreground color.",
                    Value="11",
                },
                new()
                {
                    Name= "SecondaryForeground",
                    Description="Secondary foreground color.",
                    Value="12",
                },
                new()
                {
                    Name= "TertiaryForeground",
                    Description="Tertiary foreground color.",
                    Value="13",
                },
                new()
                {
                    Name= "PrimaryBorder",
                    Description="Primary border color.",
                    Value="14",
                },
                new()
                {
                    Name= "SecondaryBorder",
                    Description="Secondary border color.",
                    Value="15",
                },
                new()
                {
                    Name= "TertiaryBorder",
                    Description="Tertiary border color.",
                    Value="16",
                }
            ]
        },
        new()
        {
            Id = "label-position-enum",
            Name = "BitLabelPosition",
            Description = "Defines where the label of a loading component sits relative to its animation.",
            Items =
            [
                new()
                {
                    Name= "Top",
                    Description="The label shows above the animation.",
                    Value="0",
                },
                new()
                {
                    Name= "End",
                    Description="The label shows at the end side of the animation, which follows the direction of the writing.",
                    Value="1",
                },
                new()
                {
                    Name= "Bottom",
                    Description="The label shows below the animation.",
                    Value="2",
                },
                new()
                {
                    Name= "Start",
                    Description="The label shows at the start side of the animation, which follows the direction of the writing.",
                    Value="3",
                },
            ]
        },
        new()
        {
            Id = "size-enum",
            Name = "BitSize",
            Description = "Defines the sizes available in the bit BlazorUI.",
            Items =
            [
                new()
                {
                    Name= "Small",
                    Description="The small size, which renders a 40px loading component.",
                    Value="0",
                },
                new()
                {
                    Name= "Medium",
                    Description="The medium size, which renders a 64px loading component.",
                    Value="1",
                },
                new()
                {
                    Name= "Large",
                    Description="The large size, which renders an 88px loading component.",
                    Value="2",
                }
            ]
        },
    ];



    private readonly string example1RazorCode = @"
<BitBarsLoading />

<BitCircleLoading />

<BitDotsRingLoading />

<BitDualRingLoading />

<BitEllipsisLoading />

<BitGridLoading />

<BitHeartLoading />

<BitHourglassLoading />

<BitRingLoading />

<BitRippleLoading />

<BitRollerLoading />

<BitSpinnerLoading />

<BitXboxLoading />

<BitSlickBarsLoading />

<BitBouncingDotsLoading />

<BitRollingDashesLoading />

<BitOrbitingDotsLoading />

<BitRollingSquareLoading />";

    private readonly string example2RazorCode = @"
<BitGridLoading Label=""Loading"" />

<BitRingLoading Label=""Uploading photos..."" />";

    private readonly string example3RazorCode = @"
<BitDotsRingLoading Label=""Top"" LabelPosition=""BitLabelPosition.Top"" />

<BitDotsRingLoading Label=""Bottom"" LabelPosition=""BitLabelPosition.Bottom"" />

<BitDotsRingLoading Label=""Start"" LabelPosition=""BitLabelPosition.Start"" />

<BitDotsRingLoading Label=""End"" LabelPosition=""BitLabelPosition.End"" />";

    private readonly string example4RazorCode = @"
<BitEllipsisLoading>
    <LabelTemplate>
        <div style=""color:green""><b>Loading</b></div>
    </LabelTemplate>
</BitEllipsisLoading>

<BitRollerLoading LabelPosition=""BitLabelPosition.Bottom"">
    <LabelTemplate>
        <BitText Typography=""BitTypography.Caption1"" Color=""BitColor.SecondaryForeground"">
            Restoring your session
        </BitText>
    </LabelTemplate>
</BitRollerLoading>";

    private readonly string example5RazorCode = @"
<BitRingLoading Label=""0.5x"" Speed=""0.5"" />

<BitRingLoading Label=""1x (default)"" />

<BitRingLoading Label=""2x"" Speed=""2"" />

<BitRingLoading Label=""4x"" Speed=""4"" />";

    private readonly string example6RazorCode = @"
<BitToggleButton @bind-IsChecked=""_isPaused"" Text=""@(_isPaused ? ""Resume"" : ""Pause"")"" />

<BitRingLoading Label=""Ring"" Paused=""_isPaused"" />

<BitBarsLoading Label=""Bars"" Paused=""_isPaused"" />

<BitHourglassLoading Label=""Hourglass"" Paused=""_isPaused"" />";
    private readonly string example6CsharpCode = @"
private bool _isPaused;";

    private readonly string example7RazorCode = @"
<BitButton OnClick=""StartWork"" IsEnabled=""@(_isWorking is false)"">Run a 1.5s task</BitButton>

@if (_isWorking)
{
    <BitSpinnerLoading />

    <BitSpinnerLoading Delay=""500"" />

    @* The task is over before the delay elapses, so this one never renders at all. *@
    <BitSpinnerLoading Delay=""3000"" />
}";
    private readonly string example7CsharpCode = @"
private bool _isWorking;

private async Task StartWork()
{
    _isWorking = true;
    await Task.Delay(1500);
    _isWorking = false;
}";

    private readonly string example8RazorCode = @"
<BitRingLoading Label=""Default"" />

<BitRingLoading Label=""Thickness=2"" Thickness=""2"" />

<BitRingLoading Label=""Thickness=12"" Thickness=""12"" />


<BitSpinnerLoading Label=""Spinner"" Thickness=""10"" />

<BitDualRingLoading Label=""DualRing"" Thickness=""2"" />

<BitRippleLoading Label=""Ripple"" Thickness=""8"" />

<BitXboxLoading Label=""Xbox"" Thickness=""6"" />";

    private readonly string example9RazorCode = @"
<div>
    Fetching the latest results
    <BitRingLoading Inline CustomSize=""16"" CustomColor=""currentColor"" />
    please wait.
</div>

<BitButton IsEnabled=""false"">
    <BitStack Horizontal FitWidth AutoHeight Gap=""0.5rem"" VerticalAlign=""BitAlignment.Center"">
        <BitRingLoading Inline CustomSize=""16"" CustomColor=""currentColor"" />
        <span>Saving</span>
    </BitStack>
</BitButton>";

    private readonly string example10RazorCode = @"
@* role=""status"" aria-live=""polite"" and a visually hidden ""Loading"" by default. *@
<BitSpinnerLoading />

@* The hidden text becomes the AriaLabel. *@
<BitSpinnerLoading AriaLabel=""Fetching your orders"" />

@* Decorative: the surroundings already report the wait. *@
<BitSpinnerLoading Role=""none"" />

@* Interrupts the screen reader rather than waiting for it. *@
<BitSpinnerLoading Label=""Signing you out"" AriaLive=""assertive"" />";

    private readonly string example11RazorCode = @"
<BitBarsLoading Label=""Primary"" Color=""BitColor.Primary"" />

<BitCircleLoading Label=""Secondary"" Color=""BitColor.Secondary"" />

<BitDotsRingLoading Label=""Tertiary"" Color=""BitColor.Tertiary"" />

<BitDualRingLoading Label=""Info"" Color=""BitColor.Info"" />

<BitEllipsisLoading Label=""Success"" Color=""BitColor.Success"" />

<BitGridLoading Label=""Warning"" Color=""BitColor.Warning"" />

<BitHeartLoading Label=""SevereWarning"" Color=""BitColor.SevereWarning"" />

<BitHourglassLoading Label=""Error"" Color=""BitColor.Error"" />


<BitBarsLoading Label=""brown"" CustomColor=""brown"" />

<BitCircleLoading Label=""rgb(0 107 185 / 75%)"" CustomColor=""rgb(0 107 185 / 75%)"" />

<BitDotsRingLoading Label=""#426985"" CustomColor=""#426985"" />

<BitDualRingLoading Label=""hsl(106 100% 22% / 1)"" CustomColor=""hsl(106 100% 22% / 1)"" />

<div style=""color:mediumvioletred"">
    <BitSpinnerLoading Label=""currentColor"" CustomColor=""currentColor"" />
</div>";

    private readonly string example12RazorCode = @"
<BitXboxLoading Label=""Small"" Size=""BitSize.Small"" />

<BitXboxLoading Label=""Medium"" Size=""BitSize.Medium"" />

<BitXboxLoading Label=""Large"" Size=""BitSize.Large"" />

<BitXboxLoading Label=""Custom (128)"" CustomSize=""128"" />

<BitXboxLoading Label=""Custom (24)"" CustomSize=""24"" />";

    private readonly string example13RazorCode = @"
<BitRingLoading Label=""Style"" Style=""padding:1rem;border:1px solid gray;border-radius:8px"" />

<BitRingLoading Label=""Class"" Class=""custom-class"" />


<BitDotsRingLoading Label=""Variables"" Style=""--bit-ldn-color:rebeccapurple;--bit-ldn-size:48px;--bit-ldn-mot-factor:0.5"" />


<BitGridLoading Label=""Styles""
                Styles=""@(new() { Root = ""padding:0.5rem"",
                                  Container = ""outline:1px dashed gray"",
                                  Child = ""border-radius:0"",
                                  Label = ""color:tomato;font-weight:bold"" })"" />

<BitGridLoading Label=""Classes""
                Classes=""@(new() { Root = ""custom-root"",
                                   Child = ""custom-child"",
                                   Label = ""custom-label"" })"" />";

    private readonly string example14RazorCode = @"
<div dir=""rtl"">
    <BitRingLoading Dir=""BitDir.Rtl"" Label=""شروع"" LabelPosition=""BitLabelPosition.Start"" />

    <BitRingLoading Dir=""BitDir.Rtl"" Label=""پایان"" LabelPosition=""BitLabelPosition.End"" />

    <BitRingLoading Dir=""BitDir.Rtl"" Label=""در حال بارگذاری"" LabelPosition=""BitLabelPosition.Bottom"" />

    @* The two loaders whose motion travels across the box are mirrored, so they run toward the end of the line. *@
    <BitEllipsisLoading Dir=""BitDir.Rtl"" Label=""نقطه‌ها"" />

    <BitRollingSquareLoading Dir=""BitDir.Rtl"" Label=""مربع"" />
</div>";
}

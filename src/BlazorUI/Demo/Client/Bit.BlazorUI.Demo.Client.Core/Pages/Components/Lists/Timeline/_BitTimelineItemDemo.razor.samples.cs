namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class _BitTimelineItemDemo
{
    private readonly string example1RazorCode = @"
<BitTimeline Items=""basicItems"" />";
    private readonly string example1CsharpCode = @"
private List<BitTimelineItem> basicItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"" }
];";

    private readonly string example2RazorCode = @"
<BitTimeline Horizontal Items=""basicItems"" />";
    private readonly string example2CsharpCode = @"
private List<BitTimelineItem> basicItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"" }
];";

    private readonly string example3RazorCode = @"
<BitTimeline Horizontal Items=""basicItems"" IsEnabled=""false"" />
<BitTimeline Horizontal Items=""disabledItems"" />";
    private readonly string example3CsharpCode = @"
private List<BitTimelineItem> basicItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"" }
];

private List<BitTimelineItem> disabledItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"" }
];";

    private readonly string example4RazorCode = @"
<BitTimeline Horizontal Variant=""BitVariant.Fill"" Items=""disabledItems"" />
<BitTimeline Horizontal Variant=""BitVariant.Outline"" Items=""disabledItems"" />
<BitTimeline Horizontal Variant=""BitVariant.Text"" Items=""disabledItems"" />";
    private readonly string example4CsharpCode = @"
private List<BitTimelineItem> disabledItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"" }
];";

    private readonly string example5RazorCode = @"
<BitTimeline Horizontal Items=""iconItems"" Variant=""BitVariant.Fill"" />
<BitTimeline Horizontal Items=""iconItems"" Variant=""BitVariant.Outline"" />
<BitTimeline Horizontal Items=""iconItems"" Variant=""BitVariant.Text"" />";
    private readonly string example5CsharpCode = @"
private List<BitTimelineItem> iconItems =
[
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit, SecondaryText = ""Item 2 Secondary"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
];";

    private readonly string example6RazorCode = @"
<BitTimeline Items=""basicItems"" Reversed />
<BitTimeline Items=""reversedItems"" />

<BitTimeline Horizontal Items=""basicItems"" Reversed />
<BitTimeline Horizontal Items=""reversedItems"" />";
    private readonly string example6CsharpCode = @"
private List<BitTimelineItem> basicItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"" }
];

private List<BitTimelineItem> reversedItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", Reversed = true },
    new() { PrimaryText = ""Item 3"" }
];";

    private readonly string example7RazorCode = @"
<style>
    .dot-template {
        z-index: 1;
        border-radius: 50%;
        background-color: tomato;
    }
</style>


<BitTimeline Items=""templateItems"" />
<BitTimeline Horizontal Items=""templateItems"" />";
    private readonly string example7CsharpCode = @"
private List<BitTimelineItem> templateItems =
[
    new()
    {
        PrimaryContent = (item => @<BitPersona PrimaryText=""Annie Lindqvist""
        Size=""@BitPersonaSize.Size32""
        Presence=""@BitPersonaPresence.Online""
        ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />),

        DotTemplate = (item => @<div class=""dot-template""><BitRingLoading Size=""30"" /></div>),

        SecondaryContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                        <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                        <BitLabel>Software Engineer</BitLabel>
                                    </div>)
    },
    new()
    {
        PrimaryContent = (item => @<BitPersona PrimaryText=""Saleh Khafan""
        Size=""@BitPersonaSize.Size32""
        Presence=""@BitPersonaPresence.Online"" />),

        DotTemplate = (item => @<div class=""dot-template""><BitSpinnerLoading Size=""30"" /></div>),

        SecondaryContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                        <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                        <BitLabel>Co-Founder & CTO</BitLabel>
                                    </div>),
        Reversed = true
    },
    new()
    {
        PrimaryContent = (item => @<BitPersona PrimaryText=""Ted Randall""
        Size=""@BitPersonaSize.Size32""
        Presence=""@BitPersonaPresence.Online""
        ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-male.png"" />),

        DotTemplate = (item => @<div class=""dot-template""><BitRollerLoading Size=""30"" /></div>),

        SecondaryContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                        <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                        <BitLabel>Project Manager</BitLabel>
                                    </div>)
    },
];";

    private readonly string example8RazorCode = @"
<BitTimeline Horizontal Color=""BitColor.Primary"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.Secondary"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.Tertiary"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.Info"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Info"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Info"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.Success"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Success"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Success"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.Warning"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Warning"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.Error"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""iconItems"" />";
    private readonly string example8CsharpCode = @"
private List<BitTimelineItem> iconItems =
[
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit, SecondaryText = ""Item 2 Secondary"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
];";

    private readonly string example9RazorCode = @"
<BitTimeline Horizontal Size=""BitSize.Small"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Size=""BitSize.Medium"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Size=""BitSize.Large"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Text"" Items=""iconItems"" />";
    private readonly string example9CsharpCode = @"
private List<BitTimelineItem> iconItems =
[
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit, SecondaryText = ""Item 2 Secondary"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
];";

    private readonly string example10RazorCode = @"
<style>
    .custom-class {
        padding: 1rem 0;
        border-radius: 1rem;
        background-color: blueviolet;
    }

    .custom-item {
        color: blueviolet;
        background-color: goldenrod;
    }

    .custom-icon {
        color: goldenrod;
    }

    .custom-divider::before {
        background: green;
    }

    .custom-text {
        color: rebeccapurple;
    }
</style>


<BitTimeline Style=""background-color: tomato; box-shadow: red 0 0 1rem;"" Items=""basicItems"" />

<BitTimeline Horizontal Class=""custom-class"" Items=""basicItems"" />

<BitTimeline Items=""styleClassItems"" />

<BitTimeline Items=""iconItems""
             Styles=""@(new() { Icon = ""color: red;"",
                               PrimaryText = ""color: aqua; font-size: 1.5rem;"",
                               Dot = ""background-color: dodgerblue;"" })"" />

<BitTimeline Items=""iconItems""
             Variant=""BitVariant.Outline""
             Classes=""@(new() { Icon = ""custom-icon"",
                                Divider = ""custom-divider"",
                                PrimaryText = ""custom-text"" })"" />";
    private readonly string example10CsharpCode = @"
private List<BitTimelineItem> basicItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"" }
];

private List<BitTimelineItem> styleClassItems =
[
    new() { PrimaryText = ""Styled"", Style = ""color: darkred;"", IconName = BitIconName.Brush },
    new() { PrimaryText = ""Classed"", Class = ""custom-item"", IconName = BitIconName.FormatPainter }
];

private List<BitTimelineItem> iconItems =
[
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit, SecondaryText = ""Item 2 Secondary"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
];";

    private readonly string example11RazorCode = @"
<BitTimeline Dir=""BitDir.Rtl"" Items=""basicRtlItems"" />
<BitTimeline Horizontal Dir=""BitDir.Rtl"" Items=""basicRtlItems"" />";
    private readonly string example11CsharpCode = @"
private List<BitTimelineItem> basicRtlItems =
[
    new() { PrimaryText = ""گزینه ۱"" },
    new() { PrimaryText = ""گزینه ۲"", SecondaryText = ""گزینه ۲ ثانویه"" },
    new() { PrimaryText = ""گزینه ۳"" }
];";
}

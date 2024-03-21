namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Timeline;

public partial class _BitTimelineItemDemo
{
    private List<BitTimelineItem> basicItems = new()
    {
        new() { PrimaryText = "Item 1" },
        new() { PrimaryText = "Item 2" },
        new() { PrimaryText = "Item 3" }
    };

    private List<BitTimelineItem> basicItems1 = new()
    {
        new() { PrimaryText = "Item 1" },
        new() { PrimaryText = "Item 2", SecondaryText = "Item 2 Secondary" },
        new() { PrimaryText = "Item 3" }
    };

    private List<BitTimelineItem> disabledItems1 = new()
    {
        new() { PrimaryText = "Item 1" },
        new() { PrimaryText = "Item 2" },
        new() { PrimaryText = "Item 3", IsEnabled = false }
    };

    private List<BitTimelineItem> disabledItems2 = new()
    {
        new() { PrimaryText = "Item 1" },
        new() { PrimaryText = "Item 2", IsEnabled = false },
        new() { PrimaryText = "Item 3" }
    };

    private List<BitTimelineItem> disabledItems3 = new()
    {
        new() { PrimaryText = "Item 1", IsEnabled = false },
        new() { PrimaryText = "Item 2" },
        new() { PrimaryText = "Item 3" }
    };

    private List<BitTimelineItem> iconItems = new()
    {
        new() { PrimaryText = "Item 1", IconName = BitIconName.Add },
        new() { PrimaryText = "Item 2", IconName = BitIconName.Edit },
        new() { PrimaryText = "Item 3", IconName = BitIconName.Delete }
    };

    private List<BitTimelineItem> reversedItems = new()
    {
        new() { PrimaryText = "Item 1" },
        new() { PrimaryText = "Item 2", Reversed = true },
        new() { PrimaryText = "Item 3" }
    };

    private List<BitTimelineItem> styleClassItems = new()
    {
        new()
        {
            PrimaryText = "Styled",
            Style = "color: darkred;",
            IconName = BitIconName.Brush,
        },
        new()
        {
            PrimaryText = "Classed",
            Class = "custom-item",
            IconName = BitIconName.FormatPainter,
        }
    };

    private List<BitTimelineItem> basicRtlItems = new()
    {
        new() { PrimaryText = "گزینه ۱" },
        new() { PrimaryText = "گزینه ۲", SecondaryText = "گزینه ۲ ثانویه" },
        new() { PrimaryText = "گزینه ۳" }
    };



    private readonly string example1RazorCode = @"
<BitTimeline Items=""basicItems1"" />
<BitTimeline Horizontal Items=""basicItems1"" />";
    private readonly string example1CsharpCode = @"
private List<BitTimelineItem> basicItems1 = new()
{
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"" }
};";

    private readonly string example2RazorCode = @"
<BitTimeline Horizontal Items=""disabledItems1"" />
<BitTimeline Horizontal Appearance=""BitAppearance.Standard"" Items=""disabledItems2"" />
<BitTimeline Horizontal Appearance=""BitAppearance.Text"" Items=""disabledItems3"" />";
    private readonly string example2CsharpCode = @"
private List<BitTimelineItem> disabledItems1 = new()
{
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"" },
    new() { PrimaryText = ""Item 3"", IsEnabled = false }
};

private List<BitTimelineItem> disabledItems2 = new()
{
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"" }
};

private List<BitTimelineItem> disabledItems3 = new()
{
    new() { PrimaryText = ""Item 1"", IsEnabled = false },
    new() { PrimaryText = ""Item 2"" },
    new() { PrimaryText = ""Item 3"" }
};";

    private readonly string example3RazorCode = @"
<BitTimeline Horizontal Items=""iconItems"" />
<BitTimeline Horizontal Items=""iconItems"" Appearance=""BitAppearance.Standard"" />
<BitTimeline Horizontal Items=""iconItems"" Appearance=""BitAppearance.Text"" />";
    private readonly string example3CsharpCode = @"
private List<BitTimelineItem> iconItems = new()
{
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
};";

    private readonly string example4RazorCode = @"
<BitTimeline Items=""reversedItems"" />
<BitTimeline Horizontal Items=""reversedItems"" />";
    private readonly string example4CsharpCode = @"
private List<BitTimelineItem> reversedItems = new()
{
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", Reversed = true },
    new() { PrimaryText = ""Item 3"" }
};";

    private readonly string example5RazorCode = @"
<style>
    .dot-template {
        z-index: 1;
        border-radius: 50%;
        background-color: tomato;
    }
</style>


<BitTimeline Items=""templateItems"" />
<BitTimeline Horizontal Items=""templateItems"" />";
    private readonly string example5CsharpCode = @"
private List<BitTimelineItem> templateItems = new()
{
    new() 
    { 
        PrimaryContent = (item => @<BitPersona Text=""Annie Lindqvist""
                                               Size=""@BitPersonaSize.Size32""
                                               Presence=""@BitPersonaPresenceStatus.Online""
                                               ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />),

        DotTemplate = (item => @<div class=""dot-template""><BitRingLoading Size = ""30"" /></div>),

        SecondaryContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                         <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                         <BitLabel>Software Engineer</BitLabel>
                                     </div>) 
    },
    new() 
    { 
        PrimaryContent = (item => @<BitPersona Text=""Saleh Khafan""
                                               Size=""@BitPersonaSize.Size32""
                                               Presence=""@BitPersonaPresenceStatus.Online""/>),

        DotTemplate = (item => @<div class=""dot-template""><BitSpinnerLoading Size=""30"" /></div>),

        SecondaryContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                         <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                         <BitLabel>Co-Founder & CTO</BitLabel>
                                     </div>),
        Reversed = true
    },
    new() 
    { 
        PrimaryContent = (item => @<BitPersona Text=""Ted Randall""
                                               Size=""@BitPersonaSize.Size32""
                                               Presence=""@BitPersonaPresenceStatus.Online""
                                               ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-male.png"" />),

        DotTemplate = (item => @<div class=""dot-template""><BitRollerLoading Size=""30"" /></div>),

        SecondaryContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                         <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                         <BitLabel>Project Manager</BitLabel>
                                     </div>) 
    },
};";

    private readonly string example6RazorCode = @"
<BitTimeline Horizontal Color=""BitColor.Info"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Info"" Appearance=""BitAppearance.Standard"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Info"" Appearance=""BitAppearance.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.Success"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Success"" Appearance=""BitAppearance.Standard"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Success"" Appearance=""BitAppearance.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.Warning"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Warning"" Appearance=""BitAppearance.Standard"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Warning"" Appearance=""BitAppearance.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Appearance=""BitAppearance.Standard"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Appearance=""BitAppearance.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Color=""BitColor.Error"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Error"" Appearance=""BitAppearance.Standard"" Items=""iconItems"" />
<BitTimeline Horizontal Color=""BitColor.Error"" Appearance=""BitAppearance.Text"" Items=""iconItems"" />";
    private readonly string example6CsharpCode = @"
private List<BitTimelineItem> iconItems = new()
{
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
};";

    private readonly string example7RazorCode = @"
<BitTimeline Horizontal Size=""BitTimelineSize.Small"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitTimelineSize.Small"" Appearance=""BitAppearance.Standard"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitTimelineSize.Small"" Appearance=""BitAppearance.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Size=""BitTimelineSize.Medium"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitTimelineSize.Medium"" Appearance=""BitAppearance.Standard"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitTimelineSize.Medium"" Appearance=""BitAppearance.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Size=""BitTimelineSize.Large"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitTimelineSize.Large"" Appearance=""BitAppearance.Standard"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitTimelineSize.Large"" Appearance=""BitAppearance.Text"" Items=""iconItems"" />";
    private readonly string example7CsharpCode = @"
private List<BitTimelineItem> iconItems = new()
{
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
};";

    private readonly string example8RazorCode = @"
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
             Appearance=""BitAppearance.Standard""
             Classes=""@(new() { Icon = ""custom-icon"",
                                Divider = ""custom-divider"",
                                PrimaryText = ""custom-text"" })"" />";
    private readonly string example8CsharpCode = @"
private List<BitTimelineItem> basicItems = new()
{
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"" },
    new() { PrimaryText = ""Item 3"" }
};

private List<BitTimelineItem> styleClassItems = new()
{
    new()
    {
        PrimaryText = ""Styled"",
        Style = ""color: darkred;"",
        IconName = BitIconName.Brush,
    },
    new()
    {
        PrimaryText = ""Classed"",
        Class = ""custom-item"",
        IconName = BitIconName.FormatPainter,
    }
};

private List<BitTimelineItem> iconItems = new()
{
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
};";

    private readonly string example9RazorCode = @"
<BitTimeline Dir=""BitDir.Rtl"" Items=""basicRtlItems"" />
<BitTimeline Horizontal Dir=""BitDir.Rtl"" Items=""basicRtlItems"" />";
    private readonly string example9CsharpCode = @"
private List<BitTimelineItem> basicRtlItems = new()
{
    new() { PrimaryText = ""گزینه ۱"" },
    new() { PrimaryText = ""گزینه ۲"", SecondaryText = ""گزینه ۲ ثانویه"" },
    new() { PrimaryText = ""گزینه ۳"" }
};";
}

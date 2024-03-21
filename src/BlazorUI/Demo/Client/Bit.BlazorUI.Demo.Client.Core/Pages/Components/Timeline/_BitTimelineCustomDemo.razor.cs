namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Timeline;

public partial class _BitTimelineCustomDemo
{
    private List<TimelineActionItem> basicCustoms = new()
    {
        new() { FirstText = "Custom 1" },
        new() { FirstText = "Custom 2" },
        new() { FirstText = "Custom 3" }
    };

    private List<TimelineActionItem> basicCustoms1 = new()
    {
        new() { FirstText = "Custom 1" },
        new() { FirstText = "Custom 2", SecondText = "Custom 2 Secondary" },
        new() { FirstText = "Custom 3" }
    };

    private List<TimelineActionItem> disabledCustoms1 = new()
    {
        new() { FirstText = "Custom 1" },
        new() { FirstText = "Custom 2" },
        new() { FirstText = "Custom 3", IsEnabled = false }
    };

    private List<TimelineActionItem> disabledCustoms2 = new()
    {
        new() { FirstText = "Custom 1" },
        new() { FirstText = "Custom 2", IsEnabled = false },
        new() { FirstText = "Custom 3" }
    };

    private List<TimelineActionItem> disabledCustoms3 = new()
    {
        new() { FirstText = "Custom 1", IsEnabled = false },
        new() { FirstText = "Custom 2" },
        new() { FirstText = "Custom 3" }
    };

    private List<TimelineActionItem> iconCustoms = new()
    {
        new() { FirstText = "Custom 1", Icon = BitIconName.Add },
        new() { FirstText = "Custom 2", Icon = BitIconName.Edit },
        new() { FirstText = "Custom 3", Icon = BitIconName.Delete }
    };

    private List<TimelineActionItem> reversedCustoms = new()
    {
        new() { FirstText = "Custom 1" },
        new() { FirstText = "Custom 2", Opposite = true },
        new() { FirstText = "Custom 3" }
    };

    private List<TimelineActionItem> styleClassCustoms = new()
    {
        new()
        {
            FirstText = "Styled",
            Style = "color: darkred;",
            Icon = BitIconName.Brush,
        },
        new()
        {
            FirstText = "Classed",
            Class = "custom-item",
            Icon = BitIconName.FormatPainter,
        }
    };

    private List<TimelineActionItem> basicRtlCustoms = new()
    {
        new() { FirstText = "گزینه ۱" },
        new() { FirstText = "گزینه ۲", SecondText = "گزینه ۲ ثانویه" },
        new() { FirstText = "گزینه ۳" }
    };



    private readonly string example1RazorCode = @"
<BitTimeline Items=""basicCustoms1""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      SecondaryText = { Selector = i => i.SecondText } })"" />

<BitTimeline Horizontal 
             Items=""basicCustoms1""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      SecondaryText = { Selector = i => i.SecondText } })"" />";
    private readonly string example1CsharpCode = @"
public class TimelineActionItem
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
}

private List<TimelineActionItem> basicCustoms1 = new()
{
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"" }
};";

    private readonly string example2RazorCode = @"
<BitTimeline Horizontal
             Items=""disabledCustoms1""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText } })"" />

<BitTimeline Horizontal
             Items=""disabledCustoms2""
             Appearance=""BitAppearance.Standard"" 
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText } })"" />

<BitTimeline Horizontal 
             Items=""disabledCustoms3""
             Appearance=""BitAppearance.Text""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText } })"" />";
    private readonly string example2CsharpCode = @"
public class TimelineActionItem
{
    public string? FirstText { get; set; }
    public bool IsEnabled { get; set; } = true;
}

private List<TimelineActionItem> disabledCustoms1 = new()
{
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"" },
    new() { FirstText = ""Custom 3"", IsEnabled = false }
};

private List<TimelineActionItem> disabledCustoms2 = new()
{
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", IsEnabled = false },
    new() { FirstText = ""Custom 3"" }
};

private List<TimelineActionItem> disabledCustoms3 = new()
{
    new() { FirstText = ""Custom 1"", IsEnabled = false },
    new() { FirstText = ""Custom 2"" },
    new() { FirstText = ""Custom 3"" }
};";

    private readonly string example3RazorCode = @"
<BitTimeline Horizontal
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />

<BitTimeline Horizontal
             Items=""iconCustoms""
             Appearance=""BitAppearance.Standard""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />

<BitTimeline Horizontal
             Items=""iconCustoms""
             Appearance=""BitAppearance.Text""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />";
    private readonly string example3CsharpCode = @"
public class TimelineActionItem
{
    public string? FirstText { get; set; }
    public string? Icon { get; set; }
}

private List<TimelineActionItem> iconCustoms = new()
{
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
};";

    private readonly string example4RazorCode = @"
<BitTimeline Items=""reversedCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      Reversed = { Selector = i => i.Opposite } })"" />

<BitTimeline Horizontal
             Items=""reversedCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      Reversed = { Selector = i => i.Opposite } })"" />";
    private readonly string example4CsharpCode = @"
public class TimelineActionItem
{
    public string? FirstText { get; set; }
    public bool Opposite { get; set; }
}

private List<TimelineActionItem> reversedCustoms = new()
{
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", Opposite = true },
    new() { FirstText = ""Custom 3"" }
};";

    private readonly string example5RazorCode = @"
<style>
    .dot-template {
        z-index: 1;
        border-radius: 50%;
        background-color: tomato;
    }
</style>


<BitTimeline Items=""templateItems""
             NameSelectors=""@(new() { PrimaryContent = { Selector = i => i.FirstContent },
                                      DotTemplate = { Selector = i => i.DotContent },
                                      SecondaryContent = { Selector = i => i.SecondContent },
                                      Reversed = { Selector = i => i.Opposite } })"" />

<BitTimeline Horizontal
             Items=""templateItems""
             NameSelectors=""@(new() { PrimaryContent = { Selector = i => i.FirstContent },
                                      DotTemplate = { Selector = i => i.DotContent },
                                      SecondaryContent = { Selector = i => i.SecondContent },
                                      Reversed = { Selector = i => i.Opposite } })"" />";
    private readonly string example5CsharpCode = @"
public class TimelineActionItem
{
    public RenderFragment<TimelineActionItem>? DotContent { get; set; }
    public RenderFragment<TimelineActionItem>? FirstContent { get; set; }
    public RenderFragment<TimelineActionItem>? SecondContent { get; set; }
    public bool Opposite { get; set; }
}

private List<TimelineActionItem> templateItems = new()
{
    new()
    {
        FirstContent = (item => @<BitPersona Text=""Annie Lindqvist""
                                             Size=""@BitPersonaSize.Size32""
                                             Presence=""@BitPersonaPresenceStatus.Online""
                                             ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />),

        DotContent = (item => @<div class=""dot-template""><BitRingLoading Size=""30"" /></div>),

        SecondContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                      <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                      <BitLabel>Software Engineer</BitLabel>
                                  </div>)
    },
    new()
    {
        FirstContent = (item => @<BitPersona Text=""Saleh Khafan""
                                             Size=""@BitPersonaSize.Size32""
                                             Presence=""@BitPersonaPresenceStatus.Online"" />),

        DotContent = (item => @<div class=""dot-template""><BitSpinnerLoading Size=""30"" /></div>),

        SecondContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                      <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                      <BitLabel>Co-Founder & CTO</BitLabel>
                                  </div>),
        Opposite = true
    },
    new()
    {
        FirstContent = (item => @<BitPersona Text=""Ted Randall""
                                             Size=""@BitPersonaSize.Size32""
                                             Presence=""@BitPersonaPresenceStatus.Online""
                                             ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-male.png"" />),

        DotContent = (item => @<div class=""dot-template""><BitRollerLoading Size=""30"" /></div>),

        SecondContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                      <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                      <BitLabel>Project Manager</BitLabel>
                                  </div>)
    },
};";

    private readonly string example6RazorCode = @"
<BitTimeline Horizontal
             Color=""BitColor.Info""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.Info""
             Appearance=""BitAppearance.Standard""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.Info""
             Appearance=""BitAppearance.Text""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />

<BitTimeline Horizontal
             Color=""BitColor.Success""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.Success""
             Appearance=""BitAppearance.Standard"" 
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.Success""
             Appearance=""BitAppearance.Text"" 
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />

<BitTimeline Horizontal
             Color=""BitColor.Warning""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.Warning""
             Appearance=""BitAppearance.Standard""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.Warning""
             Appearance=""BitAppearance.Text""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />

<BitTimeline Horizontal
             Color=""BitColor.SevereWarning""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.SevereWarning""
             Appearance=""BitAppearance.Standard""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.SevereWarning""
             Appearance=""BitAppearance.Text""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />

<BitTimeline Horizontal
             Color=""BitColor.Error""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.Error""
             Appearance=""BitAppearance.Standard""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Color=""BitColor.Error""
             Appearance=""BitAppearance.Text""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />";
    private readonly string example6CsharpCode = @"
public class TimelineActionItem
{
    public string? FirstText { get; set; }
    public string? Icon { get; set; }
}

private List<TimelineActionItem> iconCustoms = new()
{
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
};";

    private readonly string example7RazorCode = @"
<BitTimeline Horizontal
             Size=""BitTimelineSize.Small""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Size=""BitTimelineSize.Small""
             Appearance=""BitAppearance.Standard""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Size=""BitTimelineSize.Small""
             Appearance=""BitAppearance.Text""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />

<BitTimeline Horizontal
             Size=""BitTimelineSize.Medium""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Size=""BitTimelineSize.Medium""
             Appearance=""BitAppearance.Standard""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Size=""BitTimelineSize.Medium""
             Appearance=""BitAppearance.Text""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />

<BitTimeline Horizontal
             Size=""BitTimelineSize.Large""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Size=""BitTimelineSize.Large""
             Appearance=""BitAppearance.Standard""
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />
<BitTimeline Horizontal
             Size=""BitTimelineSize.Large""
             Appearance=""BitAppearance.Text"" 
             Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />";
    private readonly string example7CsharpCode = @"
public class TimelineActionItem
{
    public string? FirstText { get; set; }
    public string? Icon { get; set; }
}

private List<TimelineActionItem> iconCustoms = new()
{
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
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


<BitTimeline Items=""basicCustoms""
             Style=""background-color: tomato; box-shadow: red 0 0 1rem;""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText } })"" />
<BitTimeline Horizontal
             Items=""basicCustoms""
             Class=""custom-class""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText } })"" />

<BitTimeline Items=""styleClassCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })"" />

<BitTimeline Items=""iconCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })""
             Styles=""@(new() { Icon = ""color: red;"",
                               PrimaryText = ""color: aqua; font-size: 1.5rem;"",
                               Dot = ""background-color: dodgerblue;"" })"" />
<BitTimeline Items=""iconCustoms""
             Appearance=""BitAppearance.Standard""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      IconName = { Selector = i => i.Icon } })""
             Classes=""@(new() { Icon = ""custom-icon"",
                                Divider = ""custom-divider"",
                                PrimaryText = ""custom-text"" })"" />";
    private readonly string example8CsharpCode = @"
public class TimelineActionItem
{
    public string? FirstText { get; set; }
    public string? Icon { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

private List<TimelineActionItem> basicCustoms = new()
{
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"" },
    new() { FirstText = ""Custom 3"" }
};

private List<TimelineActionItem> styleClassCustoms = new()
{
    new()
    {
        FirstText = ""Styled"",
        Style = ""color: darkred;"",
        Icon = BitIconName.Brush,
    },
    new()
    {
        FirstText = ""Classed"",
        Class = ""custom-item"",
        Icon = BitIconName.FormatPainter,
    }
};

private List<TimelineActionItem> iconCustoms = new()
{
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
};";

    private readonly string example9RazorCode = @"
<BitTimeline Dir=""BitDir.Rtl""
             Items=""basicRtlCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      SecondaryText = { Selector = i => i.SecondText } })"" />

<BitTimeline Horizontal
             Dir=""BitDir.Rtl""
             Items=""basicRtlCustoms""
             NameSelectors=""@(new() { PrimaryText = { Selector = i => i.FirstText },
                                      SecondaryText = { Selector = i => i.SecondText } })"" />";
    private readonly string example9CsharpCode = @"
public class TimelineActionItem
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
}

private List<TimelineActionItem> basicRtlCustoms = new()
{
    new() { FirstText = ""گزینه ۱"" },
    new() { FirstText = ""گزینه ۲"", SecondText = ""گزینه ۲ ثانویه"" },
    new() { FirstText = ""گزینه ۳"" }
};";
}

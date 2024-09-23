namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class _BitTimelineCustomDemo
{
    private readonly string example1RazorCode = @"
<BitTimeline Items=""basicCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example1CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText }
};

private List<Event> basicCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"" }
];";

    private readonly string example2RazorCode = @"
<BitTimeline Horizontal Items=""basicCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example2CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText }
};

private List<Event> basicCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"" }
];";

    private readonly string example3RazorCode = @"
<BitTimeline Horizontal Items=""basicCustoms"" NameSelectors=""nameSelectors"" IsEnabled=""false"" />
<BitTimeline Horizontal Items=""disabledCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example3CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
    public bool Disabled { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText },
    IsEnabled = { Selector = i => i.Disabled is false },
};

private List<Event> basicCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"" }
];

private List<Event> disabledCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"", Disabled = true },
    new() { FirstText = ""Custom 3"" }
];";

    private readonly string example4RazorCode = @"
<BitTimeline Horizontal Variant=""BitVariant.Fill"" Items=""disabledCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Variant=""BitVariant.Outline"" Items=""disabledCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Variant=""BitVariant.Text"" Items=""disabledCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example4CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
    public bool Disabled { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText },
    IsEnabled = { Selector = i => i.Disabled is false },
};

private List<Event> disabledCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"", Disabled = true },
    new() { FirstText = ""Custom 3"" }
];";

    private readonly string example5RazorCode = @"
<BitTimeline Horizontal Items=""iconCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Fill"" />
<BitTimeline Horizontal Items=""iconCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" />
<BitTimeline Horizontal Items=""iconCustoms"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" />";
    private readonly string example5CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
    public bool Disabled { get; set; }
    public string? Icon { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText },
    IsEnabled = { Selector = i => i.Disabled is false },
    IconName = { Selector = i => i.Icon },
};

private List<Event> iconCustoms =
[
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit, SecondText = ""Item 2 Secondary"", Disabled = true },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
];";

    private readonly string example6RazorCode = @"
<BitTimeline Items=""basicCustoms"" NameSelectors=""nameSelectors"" Reversed />
<BitTimeline Items=""reversedCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Items=""basicCustoms"" NameSelectors=""nameSelectors"" Reversed />
<BitTimeline Horizontal Items=""reversedCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example6CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
    public bool Reversed { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText }
};

private List<Event> basicCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"" }
];

private List<Event> reversedCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", Reversed = true },
    new() { FirstText = ""Custom 3"" }
];";

    private readonly string example7RazorCode = @"
<style>
    .dot-template {
        z-index: 1;
        border-radius: 50%;
        background-color: tomato;
    }
</style>


<BitTimeline Items=""templateItems"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Items=""templateItems"" NameSelectors=""nameSelectors"" />";
    private readonly string example7CsharpCode = @"
public class Event
{
    public RenderFragment<Event>? FirstContent { get; set; }
    public RenderFragment<Event>? DotContent { get; set; }
    public RenderFragment<Event>? SecondContent { get; set; }
    public bool Reversed { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryContent = { Selector = i => i.FirstContent },
    DotTemplate = { Selector = i => i.DotContent },
    SecondaryContent = { Selector = i => i.SecondContent },
};

private List<Event> templateItems = 
[
    new()
    {
        FirstContent = (item => @<BitPersona PrimaryText=""Annie Lindqvist""
                                             Size=""@BitPersonaSize.Size32""
                                             Presence=""@BitPersonaPresence.Online""
                                             ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />),

        DotContent = (item => @<div class=""dot-template""><BitRingLoading CustomSize=""30"" Color=""BitColor.Tertiary"" /></div>),

        SecondContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                      <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                      <BitLabel>Software Engineer</BitLabel>
                                  </div>)
    },
    new()
    {
        FirstContent = (item => @<BitPersona PrimaryText=""Saleh Khafan""
                                             Size=""@BitPersonaSize.Size32""
                                             Presence=""@BitPersonaPresence.Online"" />),

        DotContent = (item => @<div class=""dot-template""><BitSpinnerLoading CustomSize=""30"" Color=""BitColor.Tertiary"" /></div>),

        SecondContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                      <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                      <BitLabel>Co-Founder & CTO</BitLabel>
                                  </div>),
        Reversed = true
    },
    new()
    {
        FirstContent = (item => @<BitPersona PrimaryText=""Ted Randall""
                                             Size=""@BitPersonaSize.Size32""
                                             Presence=""@BitPersonaPresence.Online""
                                             ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-male.png"" />),

        DotContent = (item => @<div class=""dot-template""><BitRollerLoading CustomSize=""30"" Color=""BitColor.Tertiary"" /></div>),

        SecondContent = (item => @<div style=""display: flex; align-items: center; gap: 1rem;"">
                                      <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                      <BitLabel>Project Manager</BitLabel>
                                  </div>)
    },
];";

    private readonly string example8RazorCode = @"
<BitTimeline Horizontal Color=""BitColor.Primary"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Color=""BitColor.Secondary"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Color=""BitColor.Tertiary"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Color=""BitColor.Info"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Info"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Info"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Color=""BitColor.Success"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Success"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Success"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Color=""BitColor.Warning"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Warning"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Color=""BitColor.Error"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example8CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
    public bool Disabled { get; set; }
    public string? Icon { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText },
    IsEnabled = { Selector = i => i.Disabled is false },
    IconName = { Selector = i => i.Icon },
};

private List<Event> iconCustoms =
[
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit, SecondText = ""Item 2 Secondary"", Disabled = true },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
];";

    private readonly string example9RazorCode = @"
<BitTimeline Horizontal Size=""BitSize.Small"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Size=""BitSize.Medium"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Size=""BitSize.Large"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example9CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
    public bool Disabled { get; set; }
    public string? Icon { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText },
    IsEnabled = { Selector = i => i.Disabled is false },
    IconName = { Selector = i => i.Icon },
};

private List<Event> iconCustoms =
[
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit, SecondText = ""Item 2 Secondary"", Disabled = true },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
];";

    private readonly string example10RazorCode = @"
<style>
    .custom-class {
        color: dodgerblue;
        font-weight: bold;
        margin-inline: 1rem;
        padding-block: 1rem;
        text-shadow: dodgerblue 0 0 1rem;
    }


    .custom-item {
        color: dodgerblue;
        font-weight: bold;
        text-shadow: dodgerblue 0 0 1rem;
    }


    .custom-dot {
        border-color: blueviolet;
        box-shadow: blueviolet 0 0 1rem;
    }

    .custom-icon {
        color: blueviolet;
    }

    .custom-divider::before {
        background: blueviolet;
    }

    .custom-item-text {
        color: blueviolet;
    }
</style>


<BitTimeline Style=""max-width: max-content; color: dodgerblue;"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Class=""custom-class"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />


<BitTimeline Items=""styleClassCustoms"" NameSelectors=""nameSelectors"" />


<BitTimeline Items=""iconCustoms"" NameSelectors=""nameSelectors""
             Styles=""@(new() { Icon = ""color: whitesmoke;"",
                               Dot = ""background-color: lightseagreen; border-color: mediumseagreen;"",
                               PrimaryText = ""color: lightseagreen; font-weight: bold;"" })"" />

<BitTimeline Items=""iconCustoms"" NameSelectors=""nameSelectors""
             Variant=""BitVariant.Outline""
             Classes=""@(new() { Dot = ""custom-dot"",
                                Icon = ""custom-icon"",
                                Item = ""custom-item-text"",
                                Divider = ""custom-divider"" })"" />";
    private readonly string example10CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
    public bool Disabled { get; set; }
    public string? Icon { get; set; }
    public string? Class { get; set; }
    public string? Style { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText },
    IsEnabled = { Selector = i => i.Disabled is false },
    IconName = { Selector = i => i.Icon },
};

private List<Event> basicCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"" }
];

private List<Event> styleClassCustoms =
[
    new() { FirstText = ""Styled"", Style = ""color: dodgerblue;"", Icon = BitIconName.Brush },
    new() { FirstText = ""Classed"", Class = ""custom-item"", Icon = BitIconName.FormatPainter }
];

private List<Event> iconCustoms = new()
{
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
};";

    private readonly string example11RazorCode = @"
<BitTimeline Dir=""BitDir.Rtl"" Items=""basicRtlCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Dir=""BitDir.Rtl"" Items=""basicRtlCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example11CsharpCode = @"
public class Event
{
    public string? FirstText { get; set; }
    public string? SecondText { get; set; }
}

BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText }
};

private List<Event> basicRtlCustoms =
[
    new() { FirstText = ""گزینه ۱"" },
    new() { FirstText = ""گزینه ۲"", SecondText = ""گزینه ۲ ثانویه"" },
    new() { FirstText = ""گزینه ۳"" }
];";
}

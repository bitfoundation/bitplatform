namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class _BitTimelineCustomDemo
{
    private readonly string example1RazorCode = @"
<BitTimeline Items=""basicCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example1CsharpCode = @"
public class Event
{
    public string? Class { get; set; }
    public BitColor? DotColor { get; set; }
    public RenderFragment<Event>? DotContent { get; set; }
    public RenderFragment<Event>? FirstContent { get; set; }
    public string? FirstText { get; set; }
    public BitIconInfo? ExternalIcon { get; set; }
    public bool NoDot { get; set; }
    public string? Icon { get; set; }
    public bool Disabled { get; set; }
    public Action<Event>? OnSelect { get; set; }
    public bool Reversed { get; set; }
    public RenderFragment<Event>? SecondContent { get; set; }
    public string? SecondText { get; set; }
    public BitSize? DotSize { get; set; }
    public string? Style { get; set; }
    public RenderFragment<Event>? Content { get; set; }
    public BitVariant? DotVariant { get; set; }
}

private BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    SecondaryText = { Selector = i => i.SecondText },
    IsEnabled = { Selector = i => i.Disabled is false },
    IconName = { Selector = i => i.Icon },
    DotTemplate = { Selector = i => i.DotContent },
    PrimaryContent = { Selector = i => i.FirstContent },
    SecondaryContent = { Selector = i => i.SecondContent },
    Icon = { Selector = i => i.ExternalIcon },
    Color = { Selector = i => i.DotColor },
    Size = { Selector = i => i.DotSize },
    Variant = { Selector = i => i.DotVariant },
    HideDot = { Selector = i => i.NoDot },
    Template = { Selector = i => i.Content },
    OnClick = { Selector = i => i.OnSelect },
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
private List<Event> iconCustoms =
[
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit, SecondText = ""Custom 2 Secondary"", Disabled = true },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
];";

    private readonly string example6RazorCode = @"
<BitTimeline Items=""basicCustoms"" NameSelectors=""nameSelectors"" Reversed />
<BitTimeline Items=""reversedCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Items=""basicCustoms"" NameSelectors=""nameSelectors"" Reversed />
<BitTimeline Horizontal Items=""reversedCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example6CsharpCode = @"
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
<BitTimeline Alternate Items=""twoSidedCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Alternate Reversed Items=""twoSidedCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Alternate Horizontal Items=""twoSidedCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example7CsharpCode = @"
private List<Event> twoSidedCustoms =
[
    new() { FirstText = ""09:00"", SecondText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""10:30"", SecondText = ""Custom 2"", Icon = BitIconName.Edit },
    new() { FirstText = ""13:15"", SecondText = ""Custom 3"", Icon = BitIconName.Delete },
    new() { FirstText = ""16:45"", SecondText = ""Custom 4"", Icon = BitIconName.Accept }
];";

    private readonly string example8RazorCode = @"
<BitTimeline ReverseOrder Items=""twoSidedCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline ReverseOrder Horizontal Items=""twoSidedCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example8CsharpCode = @"
private List<Event> twoSidedCustoms =
[
    new() { FirstText = ""09:00"", SecondText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""10:30"", SecondText = ""Custom 2"", Icon = BitIconName.Edit },
    new() { FirstText = ""13:15"", SecondText = ""Custom 3"", Icon = BitIconName.Delete },
    new() { FirstText = ""16:45"", SecondText = ""Custom 4"", Icon = BitIconName.Accept }
];";

    private readonly string example9RazorCode = @"
<BitTimeline TruncateLine=""BitTimelineTruncateLine.Both"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline TruncateLine=""BitTimelineTruncateLine.Start"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline TruncateLine=""BitTimelineTruncateLine.End"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal TruncateLine=""BitTimelineTruncateLine.Both"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example9CsharpCode = @"
private List<Event> basicCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"" }
];";

    private readonly string example10RazorCode = @"
<BitTimeline LineVariant=""BitTimelineLineVariant.Dashed"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline LineVariant=""BitTimelineLineVariant.Dotted"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Items=""lineVariantCustoms"" NameSelectors=""nameSelectors"" TruncateLine=""BitTimelineTruncateLine.Both"" />

<BitTimeline Horizontal LineVariant=""BitTimelineLineVariant.Dashed"" Items=""basicCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example10CsharpCode = @"
private BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    IconName = { Selector = i => i.Icon },
    Color = { Selector = i => i.DotColor },
    Variant = { Selector = i => i.DotVariant },
    LineVariant = { Selector = i => i.LineStyle },
};

private List<Event> basicCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"" }
];

private List<Event> lineVariantCustoms =
[
    new() { FirstText = ""Ordered"", Icon = BitIconName.Accept, DotColor = BitColor.Success },
    new() { FirstText = ""Shipped"", Icon = BitIconName.Accept, DotColor = BitColor.Success, LineStyle = BitTimelineLineVariant.Dashed },
    new() { FirstText = ""Delivered"", DotVariant = BitVariant.Outline, LineStyle = BitTimelineLineVariant.Dashed }
];";

    private readonly string example11RazorCode = @"
<BitTimeline Items=""customizedCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example11CsharpCode = @"
private BitTimelineNameSelectors<Event> nameSelectors = new()
{
    PrimaryText = { Selector = i => i.FirstText },
    IconName = { Selector = i => i.Icon },
    Color = { Selector = i => i.DotColor },
    Size = { Selector = i => i.DotSize },
    Variant = { Selector = i => i.DotVariant },
    HideDot = { Selector = i => i.NoDot },
};

private List<Event> customizedCustoms =
[
    new() { FirstText = ""Success"", Icon = BitIconName.Accept, DotColor = BitColor.Success },
    new() { FirstText = ""Warning"", Icon = BitIconName.Warning, DotColor = BitColor.Warning, DotVariant = BitVariant.Outline },
    new() { FirstText = ""Error"", Icon = BitIconName.ErrorBadge, DotColor = BitColor.Error, DotSize = BitSize.Large },
    new() { FirstText = ""No dot"", NoDot = true }
];";

    private readonly string example12RazorCode = @"
<style>
    .dot-template {
        z-index: 1;
        border-radius: 50%;
        background-color: tomato;
    }

    .template-content {
        gap: 1rem;
        display: flex;
        align-items: center;
    }

    .full-template {
        padding: 0.5rem 1rem;
        border-radius: 0.25rem;
        background-color: tomato;
    }
</style>


<BitTimeline Items=""templateItems"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Items=""templateItems"" NameSelectors=""nameSelectors"" />

<BitTimeline Items=""fullTemplateItems"" NameSelectors=""nameSelectors"" TruncateLine=""BitTimelineTruncateLine.Both"">
    <DotTemplate Context=""item"">
        <div class=""dot-template""><BitIcon IconName=""@BitIconName.CheckMark"" /></div>
    </DotTemplate>
</BitTimeline>";
    private readonly string example12CsharpCode = @"
private List<Event> templateItems =
[
    new()
    {
        FirstContent = (item => @<BitPersona PrimaryText=""Xafan Salina""
                                             Size=""@BitPersonaSize.Size32""
                                             Presence=""@BitPersonaPresence.Online""
                                             ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />),

        DotContent = (item => @<div class=""dot-template""><BitRingLoading CustomSize=""30"" Color=""BitColor.Tertiary"" /></div>),

        SecondContent = (item => @<div class=""template-content"">
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

        SecondContent = (item => @<div class=""template-content"">
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

        SecondContent = (item => @<div class=""template-content"">
                                      <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                      <BitLabel>Project Manager</BitLabel>
                                  </div>)
    },
];

private List<Event> fullTemplateItems =
[
    new() { FirstText = ""Ordered"", Content = (item => @<div class=""full-template"">@item.FirstText</div>) },
    new() { FirstText = ""Shipped"" },
    new() { FirstText = ""Delivered"" }
];";

    private readonly string example13RazorCode = @"
<BitTimeline Items=""clickCustoms"" NameSelectors=""nameSelectors""
             OnItemClick=""@(item => { clickedCustom = $""{item.FirstText} (OnItemClick)""; })"" />

<div>Clicked item: <b>@clickedCustom</b></div>";
    private readonly string example13CsharpCode = @"
private string? clickedCustom;
private List<Event> clickCustoms = [];

protected override void OnInitialized()
{
    clickCustoms =
    [
        new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
        new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit, OnSelect = HandleOnSelect },
        new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete, Disabled = true }
    ];

    base.OnInitialized();
}

private void HandleOnSelect(Event item)
{
    clickedCustom = $""{item.FirstText} (item's own OnClick)"";
    StateHasChanged();
}";

    private readonly string example14RazorCode = @"
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
<BitTimeline Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />


<div><b>Disabled</b>:</div>

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Primary"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Secondary"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Tertiary"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Info"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Success"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Warning"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.SevereWarning"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Error"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example14CsharpCode = @"
private List<Event> iconCustoms =
[
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit, SecondText = ""Custom 2 Secondary"", Disabled = true },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
];";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitTimeline Horizontal Items=""externalIconCustoms1"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Items=""externalIconCustoms2"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" />

<BitTimeline Horizontal Items=""externalIconCustoms3"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitTimeline Horizontal Items=""bootstrapIconCustoms1"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Items=""bootstrapIconCustoms2"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Outline"" />

<BitTimeline Horizontal Items=""bootstrapIconCustoms3"" NameSelectors=""nameSelectors"" Variant=""BitVariant.Text"" />";
    private readonly string example15CsharpCode = @"
private List<Event> externalIconCustoms1 =
[
    new() { FirstText = ""Custom 1"", ExternalIcon = ""fa-solid fa-plus"" },
    new() { FirstText = ""Custom 2"", ExternalIcon = ""fa-solid fa-pen"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"", ExternalIcon = ""fa-solid fa-trash"" }
];

private List<Event> externalIconCustoms2 =
[
    new() { FirstText = ""Custom 1"", ExternalIcon = BitIconInfo.Css(""fa-solid fa-plus"") },
    new() { FirstText = ""Custom 2"", ExternalIcon = BitIconInfo.Css(""fa-solid fa-pen""), SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"", ExternalIcon = BitIconInfo.Css(""fa-solid fa-trash"") }
];

private List<Event> externalIconCustoms3 =
[
    new() { FirstText = ""Custom 1"", ExternalIcon = BitIconInfo.Fa(""solid plus"") },
    new() { FirstText = ""Custom 2"", ExternalIcon = BitIconInfo.Fa(""solid pen""), SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"", ExternalIcon = BitIconInfo.Fa(""solid trash"") }
];

private List<Event> bootstrapIconCustoms1 =
[
    new() { FirstText = ""Custom 1"", ExternalIcon = ""bi bi-plus-lg"" },
    new() { FirstText = ""Custom 2"", ExternalIcon = ""bi bi-pencil"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"", ExternalIcon = ""bi bi-trash"" }
];

private List<Event> bootstrapIconCustoms2 =
[
    new() { FirstText = ""Custom 1"", ExternalIcon = BitIconInfo.Css(""bi bi-plus-lg"") },
    new() { FirstText = ""Custom 2"", ExternalIcon = BitIconInfo.Css(""bi bi-pencil""), SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"", ExternalIcon = BitIconInfo.Css(""bi bi-trash"") }
];

private List<Event> bootstrapIconCustoms3 =
[
    new() { FirstText = ""Custom 1"", ExternalIcon = BitIconInfo.Bi(""plus-lg"") },
    new() { FirstText = ""Custom 2"", ExternalIcon = BitIconInfo.Bi(""pencil""), SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"", ExternalIcon = BitIconInfo.Bi(""trash"") }
];";

    private readonly string example16RazorCode = @"
<BitTimeline Horizontal Size=""BitSize.Small"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Size=""BitSize.Medium"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Size=""BitSize.Large"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />
<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Text"" Items=""iconCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example16CsharpCode = @"
private List<Event> iconCustoms =
[
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit, SecondText = ""Custom 2 Secondary"", Disabled = true },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
];";

    private readonly string example17RazorCode = @"
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
    private readonly string example17CsharpCode = @"
private List<Event> basicCustoms =
[
    new() { FirstText = ""Custom 1"" },
    new() { FirstText = ""Custom 2"", SecondText = ""Custom 2 Secondary"" },
    new() { FirstText = ""Custom 3"" }
];

private List<Event> iconCustoms =
[
    new() { FirstText = ""Custom 1"", Icon = BitIconName.Add },
    new() { FirstText = ""Custom 2"", Icon = BitIconName.Edit, SecondText = ""Custom 2 Secondary"", Disabled = true },
    new() { FirstText = ""Custom 3"", Icon = BitIconName.Delete }
];

private List<Event> styleClassCustoms =
[
    new() { FirstText = ""Styled"", Style = ""color: dodgerblue;"", Icon = BitIconName.Brush },
    new() { FirstText = ""Classed"", Class = ""custom-item"", Icon = BitIconName.FormatPainter }
];";

    private readonly string example18RazorCode = @"
<BitTimeline Dir=""BitDir.Rtl"" Items=""basicRtlCustoms"" NameSelectors=""nameSelectors"" />

<BitTimeline Horizontal Dir=""BitDir.Rtl"" Items=""basicRtlCustoms"" NameSelectors=""nameSelectors"" />";
    private readonly string example18CsharpCode = @"
private List<Event> basicRtlCustoms =
[
    new() { FirstText = ""گزینه ۱"" },
    new() { FirstText = ""گزینه ۲"", SecondText = ""گزینه ۲ ثانویه"" },
    new() { FirstText = ""گزینه ۳"" }
];";
}

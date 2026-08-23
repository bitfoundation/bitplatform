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
<BitTimeline Alternate Items=""twoSidedItems"" />

<BitTimeline Alternate Reversed Items=""twoSidedItems"" />

<BitTimeline Alternate Horizontal Items=""twoSidedItems"" />";
    private readonly string example7CsharpCode = @"
private List<BitTimelineItem> twoSidedItems =
[
    new() { PrimaryText = ""09:00"", SecondaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""10:30"", SecondaryText = ""Item 2"", IconName = BitIconName.Edit },
    new() { PrimaryText = ""13:15"", SecondaryText = ""Item 3"", IconName = BitIconName.Delete },
    new() { PrimaryText = ""16:45"", SecondaryText = ""Item 4"", IconName = BitIconName.Accept }
];";

    private readonly string example8RazorCode = @"
<BitTimeline ReverseOrder Items=""twoSidedItems"" />

<BitTimeline ReverseOrder Horizontal Items=""twoSidedItems"" />";
    private readonly string example8CsharpCode = @"
private List<BitTimelineItem> twoSidedItems =
[
    new() { PrimaryText = ""09:00"", SecondaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""10:30"", SecondaryText = ""Item 2"", IconName = BitIconName.Edit },
    new() { PrimaryText = ""13:15"", SecondaryText = ""Item 3"", IconName = BitIconName.Delete },
    new() { PrimaryText = ""16:45"", SecondaryText = ""Item 4"", IconName = BitIconName.Accept }
];";

    private readonly string example9RazorCode = @"
<BitTimeline TruncateLine=""BitTimelineTruncateLine.Both"" Items=""basicItems"" />

<BitTimeline TruncateLine=""BitTimelineTruncateLine.Start"" Items=""basicItems"" />

<BitTimeline TruncateLine=""BitTimelineTruncateLine.End"" Items=""basicItems"" />

<BitTimeline Horizontal TruncateLine=""BitTimelineTruncateLine.Both"" Items=""basicItems"" />";
    private readonly string example9CsharpCode = @"
private List<BitTimelineItem> basicItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"" }
];";

    private readonly string example10RazorCode = @"
<BitTimeline LineVariant=""BitTimelineLineVariant.Dashed"" Items=""basicItems"" />

<BitTimeline LineVariant=""BitTimelineLineVariant.Dotted"" Items=""basicItems"" />

<BitTimeline Items=""lineVariantItems"" TruncateLine=""BitTimelineTruncateLine.Both"" />

<BitTimeline Horizontal LineVariant=""BitTimelineLineVariant.Dashed"" Items=""basicItems"" />";
    private readonly string example10CsharpCode = @"
private List<BitTimelineItem> basicItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"" }
];

private List<BitTimelineItem> lineVariantItems =
[
    new() { PrimaryText = ""Ordered"", IconName = BitIconName.Accept, Color = BitColor.Success },
    new() { PrimaryText = ""Shipped"", IconName = BitIconName.Accept, Color = BitColor.Success, LineVariant = BitTimelineLineVariant.Dashed },
    new() { PrimaryText = ""Delivered"", Variant = BitVariant.Outline, LineVariant = BitTimelineLineVariant.Dashed }
];";

    private readonly string example11RazorCode = @"
<BitTimeline Items=""customizedItems"" />";
    private readonly string example11CsharpCode = @"
private List<BitTimelineItem> customizedItems =
[
    new() { PrimaryText = ""Success"", IconName = BitIconName.Accept, Color = BitColor.Success },
    new() { PrimaryText = ""Warning"", IconName = BitIconName.Warning, Color = BitColor.Warning, Variant = BitVariant.Outline },
    new() { PrimaryText = ""Error"", IconName = BitIconName.ErrorBadge, Color = BitColor.Error, Size = BitSize.Large },
    new() { PrimaryText = ""No dot"", HideDot = true }
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


<BitTimeline Items=""templateItems"" />

<BitTimeline Items=""templateItems"" Horizontal />

<BitTimeline Items=""fullTemplateItems"" TruncateLine=""BitTimelineTruncateLine.Both"">
    <DotTemplate Context=""item"">
        <div class=""dot-template""><BitIcon IconName=""@BitIconName.CheckMark"" /></div>
    </DotTemplate>
</BitTimeline>";
    private readonly string example12CsharpCode = @"
private List<BitTimelineItem> templateItems =
[
    new()
    {
        PrimaryContent = (item => @<BitPersona PrimaryText=""Xafan Salina""
                                               Size=""@BitPersonaSize.Size32""
                                               Presence=""@BitPersonaPresence.Online""
                                               ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" />),

        DotTemplate = (item => @<div class=""dot-template""><BitRingLoading CustomSize=""30"" Color=""BitColor.Tertiary"" /></div>),

        SecondaryContent = (item => @<div class=""template-content"">
                                         <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                         <BitLabel>Software Engineer</BitLabel>
                                     </div>)
    },
    new()
    {
        PrimaryContent = (item => @<BitPersona PrimaryText=""Saleh Khafan""
                                               Size=""@BitPersonaSize.Size32""
                                               Presence=""@BitPersonaPresence.Online"" />),

        DotTemplate = (item => @<div class=""dot-template""><BitSpinnerLoading CustomSize=""30"" Color=""BitColor.Tertiary"" /></div>),

        SecondaryContent = (item => @<div class=""template-content"">
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
                                               ImageUrl=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-male.png"" />),

        DotTemplate = (item => @<div class=""dot-template""><BitRollerLoading CustomSize=""30"" Color=""BitColor.Tertiary"" /></div>),

        SecondaryContent = (item => @<div class=""template-content"">
                                         <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                                         <BitLabel>Project Manager</BitLabel>
                                     </div>)
    },
];

private List<BitTimelineItem> fullTemplateItems =
[
    new() { PrimaryText = ""Ordered"", Template = (item => @<div class=""full-template"">@item.PrimaryText</div>) },
    new() { PrimaryText = ""Shipped"" },
    new() { PrimaryText = ""Delivered"" }
];";

    private readonly string example13RazorCode = @"
<BitTimeline Items=""clickItems"" OnItemClick=""@(item => { clickedItem = $""{item.PrimaryText} (OnItemClick)""; })"" />

<div>Clicked item: <b>@clickedItem</b></div>";
    private readonly string example13CsharpCode = @"
private string? clickedItem;
private List<BitTimelineItem> clickItems = [];

protected override void OnInitialized()
{
    clickItems =
    [
        new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
        new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit, OnClick = HandleOnItemClick },
        new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete, IsEnabled = false }
    ];

    base.OnInitialized();
}

private void HandleOnItemClick(BitTimelineItem item)
{
    clickedItem = $""{item.PrimaryText} (item's own OnClick)"";
    StateHasChanged();
}";

    private readonly string example14RazorCode = @"
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
<BitTimeline Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""iconItems"" />


<div><b>Disabled</b>:</div>

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Primary"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Secondary"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Tertiary"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Info"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Info"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Info"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Success"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Success"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Success"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Warning"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Warning"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.SevereWarning"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Error"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Text"" Items=""iconItems"" />";
    private readonly string example14CsharpCode = @"
private List<BitTimelineItem> iconItems =
[
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit, SecondaryText = ""Item 2 Secondary"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
];";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitTimeline Horizontal Items=""externalIconItems1"" />

<BitTimeline Horizontal Items=""externalIconItems2"" Variant=""BitVariant.Outline"" />

<BitTimeline Horizontal Items=""externalIconItems3"" Variant=""BitVariant.Text"" />


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitTimeline Horizontal Items=""bootstrapIconItems1"" />

<BitTimeline Horizontal Items=""bootstrapIconItems2"" Variant=""BitVariant.Outline"" />

<BitTimeline Horizontal Items=""bootstrapIconItems3"" Variant=""BitVariant.Text"" />";
    private readonly string example15CsharpCode = @"
private List<BitTimelineItem> externalIconItems1 =
[
    new() { PrimaryText = ""Item 1"", Icon = ""fa-solid fa-plus"" },
    new() { PrimaryText = ""Item 2"", Icon = ""fa-solid fa-pen"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"", Icon = ""fa-solid fa-trash"" }
];

private List<BitTimelineItem> externalIconItems2 =
[
    new() { PrimaryText = ""Item 1"", Icon = BitIconInfo.Css(""fa-solid fa-plus"") },
    new() { PrimaryText = ""Item 2"", Icon = BitIconInfo.Css(""fa-solid fa-pen""), SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"", Icon = BitIconInfo.Css(""fa-solid fa-trash"") }
];

private List<BitTimelineItem> externalIconItems3 =
[
    new() { PrimaryText = ""Item 1"", Icon = BitIconInfo.Fa(""solid plus"") },
    new() { PrimaryText = ""Item 2"", Icon = BitIconInfo.Fa(""solid pen""), SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"", Icon = BitIconInfo.Fa(""solid trash"") }
];

private List<BitTimelineItem> bootstrapIconItems1 =
[
    new() { PrimaryText = ""Item 1"", Icon = ""bi bi-plus-lg"" },
    new() { PrimaryText = ""Item 2"", Icon = ""bi bi-pencil"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"", Icon = ""bi bi-trash"" }
];

private List<BitTimelineItem> bootstrapIconItems2 =
[
    new() { PrimaryText = ""Item 1"", Icon = BitIconInfo.Css(""bi bi-plus-lg"") },
    new() { PrimaryText = ""Item 2"", Icon = BitIconInfo.Css(""bi bi-pencil""), SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"", Icon = BitIconInfo.Css(""bi bi-trash"") }
];

private List<BitTimelineItem> bootstrapIconItems3 =
[
    new() { PrimaryText = ""Item 1"", Icon = BitIconInfo.Bi(""plus-lg"") },
    new() { PrimaryText = ""Item 2"", Icon = BitIconInfo.Bi(""pencil""), SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"", Icon = BitIconInfo.Bi(""trash"") }
];";

    private readonly string example16RazorCode = @"
<BitTimeline Horizontal Size=""BitSize.Small"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Size=""BitSize.Medium"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Text"" Items=""iconItems"" />

<BitTimeline Horizontal Size=""BitSize.Large"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Outline"" Items=""iconItems"" />
<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Text"" Items=""iconItems"" />";
    private readonly string example16CsharpCode = @"
private List<BitTimelineItem> iconItems =
[
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit, SecondaryText = ""Item 2 Secondary"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
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


<BitTimeline Style=""max-width: max-content; color: dodgerblue;"" Items=""basicItems"" />

<BitTimeline Horizontal Class=""custom-class"" Items=""basicItems"" />


<BitTimeline Items=""styleClassItems"" />


<BitTimeline Items=""iconItems""
             Styles=""@(new() { Icon = ""color: whitesmoke;"",
                               Dot = ""background-color: lightseagreen; border-color: mediumseagreen;"",
                               PrimaryText = ""color: lightseagreen; font-weight: bold;"" })"" />

<BitTimeline Items=""iconItems""
             Variant=""BitVariant.Outline""
             Classes=""@(new() { Dot = ""custom-dot"",
                                Icon = ""custom-icon"",
                                Item = ""custom-item-text"",
                                Divider = ""custom-divider"" })"" />";
    private readonly string example17CsharpCode = @"
private List<BitTimelineItem> basicItems =
[
    new() { PrimaryText = ""Item 1"" },
    new() { PrimaryText = ""Item 2"", SecondaryText = ""Item 2 Secondary"" },
    new() { PrimaryText = ""Item 3"" }
];

private List<BitTimelineItem> iconItems =
[
    new() { PrimaryText = ""Item 1"", IconName = BitIconName.Add },
    new() { PrimaryText = ""Item 2"", IconName = BitIconName.Edit, SecondaryText = ""Item 2 Secondary"", IsEnabled = false },
    new() { PrimaryText = ""Item 3"", IconName = BitIconName.Delete }
];

private List<BitTimelineItem> styleClassItems =
[
    new() { PrimaryText = ""Styled"", Style = ""color: dodgerblue;"", IconName = BitIconName.Brush },
    new() { PrimaryText = ""Classed"", Class = ""custom-item"", IconName = BitIconName.FormatPainter }
];";

    private readonly string example18RazorCode = @"
<BitTimeline Dir=""BitDir.Rtl"" Items=""basicRtlItems"" />

<BitTimeline Horizontal Dir=""BitDir.Rtl"" Items=""basicRtlItems"" />";
    private readonly string example18CsharpCode = @"
private List<BitTimelineItem> basicRtlItems =
[
    new() { PrimaryText = ""گزینه ۱"" },
    new() { PrimaryText = ""گزینه ۲"", SecondaryText = ""گزینه ۲ ثانویه"" },
    new() { PrimaryText = ""گزینه ۳"" }
];";
}

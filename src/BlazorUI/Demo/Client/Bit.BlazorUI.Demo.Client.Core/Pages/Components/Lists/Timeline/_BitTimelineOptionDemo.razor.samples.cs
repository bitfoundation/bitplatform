namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Lists.Timeline;

public partial class _BitTimelineOptionDemo
{
    private readonly string example1RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>";

    private readonly string example2RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"" Horizontal>
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>";

    private readonly string example3RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"" Horizontal IsEnabled=""false"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Horizontal>
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>";

    private readonly string example4RazorCode = @"
<BitTimeline Horizontal Variant=""BitVariant.Fill"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>";

    private readonly string example5RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"" Reversed>
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" Reversed />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Horizontal Reversed>
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Horizontal>
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" Reversed />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Alternate>
    <BitTimelineOption PrimaryText=""09:00"" SecondaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""10:30"" SecondaryText=""Option 2"" IconName=""@BitIconName.Edit"" />
    <BitTimelineOption PrimaryText=""13:15"" SecondaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
    <BitTimelineOption PrimaryText=""16:45"" SecondaryText=""Option 4"" IconName=""@BitIconName.Accept"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Alternate Reversed>
    <BitTimelineOption PrimaryText=""09:00"" SecondaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""10:30"" SecondaryText=""Option 2"" IconName=""@BitIconName.Edit"" />
    <BitTimelineOption PrimaryText=""13:15"" SecondaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
    <BitTimelineOption PrimaryText=""16:45"" SecondaryText=""Option 4"" IconName=""@BitIconName.Accept"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Alternate Horizontal>
    <BitTimelineOption PrimaryText=""09:00"" SecondaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""10:30"" SecondaryText=""Option 2"" IconName=""@BitIconName.Edit"" />
    <BitTimelineOption PrimaryText=""13:15"" SecondaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
    <BitTimelineOption PrimaryText=""16:45"" SecondaryText=""Option 4"" IconName=""@BitIconName.Accept"" />
</BitTimeline>";

    private readonly string example6RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"" ReverseOrder>
    <BitTimelineOption PrimaryText=""09:00"" SecondaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""10:30"" SecondaryText=""Option 2"" IconName=""@BitIconName.Edit"" />
    <BitTimelineOption PrimaryText=""13:15"" SecondaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
    <BitTimelineOption PrimaryText=""16:45"" SecondaryText=""Option 4"" IconName=""@BitIconName.Accept"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" ReverseOrder Horizontal>
    <BitTimelineOption PrimaryText=""09:00"" SecondaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""10:30"" SecondaryText=""Option 2"" IconName=""@BitIconName.Edit"" />
    <BitTimelineOption PrimaryText=""13:15"" SecondaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
    <BitTimelineOption PrimaryText=""16:45"" SecondaryText=""Option 4"" IconName=""@BitIconName.Accept"" />
</BitTimeline>";

    private readonly string example7RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"" TruncateLine=""BitTimelineTruncateLine.Both"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" TruncateLine=""BitTimelineTruncateLine.Start"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" TruncateLine=""BitTimelineTruncateLine.End"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" LineVariant=""BitTimelineLineVariant.Dashed"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" LineVariant=""BitTimelineLineVariant.Dotted"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" TruncateLine=""BitTimelineTruncateLine.Both"">
    <BitTimelineOption PrimaryText=""Ordered"" IconName=""@BitIconName.Accept"" Color=""BitColor.Success"" />
    <BitTimelineOption PrimaryText=""Shipped"" IconName=""@BitIconName.Accept"" Color=""BitColor.Success"" LineVariant=""BitTimelineLineVariant.Dashed"" />
    <BitTimelineOption PrimaryText=""Delivered"" Variant=""BitVariant.Outline"" LineVariant=""BitTimelineLineVariant.Dashed"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Horizontal LineVariant=""BitTimelineLineVariant.Dashed"" TruncateLine=""BitTimelineTruncateLine.Both"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>";

    private readonly string example8RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""09:00"" SecondaryText=""Kickoff: the scope, the milestones and an owner for each of them are agreed on."" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""11:30"" SecondaryText=""Design review: the proposal is walked through and the open questions are collected."" IconName=""@BitIconName.Edit"" />
    <BitTimelineOption PrimaryText=""15:00"" SecondaryText=""Sign-off: the plan is approved and the work is scheduled."" IconName=""@BitIconName.Accept"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" DotAlignment=""BitTimelineDotAlignment.Start"" TruncateLine=""BitTimelineTruncateLine.Both"">
    <BitTimelineOption PrimaryText=""09:00"" SecondaryText=""Kickoff: the scope, the milestones and an owner for each of them are agreed on."" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""11:30"" SecondaryText=""Design review: the proposal is walked through and the open questions are collected."" IconName=""@BitIconName.Edit"" />
    <BitTimelineOption PrimaryText=""15:00"" SecondaryText=""Sign-off: the plan is approved and the work is scheduled."" IconName=""@BitIconName.Accept"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" DotAlignment=""BitTimelineDotAlignment.End"">
    <BitTimelineOption PrimaryText=""09:00"" SecondaryText=""Kickoff: the scope, the milestones and an owner for each of them are agreed on."" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""11:30"" SecondaryText=""Design review: the proposal is walked through and the open questions are collected."" IconName=""@BitIconName.Edit"" />
    <BitTimelineOption PrimaryText=""15:00"" SecondaryText=""Sign-off: the plan is approved and the work is scheduled."" IconName=""@BitIconName.Accept"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Horizontal DotAlignment=""BitTimelineDotAlignment.Start"" TruncateLine=""BitTimelineTruncateLine.Both"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>";

    private readonly string example9RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Success"" IconName=""@BitIconName.Accept"" Color=""BitColor.Success"" />
    <BitTimelineOption PrimaryText=""Warning"" IconName=""@BitIconName.Warning"" Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" />
    <BitTimelineOption PrimaryText=""Error"" IconName=""@BitIconName.ErrorBadge"" Color=""BitColor.Error"" Size=""BitSize.Large"" />
    <BitTimelineOption PrimaryText=""No dot"" HideDot />
</BitTimeline>";

    private readonly string example10RazorCode = @"
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


<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption>
        <PrimaryContent>
            <BitPersona PrimaryText=""Xafan Salina""
                        Size=""@BitPersonaSize.Size32""
                        Presence=""@BitPersonaPresence.Online""
                        ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />
        </PrimaryContent>
        <DotTemplate>
            <div class=""dot-template"">
                <BitRingLoading CustomSize=""30"" Color=""BitColor.Tertiary"" />
            </div>
        </DotTemplate>
        <SecondaryContent>
            <div class=""template-content"">
                <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                <BitLabel>Software Engineer</BitLabel>
            </div>
        </SecondaryContent>
    </BitTimelineOption>
    <BitTimelineOption Reversed>
        <PrimaryContent>
            <BitPersona PrimaryText=""Saleh Khafan""
                        Size=""@BitPersonaSize.Size32""
                        Presence=""@BitPersonaPresence.Online"" />
        </PrimaryContent>
        <DotTemplate>
            <div class=""dot-template"">
                <BitSpinnerLoading CustomSize=""30"" Color=""BitColor.Tertiary"" />
            </div>
        </DotTemplate>
        <SecondaryContent>
            <div class=""template-content"">
                <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                <BitLabel>Co-Founder & CTO</BitLabel>
            </div>
        </SecondaryContent>
    </BitTimelineOption>
    <BitTimelineOption>
        <PrimaryContent>
            <BitPersona PrimaryText=""Ted Randall""
                        Size=""@BitPersonaSize.Size32""
                        Presence=""@BitPersonaPresence.Online""
                        ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-male.png"" />
        </PrimaryContent>
        <DotTemplate>
            <div class=""dot-template"">
                <BitRollerLoading CustomSize=""30"" Color=""BitColor.Tertiary"" />
            </div>
        </DotTemplate>
        <SecondaryContent>
            <div class=""template-content"">
                <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                <BitLabel>Project Manager</BitLabel>
            </div>
        </SecondaryContent>
    </BitTimelineOption>
</BitTimeline>

@* the same timeline is also shown with the Horizontal parameter *@


<BitTimeline TItem=""BitTimelineOption"" TruncateLine=""BitTimelineTruncateLine.Both"">
    <DotTemplate Context=""item"">
        <div class=""dot-template""><BitIcon IconName=""@BitIconName.CheckMark"" /></div>
    </DotTemplate>
    <Options>
        <BitTimelineOption PrimaryText=""Ordered"">
            <Template Context=""item"">
                <div class=""full-template"">@item.PrimaryText</div>
            </Template>
        </BitTimelineOption>
        <BitTimelineOption PrimaryText=""Shipped"" />
        <BitTimelineOption PrimaryText=""Delivered"" />
    </Options>
</BitTimeline>";

    private readonly string example11RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"" OnItemClick=""@(item => { clickedOption = $""{item.PrimaryText} (OnItemClick)""; })"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit""
                       OnClick=""@(item => { clickedOption = $""{item.PrimaryText} (option's own OnClick)""; })"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" IsEnabled=""false"" />
</BitTimeline>

<div>Clicked item: <b>@clickedOption</b></div>";
    private readonly string example11CsharpCode = @"
private string? clickedOption;";

    private readonly string example12RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"" AriaLabel=""Order history"" TruncateLine=""BitTimelineTruncateLine.Both"">
    <BitTimelineOption PrimaryText=""Ordered"" IconName=""@BitIconName.Accept"" Color=""BitColor.Success"" AriaLabel=""Ordered, done"" Title=""Done on 3 March"" />
    <BitTimelineOption PrimaryText=""Shipped"" IconName=""@BitIconName.Accept"" Color=""BitColor.Success"" LineVariant=""BitTimelineLineVariant.Dashed"" AriaLabel=""Shipped, done"" Title=""Done on 4 March"" />
    <BitTimelineOption PrimaryText=""Delivered"" Variant=""BitVariant.Outline"" LineVariant=""BitTimelineLineVariant.Dashed"" AriaLabel=""Delivered, pending"" Title=""Expected on 7 March"" />
</BitTimeline>";

    private readonly string example13RazorCode = @"
<BitParams Parameters=""timelineParams"">
    <BitTimeline TItem=""BitTimelineOption"">
        <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
        <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
        <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
    </BitTimeline>

    <BitTimeline TItem=""BitTimelineOption"" Color=""BitColor.Error"">
        <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
        <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
        <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
    </BitTimeline>
</BitParams>

<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>";
    private readonly string example13CsharpCode = @"
private readonly BitTimelineParams[] timelineParams =
[
    new()
    {
        Horizontal = true,
        Color = BitColor.Success,
        Variant = BitVariant.Outline,
        TruncateLine = BitTimelineTruncateLine.Both,
    }
];";

    private readonly string example14RazorCode = @"
<BitTimeline Horizontal Color=""BitColor.Primary"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

@* the same three timelines are repeated for Secondary, Tertiary, Info, Success, Warning, SevereWarning and Error *@


<div><b>Disabled</b>:</div>

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Primary"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline IsEnabled=""false"" Horizontal Color=""BitColor.Primary"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

@* the same three timelines are repeated for Secondary, Tertiary, Info, Success, Warning, SevereWarning and Error *@";

    private readonly string example15RazorCode = @"
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitTimeline Horizontal TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" Icon=""@(""fa-solid fa-plus"")"" />
    <BitTimelineOption PrimaryText=""Option 2"" Icon=""@(""fa-solid fa-pen"")"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" Icon=""@(""fa-solid fa-trash"")"" />
</BitTimeline>

<BitTimeline Horizontal Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" Icon=""@BitIconInfo.Css(""fa-solid fa-plus"")"" />
    <BitTimelineOption PrimaryText=""Option 2"" Icon=""@BitIconInfo.Css(""fa-solid fa-pen"")"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" Icon=""@BitIconInfo.Css(""fa-solid fa-trash"")"" />
</BitTimeline>

<BitTimeline Horizontal Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" Icon=""@BitIconInfo.Fa(""solid plus"")"" />
    <BitTimelineOption PrimaryText=""Option 2"" Icon=""@BitIconInfo.Fa(""solid pen"")"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" Icon=""@BitIconInfo.Fa(""solid trash"")"" />
</BitTimeline>


<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitTimeline Horizontal TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" Icon=""@(""bi bi-plus-lg"")"" />
    <BitTimelineOption PrimaryText=""Option 2"" Icon=""@(""bi bi-pencil"")"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" Icon=""@(""bi bi-trash"")"" />
</BitTimeline>

<BitTimeline Horizontal Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" Icon=""@BitIconInfo.Css(""bi bi-plus-lg"")"" />
    <BitTimelineOption PrimaryText=""Option 2"" Icon=""@BitIconInfo.Css(""bi bi-pencil"")"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" Icon=""@BitIconInfo.Css(""bi bi-trash"")"" />
</BitTimeline>

<BitTimeline Horizontal Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" Icon=""@BitIconInfo.Bi(""plus-lg"")"" />
    <BitTimelineOption PrimaryText=""Option 2"" Icon=""@BitIconInfo.Bi(""pencil"")"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" Icon=""@BitIconInfo.Bi(""trash"")"" />
</BitTimeline>";

    private readonly string example16RazorCode = @"
<BitTimeline Horizontal Size=""BitSize.Small"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Size=""BitSize.Medium"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Size=""BitSize.Large"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

@* each size is also shown with the Outline and the Text variants *@";

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

    .custom-divider {
        --bit-Timeline-line-color: blueviolet;
    }

    .custom-item-text {
        color: blueviolet;
    }
</style>


<BitTimeline Style=""max-width: max-content; color: dodgerblue;"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline Horizontal Class=""custom-class"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>


<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Styled"" IconName=""@BitIconName.Brush"" Style=""color: dodgerblue;"" />
    <BitTimelineOption PrimaryText=""Classed"" IconName=""@BitIconName.FormatPainter"" Class=""custom-item"" />
</BitTimeline>


<BitTimeline TItem=""BitTimelineOption""
             Styles=""@(new() { Icon = ""color: whitesmoke;"",
                               Dot = ""background-color: lightseagreen; border-color: mediumseagreen;"",
                               PrimaryText = ""color: lightseagreen; font-weight: bold;"" })"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption""
             Variant=""BitVariant.Outline""
             Classes=""@(new() { Dot = ""custom-dot"",
                                Icon = ""custom-icon"",
                                Item = ""custom-item-text"",
                                Divider = ""custom-divider"" })"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>


<BitTimeline TItem=""BitTimelineOption"" TruncateLine=""BitTimelineTruncateLine.Both""
             Style=""--bit-Timeline-line-color: #8b5cf6; --bit-Timeline-line-width: 3px; --bit-Timeline-dot-size: 2rem; --bit-Timeline-dot-radius: 0.5rem; --bit-Timeline-dot-background: #ede9fe; --bit-Timeline-dot-border-color: #8b5cf6; --bit-Timeline-icon-color: #6d28d9;"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<div style=""--bit-Timeline-item-spacing: 1.25rem; --bit-Timeline-content-gap: 1.5rem; --bit-Timeline-dot-shadow: 0 0 0 0.25rem var(--bit-clr-bg-sec);"">
    <BitTimeline TItem=""BitTimelineOption"" TruncateLine=""BitTimelineTruncateLine.Both"">
        <BitTimelineOption PrimaryText=""Ordered"" IconName=""@BitIconName.Accept"" />
        <BitTimelineOption PrimaryText=""Shipped"" IconName=""@BitIconName.Accept"" Style=""--bit-Timeline-dot-background: gold; --bit-Timeline-dot-border-color: goldenrod; --bit-Timeline-icon-color: black;"" />
        <BitTimelineOption PrimaryText=""Delivered"" Variant=""BitVariant.Outline"" />
    </BitTimeline>
</div>";

    private readonly string example18RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"" Dir=""BitDir.Rtl"">
    <BitTimelineOption PrimaryText=""گزینه ۱"" />
    <BitTimelineOption PrimaryText=""گزینه ۲"" SecondaryText=""گزینه ۲ ثانویه"" />
    <BitTimelineOption PrimaryText=""گزینه ۳"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Horizontal Dir=""BitDir.Rtl"">
    <BitTimelineOption PrimaryText=""گزینه ۱"" />
    <BitTimelineOption PrimaryText=""گزینه ۲"" SecondaryText=""گزینه ۲ ثانویه"" />
    <BitTimelineOption PrimaryText=""گزینه ۳"" />
</BitTimeline>";
}

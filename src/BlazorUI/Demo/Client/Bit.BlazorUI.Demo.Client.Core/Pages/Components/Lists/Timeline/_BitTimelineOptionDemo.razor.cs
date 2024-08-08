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
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline Horizontal Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline Horizontal Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>";

    private readonly string example5RazorCode = @"
<BitTimeline Horizontal Variant=""BitVariant.Fill"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
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

    private readonly string example6RazorCode = @"
<BitTimeline TItem=""BitTimelineOption"" Reversed>
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" Reversed />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Horizontal Reversed>
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline TItem=""BitTimelineOption"" Horizontal>
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" Reversed />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>";

    private readonly string example7RazorCode = @"
<style>
    .dot-template {
        z-index: 1;
        border-radius: 50%;
        background-color: tomato;
    }
</style>


<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption>
        <PrimaryContent>
            <BitPersona PrimaryText=""Annie Lindqvist""
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
            <div style=""display: flex; align-items: center; gap: 1rem;"">
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
            <div style=""display: flex; align-items: center; gap: 1rem;"">
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
            <div style=""display: flex; align-items: center; gap: 1rem;"">
                <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                <BitLabel>Project Manager</BitLabel>
            </div>
        </SecondaryContent>
    </BitTimelineOption>
</BitTimeline>


<BitTimeline TItem=""BitTimelineOption"" Horizontal>
    <BitTimelineOption>
        <PrimaryContent>
            <BitPersona PrimaryText=""Annie Lindqvist""
                        Size=""@BitPersonaSize.Size32""
                        Presence=""@BitPersonaPresence.Online""
                        ImageUrl=""https://static2.sharepointonline.com/files/fabric/office-ui-fabric-react-assets/persona-female.png"" />
        </PrimaryContent>
        <DotTemplate>
            <div class=""dot-template"">
                <BitRingLoading Size=""30"" />
            </div>
        </DotTemplate>
        <SecondaryContent>
            <div style=""display: flex; align-items: center; gap: 1rem;"">
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
                <BitSpinnerLoading Size=""30"" />
            </div>
        </DotTemplate>
        <SecondaryContent>
            <div style=""display: flex; align-items: center; gap: 1rem;"">
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
                <BitRollerLoading Size=""30"" />
            </div>
        </DotTemplate>
        <SecondaryContent>
            <div style=""display: flex; align-items: center; gap: 1rem;"">
                <BitIcon IconName=""Accept"" Style=""color: limegreen;"" />
                <BitLabel>Project Manager</BitLabel>
            </div>
        </SecondaryContent>
    </BitTimelineOption>
</BitTimeline>";

    private readonly string example8RazorCode = @"
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


<BitTimeline Horizontal Color=""BitColor.Secondary"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Secondary"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Secondary"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>


<BitTimeline Horizontal Color=""BitColor.Tertiary"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Tertiary"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Tertiary"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>


<BitTimeline Horizontal Color=""BitColor.Info"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Info"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Info"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>


<BitTimeline Horizontal Color=""BitColor.Success"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Success"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Success"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>


<BitTimeline Horizontal Color=""BitColor.Warning"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Warning"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Warning"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>


<BitTimeline Horizontal Color=""BitColor.SevereWarning"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.SevereWarning"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>


<BitTimeline Horizontal Color=""BitColor.Error"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Color=""BitColor.Error"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>";

    private readonly string example9RazorCode = @"
<BitTimeline Horizontal Size=""BitSize.Small"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Size=""BitSize.Small"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>


<BitTimeline Horizontal Size=""BitSize.Medium"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Size=""BitSize.Medium"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>


<BitTimeline Horizontal Size=""BitSize.Large"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Outline"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>

<BitTimeline Horizontal Size=""BitSize.Large"" Variant=""BitVariant.Text"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
    <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
</BitTimeline>";

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


<BitTimeline Style=""background-color: tomato; box-shadow: red 0 0 1rem;"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>

<BitTimeline Horizontal Class=""custom-class"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Option 1"" />
    <BitTimelineOption PrimaryText=""Option 2"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
    <BitTimelineOption PrimaryText=""Option 3"" />
</BitTimeline>


<BitTimeline TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""Styled"" IconName=""@BitIconName.Brush"" Style=""color: darkred;"" />
    <BitTimelineOption PrimaryText=""Classed"" IconName=""@BitIconName.FormatPainter"" Class=""custom-item"" />
</BitTimeline>


<div class=""example-content"">
    <BitTimeline TItem=""BitTimelineOption""
                 Styles=""@(new() { Icon = ""color: red;"",
                                   PrimaryText = ""color: aqua; font-size: 1.5rem;"",
                                   Dot = ""background-color: dodgerblue;"" })"">
        <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
        <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
        <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
    </BitTimeline>

    <BitTimeline TItem=""BitTimelineOption""
                 Variant=""BitVariant.Outline""
                 Classes=""@(new() { Icon = ""custom-icon"",
                                    Divider = ""custom-divider"",
                                    PrimaryText = ""custom-text"" })"">
        <BitTimelineOption PrimaryText=""Option 1"" IconName=""@BitIconName.Add"" />
        <BitTimelineOption PrimaryText=""Option 2"" IconName=""@BitIconName.Edit"" SecondaryText=""Option 2 Secondary"" IsEnabled=""false"" />
        <BitTimelineOption PrimaryText=""Option 3"" IconName=""@BitIconName.Delete"" />
    </BitTimeline>";

    private readonly string example11RazorCode = @"
<BitTimeline Dir=""BitDir.Rtl"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""گزینه ۱"" />
    <BitTimelineOption PrimaryText=""گزینه ۲"" SecondaryText=""گزینه ۲ ثانویه"" />
    <BitTimelineOption PrimaryText=""گزینه ۳"" />
</BitTimeline>

<BitTimeline Horizontal Dir=""BitDir.Rtl"" TItem=""BitTimelineOption"">
    <BitTimelineOption PrimaryText=""گزینه ۱"" />
    <BitTimelineOption PrimaryText=""گزینه ۲"" SecondaryText=""گزینه ۲ ثانویه"" />
    <BitTimelineOption PrimaryText=""گزینه ۳"" />
</BitTimeline>";
}

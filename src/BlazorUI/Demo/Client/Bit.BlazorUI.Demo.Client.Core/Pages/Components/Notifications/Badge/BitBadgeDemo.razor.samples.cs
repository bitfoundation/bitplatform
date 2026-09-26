namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Notifications.Badge;

public partial class BitBadgeDemo
{
    private readonly string example1RazorCode = @"
<BitBadge Content=""63"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example2RazorCode = @"
<BitBadge Content=""84"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Variant=""BitVariant.Text"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<BitBadge Content=""84"" Variant=""BitVariant.Fill"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Variant=""BitVariant.Outline"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Variant=""BitVariant.Text"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<BitBadge Content=""@(""New"")"" Shape=""BitBadgeShape.Circular"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""@(""New"")"" Shape=""BitBadgeShape.Rounded"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""@(""New"")"" Shape=""BitBadgeShape.Square"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example3RazorCode = @"
<BitBadge Content=""@(""Text"")"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge IconName=""@BitIconName.Ringer"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""@(""Sent"")"" IconName=""@BitIconName.CheckMark"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""@(""Sent"")"" IconName=""@BitIconName.CheckMark"" Reversed>
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge>
    <ContentTemplate>
        <b>99</b><span style=""opacity:0.75"">%</span>
    </ContentTemplate>
    <ChildContent>
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </ChildContent>
</BitBadge>";

    private readonly string example4RazorCode = @"
<BitBadge Max=""63"" Content=""60"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Max=""63"" Content=""100"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Max=""99"" Content=""12345L"" Title=""12345 unread messages"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<BitBadge Content=""count"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""count"" ShowZero=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""count"" Hidden=""hidden"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => count--"" IsEnabled=""@(count > 0)"">Remove one</BitButton>
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => count++"">Add one</BitButton>
<BitToggle @bind-Value=""hidden"" Label=""Hidden"" Inline />";
    private readonly string example4CsharpCode = @"
private bool hidden;
private int count = 3;";

    private readonly string example5RazorCode = @"
<BitBadge Dot Size=""BitSize.Small"" Description=""New mail"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Dot Size=""BitSize.Medium"" Description=""New mail"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Dot Size=""BitSize.Large"" Description=""New mail"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example6RazorCode = @"
<BitBadge Content=""63"" Position=""badgePosition"">
    <BitButton Variant=""BitVariant.Outline"">Position</BitButton>
</BitBadge>

<BitDropdown Items=""badgePositionList"" @bind-Value=""badgePosition"" Style=""width: 8rem;"" />";
    private readonly string example6CsharpCode = @"
private BitPosition badgePosition;

private readonly List<BitDropdownItem<BitPosition>> badgePositionList = Enum.GetValues<BitPosition>()
    .Select(enumValue => new BitDropdownItem<BitPosition>
    {
        Value = enumValue,
        Text = enumValue.ToString()
    })
    .ToList();";

    private readonly string example7RazorCode = @"
<BitBadge Content=""8"">
    <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" Width=""4rem"" Style=""border-radius:50%"" Alt=""Avatar"" />
</BitBadge>
<BitBadge Content=""8"" Overlap>
    <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" Width=""4rem"" Style=""border-radius:50%"" Alt=""Avatar"" />
</BitBadge>
<BitBadge Content=""8"" OffsetX=""-0.5rem"" OffsetY=""0.5rem"">
    <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" Width=""4rem"" Style=""border-radius:50%"" Alt=""Avatar"" />
</BitBadge>";

    private readonly string example8RazorCode = @"
<BitBadge Dot Position=""BitPosition.BottomEnd"" Overlap Description=""Online"">
    <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" Width=""4rem"" Style=""border-radius:50%"" Alt=""Avatar"" />
</BitBadge>
<BitBadge Dot Bordered Position=""BitPosition.BottomEnd"" Overlap Description=""Online"">
    <BitImage Src=""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" Width=""4rem"" Style=""border-radius:50%"" Alt=""Avatar"" />
</BitBadge>
<BitBadge Dot Pulse Description=""Syncing"">
    <BitIcon IconName=""@BitIconName.Cloud"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""3"" Pulse Bordered>
    <BitIcon IconName=""@BitIconName.Ringer"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example9RazorCode = @"
<BitBadge Content=""@(""Draft"")"" />
<BitBadge Content=""@(""Beta"")"" IconName=""@BitIconName.TestBeaker"" Shape=""BitBadgeShape.Rounded"" Variant=""BitVariant.Outline"" />
<BitBadge Dot Description=""Degraded"" />


<BitBadge Inline Content=""24"">
    <BitText Typography=""BitTypography.Body1"">Inbox</BitText>
</BitBadge>
<BitBadge Inline Dot Position=""BitPosition.CenterStart"" Description=""Operational"">
    <BitText Typography=""BitTypography.Body1"">Build server</BitText>
</BitBadge>";

    private readonly string example10RazorCode = @"
<BitBadge Content=""counter"" OnClick=""() => counter++"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""counter"" OnClick=""() => counter++"" IsEnabled=""false"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""@(""Docs"")"" Variant=""BitVariant.Outline"" Href=""https://blazorui.bitplatform.dev"" Target=""_blank"" />
<BitBadge Content=""@(""Source"")"" IconName=""@BitIconName.OpenInNewWindow""
          Href=""https://github.com/bitfoundation/bitplatform"" Target=""_blank"" Rel=""BitLinkRels.NoFollow | BitLinkRels.NoReferrer"" />";
    private readonly string example10CsharpCode = @"
private int counter;";

    private readonly string example11RazorCode = @"
<BitBadge Content=""unread"" Description=""@($""{unread} unread messages"")"" Live>
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Dot Description=""Online"">
    <BitIcon IconName=""@BitIconName.Contact"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Dot Href=""#example11"" AriaLabel=""Alerts"" Description=""3 unread"">
    <BitIcon IconName=""@BitIconName.Ringer"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitButton Variant=""BitVariant.Outline"" OnClick=""() => unread++"">Receive a message</BitButton>";
    private readonly string example11CsharpCode = @"
private int unread = 3;";

    private readonly string example12RazorCode = @"
<BitBadge Content=""@(""Tint"")"" Style=""--bit-Badge-background: color-mix(in srgb, var(--bit-clr-pri) 16%, transparent); --bit-Badge-color: var(--bit-clr-pri);"" />
<BitBadge Content=""@(""Custom"")"" Style=""--bit-Badge-background: rebeccapurple; --bit-Badge-color: white;"" />
<BitBadge Content=""@(""Tall"")"" Style=""--bit-Badge-height: 2rem; --bit-Badge-padding: 0 1rem; --bit-Badge-radius: 0.5rem; --bit-Badge-font-weight: 400;"" />
<BitBadge Dot Pulse Description=""Recording"" Style=""--bit-Badge-dot-size: 0.75rem; --bit-Badge-background: crimson; --bit-Badge-pulse-color: crimson;"" />


<div style=""--bit-Badge-inset: 0px; --bit-Badge-height: 1rem; --bit-Badge-font-size: 0.625rem; --bit-Badge-padding: 0 0.25rem;"">
    <BitBadge Content=""4"">
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </BitBadge>
    <BitBadge Content=""12"">
        <BitIcon IconName=""@BitIconName.Ringer"" Color=""BitColor.Tertiary"" />
    </BitBadge>
    <BitBadge Content=""99"" Max=""9"">
        <BitIcon IconName=""@BitIconName.ShoppingCart"" Color=""BitColor.Tertiary"" />
    </BitBadge>
</div>";

    private readonly string example13RazorCode = @"
<BitParams Parameters=""@badgeParams"">
    <BitBadge Content=""4"">
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </BitBadge>
    <BitBadge Content=""120"">
        <BitIcon IconName=""@BitIconName.Ringer"" Color=""BitColor.Tertiary"" />
    </BitBadge>
    <BitBadge Content=""7"" Color=""BitColor.Error"">
        <BitIcon IconName=""@BitIconName.ShoppingCart"" Color=""BitColor.Tertiary"" />
    </BitBadge>
</BitParams>";
    private readonly string example13CsharpCode = @"
private readonly BitBadgeParams[] badgeParams =
[
    new()
    {
        Max = 99,
        Overlap = true,
        Bordered = true,
        Size = BitSize.Small,
        Color = BitColor.Success,
    }
];";

    private readonly string example14RazorCode = @"
@foreach (var color in semanticColors)
{
    <BitBadge Content=""84"" Color=""color"">
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </BitBadge>
    <BitBadge Content=""84"" Color=""color"" Variant=""BitVariant.Outline"">
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </BitBadge>
    <BitBadge Content=""84"" Color=""color"" Variant=""BitVariant.Text"">
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </BitBadge>
}


<BitBadge Content=""84"" Color=""BitColor.PrimaryBackground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SecondaryBackground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.TertiaryBackground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<BitBadge Content=""84"" Color=""BitColor.PrimaryForeground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SecondaryForeground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.TertiaryForeground"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<BitBadge Content=""84"" Color=""BitColor.PrimaryBorder"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.SecondaryBorder"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Color=""BitColor.TertiaryBorder"" Variant=""BitVariant.Outline"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";
    private readonly string example14CsharpCode = @"
private readonly BitColor[] semanticColors =
[
    BitColor.Primary,
    BitColor.Secondary,
    BitColor.Tertiary,
    BitColor.Info,
    BitColor.Success,
    BitColor.Warning,
    BitColor.SevereWarning,
    BitColor.Error,
];";

    private readonly string example15RazorCode = @"
<!-- FontAwesome -->
<link rel=""stylesheet"" href=""https://cdnjs.cloudflare.com/ajax/libs/font-awesome/7.0.1/css/all.min.css"" />

<BitBadge Content=""4"" Icon=""@BitIconInfo.Css(""fa-solid fa-heart"")"" Variant=""BitVariant.Fill"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""63"" Icon=""@BitIconInfo.Fa(""solid bell"")"" Variant=""BitVariant.Outline"" Color=""BitColor.Secondary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<!-- Bootstrap Icons -->
<link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css"" />

<BitBadge Content=""3"" Icon=""@BitIconInfo.Css(""bi bi-heart-fill"")"" Variant=""BitVariant.Fill"" Color=""BitColor.Error"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Icon=""@BitIconInfo.Bi(""gear-fill"")"" Variant=""BitVariant.Text"" Color=""BitColor.Tertiary"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example16RazorCode = @"
<BitBadge Content=""84"" Size=""BitSize.Small"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Medium"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Size=""BitSize.Large"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>


<BitBadge Content=""@(""Small"")"" Size=""BitSize.Small"" Variant=""BitVariant.Outline"" />
<BitBadge Content=""@(""Medium"")"" Size=""BitSize.Medium"" Variant=""BitVariant.Outline"" />
<BitBadge Content=""@(""Large"")"" Size=""BitSize.Large"" Variant=""BitVariant.Outline"" />";

    private readonly string example17RazorCode = @"
<style>
    .custom-class {
        border-radius: 1rem;
        box-shadow: aqua 0 0 0.5rem;
    }

    .custom-class div {
        padding: 0.5rem;
        color: blueviolet;
    }

    .custom-root {
        margin-left: 2rem;
        text-shadow: aqua 0 0 0.5rem;
    }

    .custom-wrapper {
        padding: 1rem;
    }

    .custom-badge {
        border-end-end-radius: 0.5rem;
        border-start-end-radius: unset;
        border-end-start-radius: unset;
        border-start-start-radius: 0.5rem;
    }

    .custom-icon {
        color: dodgerblue;
    }

    .custom-content {
        font-style: italic;
    }
</style>


<BitBadge Content=""84"" Style=""color: dodgerblue;"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>
<BitBadge Content=""84"" Class=""custom-class"" Variant=""BitVariant.Outline"">
    <div>Anchor</div>
</BitBadge>


<BitBadge Content=""84"" IconName=""@BitIconName.Info""
          Styles=""@(new() { Root = ""color: tomato;"",
                            Badge = ""border-radius: unset;"",
                            Icon = ""color: tomato;"" })"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>

<BitBadge Content=""84"" IconName=""@BitIconName.Info""
          Variant=""BitVariant.Outline""
          Classes=""@(new() { Root = ""custom-root"",
                             BadgeWrapper = ""custom-wrapper"",
                             Badge = ""custom-badge"",
                             Icon = ""custom-icon"",
                             Content = ""custom-content"" })"">
    <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
</BitBadge>";

    private readonly string example18RazorCode = @"
<div dir=""rtl"">
    <BitBadge Dir=""BitDir.Rtl"" Content=""63"" Position=""BitPosition.TopEnd"">
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </BitBadge>

    <BitBadge Dir=""BitDir.Rtl"" Content=""63"" Position=""BitPosition.TopStart"">
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </BitBadge>

    <BitBadge Dir=""BitDir.Rtl"" Content=""@(""جدید"")"" IconName=""@BitIconName.CheckMark"" Color=""BitColor.Success"" Position=""BitPosition.BottomEnd"">
        <BitIcon IconName=""@BitIconName.Mail"" Color=""BitColor.Tertiary"" />
    </BitBadge>

    <BitBadge Dir=""BitDir.Rtl"" Content=""@(""پیش‌نویس"")"" Color=""BitColor.Tertiary"" />
</div>";
}

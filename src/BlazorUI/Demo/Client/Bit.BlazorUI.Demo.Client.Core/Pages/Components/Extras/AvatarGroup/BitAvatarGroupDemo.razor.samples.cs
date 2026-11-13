namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.AvatarGroup;

public partial class BitAvatarGroupDemo
{
    private const string PeopleCode = @"
private static readonly List<BitAvatarGroupItem> people =
[
    new() { Key = ""annie"", Text = ""Annie Lindqvist"", ImageUrl = ""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" },
    new() { Key = ""aaron"", Text = ""Aaron Reid"" },
    new() { Key = ""alex"", Text = ""Alex Lundberg"" },
    new() { Key = ""roko"", Text = ""Roko Kolar"" },
    new() { Key = ""christian"", Text = ""Christian Bergqvist"" },
    new() { Key = ""valentina"", Text = ""Valentina Lovric"" },
    new() { Key = ""maor"", Text = ""Maor Sharett"" },
];";

    private readonly string example1RazorCode = @"
<BitAvatarGroup Items=""people"" AriaLabel=""Project team"" />";
    private readonly string example1CsharpCode = PeopleCode;

    private readonly string example2RazorCode = @"
<BitAvatarGroup Items=""people"" MaxDisplayed=""3"" AriaLabel=""Project team"" />

<BitAvatarGroup Items=""people"" MaxDisplayed=""4"" OverflowAriaLabelFormat=""and {0} other people: {1}"" AriaLabel=""Project team"" />";
    private readonly string example2CsharpCode = PeopleCode;

    private readonly string example3RazorCode = @"
<BitAvatarGroup Items=""presentPeople"" Size=""BitPersonaSize.Size40"" AriaLabel=""Online now"" />";
    private readonly string example3CsharpCode = @"
private static readonly List<BitAvatarGroupItem> presentPeople =
[
    new() { Text = ""Annie Lindqvist"", ImageUrl = ""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"", Presence = BitPersonaPresence.Online },
    new() { Text = ""Aaron Reid"", Presence = BitPersonaPresence.Away },
    new() { Text = ""Alex Lundberg"", Presence = BitPersonaPresence.Busy },
    new() { Text = ""Roko Kolar"", Presence = BitPersonaPresence.Offline },
];";

    private readonly string example4RazorCode = @"
<BitAvatarGroup Items=""people"" MaxDisplayed=""4"" Spread AriaLabel=""Project team"" />";
    private readonly string example4CsharpCode = PeopleCode;

    private readonly string example5RazorCode = @"
<BitAvatarGroup Items=""people""
                MaxDisplayed=""3""
                AriaLabel=""Project team""
                OnItemClick=""(BitAvatarGroupItem item) => clickedPerson = item.Text""
                OnOverflowClick=""HandleOverflowClick"" />

<div>Clicked: <b>@clickedPerson</b></div>
<div>Hidden: <b>@hiddenPeople</b></div>";
    private readonly string example5CsharpCode = @"
private string? clickedPerson;
private string? hiddenPeople;

private void HandleOverflowClick(IReadOnlyList<BitAvatarGroupItem> hidden)
{
    hiddenPeople = string.Join("", "", hidden.Select(p => p.Text));
}
" + PeopleCode;

    private readonly string example6RazorCode = @"
<BitAvatarGroup Items=""linkedPeople"" AriaLabel=""Reviewers"" />";
    private readonly string example6CsharpCode = @"
private static readonly List<BitAvatarGroupItem> linkedPeople =
[
    new() { Text = ""Annie Lindqvist"", ImageUrl = ""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"", Href = ""/components/persona"" },
    new() { Text = ""Aaron Reid"", Href = ""/components/persona"" },
    new() { Text = ""Alex Lundberg"", Href = ""/components/persona"", Target = ""_blank"" },
];";

    private readonly string example7RazorCode = @"
<BitAvatarGroup Items=""people"" MaxDisplayed=""3"" Disabled AriaLabel=""Project team""
                OnItemClick=""(BitAvatarGroupItem _) => { }"" />

<BitAvatarGroup Items=""partlyDisabledPeople"" AriaLabel=""Project team""
                OnItemClick=""(BitAvatarGroupItem _) => { }"" />";
    private readonly string example7CsharpCode = @"
private static readonly List<BitAvatarGroupItem> partlyDisabledPeople =
[
    new() { Text = ""Annie Lindqvist"", ImageUrl = ""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" },
    new() { Text = ""Aaron Reid"", IsDisabled = true },
    new() { Text = ""Alex Lundberg"" },
];
" + PeopleCode;

    private readonly string example8RazorCode = @"
<BitAvatarGroup Items=""coloredPeople"" Size=""BitPersonaSize.Size40"" AriaLabel=""Project team"" />";
    private readonly string example8CsharpCode = @"
private static readonly List<BitAvatarGroupItem> coloredPeople =
[
    new() { Text = ""Aaron Reid"", CoinColor = BitColor.Primary },
    new() { Text = ""Alex Lundberg"", CoinColor = BitColor.Success },
    new() { Text = ""Roko Kolar"", CoinColor = BitColor.Warning },
    new() { Text = ""Christian Bergqvist"", CoinColor = BitColor.Error },
    new() { Text = ""Valentina Lovric"" },
];";

    private readonly string example9RazorCode = @"
<BitAvatarGroup Items=""people"" MaxDisplayed=""3"" Size=""BitPersonaSize.Size24"" AriaLabel=""Project team"" />
<BitAvatarGroup Items=""people"" MaxDisplayed=""3"" Size=""BitPersonaSize.Size32"" AriaLabel=""Project team"" />
<BitAvatarGroup Items=""people"" MaxDisplayed=""3"" Size=""BitPersonaSize.Size48"" AriaLabel=""Project team"" />
<BitAvatarGroup Items=""people"" MaxDisplayed=""3"" Size=""BitPersonaSize.Size72"" AriaLabel=""Project team"" />";
    private readonly string example9CsharpCode = PeopleCode;

    private readonly string example10RazorCode = @"
<style>
    .custom-group {
        padding: 0.5rem;
        border-radius: 2rem;
        background-color: var(--bit-clr-bg-ter);
        --bit-AvatarGroup-ring-color: var(--bit-clr-bg-ter);
    }

    .custom-overflow {
        --bit-Persona-coin-radius: 0.5rem;
    }
</style>

<BitAvatarGroup Items=""people""
                MaxDisplayed=""4""
                AriaLabel=""Project team""
                Style=""--bit-AvatarGroup-overlap: 0.4; --bit-AvatarGroup-overflow-background: var(--bit-clr-pri); --bit-AvatarGroup-overflow-color: var(--bit-clr-pri-text);"" />

<BitAvatarGroup Items=""people""
                MaxDisplayed=""4""
                AriaLabel=""Project team""
                Classes=""@(new() { Root = ""custom-group"", Overflow = ""custom-overflow"" })"" />";
    private readonly string example10CsharpCode = PeopleCode;

    private readonly string example11RazorCode = @"
<div dir=""rtl"">
    <BitAvatarGroup Dir=""BitDir.Rtl"" Items=""rtlPeople"" MaxDisplayed=""3"" AriaLabel=""اعضای تیم"" OverflowAriaLabelFormat=""{0} نفر دیگر: {1}"" />
</div>";
    private readonly string example11CsharpCode = @"
private static readonly List<BitAvatarGroupItem> rtlPeople =
[
    new() { Text = ""سارا نوری"", ImageUrl = ""/_content/Bit.BlazorUI.Demo.Client.Core/images/persona/persona-female.png"" },
    new() { Text = ""مریم احمدی"" },
    new() { Text = ""علی رضایی"" },
    new() { Text = ""زهرا کریمی"" },
    new() { Text = ""حسین محمدی"" },
];";
}

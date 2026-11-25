namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.CommandPalette;

public partial class BitCommandPaletteDemo
{
    private readonly string example1RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" OnCommandExecuted=""e => basicLastRun = e.Command.Title"">
    <BitCommandPaletteGroup Heading=""Navigation"">
        <BitCommandPaletteOption Title=""Go to home"" Keywords=""start, dashboard"" />
        <BitCommandPaletteOption Title=""Go to settings"" Keywords=""preferences, options"" />
        <BitCommandPaletteOption Title=""Go to profile"" Subtitle=""Your account"" />
    </BitCommandPaletteGroup>
    <BitCommandPaletteGroup Heading=""Actions"">
        <BitCommandPaletteOption Title=""Create a document"" Badge=""New"" />
        <BitCommandPaletteOption Title=""Invite a teammate"" />
        <BitCommandPaletteOption Title=""Archive the project"" Disabled DisabledReason=""Only owners can archive"" />
    </BitCommandPaletteGroup>
</BitCommandPalette>

<BitText>Last run: <b>@basicLastRun</b></BitText>

@code {
    private string? basicLastRun;
}";

    private readonly string example2RazorCode = @"
<BitCommandPaletteTrigger />
<BitButton Variant=""BitVariant.Outline"" OnClick=""() => isModalOpen = true"">Open from code</BitButton>

<BitCommandPalette @bind-Open=""isModalOpen"" Commands=""modalCommands"" />

<BitText>Open: <b>@isModalOpen</b>, last run: <b>@modalLastRun</b></BitText>";
    private readonly string example2CsharpCode = @"
private bool isModalOpen;
private string? modalLastRun;
private BitCommandPaletteCommand[] modalCommands = default!;

protected override void OnInitialized()
{
    modalCommands =
    [
        BitCommandPaletteCmd.Action(""Say hello"", () => modalLastRun = ""Say hello"").Group(""Actions"").Keywords(""greet""),
        BitCommandPaletteCmd.Action(""Copy the link"", () => modalLastRun = ""Copy the link"").Group(""Actions""),
        BitCommandPaletteCmd.Navigate(""Go to getting started"", ""/getting-started"").Group(""Navigation""),
    ];
}";

    private readonly string example3RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""codeCommands"" />

<BitText>@codeStatus</BitText>";
    private readonly string example3CsharpCode = @"
private string codeStatus = ""Nothing run yet."";
private bool darkMode;
private bool compactMode;
private BitCommandPaletteCommand[] codeCommands = default!;

protected override void OnInitialized()
{
    BuildCodeCommands();
}

private void BuildCodeCommands()
{
    codeCommands =
    [
        BitCommandPaletteCmd.Page(""Theme"",
            BitCommandPaletteCmd.Action(""Light"", () => SetTheme(false)).Id(""theme-light""),
            BitCommandPaletteCmd.Action(""Dark"", () => SetTheme(true)).Id(""theme-dark""))
            .Group(""Settings""),
        BitCommandPaletteCmd.New(""Compact mode"").Group(""Settings"")
            .Checked(compactMode)
            .Handler(() => { compactMode = !compactMode; codeStatus = $""Compact mode: {compactMode}""; BuildCodeCommands(); }),
        BitCommandPaletteCmd.New(""Rename the project"").Group(""Project"")
            .Handler(ctx => Task.FromResult(BitCommandPaletteCommandResult.Prompt(""New name"", submit =>
            {
                codeStatus = $""Renamed to {submit.PromptValue}"";
                return Task.FromResult(BitCommandPaletteCommandResult.Toast(""Renamed""));
            }))),
        BitCommandPaletteCmd.Action(""Delete the project"", () => codeStatus = ""Deleted"").Group(""Project"")
            .Destructive(""Delete the project?""),
    ];
}

private void SetTheme(bool dark)
{
    darkMode = dark;
    codeStatus = $""Theme: {(dark ? ""Dark"" : ""Light"")}"";
}";

    private readonly string example4RazorCode = @"
<BitCommandPaletteCommands Commands=""pageCommands"" />

<BitText>@registeredStatus</BitText>";
    private readonly string example4CsharpCode = @"
private string registeredStatus = ""Press Ctrl+Shift+L (Cmd+Shift+L) or G then L anywhere on this page."";
private int likes;
private BitCommandPaletteCommand[] pageCommands = default!;

protected override void OnInitialized()
{
    pageCommands =
    [
        BitCommandPaletteCmd.Action(""Like this page"", () => { likes++; registeredStatus = $""Liked {likes} time(s).""; })
            .Group(""This page"").Shortcut(""Mod+Shift+L""),
        BitCommandPaletteCmd.Action(""Go to the top"", () => registeredStatus = ""Went to the top."")
            .Group(""This page"").Shortcut(""G L""),
    ];
}";

    private readonly string example5RazorCode = @"
<BitCommandPalette Inline Placeholder=""Type # to search fruits…"" />

<BitText>Picked: <b>@fruitPicked</b></BitText>";
    private readonly string example5CsharpCode = @"
[Inject] private IBitCommandPaletteService paletteService { get; set; } = default!;

private string? fruitPicked;
private IDisposable? fruitMode;

private static readonly string[] fruits = [""Apple"", ""Apricot"", ""Banana"", ""Blueberry"", ""Cherry"", ""Grape"", ""Kiwi"", ""Lemon"", ""Mango"", ""Orange"", ""Peach"", ""Pear"", ""Plum"", ""Strawberry""];

protected override void OnInitialized()
{
    fruitMode = paletteService.RegisterMode(new()
    {
        Prefix = ""#"",
        Name = ""Fruits"",
        Placeholder = ""Search fruits…"",
        HideRegistryCommands = true,
        Sources = [new BitCommandPaletteDelegateSource(""Fruits"", SearchFruits)],
    });
}

private async Task<IReadOnlyList<BitCommandPaletteCommand>> SearchFruits(string query, CancellationToken cancellationToken)
{
    await Task.Delay(300, cancellationToken);

    return fruits.Where(f => f.Contains(query, StringComparison.OrdinalIgnoreCase))
                 .Select(f => (BitCommandPaletteCommand)BitCommandPaletteCmd.Action(f, () => fruitPicked = f).Id($""fruit-{f}""))
                 .ToList();
}

public void Dispose()
{
    fruitMode?.Dispose();
}";

    private readonly string example6RazorCode = @"
<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""labelCommands"" />

<BitText>@labelsStatus</BitText>";
    private readonly string example6CsharpCode = @"
private string labelsStatus = ""No labels set."";
private BitCommandPaletteCommand[] labelCommands = default!;

protected override void OnInitialized()
{
    labelCommands =
    [
        BitCommandPaletteCmd.Page(""Set labels"",
            BitCommandPaletteCmd.New(""Bug"").Id(""label-bug""),
            BitCommandPaletteCmd.New(""Feature"").Id(""label-feature""),
            BitCommandPaletteCmd.New(""Docs"").Id(""label-docs""),
            BitCommandPaletteCmd.New(""Performance"").Id(""label-performance""))
            .MultiSelect()
            .Handler(ctx => labelsStatus = ctx.SelectedCommands.Count == 0 ? ""No labels set."" : $""Labels: {string.Join("", "", ctx.SelectedCommands.Select(c => c.Title))}""),
    ];
}";

    private readonly string example7RazorCode = @"
<style>
    .no-results {
        padding: 2rem 1rem;
        text-align: center;
        color: var(--bit-clr-fg-sec);
    }
</style>

<BitCommandPalette Inline IncludeRegisteredCommands=""false"" Commands=""detailCommands"">
    <NoResultsTemplate Context=""query"">
        <div class=""no-results"">Nothing matches ""@query"". Try a fruit.</div>
    </NoResultsTemplate>
    <DetailTemplate Context=""command"">
        <b>@command.Title</b>
        <p>@command.Description</p>
    </DetailTemplate>
</BitCommandPalette>";
    private readonly string example7CsharpCode = @"
private static readonly BitCommandPaletteCommand[] detailCommands =
[
    BitCommandPaletteCmd.New(""Apple"").Group(""Fruits"").Description(""A crisp fruit that grows on trees, red, green or yellow.""),
    BitCommandPaletteCmd.New(""Banana"").Group(""Fruits"").Description(""A long, curved fruit with a soft, sweet inside.""),
    BitCommandPaletteCmd.New(""Cherry"").Group(""Fruits"").Description(""A small, round stone fruit, usually dark red.""),
];";

    private readonly string example8RazorCode = @"
<style>
    .custom-heading {
        color: #7c3aed;
        letter-spacing: 0;
        text-transform: none;
    }
</style>

<BitCommandPalette Inline
                   IncludeRegisteredCommands=""false""
                   Style=""--bit-CommandPalette-accent: #7c3aed; --bit-CommandPalette-active-background: #ede9fe; --bit-CommandPalette-item-radius: 0;""
                   Styles=""@(new() { Footer = ""display: none;"" })""
                   Classes=""@(new() { GroupHeading = ""custom-heading"" })"">
    <BitCommandPaletteGroup Heading=""Colors"">
        <BitCommandPaletteOption Title=""Violet"" Badge=""Accent"" />
        <BitCommandPaletteOption Title=""Indigo"" />
        <BitCommandPaletteOption Title=""Teal"" />
    </BitCommandPaletteGroup>
</BitCommandPalette>";

    private readonly string example9RazorCode = @"
<div dir=""rtl"">
    <BitCommandPalette Dir=""BitDir.Rtl"" Inline IncludeRegisteredCommands=""false"" Strings=""persianStrings"">
        <BitCommandPaletteGroup Heading=""رفتن به"">
            <BitCommandPaletteOption Title=""خانه"" />
            <BitCommandPaletteOption Title=""تنظیمات"" />
            <BitCommandPaletteOption Title=""نمایه"" />
        </BitCommandPaletteGroup>
    </BitCommandPalette>
</div>";
    private readonly string example9CsharpCode = @"
private static readonly BitCommandPaletteStrings persianStrings = new()
{
    Placeholder = ""فرمانی بنویسید یا جستجو کنید…"",
    NoResults = ""نتیجه‌ای برای «{0}» یافت نشد"",
    DialogLabel = ""پالت فرمان"",
};";
}

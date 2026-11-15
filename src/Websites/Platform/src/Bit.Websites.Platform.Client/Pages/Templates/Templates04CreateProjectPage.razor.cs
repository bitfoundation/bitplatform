using System.Globalization;
using Bit.Butil;
using Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

namespace Bit.Websites.Platform.Client.Pages.Templates;

public partial class Templates04CreateProjectPage
{
    private const int LastStep = 4;

    private static readonly string[] stepTitles = ["Your product", "What it does", "Extras", "Your devices", "Get it"];

    private static readonly CreateProjectChoice<CreateProjectGoal>[] goalChoices =
    [
        new(CreateProjectGoal.Real, "My real product", "Set up the way it will run for your customers, with a full database (PostgreSQL).", "Recommended"),
        new(CreateProjectGoal.Try, "Just trying it out", "Ready sooner and installs less: your data stays in one file, and sample pages show how things fit together.")
    ];

    private static readonly CreateProjectChoice<CreateProjectStart>[] startChoices =
    [
        new(CreateProjectStart.Blank, "Just the essentials", "A complete identity solution and the settings already work. You build the rest."),
        new(CreateProjectStart.Admin, "An admin panel with a dashboard", "Charts, products and categories, ready to turn into your own. Great for tools businesses use."),
        new(CreateProjectStart.Store, "An online store", "A shop front with products and their pages, ready to make your own.")
    ];

    private static readonly CreateProjectChoice<bool>[] customersChoices =
    [
        new(true, "Several customer companies (Multi-tenancy)", "Each company gets its own users and sees only its own data. How most software for businesses works.", "Recommended for SaaS"),
        new(false, "One organization", "One set of users and one set of data, like a tool for your own company.")
    ];

    private static readonly CreateProjectChoice<CreateProjectErrors>[] errorsChoices =
    [
        new(CreateProjectErrors.OwnServer, "On your own server", "Kept on the server your app already runs on, without additional cost. No other account needed.", "Recommended"),
        new(CreateProjectErrors.Sentry, "Sentry", "A service made for app errors that also catches your app's crashes on Android and iOS, with an alert and a clear page for each one. Free for one person and up to 5,000 errors a month; for a team, from about $26 a month."),
        new(CreateProjectErrors.AppInsights, "Azure Application Insights", "Microsoft's service, a good fit if your app runs on Azure. You pay for what it keeps: the first 5 GB a month are free, then about $2.30 for each GB.")
    ];

    private static readonly CreateProjectChoice<CreateProjectComputer>[] computerChoices =
    [
        new(CreateProjectComputer.Windows, CreateProjectNames.Of(CreateProjectComputer.Windows)),
        new(CreateProjectComputer.MacOs, CreateProjectNames.Of(CreateProjectComputer.MacOs)),
        new(CreateProjectComputer.Linux, CreateProjectNames.Of(CreateProjectComputer.Linux))
    ];

    private static readonly CreateProjectChoice<CreateProjectDatabase>[] databaseChoices =
    [
        new(CreateProjectDatabase.PostgreSql, "PostgreSQL", "Free, popular and the safe choice for a real product.", "Recommended"),
        new(CreateProjectDatabase.Sqlite, "SQLite", "Everything in one file, nothing to set up. Move to PostgreSQL before you launch."),
        new(CreateProjectDatabase.SqlServer, "SQL Server", "Microsoft's database, common in companies."),
        new(CreateProjectDatabase.MySql, "MySQL", "Offered by most hosting companies.")
    ];

    private static readonly CreateProjectOption filesStorage = CreateProjectOption.Find("filesStorage")!;

    private static readonly CreateProjectOption[] superTechnicalChoicesBeforeSwitches =
        [.. CreateProjectOption.SuperTechnical.TakeWhile(option => option.Kind is CreateProjectOptionKind.Choice)];

    private static readonly CreateProjectOption[] superTechnicalSwitches =
        [.. CreateProjectOption.SuperTechnical.Where(option => option.Kind is CreateProjectOptionKind.Toggle)];

    private static readonly CreateProjectOption[] superTechnicalChoicesAfterSwitches =
        [.. CreateProjectOption.SuperTechnical.SkipWhile(option => option.Kind is CreateProjectOptionKind.Choice).Where(option => option.Kind is CreateProjectOptionKind.Choice)];

    [AutoInject] private UserAgent userAgent = default!;
    [AutoInject] private History history = default!;

    private readonly CreateProjectState state = new();
    private ElementReference stepElement;
    private bool focusStep;

    private CreateProjectAnswers Answers => state.Answers;

    private int CurrentStep => Answers.IsNameValid ? Math.Clamp(state.Step, 0, LastStep) : 0;

    private string? ComputerHint => state.Computer is not null
        ? null
        : state.DetectedComputer is null
            ? "Pick the computer you'll create your project on."
            : "We guessed it from your browser. Change it if it's wrong.";

    private string SuperTechnicalSummary
    {
        get
        {
            var changed = Answers.TemplateOptions.Keys.Count(name => CreateProjectOption.Find(name) is { IsTechnical: false });

            return changed is 0 ? "Super technical: the template's defaults" : $"Super technical: {changed} changed";
        }
    }

    protected override async Task OnInitAsync()
    {
        state.PageUrl = NavigationManager.ToAbsoluteUri("templates/create-project").ToString();

        var query = new Uri(NavigationManager.Uri).Query.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(pair => pair.Split('=', 2))
            .GroupBy(pair => Uri.UnescapeDataString(pair[0]), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First() is [_, var value] ? Uri.UnescapeDataString(value.Replace('+', ' ')) : string.Empty, StringComparer.OrdinalIgnoreCase);
        state.Restore(key => query.GetValueOrDefault(key));
        state.Changed += Render;

        await base.OnInitAsync();
    }

    protected override async Task OnAfterFirstRenderAsync()
    {
        await base.OnAfterFirstRenderAsync();

        state.Changed += WriteAddress;

        var browser = await userAgent.Extract();
        state.ReadBrowser(browser.OsName);
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (focusStep is false)
            return;

        focusStep = false;
        await stepElement.FocusAsync();
    }

    private void GoToStep(int step)
    {
        focusStep = true;
        state.GoToStep(step);
    }

    private void StartOver()
    {
        focusStep = true;
        state.StartOver();
    }

    private void SwitchOption(string name, bool isOn) => state.SetTemplateOption(name, isOn ? "true" : "false");

    private static CreateProjectChoice<string>[] OptionChoices(CreateProjectOption option, string defaultBadge) =>
        [.. option.Choices.Select(choice => new CreateProjectChoice<string>(choice, option.ChoiceText(choice), option.ChoiceDescription(choice), choice == option.Default ? defaultBadge : null))];

    private string DeviceNote(CreateProjectDevice device) => (device, Answers.CanBuild(device)) switch
    {
        (CreateProjectDevice.Android, _) => "Adds about 8 GB and 20 minutes. The phone emulator needs 16 GB of memory, or plug in your own Android phone.",
        (CreateProjectDevice.Ios, true) when Answers.Computer is CreateProjectComputer.Windows => "Adds about 2 GB and 10 minutes here, and builds on a Mac with Xcode: one on your network is enough, or a Mac in the cloud.",
        (CreateProjectDevice.Ios, true) => "Adds about 2 GB and 10 minutes, and needs Xcode from the App Store.",
        (CreateProjectDevice.Ios, false) => "Needs a Mac, where Apple's tools run. The app is in your project for when you have one.",
        (CreateProjectDevice.Windows, true) => "Adds about 1 GB and 5 minutes.",
        (CreateProjectDevice.Windows, false) => "Needs a Windows computer. The app is in your project for when you have one.",
        (_, true) => "Adds about 1 GB and 5 minutes, and needs Xcode from the App Store.",
        _ => "Needs a Mac. The app is in your project for when you have one."
    };

    private void WriteAddress() => _ = InvokeAsync(async () =>
    {
        var share = Answers.ShareQuery;
        var step = CurrentStep is 0 ? string.Empty : $"step={CurrentStep.ToString(CultureInfo.InvariantCulture)}";
        var query = string.Join('&', new[] { share, step }.Where(part => part.Length > 0));
        var path = new Uri(NavigationManager.Uri).AbsolutePath;

        await history.ReplaceState(null, query.Length is 0 ? path : $"{path}?{query}");
    });

    private void Render() => _ = InvokeAsync(StateHasChanged);

    protected override async ValueTask DisposeAsync(bool disposing)
    {
        state.Changed -= Render;
        state.Changed -= WriteAddress;

        await base.DisposeAsync(disposing);
    }
}

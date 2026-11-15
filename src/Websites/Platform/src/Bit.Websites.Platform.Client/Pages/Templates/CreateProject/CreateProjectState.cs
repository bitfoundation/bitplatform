namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public class CreateProjectState
{
    public event Action? Changed;

    public string PageUrl { get; set; } = "https://bitplatform.dev/templates/create-project";

    public string? ProductName { get; private set; }
    public CreateProjectGoal? Goal { get; private set; }
    public CreateProjectStart Start { get; private set; }
    public bool IsOneOrganization { get; private set; }
    public CreateProjectDatabase? Database { get; private set; }
    public bool IsAssistantOff { get; private set; }
    public bool IsPushOff { get; private set; }
    public CreateProjectErrors Errors { get; private set; }
    public bool IsGitHubRepoOff { get; private set; }
    public bool IsOffline { get; private set; }
    public CreateProjectDevice[]? Devices { get; private set; }
    public IReadOnlyDictionary<string, string> TemplateOptions { get; private set; } = new Dictionary<string, string>();
    public CreateProjectComputer? Computer { get; private set; }
    public string? BrowserOs { get; private set; }
    public int Step { get; private set; }
    public CreateProjectHelpTopic? HelpTopic { get; private set; }
    public bool IsMoreOpen { get; private set; }
    public bool IsSuperTechnicalOpen { get; private set; }
    public bool IsLineExplained { get; private set; }

    public CreateProjectAnswers Answers => new()
    {
        PageUrl = PageUrl,
        Name = ProductName,
        Goal = Goal ?? CreateProjectGoal.Real,
        Start = Start,
        IsMultiTenant = IsOneOrganization is false,
        PickedDatabase = Database,
        HasAssistant = IsAssistantOff is false,
        HasPush = IsPushOff is false,
        Errors = Errors,
        PickedDevices = Devices ?? [],
        Computer = Computer ?? DetectedComputer ?? CreateProjectComputer.Windows,
        HasGitHubRepo = IsGitHubRepoOff is false,
        IsOffline = IsOffline,
        TemplateOptions = TemplateOptions
    };

    public CreateProjectComputer? DetectedComputer => BrowserOs?.ToLowerInvariant() switch
    {
        null => null,
        var name when name.Contains("windows") => CreateProjectComputer.Windows,
        var name when name.Contains("mac") || name.Contains("os x") => CreateProjectComputer.MacOs,
        var name when name.Contains("android") || name.Contains("ios") || name.Contains("chrome") => null,
        var name when name.Contains("linux") || name.Contains("ubuntu") || name.Contains("fedora") || name.Contains("debian") => CreateProjectComputer.Linux,
        _ => null
    };

    public void Rename(string? name) => Change(() => ProductName = string.IsNullOrWhiteSpace(name) ? null : name.Trim());

    public void Aim(CreateProjectGoal goal) => Change(() =>
    {
        Goal = goal;

        if (goal is CreateProjectGoal.Try)
        {
            Database = null;
        }
    });

    public void StartWith(CreateProjectStart start) => Change(() => Start = start);

    public void Serve(bool isOneOrganization) => Change(() => IsOneOrganization = isOneOrganization);

    public void PickDatabase(CreateProjectDatabase database) => Change(() => Database = database);

    public void SwitchAssistant(bool isOn) => Change(() => IsAssistantOff = isOn is false);

    public void SwitchPush(bool isOn) => Change(() => IsPushOff = isOn is false);

    public void SendErrorsTo(CreateProjectErrors errors) => Change(() => Errors = errors);

    public void SwitchGitHubRepo(bool isOn) => Change(() => IsGitHubRepoOff = isOn is false);

    public void SwitchOffline(bool isOn) => Change(() => IsOffline = isOn);

    public void SwitchDevice(CreateProjectDevice device, bool isOn) => Change(() =>
    {
        var devices = (Devices ?? []).Where(d => d != device);

        Devices = isOn ? [.. devices, device] : [.. devices];
    });

    public void SetTemplateOption(string name, string? value) => Change(() =>
    {
        if (CreateProjectOption.Find(name) is not { } option || option.Normalize(value) is not { } normalized)
            return;

        var options = new Dictionary<string, string>(TemplateOptions);

        if (normalized == option.Default)
        {
            options.Remove(option.Name);
        }
        else
        {
            options[option.Name] = normalized;
        }

        TemplateOptions = options;
    });

    public void PickComputer(CreateProjectComputer computer) => Change(() => Computer = computer);

    public void ReadBrowser(string? os) => Change(() => BrowserOs = os);

    public void GoToStep(int step) => Change(() => Step = Math.Max(0, step));

    public void OpenHelp(CreateProjectHelpTopic? topic) => Change(() => HelpTopic = topic);

    public void ShowMore(bool isOpen) => Change(() => IsMoreOpen = isOpen);

    public void ShowSuperTechnical(bool isOpen) => Change(() => IsSuperTechnicalOpen = isOpen);

    public void ExplainLine(bool isOpen) => Change(() => IsLineExplained = isOpen);

    public void StartOver() => Change(() =>
    {
        ProductName = null;
        Goal = null;
        Start = CreateProjectStart.Blank;
        IsOneOrganization = false;
        Database = null;
        IsAssistantOff = false;
        IsPushOff = false;
        Errors = CreateProjectErrors.OwnServer;
        IsGitHubRepoOff = false;
        IsOffline = false;
        Devices = null;
        TemplateOptions = new Dictionary<string, string>();
        Computer = null;
        Step = 0;
        HelpTopic = null;
        IsMoreOpen = false;
        IsSuperTechnicalOpen = false;
        IsLineExplained = false;
    });

    public void Restore(Func<string, string?> query)
    {
        ProductName = string.IsNullOrWhiteSpace(query("name")) ? null : query("name")!.Trim();
        Goal = query("goal") is "try" ? CreateProjectGoal.Try : null;
        Start = Parse(query("start"), CreateProjectStart.Blank);
        IsOneOrganization = query("customers") is "one";
        Database = Enum.TryParse<CreateProjectDatabase>(query("db"), ignoreCase: true, out var database) && Enum.IsDefined(database) ? database : null;
        IsAssistantOff = query("assistant") is "off";
        IsPushOff = query("push") is "off";
        Errors = Parse(query("errors"), CreateProjectErrors.OwnServer);
        IsGitHubRepoOff = query("github") is "off";
        IsOffline = query("offline") is "on";

        var devices = (query("devices") ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(d => Enum.TryParse<CreateProjectDevice>(d, ignoreCase: true, out var device) && Enum.IsDefined(device) ? device : (CreateProjectDevice?)null)
            .OfType<CreateProjectDevice>()
            .Distinct()
            .ToArray();
        Devices = devices.Length is 0 ? null : devices;

        TemplateOptions = CreateProjectOption.All
            .Select(option => (option, Value: option.Normalize(query(option.Name))))
            .Where(pair => pair.Value is not null && pair.Value != pair.option.Default)
            .ToDictionary(pair => pair.option.Name, pair => pair.Value!);

        Step = int.TryParse(query("step"), out var step) ? Math.Max(0, step) : 0;
    }

    private static T Parse<T>(string? value, T fallback) where T : struct, Enum
        => Enum.TryParse<T>(value, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed) ? parsed : fallback;

    private void Change(Action change)
    {
        change();
        Changed?.Invoke();
    }
}

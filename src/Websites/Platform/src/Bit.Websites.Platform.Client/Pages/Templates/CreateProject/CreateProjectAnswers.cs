using System.Globalization;
using System.Text;

namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public sealed record CreateProjectAnswers
{
    public string PageUrl { get; init; } = "https://bitplatform.dev/templates/create-project";

    public string? Name { get; init; }

    public CreateProjectGoal Goal { get; init; }

    public CreateProjectStart Start { get; init; }

    public bool IsMultiTenant { get; init; } = true;

    public CreateProjectDatabase? PickedDatabase { get; init; }

    public bool HasAssistant { get; init; } = true;

    public bool HasPush { get; init; } = true;

    public CreateProjectErrors Errors { get; init; }

    public CreateProjectDevice[] PickedDevices { get; init; } = [];

    public CreateProjectComputer Computer { get; init; }

    public bool HasGitHubRepo { get; init; } = true;

    public bool IsOffline { get; init; }

    public IReadOnlyDictionary<string, string> TemplateOptions { get; init; } = new Dictionary<string, string>();

    public string Option(string name) => TemplateOptions.TryGetValue(name, out var value) ? value : CreateProjectOption.Find(name)?.Default ?? string.Empty;

    public bool IsTry => Goal is CreateProjectGoal.Try;

    public CreateProjectDatabase Database => IsTry ? CreateProjectDatabase.Sqlite : PickedDatabase ?? CreateProjectDatabase.PostgreSql;

    public bool NeedsContainers => Option("aspire") is "true" && (Database is not CreateProjectDatabase.Sqlite || Option("redis") is "true");

    public IReadOnlyList<CreateProjectDevice> Devices => [.. PickedDevices.Distinct().Where(CanBuild).Order()];

    public bool HasLiveDashboard => Start is CreateProjectStart.Admin && HasAssistant;

    public string CodeName => IsNameValid && string.IsNullOrWhiteSpace(Name) is false ? Name! : SuggestedName;

    public string DisplayName => string.IsNullOrWhiteSpace(Name) ? CodeName : Name;

    public bool IsNameValid => string.IsNullOrWhiteSpace(Name) || IsValidProjectName(Name);

    public string SuggestedName
    {
        get
        {
            var name = ToCodeName(Name);

            if (IsValidProjectName(name))
                return name;

            foreach (var candidate in new[] { name + "App", "My" + name })
            {
                var trimmed = candidate.Length > 40 ? candidate[..40].TrimEnd('.') : candidate;

                if (IsValidProjectName(trimmed))
                    return trimmed;
            }

            return "MyProduct";
        }
    }

    public static bool IsValidProjectName(string? name)
    {
        if (string.IsNullOrEmpty(name) || name.Length > 40)
            return false;

        var parts = name.Split('.');

        if (WindowsDeviceNames.Contains(parts[0]) || ReservedRoots.Contains(parts[0]) || parts.Skip(1).Contains(parts[0]))
            return false;

        return parts.All(part => part.Length > 0
            && char.IsAsciiLetter(part[0])
            && part.All(char.IsAsciiLetterOrDigit)
            && CSharpKeywords.Contains(part) is false);
    }

    private static readonly HashSet<string> WindowsDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private static readonly HashSet<string> ReservedRoots = new(StringComparer.OrdinalIgnoreCase) { "System", "Microsoft" };

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const", "continue",
        "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
        "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
        "long", "namespace", "new", "null", "object", "operator", "out", "override", "params", "private", "protected",
        "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof", "stackalloc", "static", "string",
        "struct", "switch", "this", "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
        "using", "virtual", "void", "volatile", "while"
    };

    public bool CanBuild(CreateProjectDevice device) => device switch
    {
        CreateProjectDevice.Ios => Computer is CreateProjectComputer.MacOs or CreateProjectComputer.Windows,
        CreateProjectDevice.MacOs => Computer is CreateProjectComputer.MacOs,
        CreateProjectDevice.Windows => Computer is CreateProjectComputer.Windows,
        _ => true
    };

    public string Line => BuildLine(withName: true);

    public string LineWithoutName => BuildLine(withName: false);

    private string BuildLine(bool withName)
    {
        List<string> args = withName ? ["bit", "new", CodeName] : ["bit", "new"];

        if (Database is not CreateProjectDatabase.Sqlite) args.AddRange(["--database", Database switch
        {
            CreateProjectDatabase.PostgreSql => "PostgreSQL",
            CreateProjectDatabase.SqlServer => "SqlServer",
            _ => "MySql"
        }]);
        if (Start is not CreateProjectStart.Blank) args.AddRange(["--module", Start is CreateProjectStart.Admin ? "Admin" : "Sales"]);
        if (IsMultiTenant is false) args.AddRange(["--multitenant", "false"]);
        if (HasAssistant is false) args.AddRange(["--signalR", "false"]);
        if (HasPush is false) args.AddRange(["--notification", "false"]);
        if (Errors is CreateProjectErrors.Sentry) args.AddRange(["--sentry", "true"]);
        if (Errors is CreateProjectErrors.AppInsights) args.AddRange(["--appInsights", "true"]);
        if (IsOffline) args.AddRange(["--offlineDb", "true"]);
        if (IsTry) args.AddRange(["--sample", "true"]);
        foreach (var option in CreateProjectOption.All)
        {
            var value = Option(option.Name);

            if (value != option.CliDefault) args.AddRange([$"--{option.Name}", value]);
        }
        if (HasGitHubRepo) args.Add("--github-repo");
        if (Devices.Count > 0) args.AddRange(["--platforms", string.Join(',', Devices.Select(d => d.ToString().ToLowerInvariant()))]);
        args.AddRange(["--yes", "--non-interactive"]);

        return string.Join(' ', args);
    }

    public string ShareQuery
    {
        get
        {
            var query = new List<string>();

            if (string.IsNullOrWhiteSpace(Name) is false) query.Add($"name={Uri.EscapeDataString(Name)}");
            if (IsTry) query.Add("goal=try");
            if (Start is not CreateProjectStart.Blank) query.Add($"start={Start.ToString().ToLowerInvariant()}");
            if (IsMultiTenant is false) query.Add("customers=one");
            if (PickedDatabase is not null) query.Add($"db={PickedDatabase.ToString()!.ToLowerInvariant()}");
            if (HasAssistant is false) query.Add("assistant=off");
            if (HasPush is false) query.Add("push=off");
            if (Errors is not CreateProjectErrors.OwnServer) query.Add($"errors={Errors.ToString().ToLowerInvariant()}");
            if (PickedDevices.Length > 0) query.Add($"devices={string.Join(',', PickedDevices.Select(d => d.ToString().ToLowerInvariant()))}");
            if (HasGitHubRepo is false) query.Add("github=off");
            if (IsOffline) query.Add("offline=on");
            foreach (var option in CreateProjectOption.All)
            {
                if (TemplateOptions.TryGetValue(option.Name, out var value)) query.Add($"{option.Name}={Uri.EscapeDataString(value)}");
            }

            return string.Join('&', query);
        }
    }

    public string ShareLink => ShareQuery is { Length: > 0 } query ? $"{PageUrl}?{query}" : PageUrl;

    public double DownloadGb => 2 + (NeedsContainers ? 1.5 : 0) + Devices.Sum(d => d switch
    {
        CreateProjectDevice.Android => 8,
        CreateProjectDevice.Ios => 2,
        _ => 1
    });

    public (int From, int To) Minutes
    {
        get
        {
            var extra = (NeedsContainers ? 5 : 0) + Devices.Sum(d => d switch
            {
                CreateProjectDevice.Android => 20,
                CreateProjectDevice.Ios => 10,
                _ => 5
            });

            return (10 + extra, 15 + extra);
        }
    }

    public int WebReadyMinutes => 15 + (NeedsContainers ? 5 : 0);

    public string DownloadText => DownloadGb.ToString(DownloadGb % 1 == 0 ? "0" : "0.#", CultureInfo.InvariantCulture);

    public int DiskGb => (int)(Math.Ceiling((DownloadGb * 2.5 + 5) / 5) * 5);

    public int MemoryGb => Devices.Contains(CreateProjectDevice.Android) ? 16 : 8;

    public bool NeedsXcode => Computer is CreateProjectComputer.MacOs && Devices.Any(d => d is CreateProjectDevice.Ios or CreateProjectDevice.MacOs);

    public bool NeedsMacForIos => Computer is CreateProjectComputer.Windows && Devices.Contains(CreateProjectDevice.Ios);

    public static string ToCodeName(string? name)
    {
        var code = new StringBuilder();
        var startsWord = true;

        foreach (var c in (name ?? string.Empty).Normalize(NormalizationForm.FormD))
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                code.Append(startsWord && char.IsAsciiLetter(c) ? char.ToUpperInvariant(c) : c);
                startsWord = false;
            }
            else if (c == '.' && code.Length > 0 && code[^1] != '.')
            {
                code.Append('.');
                startsWord = true;
            }
            else if (CharUnicodeInfo.GetUnicodeCategory(c) is not UnicodeCategory.NonSpacingMark)
            {
                startsWord = true;
            }
        }

        var result = code.ToString().Trim('.');

        if (result.Length == 0)
            return "MyProduct";

        if (char.IsAsciiLetter(result[0]) is false)
            result = "App" + result;

        return result.Length > 40 ? result[..40].TrimEnd('.') : result;
    }
}

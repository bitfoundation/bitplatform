namespace Bit.Websites.Platform.Client.Pages.Templates.CreateProject;

public enum CreateProjectOptionKind
{
    Choice,
    Toggle
}

public sealed record CreateProjectOption(string Name, CreateProjectOptionKind Kind, string Default, string CliDefault, string[] Choices)
{
    public static IReadOnlyList<CreateProjectOption> All { get; } =
    [
        new("framework", CreateProjectOptionKind.Choice, "net10.0", "net10.0", ["net10.0", "net11.0"]),
        new("api", CreateProjectOptionKind.Choice, "Integrated", "Integrated", ["Integrated", "Standalone"]),
        new("filesStorage", CreateProjectOptionKind.Choice, "S3", "Local", ["Local", "S3", "AzureBlobStorage", "Other"]) { IsTechnical = true },
        new("captcha", CreateProjectOptionKind.Choice, "None", "None", ["reCaptcha", "None"]),
        new("aspire", CreateProjectOptionKind.Toggle, "true", "true", []),
        new("redis", CreateProjectOptionKind.Toggle, "false", "false", []),
        new("cloudflare", CreateProjectOptionKind.Toggle, "true", "true", []) { IsTechnical = true },
        new("brouter", CreateProjectOptionKind.Toggle, "true", "true", []),
        new("ads", CreateProjectOptionKind.Toggle, "false", "false", []),
        new("advancedTests", CreateProjectOptionKind.Toggle, "false", "false", []),
        new("theme", CreateProjectOptionKind.Choice, "Fluent2", "Fluent2", ["Fluent2", "Fluent", "Cupertino", "Material"])
    ];

    public bool IsTechnical { get; init; }

    public static IEnumerable<CreateProjectOption> SuperTechnical => All.Where(option => option.IsTechnical is false);

    public static CreateProjectOption? Find(string name) =>
        All.FirstOrDefault(option => string.Equals(option.Name, name, StringComparison.OrdinalIgnoreCase));

    public string? Normalize(string? value) => Kind switch
    {
        CreateProjectOptionKind.Choice => Choices.FirstOrDefault(choice => string.Equals(choice, value?.Trim(), StringComparison.OrdinalIgnoreCase)),
        CreateProjectOptionKind.Toggle => value?.Trim().ToLowerInvariant() switch
        {
            "true" or "on" => "true",
            "false" or "off" => "false",
            _ => null
        },
        _ => null
    };

    public string Title => Name switch
    {
        "framework" => ".NET version",
        "theme" => "Design system",
        "api" => "API hosting",
        "filesStorage" => "File storage",
        "captcha" => "Bot protection",
        "aspire" => "Aspire",
        "redis" => "Redis",
        "cloudflare" => "Cloudflare",
        "brouter" => "bit Brouter",
        "ads" => "Google Ads",
        "advancedTests" => "Advanced tests",
        _ => Name
    };

    public string? Lead => Name switch
    {
        "framework" => "The version of .NET the project is built on.",
        "theme" => "The look your app's buttons, fields and menus start with. Each has a light and a dark mode.",
        "api" => "Whether the API can be published apart from the web app when it needs to be.",
        "filesStorage" => "Where the files people upload, such as profile pictures, are kept.",
        "captcha" => "Keeps bots from creating accounts on the sign-up page.",
        "aspire" => "Runs the database, Redis, file storage and a test mail server for you while you build.",
        "redis" => "A shared cache and background jobs, for running the app on more than one server.",
        "cloudflare" => "Serves the app through Cloudflare's network, so it loads faster and your server does less.",
        "brouter" => "Nested pages, guarded pages and pages that keep their state. Off keeps Blazor's own router.",
        "ads" => "Rewarded ads: people watch one in return for something you give them.",
        "advancedTests" => "Extra automated tests, such as the offline sync test, with the setup they need.",
        _ => null
    };

    public string ChoiceText(string value) => (Name, value) switch
    {
        ("framework", "net10.0") => ".NET 10",
        ("framework", _) => ".NET 11",
        ("theme", "Fluent2") => "Fluent 2",
        ("api", "Integrated") => "Inside the web app",
        ("api", _) => "A project of its own",
        ("filesStorage", "Local") => "The server's own disk",
        ("filesStorage", "S3") => "S3 compatible storage",
        ("filesStorage", "AzureBlobStorage") => "Azure Blob Storage",
        ("filesStorage", _) => "Another storage",
        ("captcha", "reCaptcha") => "Google reCAPTCHA",
        ("captcha", _) => "None",
        _ => value
    };

    public string? ChoiceDescription(string value) => (Name, value) switch
    {
        ("framework", "net10.0") => "Long-term support: updates for three years.",
        ("framework", _) => "The newest version, with a shorter support period.",
        ("theme", "Cupertino") or ("theme", "Material") => "Experimental",
        ("api", "Integrated") => "The API is part of the web app's project, so the two are always published together.",
        ("api", _) => "The API can be published and scaled apart from the web app when needed, or together with it.",
        ("filesStorage", "Local") => "Nothing else to set up.",
        ("filesStorage", "S3") => "Amazon S3, Cloudflare R2 or any storage that speaks S3.",
        ("filesStorage", "AzureBlobStorage") => "Microsoft Azure's file storage.",
        ("filesStorage", _) => "Any storage FluentStorage supports, which you set up yourself.",
        ("captcha", "None") => "Anyone can sign up without the check.",
        _ => null
    };
}

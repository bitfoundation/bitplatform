namespace Bit.Cli.Templates;

public static class TemplateLabels
{
    private static readonly Dictionary<string, string> labels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["database"] = "Database",
        ["filesStorage"] = "Files storage",
        ["api"] = "API hosting",
        ["pipeline"] = "CI/CD pipeline",
        ["module"] = "Module",
        ["multitenant"] = "Multi-tenancy",
        ["captcha"] = "Captcha",
        ["aspire"] = ".NET Aspire",
        ["redis"] = "Redis",
        ["notification"] = "Push notifications",
        ["sample"] = "Todo sample page",
        ["sentry"] = "Sentry",
        ["appInsights"] = "Azure Application Insights",
        ["signalR"] = "SignalR",
        ["offlineDb"] = "Offline database (EF Core in the client)",
        ["cloudflare"] = "Cloudflare CDN",
        ["ads"] = "Google Ads",
        ["brouter"] = "bit Brouter instead of the Blazor router",
        ["theme"] = "Theme",
        ["apiServerUrl"] = "API server URL",
        ["webAppUrl"] = "Web app URL",
        ["advancedTests"] = "Advanced automated tests",
        ["realProject"] = "Real project, with persistent containers"
    };

    public static string For(TemplateParameter parameter)
    {
        if (labels.TryGetValue(parameter.Name, out var label))
            return label;

        var text = (parameter.DisplayName ?? parameter.Name).Trim().TrimEnd('?');

        foreach (var prefix in new[] { "Add ", "Use ", "Include " })
        {
            if (text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                text = text[prefix.Length..];
            }
        }

        foreach (var suffix in new[] { " to project", " to the project" })
        {
            if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
            {
                text = text[..^suffix.Length];
            }
        }

        return text.Length > 0 ? char.ToUpperInvariant(text[0]) + text[1..] : parameter.Name;
    }

    public static string For(TemplateChoice choice)
    {
        if (string.IsNullOrWhiteSpace(choice.Description) || string.Equals(choice.Description, choice.Value, StringComparison.OrdinalIgnoreCase))
            return choice.Value;

        var description = choice.Description.Split(" - ", 2)[^1];
        return $"{choice.Value} ({Infrastructure.CliConsole.Truncate(description, 70)})";
    }
}

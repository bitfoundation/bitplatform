using System.Text;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Bit.Bmotion.Demo.Server.Services;

namespace Bit.Bmotion.Demo.Server.Controllers;

/// <summary>
/// The same body of knowledge the tools serve, exposed as MCP resources.
/// <para>
/// Tools are for an agent that has decided what it needs; resources are for a client that wants to
/// attach documentation to a conversation up front, or let a person browse and pin it. Both read
/// the same catalogs, so neither can go stale relative to the other.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "bmotion://guide", Name = "Bit.Bmotion guide", MimeType = "text/markdown")]
    [Description("The complete Bit.Bmotion guide (the library README), every section in one document.")]
    public static string Guide()
    {
        var readme = BmotionSourceCatalog.Readme;

        // The guide is an embedded resource, so an empty one means it was not embedded in this build.
        // Saying so is an answer; an empty document reads as a library with nothing written about it.
        return readme.Length > 0
            ? readme
            : "The Bit.Bmotion guide is not available in this build: the README was not embedded in the assembly.";
    }

    [McpServerResource(UriTemplate = "bmotion://guide/{heading}", Name = "Guide section", MimeType = "text/markdown")]
    [Description("One section of the Bit.Bmotion guide by heading, e.g. bmotion://guide/Variants.")]
    public static string GuideSection(string heading)
    {
        var section = BmotionSourceCatalog.GetGuideSection(heading);

        return section is null
            ? $"The guide has no section called '{heading}'."
            : McpController.Truncate(section);
    }

    [McpServerResource(UriTemplate = "bmotion://api", Name = "Bit.Bmotion public API", MimeType = "text/markdown")]
    [Description("Every public Bit.Bmotion type with its kind and summary.")]
    public static string ApiList()
    {
        // The one-line summaries, as GetBmotionApiList uses: a pinned index of 66 types is read to
        // find a name, and the full prose behind each one is what bmotion://api/{typeName} is for.
        var lines = BmotionApiCatalog.TypeSummaries.Select(type => $"- **{type.Name}** ({type.Kind}) - {type.Summary}");

        return $"# Bit.Bmotion public API\n\n{string.Join('\n', lines)}";
    }

    [McpServerResource(UriTemplate = "bmotion://api/{typeName}", Name = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Bmotion type, e.g. bmotion://api/BmSpring.")]
    public static string ApiType(string typeName)
    {
        var details = BmotionApiCatalog.GetTypeDetails(typeName);

        if (details is null) return $"Bit.Bmotion has no public type called '{typeName}'.";

        var builder = new StringBuilder();

        builder.AppendLine($"# {details.Name} ({details.Kind})").AppendLine();
        if (details.Summary is not null) builder.AppendLine(details.Summary).AppendLine();
        if (details.Remarks is not null) builder.AppendLine(details.Remarks).AppendLine();

        foreach (var group in details.Members.GroupBy(member => member.Kind))
        {
            builder.AppendLine($"## {group.Key}").AppendLine();

            foreach (var member in group)
            {
                builder.Append($"- **{member.Name}**{member.Signature}");
                if (member.Type is not null) builder.Append($" : `{member.Type}`");
                if (member.Default is not null) builder.Append($" = `{member.Default}`");
                if (member.Required) builder.Append(" **(required)**");
                if (member.Summary is not null) builder.Append($" - {member.Summary}");
                builder.AppendLine();
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "bmotion://properties", Name = "Animatable properties", MimeType = "text/markdown")]
    [Description("Every property Bit.Bmotion can animate, and whether it survives on Blazor Server.")]
    public static async Task<string> Properties()
    {
        var properties = await BmotionPropertyCatalog.GetAsync();

        var builder = new StringBuilder();

        builder.AppendLine("# Animatable properties").AppendLine();
        builder.AppendLine("| Property | Category | CSS | Compositor | On Blazor Server |");
        builder.AppendLine("|---|---|---|---|---|");

        foreach (var property in properties)
        {
            builder.AppendLine($"| `{property.Name}` | {property.Category} | `{property.Css}` | " +
                               $"{(property.CompositorEligible ? "yes" : "no")} | {property.OnBlazorServer} |");
        }

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "bmotion://easings", Name = "Easing presets", MimeType = "text/markdown")]
    [Description("Every BmEase preset with its curve sampled from the library's own easing implementation.")]
    public static async Task<string> Easings()
    {
        var easings = await BmotionEasingCatalog.GetAsync();

        var builder = new StringBuilder();

        builder.AppendLine("# BmEase presets").AppendLine();
        builder.AppendLine("| Preset | Family | Curve | Overshoots |");
        builder.AppendLine("|---|---|---|---|");

        foreach (var easing in easings)
        {
            builder.AppendLine($"| `BmEase.{easing.Name}` | {easing.Family} | `{easing.Sparkline}` | " +
                               $"{(easing.Overshoots ? "yes" : "no")} |");
        }

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "bmotion://recipes", Name = "Bmotion recipes", MimeType = "text/markdown")]
    [Description("Every ready-made Bit.Bmotion pattern, with its markup and its caveats.")]
    public static string Recipes()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Bmotion recipes").AppendLine();

        foreach (var recipe in BmotionRecipeCatalog.All)
        {
            builder.AppendLine($"## {recipe.Title}").AppendLine();
            builder.AppendLine(recipe.Intent).AppendLine();
            builder.AppendLine("```razor").AppendLine(recipe.Code).AppendLine("```").AppendLine();

            if (recipe.Notes is not null) builder.AppendLine($"> {recipe.Notes}").AppendLine();
        }

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "bmotion://source/{path}", Name = "Demo source file", MimeType = "text/plain")]
    [Description("One source file of the demo site, e.g. bmotion://source/Demo%2FClient%2FPages%2FSprings.razor.")]
    public static string Source(string path)
    {
        var content = BmotionSourceCatalog.GetSourceFile(path);

        return content is null
            ? $"No source file at '{path}'."
            : McpController.Truncate(content, path);
    }

    [McpServerResource(UriTemplate = "bmotion://setup/{renderMode}", Name = "Setup guide", MimeType = "text/markdown")]
    [Description("The complete wiring for one Blazor render mode, e.g. bmotion://setup/wasm or bmotion://setup/server.")]
    public static string Setup(string renderMode)
        => BmotionSetupGuide.Get(renderMode)
           ?? $"'{renderMode}' is not a known render mode. Use one of: {string.Join(", ", BmotionSetupGuide.RenderModes)}.";
}

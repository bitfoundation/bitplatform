using System.Text;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Bit.Bmage.Demo.Server.Services;

namespace Bit.Bmage.Demo.Server.Controllers;

/// <summary>
/// The same body of knowledge the tools serve, exposed as MCP resources.
/// <para>
/// Tools are for an agent that has decided what it needs; resources are for a client that wants to
/// attach documentation to a conversation up front, or let a person browse and pin it. Both read the
/// same catalogs, so neither can go stale relative to the other.
/// </para>
/// </summary>
[McpServerResourceType]
public static class McpResources
{
    [McpServerResource(UriTemplate = "bmage://guide", Name = "Bit.Bmage guide", MimeType = "text/markdown")]
    [Description("The Bit.Bmage README: what the library is, its packages, installation and where it runs, with the list of the other guides - each of which is bmage://docs/{name}.")]
    public static string Guide()
    {
        var readme = BmageSourceCatalog.Readme;

        // The guide is an embedded resource, so an empty one means it was not embedded in this build.
        // Saying so is an answer; an empty document reads as a library with nothing written about it.
        if (readme.Length == 0)
        {
            return "The Bit.Bmage guide is not available in this build: the README was not embedded in the assembly.";
        }

        var names = string.Join(", ", BmageSourceCatalog.Documents.Select(document => document.Name));

        return McpController.Truncate(readme, continuation: $"Read the guides one at a time from bmage://docs/{{name}}: {names}.");
    }

    [McpServerResource(UriTemplate = "bmage://docs/{name}", Name = "Guide", MimeType = "text/markdown")]
    [Description("One Bit.Bmage guide by name, e.g. bmage://docs/pipelines, bmage://docs/formats or bmage://docs/troubleshooting.")]
    public static string Document(string name)
    {
        var resolved = BmageSourceCatalog.ResolveDocumentName(name);

        if (resolved is null)
        {
            return $"There is no Bit.Bmage guide called '{name}'. The guides are: " +
                   $"{string.Join(", ", BmageSourceCatalog.Documents.Select(document => document.Name))}.";
        }

        return McpController.Truncate(BmageSourceCatalog.GetDocument(resolved)!, continuation: $"The sections of this guide are: " +
            $"{string.Join(", ", BmageSourceCatalog.GetHeadings(resolved).Select(heading => heading.Heading))}.");
    }

    [McpServerResource(UriTemplate = "bmage://api", Name = "Bit.Bmage public API", MimeType = "text/markdown")]
    [Description("Every public type of the three Bit.Bmage packages, grouped by namespace, with its kind and summary.")]
    public static string ApiList()
    {
        var builder = new StringBuilder("# Bit.Bmage public API\n");

        foreach (var group in BmageApiCatalog.TypeSummaries.GroupBy(type => (type.Package, type.Namespace)).OrderBy(group => group.Key.Namespace, StringComparer.Ordinal))
        {
            builder.AppendLine().AppendLine($"## {group.Key.Namespace} ({group.Key.Package})").AppendLine();

            foreach (var type in group)
            {
                builder.AppendLine($"- **{type.Name}** ({type.Kind}){(type.Summary is null ? null : $" - {type.Summary}")}");
            }
        }

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "bmage://api/{typeName}", Name = "Type reference", MimeType = "text/markdown")]
    [Description("The full reference of one Bit.Bmage type, e.g. bmage://api/ImagePipeline or bmage://api/ImageExportOptions.")]
    public static string ApiType(string typeName)
    {
        var details = BmageApiCatalog.GetTypeDetails(typeName);

        if (details is null) return $"Bit.Bmage has no public type called '{typeName}'.";

        var builder = new StringBuilder();

        builder.AppendLine($"# {details.Name} ({details.Kind})").AppendLine();
        builder.AppendLine($"`using {details.Namespace};` - package `{details.Package}`").AppendLine();
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

        return McpController.Truncate(builder.ToString(), continuation: $"Call GetBmageApiDetails(typeName: \"{details.Name}.MemberName\") for one member at a time.");
    }

    [McpServerResource(UriTemplate = "bmage://recipes", Name = "Bmage recipes", MimeType = "text/markdown")]
    [Description("The index of the ready-made Bit.Bmage patterns, each readable in full from bmage://recipes/{id}.")]
    public static string Recipes()
    {
        var builder = new StringBuilder("# Bit.Bmage recipes\n\n");

        foreach (var recipe in BmageRecipeCatalog.All)
        {
            builder.AppendLine($"- **{recipe.Id}** - {recipe.Title} ({recipe.RunsIn}). {recipe.Intent}");
        }

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "bmage://recipes/{id}", Name = "Recipe", MimeType = "text/markdown")]
    [Description("One Bit.Bmage recipe with its code and its caveats, e.g. bmage://recipes/avatar-crop.")]
    public static string Recipe(string id)
    {
        var recipe = BmageRecipeCatalog.Find(id);

        if (recipe is null)
        {
            return $"There is no recipe called '{id}'. The recipes are: {string.Join(", ", BmageRecipeCatalog.All.Select(entry => entry.Id))}.";
        }

        var language = recipe.Code!.TrimStart().StartsWith('@') || recipe.Code.Contains("<div", StringComparison.Ordinal) ? "razor" : "csharp";

        var builder = new StringBuilder();

        builder.AppendLine($"# {recipe.Title}").AppendLine();
        builder.AppendLine($"{recipe.Intent} Runs in: {recipe.RunsIn}.").AppendLine();
        builder.AppendLine($"```{language}").AppendLine(recipe.Code).AppendLine("```").AppendLine();
        if (recipe.Notes is not null) builder.AppendLine($"> {recipe.Notes}").AppendLine();
        if (recipe.SeeAlso is not null) builder.AppendLine($"Running on the demo site at {recipe.SeeAlso}.");

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "bmage://review-rules", Name = "Code review rules", MimeType = "text/markdown")]
    [Description("Every rule ReviewBmageCode applies: the mistake it catches, its severity and the guide that documents the behaviour behind it.")]
    public static string ReviewRules()
    {
        var builder = new StringBuilder("# Bit.Bmage code review rules\n\n");
        builder.AppendLine("| Rule | Severity | Catches | Guide |");
        builder.AppendLine("|---|---|---|---|");

        foreach (var rule in BmageCodeReview.Catalog)
        {
            builder.AppendLine($"| `{rule.Id}` | {rule.Severity} | {rule.Catches} | {rule.Source} |");
        }

        return builder.ToString();
    }

    [McpServerResource(UriTemplate = "bmage://source/{path}", Name = "Demo source file", MimeType = "text/plain")]
    [Description("One source file of the demo site, e.g. bmage://source/Demo%2FClient%2FPages%2FAvatar.razor.")]
    public static string Source(string path)
    {
        var content = BmageSourceCatalog.GetSourceFile(path);

        return content is null
            ? $"No source file at '{path}'."
            : McpController.Truncate(content, path);
    }

    [McpServerResource(UriTemplate = "bmage://setup/{renderMode}", Name = "Setup guide", MimeType = "text/markdown")]
    [Description("The complete wiring for one hosting model, e.g. bmage://setup/wasm, bmage://setup/server or bmage://setup/headless.")]
    public static string Setup(string renderMode)
        => BmageSetupGuide.Get(renderMode)
           ?? $"'{renderMode}' is not a known hosting model. Use one of: {string.Join(", ", BmageSetupGuide.RenderModes)}.";
}

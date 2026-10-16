using System.Net;
using System.Text;
using System.Reflection;
using Bit.Blex.Demo.Client;
using Bit.Blex.Generators;
using Microsoft.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Bit.Blex.Demo.Server.Services;

/// <summary>
/// The compile-time diagnostics of the Blex source generator, as GetBlexDiagnostics hands them out.
/// <para>
/// Two sources, each authoritative for its half: the generator's own <see cref="DiagnosticDescriptor"/>s
/// - read out of the shipped analyzer, so the id, severity and message format are the ones a build
///   actually reports - and the documentation site's <see cref="DiagnosticsCatalog"/>, which explains
///   the cause and shows a rejected and a fixed store. Neither is copied into the other.
/// </para>
/// </summary>
public static partial class BlexDiagnosticsReference
{
    /// <summary>One diagnostic, from both sources.</summary>
    public sealed record Entry(string Id, string Title, string Severity, string MessageFormat, BlexDiagnosticDoc? Doc);

    private static readonly Lazy<Entry[]> _entries = new(Build);

    /// <summary>Every diagnostic the generator can report, in id order.</summary>
    public static Entry[] Entries => _entries.Value;

    /// <summary>The descriptors the generator declares, read off the shipped analyzer type.</summary>
    public static DiagnosticDescriptor[] Descriptors { get; } =
        [.. typeof(BlexGenerator).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.FieldType == typeof(DiagnosticDescriptor))
            .Select(field => (DiagnosticDescriptor)field.GetValue(null)!)
            .OrderBy(descriptor => descriptor.Id, StringComparer.Ordinal)];

    private static Entry[] Build() =>
        [.. Descriptors.Select(descriptor => new Entry(
            descriptor.Id,
            descriptor.Title.ToString(),
            descriptor.DefaultSeverity.ToString(),
            descriptor.MessageFormat.ToString(),
            DiagnosticsCatalog.Find(descriptor.Id)))];

    /// <summary>The entry for one id ("BLEX001", "blex1", "1" all work), or null.</summary>
    public static Entry? Find(string? code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var digits = DigitsRegex().Match(code);
        if (digits.Success is false) return null;

        var id = $"BLEX{int.Parse(digits.Value, System.Globalization.CultureInfo.InvariantCulture):000}";

        return Entries.FirstOrDefault(entry => entry.Id == id);
    }

    /// <summary>Every diagnostic as one table, as GetBlexDiagnostics answers when it is called without a code.</summary>
    public static string RenderIndex()
    {
        var builder = new StringBuilder();

        builder.AppendLine("# Bit.Blex compile-time diagnostics").AppendLine();
        builder.AppendLine($"The {Entries.Length} diagnostics the Bit.Blex.Generators source generator reports. Call ")
               .AppendLine("`GetBlexDiagnostics(code: \"BLEX001\")` for one of them with its cause, a rejected store and the fix. ")
               .AppendLine("A store the generator rejects gets no generated partial at all, so its own diagnostic arrives with a cascade of ")
               .AppendLine("\"does not contain a definition for ...\" compiler errors - fix the BLEX one first.")
               .AppendLine();

        builder.AppendLine("| Id | Severity | Title |").AppendLine("| --- | --- | --- |");

        foreach (var entry in Entries)
        {
            builder.AppendLine($"| {entry.Id} | {entry.Severity} | {entry.Title} |");
        }

        return builder.ToString();
    }

    /// <summary>One diagnostic in full, as Markdown.</summary>
    public static string Render(Entry entry)
    {
        var builder = new StringBuilder();

        builder.AppendLine($"# {entry.Id} - {entry.Title}").AppendLine();
        builder.AppendLine($"Severity: **{entry.Severity}**").AppendLine();
        builder.AppendLine($"Reported as: _{entry.MessageFormat}_").AppendLine();

        if (entry.Doc is not null)
        {
            builder.AppendLine(PlainText(entry.Doc.Cause)).AppendLine();

            if (string.IsNullOrWhiteSpace(entry.Doc.Bad) is false)
            {
                builder.AppendLine("## Rejected").AppendLine().AppendLine("```csharp").AppendLine(entry.Doc.Bad.TrimEnd()).AppendLine("```").AppendLine();
            }

            if (string.IsNullOrWhiteSpace(entry.Doc.Good) is false)
            {
                builder.AppendLine("## Fix").AppendLine().AppendLine("```csharp").AppendLine(entry.Doc.Good.TrimEnd()).AppendLine("```").AppendLine();
            }
        }

        if (entry.Severity == nameof(DiagnosticSeverity.Warning))
        {
            builder.AppendLine($"Suppress it with `<NoWarn>$(NoWarn);{entry.Id}</NoWarn>` or `#pragma warning disable {entry.Id}` when the code is deliberate.");
        }

        return builder.ToString();
    }

    /// <summary>The docs write the cause as HTML for the site; an agent reads Markdown.</summary>
    private static string PlainText(string html)
    {
        var text = CodeTagRegex().Replace(html, match => $"`{match.Groups["text"].Value}`");

        return WebUtility.HtmlDecode(TagRegex().Replace(text, string.Empty));
    }

    [GeneratedRegex(@"\d+")]
    private static partial Regex DigitsRegex();

    [GeneratedRegex(@"<code>(?<text>.*?)</code>", RegexOptions.Singleline)]
    private static partial Regex CodeTagRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();
}

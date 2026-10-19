using System.Reflection;
using System.Text.RegularExpressions;
using Bit.Bcrdt.Demo.Server.Controllers;
using Bit.Bcrdt.Demo.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.Bcrdt.Tests.Mcp;

/// <summary>
/// The code the server hands out, held to the packages it is written against. A setup guide that names a
/// member the library does not have teaches an agent to write code that does not compile - or worse, code that
/// compiles against a different overload than the one described - so every C# file a guide offers is compiled
/// here against the real assemblies, and every Bcrdt name the guides, the prompts, the instructions and the
/// README use is looked up in them.
/// </summary>
[TestClass]
public partial class McpSnippetTests
{
    /// <summary>
    /// Files that need a framework this test host does not have - MAUI - and so are only name-checked. Each is
    /// listed by the heading its guide gives it, so a new file that cannot compile has to be added on purpose.
    /// </summary>
    private static readonly string[] _uncompilable = ["MauiProgram.cs"];

    /// <summary>What the SDKs import implicitly: a guide's files are written for a project that has these.</summary>
    private const string WebImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using Microsoft.AspNetCore.Builder;
        global using Microsoft.AspNetCore.Hosting;
        global using Microsoft.AspNetCore.Http;
        global using Microsoft.AspNetCore.Routing;
        global using Microsoft.Extensions.Configuration;
        global using Microsoft.Extensions.DependencyInjection;
        global using Microsoft.Extensions.Hosting;
        global using Microsoft.Extensions.Logging;
        """;

    private const string ConsoleImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.IO;
        global using System.Linq;
        global using System.Net.Http;
        global using System.Threading;
        global using System.Threading.Tasks;
        """;

    /// <summary>The app's root component, which every Blazor Program.cs names and no guide spells out.</summary>
    private const string AppStub = "public sealed class App : Microsoft.AspNetCore.Components.ComponentBase { }";

    private static readonly Lazy<MetadataReference[]> _references = new(LoadReferences);

    [TestMethod]
    public void Every_csharp_file_of_every_setup_guide_compiles_against_the_real_packages()
    {
        var failures = new List<string>();
        var compiled = 0;

        foreach (var model in BcrdtSetupGuide.HostingModels)
        {
            var files = Files(BcrdtSetupGuide.Get(model)!).Where(file => file.Language == "csharp").ToArray();

            var programs = files.Where(file => IsProgram(file.Code)).ToArray();
            var classes = files.Where(file => IsProgram(file.Code) is false).ToArray();

            // Each Program.cs is a project of its own, compiled together with the classes the guide gives next to it;
            // a guide with no Program.cs is compiled as a library.
            var projects = programs.Length == 0
                ? [(Name: model, Files: classes)]
                : programs.Select(program => (Name: $"{model}: {program.Heading}", Files: classes.Prepend(program).ToArray())).ToArray();

            foreach (var (name, project) in projects)
            {
                if (project.Any(file => _uncompilable.Any(skip => file.Heading.Contains(skip, StringComparison.Ordinal)))) continue;

                var console = model == "console";

                var trees = project.Select(file => CSharpSyntaxTree.ParseText(file.Code, path: file.Heading))
                    .Append(CSharpSyntaxTree.ParseText(console ? ConsoleImplicitUsings : WebImplicitUsings, path: "GlobalUsings.cs"))
                    .Concat(console ? Enumerable.Empty<SyntaxTree>() : [CSharpSyntaxTree.ParseText(AppStub, path: "App.cs")]);

                var compilation = CSharpCompilation.Create(
                    $"Snippet{compiled}",
                    trees,
                    _references.Value,
                    new CSharpCompilationOptions(programs.Length == 0 ? OutputKind.DynamicallyLinkedLibrary : OutputKind.ConsoleApplication, nullableContextOptions: NullableContextOptions.Enable));

                using var output = new MemoryStream();
                var result = compilation.Emit(output);

                failures.AddRange(result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).Select(diagnostic => $"{name}: {diagnostic}"));
                compiled++;
            }
        }

        Assert.IsGreaterThanOrEqualTo(7, compiled, "The guides should hand out at least seven compilable projects.");
        Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public void Every_bcrdt_name_the_server_and_the_readme_use_exists()
    {
        var types = BcrdtApiCatalog.Assemblies.SelectMany(assembly => assembly.GetExportedTypes())
            .GroupBy(type => StripArity(type.Name))
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var texts = BcrdtSetupGuide.HostingModels.Select(model => ($"setup guide '{model}'", HandWritten(BcrdtSetupGuide.Get(model)!)))
            .Append(("server instructions", BcrdtServerInstructions.Text))
            .Append(("README", BcrdtSourceCatalog.Readme))
            .Append(("add-bcrdt-to-app", McpPrompts.AddBcrdtToApp("web-app")))
            .Append(("implement-bcrdt-feature", McpPrompts.ImplementBcrdtFeature("x")))
            .Append(("debug-bcrdt-sync", McpPrompts.DebugBcrdtSync("x")));

        var failures = new List<string>();

        foreach (var (source, text) in texts)
        {
            foreach (var code in CodeRegex().Matches(text).Select(match => match.Value))
            {
                foreach (Match name in TypeNameRegex().Matches(code))
                {
                    var typeName = name.Groups["type"].Value;

                    if (types.TryGetValue(typeName, out var type) is false)
                    {
                        failures.Add($"{source} names '{typeName}', which no Bit.Bcrdt package declares.");
                        continue;
                    }

                    // BcrdtPresenceService.SuggestColor, BcrdtSyncProtocol.EncodeUpdate: the member has to be on that type.
                    if (name.Groups["member"].Success && HasMember(type, name.Groups["member"].Value) is false)
                    {
                        failures.Add($"{source} names '{typeName}.{name.Groups["member"].Value}', which {typeName} does not have.");
                    }
                }

                foreach (Match extension in ExtensionRegex().Matches(code))
                {
                    var method = extension.Groups["method"].Value;

                    if (types.Values.Any(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static).Any(candidate => candidate.Name == method)) is false)
                    {
                        failures.Add($"{source} calls '{method}', which no Bit.Bcrdt package declares.");
                    }
                }
            }
        }

        Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures.Distinct()));
    }

    [TestMethod]
    public void Every_component_attribute_the_setup_guides_and_the_readme_write_is_a_parameter()
    {
        var components = typeof(Bit.Bcrdt.Blazor.BcrdtComponentBase).Assembly.GetExportedTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type))
            .ToDictionary(type => type.Name, StringComparer.Ordinal);

        var texts = BcrdtSetupGuide.HostingModels.Select(model => ($"setup guide '{model}'", HandWritten(BcrdtSetupGuide.Get(model)!)))
            .Append(("README", BcrdtSourceCatalog.Readme));

        var failures = new List<string>();
        var checkedAttributes = 0;

        foreach (var (source, text) in texts)
        {
            foreach (Match tag in ComponentTagRegex().Matches(text))
            {
                if (components.TryGetValue(tag.Groups["component"].Value, out var component) is false)
                {
                    failures.Add($"{source} renders <{tag.Groups["component"].Value}>, which is not a Bit.Bcrdt.Blazor component.");
                    continue;
                }

                var parameters = component.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property => property.IsDefined(typeof(ParameterAttribute), inherit: true))
                    .Select(property => property.Name)
                    .ToHashSet(StringComparer.Ordinal);

                foreach (var attribute in AttributeRegex().Matches(tag.Groups["attributes"].Value).Select(match => match.Groups["name"].Value))
                {
                    // Context names the template's parameter; the @ directives are Razor's own.
                    if (attribute is "Context" || attribute.StartsWith('@')) continue;

                    checkedAttributes++;
                    if (parameters.Contains(attribute) is false) failures.Add($"{source} sets {component.Name}.{attribute}, which is not a parameter of it.");
                }
            }
        }

        Assert.IsGreaterThan(10, checkedAttributes);
        Assert.IsEmpty(failures, string.Join(Environment.NewLine, failures));
    }

    /// <summary>The files a guide hands out: every fenced block under a "### `...`" heading, in order.</summary>
    private static IEnumerable<(string Heading, string Language, string Code)> Files(string guide)
    {
        foreach (Match match in FileRegex().Matches(guide.ReplaceLineEndings("\n")))
        {
            // Under "The files, from this demo" the code is the demo's own, compiled by the demo's build.
            if (match.Groups["heading"].Value.StartsWith("Demo/", StringComparison.Ordinal)) continue;

            yield return (match.Groups["heading"].Value, match.Groups["language"].Value, match.Groups["code"].Value);
        }
    }

    /// <summary>
    /// A setup guide without the demo files it quotes: those are the demo's own, compiled by the demo's build, and
    /// name the demo's own types - its MCP server among them - next to the library's.
    /// </summary>
    private static string HandWritten(string guide)
    {
        var start = guide.IndexOf("## The files, from this demo", StringComparison.Ordinal);
        if (start < 0) return guide;

        var end = guide.IndexOf("## From the guide", start, StringComparison.Ordinal);

        return guide[..start] + (end < 0 ? string.Empty : guide[end..]);
    }

    private static bool IsProgram(string code) =>
        CSharpSyntaxTree.ParseText(code).GetCompilationUnitRoot().Members.OfType<GlobalStatementSyntax>().Any();

    private static bool HasMember(Type type, string member) =>
        type.GetMember(member, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy).Length > 0
        || type.GetInterfaces().Any(contract => contract.GetMember(member).Length > 0);

    private static string StripArity(string name) => name.Contains('`') ? name[..name.IndexOf('`')] : name;

    private static MetadataReference[] LoadReferences()
    {
        // Everything the test host runs on - the shared frameworks and the demo's packages, the five Bit.Bcrdt
        // ones among them - by simple name, so no assembly is referenced twice.
        var platform = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator);
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        return [.. platform.Concat(Directory.EnumerateFiles(AppContext.BaseDirectory, "*.dll"))
            .Where(path => names.Add(Path.GetFileNameWithoutExtension(path)) && IsManaged(path))
            .Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))];

        static bool IsManaged(string path)
        {
            try
            {
                AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    [GeneratedRegex(@"^### `(?<heading>[^`]+)`[^\n]*\n\n(?<fence>`{3,})(?<language>\w*)\n(?<code>.*?)\n\k<fence>", RegexOptions.Multiline | RegexOptions.Singleline)]
    private static partial Regex FileRegex();

    /// <summary>A fenced block or an inline code span: where a name is meant to be taken literally.</summary>
    [GeneratedRegex(@"```.*?```|`[^`\n]+`", RegexOptions.Singleline)]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"(?<![\w.])(?<type>I?Bcrdt[A-Z]\w*)(?:<[^>]*>)?(?:\.(?<member>[A-Z]\w*))?")]
    private static partial Regex TypeNameRegex();

    [GeneratedRegex(@"\.(?<method>(Add|Map)BitBcrdt\w*)\s*[<(]")]
    private static partial Regex ExtensionRegex();

    /// <summary>A component tag - not a type argument, which follows a name: MapHub&lt;BcrdtHub&gt;, ValueTask&lt;BcrdtStoredDocument&gt;.</summary>
    [GeneratedRegex(@"(?<![\w<])<(?<component>Bcrdt[A-Z]\w*)(?<attributes>(?:\s+[^>]*?)?)\s*/?>", RegexOptions.Singleline)]
    private static partial Regex ComponentTagRegex();

    [GeneratedRegex(@"(?<![\w@""])(?<name>@?[A-Za-z][\w:]*)\s*=")]
    private static partial Regex AttributeRegex();
}

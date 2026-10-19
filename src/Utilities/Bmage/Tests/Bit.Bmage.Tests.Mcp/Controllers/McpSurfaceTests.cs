using System.Reflection;
using ModelContextProtocol.Server;
using System.Text.RegularExpressions;

namespace Bit.Bmage.Tests.Mcp.Controllers;

/// <summary>
/// The server's surface as a whole - the tools, the prompts and the resources - and the
/// cross-references between them.
/// <para>
/// Nearly every word this server hands out tells an agent what to call next: the overview names the
/// tools of its working order, each tool's description names the others, every prompt is a numbered
/// sequence of calls, and every search hit carries one. Renaming one tool breaks instructions in a
/// dozen places, and nothing about that fails to compile. The tests here are mostly one idea applied
/// everywhere: every tool named in prose has to be a tool this server actually exposes.
/// </para>
/// </summary>
[TestClass]
public class McpSurfaceTests
{
    /// <summary>Anything shaped like one of this server's tool names, wherever it appears in prose.</summary>
    private static readonly Regex ToolReference = new(@"\b(?:Get|Search|Run|Compare|Review|List)Bmage[A-Za-z]*\b");

    private static readonly BmageMcpCatalogDto Catalog = new McpController().GetMcpCatalog();

    private static string[] ToolNames => [.. Catalog.Tools.Select(tool => tool.Name)];

    [TestMethod]
    public void Tools_TheProductToolsAllCarryTheBmageWord()
    {
        foreach (var name in new[] { "GetBmageOverview", "SearchBmage", "GetBmageSetupGuide", "GetBmageApiDetails", "GetBmageRecipes", "GetBmageRecipe", "GetBmageDocument", "ReviewBmageCode", "RunBmagePipeline" })
        {
            CollectionAssert.Contains(ToolNames, name, $"The server has no '{name}' tool.");
        }

        foreach (var name in ToolNames)
        {
            StringAssert.Contains(name, "Bmage", $"'{name}' does not carry the product word, so it collides with the other bit products on bitplatform.dev/mcp.");
        }
    }

    [TestMethod]
    public void Tools_EveryOne_IsNamedLegallyAndDescribedOnce()
    {
        var names = ToolNames;

        CollectionAssert.AreEquivalent(names.Distinct().ToArray(), names, "A tool name is registered twice.");

        foreach (var tool in Catalog.Tools)
        {
            Assert.IsTrue(Regex.IsMatch(tool.Name, "^[A-Za-z0-9_-]{1,64}$"), $"'{tool.Name}' is not a legal MCP tool name.");

            Assert.IsGreaterThan(80, tool.Description.Length,
                                 $"'{tool.Name}' has a {tool.Description.Length}-character description, which is too little to choose it by.");
        }
    }

    [TestMethod]
    public void Tools_TheStartHereTool_SaysSo()
    {
        var search = Catalog.Tools.Single(tool => tool.Name == nameof(McpController.SearchBmage));

        StringAssert.StartsWith(search.Description, "Start here.");
    }

    [TestMethod]
    public void Overview_EveryToolItSendsAnAgentTo_Exists()
    {
        AssertEveryToolReferenceResolves(new McpController().GetBmageOverview(), "the overview");
    }

    /// <summary>
    /// The overview must not grow into a directory of the tools: a client already holds every
    /// description from tools/list, so restating them is paid on every call to say nothing new.
    /// </summary>
    [TestMethod]
    public void Overview_DoesNotRestateTheToolCatalog()
    {
        var overview = new McpController().GetBmageOverview();

        var mentioned = ToolNames.Count(name => overview.Contains(name, StringComparison.Ordinal));

        Assert.IsLessThanOrEqualTo(ToolNames.Length / 2, mentioned,
                                   $"The overview names {mentioned} of {ToolNames.Length} tools, which is a tool directory.");
    }

    [TestMethod]
    public void Instructions_EveryToolTheyNameExists()
    {
        AssertEveryToolReferenceResolves(BmageServerInstructions.Text, "the server instructions");
    }

    [TestMethod]
    public void Tools_EveryToolTheDescriptionsCrossReference_Exists()
    {
        foreach (var tool in Catalog.Tools)
        {
            AssertEveryToolReferenceResolves(tool.Description, $"the description of {tool.Name}");
        }
    }

    [TestMethod]
    public void SetupGuidesAndRecipes_EveryToolTheyName_Exists()
    {
        foreach (var mode in BmageSetupGuide.RenderModes)
        {
            AssertEveryToolReferenceResolves(BmageSetupGuide.Get(mode)!, $"the {mode} setup guide");
        }

        foreach (var recipe in BmageRecipeCatalog.All)
        {
            AssertEveryToolReferenceResolves($"{recipe.Notes} {recipe.Code}", $"the {recipe.Id} recipe");
        }

        foreach (var rule in BmageCodeReview.Catalog)
        {
            AssertEveryToolReferenceResolves(rule.Catches, $"the {rule.Id} rule");
        }
    }

    [TestMethod]
    public void Prompts_EveryToolTheyInstructAnAgentToCall_Exists()
    {
        foreach (var (name, text) in RenderEveryPrompt())
        {
            AssertEveryToolReferenceResolves(text, $"the '{name}' prompt");
        }
    }

    [TestMethod]
    public void Prompts_EachOne_RendersAWorkflowRatherThanARestatementOfTheRequest()
    {
        var prompts = RenderEveryPrompt();

        Assert.AreEqual(Catalog.Prompts.Length, prompts.Count);
        Assert.IsGreaterThanOrEqualTo(4, prompts.Count);

        foreach (var (name, text) in prompts)
        {
            Assert.IsGreaterThan(500, text.Length, $"The '{name}' prompt is only {text.Length} characters.");
            Assert.IsTrue(ToolReference.IsMatch(text), $"The '{name}' prompt names no tool to call.");

            // Every argument it was given has to reach the text, or the prompt ignores its input.
            var method = typeof(McpPrompts).GetMethods().Single(m => m.GetCustomAttribute<McpServerPromptAttribute>()?.Name == name);

            foreach (var parameter in method.GetParameters())
            {
                StringAssert.Contains(text, $"SENTINEL-{parameter.Name}", $"The '{name}' prompt dropped its '{parameter.Name}' argument.");
            }
        }
    }

    /// <summary>
    /// The workflows that write or change code end by running it back through the review or the engine
    /// rather than by declaring victory when it builds.
    /// </summary>
    [TestMethod]
    public void Prompts_EveryOne_HasTheAgentCheckItsOwnWork()
    {
        foreach (var (name, text) in RenderEveryPrompt())
        {
            Assert.IsTrue(text.Contains(nameof(McpController.ReviewBmageCode), StringComparison.Ordinal) ||
                          text.Contains(nameof(McpController.RunBmagePipeline), StringComparison.Ordinal),
                          $"The '{name}' prompt never has the agent check its own work.");
        }
    }

    [TestMethod]
    public void Resources_EveryUriTemplate_IsWellFormedAndUnique()
    {
        var uris = Catalog.Resources.Select(resource => resource.Name).ToArray();

        CollectionAssert.AreEquivalent(uris.Distinct().ToArray(), uris, "A resource URI is registered twice.");

        foreach (var uri in uris)
        {
            StringAssert.StartsWith(uri, "bmage://", $"'{uri}' is not under this server's scheme.");
        }
    }

    /// <summary>
    /// Tools and resources are two doors onto the same catalogs. A client that pins the resource has to
    /// be reading what a client that called the tool would get.
    /// </summary>
    [TestMethod]
    public void Resources_AnswerTheSameAsTheToolsOverTheSameCatalogs()
    {
        var controller = new McpController();

        foreach (var document in BmageSourceCatalog.Documents)
        {
            Assert.AreEqual(controller.GetBmageDocument(document.Name), McpResources.Document(document.Name),
                            $"The guide '{document.Name}' reads differently through the resource.");
        }

        foreach (var mode in BmageSetupGuide.RenderModes)
        {
            Assert.AreEqual(controller.GetBmageSetupGuide(mode), McpResources.Setup(mode));
        }

        foreach (var file in BmageSourceCatalog.SourceFiles)
        {
            Assert.AreEqual(controller.GetBmageSourceFile(file.Path), McpResources.Source(file.Path),
                            $"The source file '{file.Path}' reads differently through the resource.");
        }

        foreach (var recipe in BmageRecipeCatalog.All)
        {
            StringAssert.Contains(McpResources.Recipe(recipe.Id), recipe.Code!, $"The recipe '{recipe.Id}' reads differently through the resource.");
        }
    }

    [TestMethod]
    public void Resources_EachOne_RendersSomethingReadable()
    {
        StringAssert.StartsWith(McpResources.ApiList(), "# Bit.Bmage public API");
        StringAssert.Contains(McpResources.ApiList(), "## Bit.Bmage.Pipeline (Bit.Bmage.Core)");
        StringAssert.Contains(McpResources.ApiList(), "**BmageEditor** (Component)");

        StringAssert.Contains(McpResources.ApiType("ImagePipeline"), "# ImagePipeline");
        StringAssert.Contains(McpResources.ApiType("ImagePipeline"), "`using Bit.Bmage.Pipeline;`");

        StringAssert.Contains(McpResources.Guide(), "# bit Bmage");

        foreach (var recipe in BmageRecipeCatalog.All)
        {
            StringAssert.Contains(McpResources.Recipes(), recipe.Id, $"The recipes resource omits '{recipe.Id}'.");
        }

        foreach (var rule in BmageCodeReview.Catalog)
        {
            StringAssert.Contains(McpResources.ReviewRules(), $"`{rule.Id}`");
        }
    }

    [TestMethod]
    public void Resources_AskedForWhatIsNotThere_ExplainRatherThanThrow()
    {
        StringAssert.Contains(McpResources.Document("teleportation"), "no Bit.Bmage guide called");
        StringAssert.Contains(McpResources.ApiType("Teleporter"), "no public type called");
        StringAssert.Contains(McpResources.Source("nowhere.razor"), "No source file at");
        StringAssert.Contains(McpResources.Recipe("teleport"), "no recipe called");

        var setup = McpResources.Setup("maui");

        foreach (var mode in BmageSetupGuide.RenderModes)
        {
            StringAssert.Contains(setup, mode);
        }
    }

    /// <summary>
    /// Every tool method is also a plain HTTP GET, which is what the demo's own MCP page calls. A tool
    /// that loses its route disappears from that page silently.
    /// </summary>
    [TestMethod]
    public void Tools_AreAlsoReachableAsPlainHttpEndpoints()
    {
        foreach (var method in typeof(McpController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsDefined(typeof(McpServerToolAttribute))))
        {
            Assert.IsTrue(method.IsDefined(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute)), $"'{method.Name}' is an MCP tool but not an HTTP endpoint.");
        }
    }

    /// <summary>
    /// A tool taking free text has to show what that text looks like, in its own description or on the
    /// parameter. Without an example the model invents a syntax.
    /// </summary>
    [TestMethod]
    public void Tools_TheOnesTakingFreeText_ShowAnExampleOfWhatToWrite()
    {
        foreach (var method in typeof(McpController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsDefined(typeof(McpServerToolAttribute)))
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(string))))
        {
            var described = method.GetParameters()
                .Where(parameter => parameter.ParameterType == typeof(string))
                .All(parameter => parameter.IsDefined(typeof(System.ComponentModel.DescriptionAttribute)));

            var description = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? string.Empty;

            var shown = description.Contains("e.g.", StringComparison.OrdinalIgnoreCase) ||
                        description.Contains("example", StringComparison.OrdinalIgnoreCase);

            Assert.IsTrue(described || shown, $"'{method.Name}' takes free text and never shows what it should look like.");
        }
    }

    /// <summary>Every search hit names its follow-up call, so every one of those has to be a real tool.</summary>
    [TestMethod]
    public void Search_EveryHitsFollowUpCall_IsARealTool()
    {
        foreach (var query in new[] { "avatar", "strip gps", "prerender", "MaxFileSize", "editor toolbar", "thumbnail server", "webp", "dispose" })
        {
            foreach (var hit in BmageSearchIndex.Search(query, 50))
            {
                var tool = hit.Tool[..hit.Tool.IndexOf('(', StringComparison.Ordinal)];

                CollectionAssert.Contains(ToolNames, tool, $"The hit '{hit.Title}' for '{query}' sends the agent to '{tool}'.");
            }
        }
    }

    private static void AssertEveryToolReferenceResolves(string text, string where)
    {
        var names = ToolNames;

        var dangling = ToolReference.Matches(text)
            .Select(match => match.Value)
            .Distinct(StringComparer.Ordinal)
            .Where(name => names.Contains(name) is false)
            .ToArray();

        Assert.IsEmpty(dangling, $"{where} names tools this server does not expose: {string.Join(", ", dangling)}.");
    }

    /// <summary>Every prompt, rendered with a recognisable marker in each argument.</summary>
    private static List<(string Name, string Text)> RenderEveryPrompt()
    {
        var rendered = new List<(string, string)>();

        foreach (var method in typeof(McpPrompts).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            var prompt = method.GetCustomAttribute<McpServerPromptAttribute>();

            if (prompt is null) continue;

            var arguments = method.GetParameters().Select(parameter => (object?)$"SENTINEL-{parameter.Name}").ToArray();

            var text = method.Invoke(null, arguments) as string;

            Assert.IsNotNull(text, $"The '{prompt.Name ?? method.Name}' prompt rendered nothing.");

            rendered.Add((prompt.Name ?? method.Name, text));
        }

        return rendered;
    }
}

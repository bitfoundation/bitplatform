using System.Reflection;

using ModelContextProtocol.Server;
using System.Text.RegularExpressions;

namespace Bit.Bmotion.Tests.Mcp.Controllers;

/// <summary>
/// The server's surface as a whole - the tools, the prompts and the resources - and the
/// cross-references between them.
/// <para>
/// Nearly every word this server hands out tells an agent what to call next: the overview lists the
/// tools, each tool's description names the others, and every prompt is a numbered sequence of
/// calls. Renaming one tool therefore breaks instructions in a dozen places, and nothing about that
/// fails to compile - the server keeps working, and starts telling agents to call something that
/// does not exist. The tests here are mostly one idea applied everywhere: every tool named in prose
/// has to be a tool this server actually exposes.
/// </para>
/// </summary>
[TestClass]
public class McpSurfaceTests
{
    /// <summary>Anything shaped like one of this server's tool names, wherever it appears in prose.</summary>
    private static readonly Regex ToolReference = new(@"\b(?:Get|Search|Simulate|Compare|Analyze|Review|List)Bmotion[A-Za-z]*\b");

    private static readonly BmotionMcpCatalogDto Catalog = new McpController().GetMcpCatalog();

    private static string[] ToolNames => [.. Catalog.Tools.Select(tool => tool.Name)];

    [TestMethod]
    public void Tools_EveryOne_IsNamedLegallyAndDescribedOnce()
    {
        var names = ToolNames;

        CollectionAssert.AreEquivalent(names.Distinct().ToArray(), names, "A tool name is registered twice.");

        foreach (var tool in Catalog.Tools)
        {
            // The MCP name grammar: what a client is allowed to send back as tools/call.
            Assert.IsTrue(Regex.IsMatch(tool.Name, "^[A-Za-z0-9_-]{1,64}$"), $"'{tool.Name}' is not a legal MCP tool name.");

            // The description is the entire basis on which a model chooses a tool.
            Assert.IsTrue(tool.Description.Length > 60,
                          $"'{tool.Name}' has a {tool.Description.Length}-character description, which is too little to choose it by.");
        }
    }

    /// <summary>
    /// The overview is the tool an agent is told to start with, and its "Which tool to call" section
    /// is the map it navigates by. A name that has drifted there sends it to a tool that is not here.
    /// </summary>
    [TestMethod]
    public void Overview_EveryToolItSendsAnAgentTo_Exists()
    {
        AssertEveryToolReferenceResolves(new McpController().GetBmotionOverview(), "the overview");
    }

    /// <summary>
    /// The map has to be complete as well as correct: a tool listed nowhere in the overview is one
    /// an agent that read the overview has no reason to call. The overview does not list itself,
    /// which is the one omission that costs nothing - it is already being read.
    /// </summary>
    [TestMethod]
    public void Overview_MentionsEveryOtherToolTheServerExposes()
    {
        var overview = new McpController().GetBmotionOverview();

        var unmentioned = ToolNames
            .Where(name => name != nameof(McpController.GetBmotionOverview))
            .Where(name => overview.Contains(name, StringComparison.Ordinal) is false)
            .ToArray();

        Assert.AreEqual(0, unmentioned.Length, $"Exposed but never mentioned in the overview: {string.Join(", ", unmentioned)}.");
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
        Assert.AreNotEqual(0, prompts.Count);

        foreach (var (name, text) in prompts)
        {
            Assert.IsTrue(text.Length > 500, $"The '{name}' prompt is only {text.Length} characters.");

            // A workflow is an order of calls; a prompt that names no tool is just prose.
            Assert.IsTrue(ToolReference.IsMatch(text), $"The '{name}' prompt names no tool to call.");

            // The argument it was given has to reach the text, or the prompt ignores its input.
            StringAssert.Contains(text, "SENTINEL", $"The '{name}' prompt dropped one of its arguments.");
        }
    }

    /// <summary>
    /// The workflows that write or change motion end by running the code back through the engine
    /// rather than by declaring victory when it builds - which is the one habit that catches the
    /// failures this library has. 'add-bmotion-to-app' is exempt: it wires a project up, and its
    /// proof is that the app builds and the sample animation moves.
    /// </summary>
    [TestMethod]
    public void Prompts_TheOnesThatWriteMotion_EndByVerifyingIt()
    {
        var wiringOnly = new[] { "add-bmotion-to-app" };

        foreach (var (name, text) in RenderEveryPrompt().Where(prompt => wiringOnly.Contains(prompt.Name) is false))
        {
            Assert.IsTrue(text.Contains(nameof(McpController.AnalyzeBmotionAnimation), StringComparison.Ordinal) ||
                          text.Contains(nameof(McpController.ReviewBmotionCode), StringComparison.Ordinal) ||
                          text.Contains(nameof(McpController.SimulateBmotionTransition), StringComparison.Ordinal),
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
            StringAssert.StartsWith(uri, "bmotion://", $"'{uri}' is not under this server's scheme.");
        }
    }

    /// <summary>
    /// Tools and resources are two doors onto the same catalogs. A client that pins the resource has
    /// to be reading what a client that called the tool would get, or the two answers drift apart
    /// and only one of them is ever checked.
    /// </summary>
    [TestMethod]
    public void Resources_AnswerTheSameAsTheToolsOverTheSameCatalogs()
    {
        var controller = new McpController();

        Assert.AreEqual(BmotionSourceCatalog.Readme, McpResources.Guide());

        foreach (var section in BmotionSourceCatalog.GuideSections)
        {
            Assert.AreEqual(controller.GetBmotionGuideSection(section.Heading), McpResources.GuideSection(section.Heading),
                            $"The guide section '{section.Heading}' reads differently through the resource.");
        }

        foreach (var mode in BmotionSetupGuide.RenderModes)
        {
            Assert.AreEqual(controller.GetBmotionSetupGuide(mode), McpResources.Setup(mode));
        }

        foreach (var file in BmotionSourceCatalog.SourceFiles)
        {
            Assert.AreEqual(BmotionSourceCatalog.GetSourceFile(file.Path), McpResources.Source(file.Path));
        }
    }

    [TestMethod]
    public async Task Resources_EachOne_RendersSomethingReadable()
    {
        Assert.IsTrue(McpResources.ApiList().Length > 500);
        StringAssert.StartsWith(McpResources.ApiList(), "# Bit.Bmotion public API");

        StringAssert.Contains(McpResources.ApiType("BmSpring"), "# BmSpring");
        StringAssert.Contains(McpResources.ApiType("BmSpring"), "Stiffness");

        var properties = await McpResources.Properties();

        StringAssert.Contains(properties, "| Property | Category |");
        StringAssert.Contains(properties, "`opacity`");

        var easings = await McpResources.Easings();

        StringAssert.Contains(easings, "`BmEase.Linear`");

        var recipes = McpResources.Recipes();

        foreach (var recipe in BmotionRecipeCatalog.All)
        {
            StringAssert.Contains(recipes, recipe.Title, $"The recipes resource omits '{recipe.Id}'.");
        }
    }

    /// <summary>
    /// A resource asked for something that is not there answers in text as well: a client browsing
    /// them cannot be handed a stack trace.
    /// </summary>
    [TestMethod]
    public void Resources_AskedForWhatIsNotThere_ExplainRatherThanThrow()
    {
        StringAssert.Contains(McpResources.GuideSection("Teleportation"), "no section called");
        StringAssert.Contains(McpResources.ApiType("Teleporter"), "no public type called");
        StringAssert.Contains(McpResources.Source("nowhere.razor"), "No source file at");

        var setup = McpResources.Setup("maui");

        foreach (var mode in BmotionSetupGuide.RenderModes)
        {
            StringAssert.Contains(setup, mode);
        }
    }

    /// <summary>
    /// Every tool method is also a plain HTTP GET, which is what the demo's own MCP page calls to
    /// show the tools working. A tool that loses its route disappears from that page silently.
    /// </summary>
    [TestMethod]
    public void Tools_AreAlsoReachableAsPlainHttpEndpoints()
    {
        foreach (var method in typeof(McpController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsDefined(typeof(McpServerToolAttribute))))
        {
            Assert.IsTrue(method.IsDefined(typeof(Microsoft.AspNetCore.Mvc.HttpGetAttribute)),
                          $"'{method.Name}' is an MCP tool but not an HTTP endpoint.");
        }
    }

    /// <summary>
    /// A tool taking free text has to show what that text looks like, in its own description or on
    /// the parameter. Without an example the model invents a syntax, and the tool answers about a
    /// spec it half-understood rather than refusing one it did not.
    /// </summary>
    [TestMethod]
    public void Tools_TheOnesTakingFreeText_ShowAnExampleOfWhatToWrite()
    {
        foreach (var method in typeof(McpController)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => method.IsDefined(typeof(McpServerToolAttribute)))
            .Where(method => method.GetParameters().Any(parameter => parameter.ParameterType == typeof(string))))
        {
            // Either every free-text argument carries its own description...
            var described = method.GetParameters()
                .Where(parameter => parameter.ParameterType == typeof(string))
                .All(parameter => parameter.IsDefined(typeof(System.ComponentModel.DescriptionAttribute)));

            // ...or the tool's own description shows what to write, which is enough for the ones
            // whose single argument is an id, a path or a heading.
            var description = method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()?.Description ?? string.Empty;

            var shown = description.Contains("e.g.", StringComparison.OrdinalIgnoreCase) ||
                        description.Contains("for example", StringComparison.OrdinalIgnoreCase) ||
                        description.Contains('\'');

            Assert.IsTrue(described || shown,
                          $"'{method.Name}' takes free text and never shows what it should look like.");
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

        Assert.AreEqual(0, dangling.Length, $"{where} names tools this server does not expose: {string.Join(", ", dangling)}.");
    }

    /// <summary>
    /// Every prompt, rendered with a recognisable marker in each of its arguments - so a prompt that
    /// quietly drops one of them is visible.
    /// </summary>
    private static List<(string Name, string Text)> RenderEveryPrompt()
    {
        var rendered = new List<(string, string)>();

        foreach (var method in typeof(McpPrompts).GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            var prompt = method.GetCustomAttribute<McpServerPromptAttribute>();

            if (prompt is null) continue;

            var arguments = method.GetParameters()
                .Select(parameter => (object?)$"SENTINEL-{parameter.Name}")
                .ToArray();

            var text = method.Invoke(null, arguments) as string;

            Assert.IsNotNull(text, $"The '{prompt.Name ?? method.Name}' prompt rendered nothing.");

            rendered.Add((prompt.Name ?? method.Name, text));
        }

        return rendered;
    }
}

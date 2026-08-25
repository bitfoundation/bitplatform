//+:cnd:noEmit
using System.Reflection;
using ModelContextProtocol.Server;
using Boilerplate.Server.Api.Features.Chatbot;
using Boilerplate.Server.Api.Infrastructure.SignalR;

namespace Boilerplate.Tests.Features.Chatbot;

/// <summary>
/// The seeded system prompts are the only description the model gets of what it may do, and nothing anywhere
/// validates them: they are plain strings assembled with <c>+</c> across template conditional arms and stamped into the
/// database with <c>HasData</c>. A wrong tool name, an unsupplied <c>{{Variable}}</c> or a lost newline is
/// invisible in code review, invisible at build time, and only shows up as the assistant behaving oddly.
/// <para>
/// These are pure assertions over strings and reflection metadata - no server, no database, no model call - so
/// they cost nothing to run and they close the whole class permanently.
/// </para>
/// </summary>
[TestClass]
public partial class SystemPromptContractTests
{
    /// <summary>
    /// The variables <c>AppChatbot</c> actually emits into the per-message <c>### Variables:</c> system message:
    /// <c>variablesDefault</c> supplies the first three (See <c>AppChatbot.StartChat</c>) and
    /// <c>variablesPrompt</c> the last three (See <c>AppChatbot.ProcessNewMessage</c>). A prompt that references
    /// anything else hands the model a placeholder that is never substituted.
    /// </summary>
    private static readonly string[] suppliedVariables =
    [
        "UserCulture", "DeviceInfo", "UserTimeZoneId",
        "IsAuthenticated", "UserEmail", "WebAppUrl"
    ];

    private static string[] AllSeededPrompts =>
    [
        SystemPromptConfiguration.GetInitialSystemPromptMarkdown(),
        //#if (module == "Sales" || module == "Admin")
        SystemPromptConfiguration.GetAnalyzeProductImageSystemPromptMarkdown()
        //#endif
    ];

    /// <summary>
    /// The prompt used to tell the model to call <c>SetCulture</c> and <c>SetTheme</c>, while the registered tools
    /// are <c>SetApplicationCulture</c> and <c>SetApplicationTheme</c> - so asking the assistant to change the
    /// language produced a call to a tool that does not exist. Every other tool name in the prompt was correct,
    /// which is exactly why nobody spotted the two that were not.
    /// </summary>
    [TestMethod]
    public void SeededPrompts_Should_OnlyNameToolsThatExist()
    {
        var registeredTools = typeof(AppChatbot)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null)
            .Select(m => m.Name)
            .ToArray();

        Assert.IsNotEmpty(registeredTools, "No [McpServerTool] methods were found on AppChatbot - the reflection query itself is broken, so a green result would mean nothing.");

        foreach (var prompt in AllSeededPrompts)
        {
            // The prompt always names a tool as "the `Name` tool", which is what makes this greppable without
            // matching the many other backtick-quoted things in the text (urls, literal values, page paths).
            var namedTools = Regex.Matches(prompt, "`(?<tool>[A-Z][A-Za-z]+)` tool")
                                  .Select(m => m.Groups["tool"].Value)
                                  .Distinct()
                                  .ToArray();

            foreach (var namedTool in namedTools)
            {
                Assert.Contains(namedTool, registeredTools,
                    $"The seeded system prompt tells the model to call a '{namedTool}' tool, but no [McpServerTool] with that name exists on AppChatbot. Registered: [{string.Join(", ", registeredTools)}].");
            }
        }
    }

    /// <summary>
    /// There is no template engine behind <c>{{...}}</c>: <c>AppChatbot</c> sends a second system message listing
    /// literal <c>{{Name}}: "value"</c> pairs and the model correlates them itself. A placeholder the prompt names
    /// but the Variables block never emits therefore reaches the model verbatim, and it either echoes it or
    /// invents a value - which is what <c>{{SignalRConnectionId}}</c> did.
    /// </summary>
    [TestMethod]
    public void SeededPrompts_Should_OnlyReferenceVariablesTheChatbotSupplies()
    {
        foreach (var prompt in AllSeededPrompts)
        {
            var referenced = Regex.Matches(prompt, @"\{\{(?<name>[A-Za-z]+)\}\}")
                                  .Select(m => m.Groups["name"].Value)
                                  .Distinct()
                                  .ToArray();

            foreach (var name in referenced)
            {
                Assert.Contains(name, suppliedVariables,
                    $"The seeded system prompt references a '{{{{{name}}}}}' variable that AppChatbot never emits, so the model receives the literal placeholder. Supplied: [{string.Join(", ", suppliedVariables)}].");
            }
        }
    }

    /// <summary>
    /// The prompts are built by concatenating verbatim literals across template conditional arms. The newline between two
    /// literals in the source is C# whitespace, not string content, so a segment that does not end in one welds
    /// the next segment onto its last line - which turned a security instruction, a canned user-facing sentence
    /// and the heading of the next section into a single markdown line in the DEFAULT configuration.
    /// </summary>
    [TestMethod]
    public void SeededPrompts_Should_NotWeldSectionsOntoThePrecedingLine()
    {
        foreach (var prompt in AllSeededPrompts)
        {
            foreach (var line in prompt.Split('\n'))
            {
                var trimmed = line.TrimEnd('\r');

                // A markdown heading or bullet is only legal at the START of a line. Finding one further along
                // means a concatenation seam ate the newline in front of it.
                var body = trimmed.TrimStart();

                var weldIndex = body.Length > 1 ? body.IndexOf("- ###", 1, StringComparison.Ordinal) : -1;

                Assert.AreEqual(-1, weldIndex,
                    $"A '- ###' section heading appears mid-line, so the segment before a '+' concatenation seam is missing its trailing newline. Line: '{trimmed}'.");

                Assert.DoesNotContain(".- ", body,
                    $"A markdown bullet is welded onto the end of the previous sentence, so a concatenated prompt segment is missing its trailing newline. Line: '{trimmed}'.");
            }
        }
    }

    /// <summary>
    /// The prompts delimit their sections with paired <c>**[[[X_BEGIN]]]**</c> / <c>**[[[X_END]]]**</c> markers.
    /// The ads section's BEGIN marker was written <c>**[[[ADS_TROUBLE_RULES_BEGIN]]]""</c> - and inside a verbatim
    /// string <c>""</c> is one literal quote - so it emitted a stray <c>"</c> where its partner had <c>**</c>.
    /// </summary>
    [TestMethod]
    public void SeededPrompts_Should_UsePairedAndWellFormedSectionMarkers()
    {
        foreach (var prompt in AllSeededPrompts)
        {
            var markers = Regex.Matches(prompt, @"(?<prefix>.{2})\[\[\[(?<name>[A-Z_]+)_(?<side>BEGIN|END)\]\]\](?<suffix>.{2})")
                               .Select(m => (Name: m.Groups["name"].Value, Side: m.Groups["side"].Value, Prefix: m.Groups["prefix"].Value, Suffix: m.Groups["suffix"].Value))
                               .ToArray();

            foreach (var marker in markers)
            {
                Assert.AreEqual("**", marker.Prefix, $"The [[[{marker.Name}_{marker.Side}]]] marker is not wrapped in '**' on the left.");
                Assert.AreEqual("**", marker.Suffix, $"The [[[{marker.Name}_{marker.Side}]]] marker is not wrapped in '**' on the right - a stray character here usually means a doubled quote inside the verbatim string.");
            }

            foreach (var group in markers.GroupBy(m => m.Name))
            {
                Assert.AreEqual(1, group.Count(m => m.Side is "BEGIN"), $"[[[{group.Key}_BEGIN]]] must appear exactly once.");
                Assert.AreEqual(1, group.Count(m => m.Side is "END"), $"[[[{group.Key}_END]]] must appear exactly once.");
            }
        }
    }
}

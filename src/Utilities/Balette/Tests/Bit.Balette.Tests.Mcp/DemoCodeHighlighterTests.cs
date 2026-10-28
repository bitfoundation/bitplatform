using System.Net;
using System.Reflection;
using System.Text.RegularExpressions;
using Bit.Balette.Demo.Client.Shared;
using Bit.Balette.Demo.Server.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;

namespace Bit.Balette.Tests.Mcp;

/// <summary>
/// The demo's code viewer: every snippet a page shows goes through the highlighter without its safety net, so a lexer
/// bug fails here rather than shipping as an uncoloured snippet; and the colours tell apart what a reader has to - a
/// member from a type, C# from text, a selector from a declaration.
/// </summary>
[TestClass]
public class DemoCodeHighlighterTests
{
    private static readonly Assembly Client = typeof(CodeBlock).Assembly;

    /// <summary>
    /// Every snippet a page shows in a CodeBlock of its own, in the language the block is given. The page's source - as
    /// the MCP server serves it - says which field or property holds the snippet, and it is read from the compiled page,
    /// so what is lexed is the text the page renders. A block whose snippet cannot be read that way - its Code is an
    /// expression, or names nothing on the page - is listed with no code, and fails rather than drops out. The MCP page
    /// is not served, so its one snippet has a test of its own.
    /// </summary>
    public static IEnumerable<object?[]> Snippets =>
        from type in Client.GetTypes()
        where type.GetCustomAttributes<RouteAttribute>().Any()
        let source = BaletteSourceCatalog.GetSourceFile($"Demo/Client/Pages/{type.Name}.razor")
        where source is not null
        from block in Regex.Matches(source, "<CodeBlock\\b[^>]*>")
        let member = Regex.Match(block.Value, "\\bCode=\"@(\\w+)\"")
        let language = Regex.Match(block.Value, "\\bLanguage=\"(\\w+)\"")
        select new object?[]
        {
            $"/{type.Name}: {(member.Success ? member.Groups[1].Value : block.Value)}",
            member.Success ? ReadSnippet(type, member.Groups[1].Value) : null,
            language.Success ? language.Groups[1].Value : "razor",
        };

    [TestMethod]
    [DynamicData(nameof(Snippets))]
    public void Every_snippet_a_page_shows_is_highlighted_without_a_lexer_error_and_keeps_its_text(string name, string? code, string language)
    {
        Assert.IsNotNull(code, $"{name}: the snippet is no field or property of the page, so it is not tested");
        AssertHighlighted(name, code, language);
    }

    [TestMethod]
    public void The_snippets_of_the_pages_are_found_in_every_language()
    {
        var snippets = Snippets.ToArray();

        // A page that renders its samples some other way, or a block whose field went missing, would drop out silently.
        foreach (var page in new[] { "Home", "GettingStarted", "Commands", "PagesAndFlows", "SearchAndRanking", "Modes", "Keyboard", "Theming", "Localization", "Performance", "OptionsAndSecurity" })
        {
            Assert.IsTrue(snippets.Any(snippet => ((string)snippet[0]!).StartsWith($"/{page}: ", StringComparison.Ordinal)), $"{page} shows no snippet");
        }
        CollectionAssert.IsSubsetOf(new[] { "razor", "csharp", "css" }, snippets.Select(snippet => (string)snippet[2]!).Distinct().ToArray());
        Assert.AreEqual("css", snippets.Single(snippet => (string)snippet[0]! == "/Theming: TokensExample")[2]);

        // A Razor comment written inside a snippet's string is the snippet's, not a comment of the page that holds it.
        StringAssert.StartsWith((string)snippets.Single(snippet => (string)snippet[0]! == "/Localization: ParameterExample")[1]!, "@* One palette. *@");

        // A snippet built from the page's state is a property, read off a page with its services injected.
        StringAssert.StartsWith((string)snippets.Single(snippet => (string)snippet[0]! == "/Theming: PaletteMarkup")[1]!, "<Balette Theme=");
        StringAssert.StartsWith((string)snippets.Single(snippet => (string)snippet[0]! == "/SearchAndRanking: RankFormula")[1]!, "rank = score + priority");
    }

    [TestMethod]
    public void A_service_the_demo_injects_is_not_coloured_as_a_type_in_a_csharp_sample()
    {
        // The C# samples are written as code in one of the demo's components, which inject these as @inject does.
        var tokens = Tokens("""
            IDisposable greeting = Palette.Register(BaletteCmd.New("Say hello"));
            IReadOnlyList<string> pinned = History.Pinned();
            if (Options.Matcher.Match(command, query, Options.Weights) is { } match) { }
            var ranked = Ranker.Rank(matches, hasQuery: true, id => History.Frecency(id));
            return BaletteCommandResult.Toast("Done");
            """, "csharp");

        foreach (var service in new[] { "Palette", "History", "Options", "Ranker" })
        {
            Assert.DoesNotContain($"[type:{service}]", tokens, $"{service} is coloured as a type");
        }
        Assert.Contains("[type:BaletteCmd].[fn:New]", tokens, "a type's static method lost the type's colour");
        Assert.Contains("[type:BaletteCommandResult].[fn:Toast]", tokens, "a type's static method lost the type's colour");

        // And none of the samples a page shows colours one as a type.
        foreach (var snippet in Snippets.Where(snippet => snippet[1] is not null))
        {
            var sample = Tokens((string)snippet[1]!, (string)snippet[2]!);
            foreach (var service in new[] { "Palette", "History", "Options", "Ranker", "Api" })
            {
                Assert.DoesNotContain($"[type:{service}].", sample, $"{snippet[0]}: {service} is coloured as a type");
            }
        }
    }

    [TestMethod]
    public void The_types_a_type_derives_from_are_coloured_as_types()
    {
        var tokens = Tokens("""
            public sealed class PeopleSource : IBaletteSource
            {
            }
            public sealed record Note(string Text, Card Item) : Base(Text), IComparable<Note> where T : class, INote;
            var size = compact ? Small : Large;
            """, "csharp");

        Assert.Contains("[type:PeopleSource] : [type:IBaletteSource]\n{", tokens, "a base type at the end of its line is not a type");
        Assert.Contains("([key:string] Text, [type:Card] Item) : [type:Base](Text), [type:IComparable]<[type:Note]>", tokens, "a base type, or its constructor's call, is not a type");
        Assert.Contains("[key:where] [type:T] : [key:class], [type:INote];", tokens, "a constraint is not a type");
        Assert.Contains("compact ? Small : Large;", tokens, "a value after a conditional's ':' is taken for a base type");
    }

    [TestMethod]
    public void A_function_in_an_at_rule_is_a_function_rather_than_a_media_feature()
    {
        var tokens = Tokens("""
            @import url(theme.css) layer(base);
            @import url("print.css") print;
            @media (max-width: 600px) { }
            """, "css");

        Assert.Contains("[key:@import] [fn:url]([str:theme.css]) [fn:layer](base)[pun:;]", tokens, "an unquoted address, or a layer, is taken for a media feature");
        Assert.Contains("[key:@import] [fn:url]([str:\"print.css\"]) print[pun:;]", tokens, "a quoted address is not a string");
        Assert.Contains("[key:@media] ([attr:max-width]: [num:600px])", tokens, "the media query is not one");
    }

    [TestMethod]
    public void A_snippet_is_lexed_once_and_its_html_handed_out_again()
    {
        var code = $"<p>@Now {Guid.NewGuid()}</p>";

        Assert.AreSame(SyntaxHighlighter.Highlight(code), SyntaxHighlighter.Highlight(code), "the snippet is lexed again");
        Assert.AreEqual(SyntaxHighlighter.HighlightOrThrow(code), SyntaxHighlighter.Highlight(code));
    }

    [TestMethod]
    public void The_mcp_page_s_client_configuration_is_highlighted_as_json()
    {
        var page = Client.GetTypes().Single(type => type.Name == "McpServer" && type.GetCustomAttributes<RouteAttribute>().Any());
        var config = (string)page.GetField("ClientConfig", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;

        AssertHighlighted("/McpServer: ClientConfig", config, "json");
        var tokens = Tokens(config, "json");
        Assert.Contains("[attr:\"mcpServers\"]", tokens, "a key is not a key");
        Assert.Contains("[attr:\"url\"]: [str:\"https://balette.bitplatform.dev/mcp\"]", tokens, "a value is not a string");
        Assert.Contains("[com:// .mcp.json", tokens, "the comment is not a comment");
    }

    [TestMethod]
    public void A_language_other_than_razor_csharp_css_or_json_throws_rather_than_being_read_as_razor()
    {
        Assert.ThrowsExactly<ArgumentException>(() => SyntaxHighlighter.Highlight("var x = a < b;", "cs"));
        Assert.ThrowsExactly<ArgumentException>(() => SyntaxHighlighter.HighlightOrThrow("var x = a < b;", "c#"));
        Assert.Contains("[key:var]", Tokens("var x = 1;", "CSharp"), "the language's name is matched regardless of case");
        Assert.Contains("[attr:color]", Tokens("a { color: red; }", "CSS"), "the language's name is matched regardless of case");
    }

    [TestMethod]
    public void A_stylesheet_tells_its_selectors_from_its_declarations()
    {
        var tokens = Tokens("""
            /* after the library's stylesheet */
            :root[data-theme="dark"] {
                --bit-balette-accent: #2dd4bf;
                --bit-balette-width: 760px;
            }

            .my-palette [data-balette="item"][data-active] > kbd { background: var(--bit-balette-bg) !important; opacity: .5; }

            @media (max-width: 600px) {
                .my-palette { font-size: 0.75rem }
            }
            """, "css");

        Assert.Contains("[com:/* after the library's stylesheet */]", tokens, "the comment is not a comment");
        Assert.Contains("[fn::root][pun:[][attr:data-theme]=[str:\"dark\"][pun:]]", tokens, "the attribute selector is not one");
        Assert.Contains("[attr:--bit-balette-accent][pun::] [num:#2dd4bf][pun:;]", tokens, "a custom property's colour is not a value");
        Assert.Contains("[num:760px]", tokens, "a length is not a number");
        Assert.Contains("[type:.my-palette] [pun:[][attr:data-balette]=[str:\"item\"][pun:]][pun:[][attr:data-active][pun:]] > [tag:kbd]", tokens, "the selector is not a selector");
        Assert.Contains("[fn:var]([attr:--bit-balette-bg]) [key:!important]", tokens, "a function, a custom property or !important is not one");
        Assert.Contains("[attr:opacity][pun::] [num:.5]", tokens, "a fraction is not a number");
        Assert.Contains("[key:@media] ([attr:max-width]: [num:600px])", tokens, "the media query is not one");
        Assert.Contains("[attr:font-size][pun::] [num:0.75rem] [pun:}]", tokens, "a declaration without a ';' before the '}' is not one");
    }

    [TestMethod]
    public void A_json_key_is_told_from_a_string_value()
    {
        var tokens = Tokens("""
            {
              "name": "balette",
              "port": 5000,
              "secure": true,
              "tags": ["mcp", null]
            }
            """, "json");

        Assert.Contains("[attr:\"name\"]: [str:\"balette\"]", tokens);
        Assert.Contains("[attr:\"port\"]: [num:5000]", tokens);
        Assert.Contains("[attr:\"secure\"]: [key:true]", tokens);
        Assert.Contains("[[str:\"mcp\"], [key:null]]", tokens);
    }

    [TestMethod]
    public void A_member_the_source_declares_is_not_coloured_as_a_type()
    {
        var tokens = Tokens("""
            @inject IBaletteService Palette

            <div>@Command.Title</div>
            <span>@Now</span>

            @code {
                [Parameter] public BaletteCommand Command { get; set; } = default!;
                [Parameter] public EventCallback<int> OnStep { get; set; }

                private Task Step() => OnStep.InvokeAsync(Palette.Commands.Count);
                private BaletteCommandResult Next() => BaletteCommandResult.KeepOpen;
            }
            """);

        foreach (var member in new[] { "Command", "OnStep", "Palette" })
        {
            Assert.DoesNotContain($"[type:{member}].", tokens, $"{member} is coloured as a type");
        }
        Assert.Contains("[type:BaletteCommandResult].KeepOpen", tokens, "a type's static member lost the type's colour");
        Assert.Contains("[raz:@]Now[pun:</]", tokens, "the markup after an implicit expression is taken for type arguments");
    }

    [TestMethod]
    public void A_component_parameter_that_takes_csharp_is_coloured_as_csharp_and_one_that_takes_a_string_as_a_string()
    {
        var tokens = Tokens("""
            <Balette Inline="true" Commands="_commands" Placeholder="Search files…" Density="compact" class="palette" />

            @code {
                private List<BaletteCommand> _commands = [];
            }
            """);

        Assert.Contains("[attr:Inline]=[str:\"][key:true][str:\"]", tokens, "a literal is a string");
        Assert.Contains("[attr:Commands]=[str:\"]_commands[str:\"]", tokens, "a field is a string");
        Assert.Contains("[attr:Placeholder]=[str:\"Search files…\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:Density]=[str:\"compact\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:class]=[str:\"palette\"]", tokens, "an attribute passed on to an element is not a string");
    }

    // The snippet a page keeps in a field or a property named `name`: a constant, or one built from the page's state, for
    // which the page is created with its services injected, as the renderer would - but for the browser's JS runtime,
    // which nothing a snippet is built from calls.
    private static string? ReadSnippet(Type page, string name)
    {
        const BindingFlags Members = BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        if (page.GetField(name, Members) is { } field)
        {
            return (string?)field.GetValue(field.IsStatic ? null : Instance(page));
        }
        if (page.GetProperty(name, Members) is { } property)
        {
            return (string?)property.GetValue(property.GetMethod!.IsStatic ? null : Instance(page));
        }
        return null;
    }

    private static object Instance(Type page)
    {
        var services = new ServiceCollection()
            .AddDemoServices()
            .AddScoped<IJSRuntime, NoJSRuntime>()
            .BuildServiceProvider()
            .CreateScope()
            .ServiceProvider;

        var instance = Activator.CreateInstance(page)!;
        foreach (var property in page.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        {
            if (property.IsDefined(typeof(InjectAttribute)))
            {
                property.SetValue(instance, services.GetService(property.PropertyType));
            }
        }
        return instance;
    }

    private sealed class NoJSRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
    }

    // Highlights `code` without the safety net, and checks that every character of it is kept and some are coloured.
    private static void AssertHighlighted(string name, string code, string language)
    {
        var html = SyntaxHighlighter.HighlightOrThrow(code, language);

        Assert.AreEqual(code.Replace("\r\n", "\n"), WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", "")), $"{name}: the highlighted text differs from the source");
        Assert.Contains("class=\"tok-", html, $"{name} is not coloured");
    }

    /// <summary>
    /// The highlighted text with each token written as <c>[kind:text]</c>, adjacent tokens of one kind as one -
    /// the quotes and the text of a string are a single <c>[str:"item"]</c> - and the entities decoded.
    /// </summary>
    private static string Tokens(string code, string language = "razor")
    {
        var tokens = Regex.Replace(SyntaxHighlighter.HighlightOrThrow(code, language), "<span class=\"tok-(\\w+)\">([^<]*)</span>", "[$1:$2]");
        string merged;
        while ((merged = Regex.Replace(tokens, @"\[(\w+):([^\]]*)\]\[\1:", "[$1:$2")) != tokens)
        {
            tokens = merged;
        }
        return WebUtility.HtmlDecode(tokens);
    }
}

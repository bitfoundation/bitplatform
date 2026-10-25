using System.Net;
using System.Text.RegularExpressions;
using Bit.Bortable.Demo.Client.Examples;

namespace Bit.Bortable.Tests.Mcp;

/// <summary>
/// The demo's code viewer: every source a "View code" panel shows goes through the highlighter without its safety
/// net, so a lexer bug fails here rather than shipping as an uncoloured snippet; and the colours tell apart what a
/// reader has to - a member from a type, C# from text.
/// </summary>
[TestClass]
public class DemoCodeHighlighterTests
{
    /// <summary>Every source the demo shows: each file of the Samples folder, in the language DemoCode colours it in.</summary>
    public static IEnumerable<object[]> Sources =>
        DemoSources.Files.Select(file => new object[] { file.Key, file.Value, file.Key.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ? "csharp" : "razor" });

    [TestMethod]
    [DynamicData(nameof(Sources))]
    public void Every_demo_source_is_highlighted_without_a_lexer_error_and_keeps_its_text(string name, string code, string language)
    {
        var html = SyntaxHighlighter.HighlightOrThrow(code, language);

        Assert.AreEqual(code.Replace("\r\n", "\n"), WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", "")), $"{name}: the highlighted text differs from the source");
        Assert.Contains("class=\"tok-", html, $"{name} is not coloured");
    }

    [TestMethod]
    public void A_member_the_source_declares_is_not_coloured_as_a_type()
    {
        var tokens = Tokens("""
            @inject IJSRuntime JS

            <div>@Card.Text</div>
            <span>@Now</span>

            @code {
                private static readonly string[] Columns = ["To do", "Done"];

                [Parameter] public CardModel Card { get; set; } = default!;
                [Parameter] public EventCallback<int> OnMove { get; set; }

                private Task Begin() => OnMove.InvokeAsync(Columns.Length);
                private ValueTask Focus() => JS.InvokeVoidAsync("focus");
                private string Kind() => BortablePull.Clone.ToString();
            }
            """);

        foreach (var member in new[] { "Card", "OnMove", "Columns", "JS" })
        {
            Assert.DoesNotContain($"[type:{member}].", tokens, $"{member} is coloured as a type");
        }
        Assert.Contains("[type:BortablePull].", tokens, "a type's static member lost the type's colour");
        Assert.Contains("[raz:@]Now[pun:</]", tokens, "the markup after an implicit expression is taken for type arguments");
    }

    [TestMethod]
    public void A_bortable_parameter_that_takes_csharp_is_coloured_as_csharp_and_one_that_takes_a_string_as_a_string()
    {
        var tokens = Tokens("""
            @foreach (var column in _columns)
            {
                <Bortable TItem="Card" Items="column.Cards" Group="board" Handle=".grip" Animation="200" Disabled="true"
                          OnAdd="Completed" Direction="BortableDirection.Vertical" class="todo" />
            }

            @code {
                private readonly List<Column> _columns = [];

                private void Completed(BortableEventArgs<Card> e) { }
            }
            """);

        // `Group` takes a string and `Items` does not: the same kind of word is text in one and C# in the other
        Assert.Contains("[attr:Group]=[str:\"board\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:Handle]=[str:\".grip\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:class]=[str:\"todo\"]", tokens, "an attribute passed on to the list's element is not a string");
        Assert.Contains("[attr:Items]=[str:\"]column.Cards[str:\"]", tokens, "a variable is a string");
        Assert.Contains("[attr:Animation]=[str:\"][num:200][str:\"]", tokens, "a number is a string");
        Assert.Contains("[attr:Disabled]=[str:\"][key:true][str:\"]", tokens, "a literal is a string");
        Assert.Contains("[attr:OnAdd]=[str:\"][fn:Completed][str:\"]", tokens, "a method group is a string");
        Assert.Contains("[attr:Direction]=[str:\"][type:BortableDirection].Vertical[str:\"]", tokens, "an enum value is a string");
        Assert.Contains("[attr:TItem]=[str:\"][type:Card][str:\"]", tokens, "a type argument is not a type");
    }

    [TestMethod]
    public void A_component_that_cannot_be_looked_up_has_its_values_read_by_their_shape()
    {
        var tokens = Tokens("""
            <Chart Points="_points" OnSelect="Pick" Height="240" Title="Sales by month" />

            @code {
                private readonly double[] _points = [];
            }
            """);

        Assert.Contains("[attr:Points]=[str:\"]_points[str:\"]", tokens, "a field is a string");
        Assert.Contains("[attr:OnSelect]=[str:\"]Pick[str:\"]", tokens, "a callback is a string");
        Assert.Contains("[attr:Height]=[str:\"][num:240][str:\"]", tokens, "a number is a string");
        Assert.Contains("[attr:Title]=[str:\"Sales by month\"]", tokens, "text is not a string");
    }

    [TestMethod]
    public void A_plain_csharp_file_is_coloured_as_csharp()
    {
        var tokens = Tokens("""
            namespace Bit.Bortable.Demo.Client.Pages;

            public sealed class NestedItem
            {
                public string Title { get; set; } = "";
                public List<NestedItem> Children { get; } = [];
            }
            """, "csharp");

        Assert.Contains("[key:public] [key:sealed] [key:class] [type:NestedItem]", tokens);
        Assert.Contains("[key:public] [key:string] Title", tokens);
        Assert.Contains("[type:List]<[type:NestedItem]>", tokens);
    }

    [TestMethod]
    public void A_generic_method_is_a_method_and_its_type_arguments_are_types()
    {
        var tokens = Tokens("""
            var modes = Enum.GetValues<Mode>();
            private static T Make<T>() => default!;
            """, "csharp");

        Assert.Contains("[fn:GetValues]<[type:Mode]>", tokens);
        Assert.Contains("[fn:Make]<[type:T]>", tokens);
    }

    [TestMethod]
    public void A_tuple_element_name_in_type_arguments_is_not_a_type()
    {
        var tokens = Tokens("[Parameter] public EventCallback<(int From, int To)> OnMove { get; set; }", "csharp");

        Assert.Contains("[type:EventCallback]<([key:int] From, [key:int] To)>", tokens);
    }

    [TestMethod]
    public void A_component_named_like_a_void_element_has_content()
    {
        var tokens = Tokens("""
            @foreach (var link in _links)
            {
                <Link Href="x">Don't go</Link>
            }

            @code {
                private string[] _links = [];
            }
            """);

        Assert.Contains("Don't go[pun:</][type:Link][pun:>]", tokens, "the component's content is not markup");
        Assert.Contains("[key:private]", tokens, "the code block after the component is not C#");
    }

    [TestMethod]
    public void A_catch_with_an_exception_filter_keeps_its_body_a_code_block()
    {
        var tokens = Tokens("""
            @try { <p>ok</p> } catch (Exception ex) when (ex is IOException) { <p>@ex.Message</p> }
            """);

        Assert.Contains("[key:when]", tokens);
        Assert.Contains("[tag:p][pun:>][raz:@]ex.Message[pun:</][tag:p]", tokens);
    }

    [TestMethod]
    public void An_interpolation_hole_takes_an_alias_qualifier_and_spans_lines()
    {
        var tokens = Tokens("""
            var a = $"{global::System.Environment.NewLine}";
            var b = $"{
                Format(x)}";
            var c = 1;
            """, "csharp");

        Assert.Contains("[key:global]::[type:System].Environment.NewLine}", tokens, "the alias qualifier is taken for a format");
        Assert.Contains("[fn:Format](x)}[str:\"]", tokens, "a hole across lines ends at the line break");
        Assert.Contains("[key:var] c = [num:1];", tokens, "the code after a hole across lines is not C#");
    }

    /// <summary>
    /// The highlighted text with each token written as <c>[kind:text]</c>, adjacent tokens of one kind as one -
    /// the quotes and the text of a string are a single <c>[str:"box"]</c> - and the entities decoded.
    /// </summary>
    private static string Tokens(string code, string language = "razor")
    {
        var tokens = Regex.Replace(SyntaxHighlighter.HighlightOrThrow(code, language), "<span class=\"tok-(\\w+)\">([^<]*)</span>", "[$1:$2]");
        string merged;
        while ((merged = Regex.Replace(tokens, @"\[(\w+):([^\]]*)\]\[\1:", "[$1:$2")) != tokens) tokens = merged;
        return WebUtility.HtmlDecode(tokens);
    }
}

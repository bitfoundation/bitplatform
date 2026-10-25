using System.Net;
using System.Text.RegularExpressions;
using Bit.Bdnd.Demo.Client.Examples;

namespace Bit.Bdnd.Tests.Mcp;

/// <summary>
/// The demo's code viewer: every source a "View code" panel or a page shows goes through the highlighter without
/// its safety net, so a lexer bug fails here rather than shipping as an uncoloured snippet; and the colours tell
/// apart what a reader has to - a member from a type, C# from text.
/// </summary>
[TestClass]
public class DemoCodeHighlighterTests
{
    /// <summary>Every source the demo shows: each page's files, keyed by route, and every component on its own.</summary>
    public static IEnumerable<object[]> Sources =>
        DemoSources.ByRoute
            .SelectMany(route => route.Value.Select(file => new object[] { $"/{route.Key}: {file.Name}", file.Code }))
            .Concat(DemoSources.Components.Select(component => new object[] { component.Key, component.Value }));

    [TestMethod]
    [DynamicData(nameof(Sources))]
    public void Every_demo_source_is_highlighted_without_a_lexer_error_and_keeps_its_text(string name, string code)
    {
        var html = SyntaxHighlighter.HighlightOrThrow(code);

        Assert.AreEqual(code.Replace("\r\n", "\n"), WebUtility.HtmlDecode(Regex.Replace(html, "<[^>]+>", "")), $"{name}: the highlighted text differs from the source");
        Assert.Contains("class=\"tok-", html, $"{name} is not coloured");
    }

    [TestMethod]
    public void A_member_the_source_declares_is_not_coloured_as_a_type()
    {
        var tokens = Tokens("""
            @inject BdndInterop Interop

            <div>@Card.Text</div>
            <span>@Now</span>

            @code {
                private static readonly string[] Columns = ["To do", "Done"];

                [Parameter] public CardModel Card { get; set; } = default!;
                [Parameter] public EventCallback<int> OnDragBegin { get; set; }

                private Task Begin() => OnDragBegin.InvokeAsync(Columns.Length);
                private Task Measure() => Interop.GetBoundingClientRectAsync(_el);
                private string Kind() => ItemTypes.Box;
            }
            """);

        foreach (var member in new[] { "Card", "OnDragBegin", "Columns", "Interop" })
        {
            Assert.DoesNotContain($"[type:{member}].", tokens, $"{member} is coloured as a type");
        }
        Assert.Contains("[type:ItemTypes].", tokens, "a type's static member lost the type's colour");
        Assert.Contains("[raz:@]Now[pun:</]", tokens, "the markup after an implicit expression is taken for type arguments");
    }

    [TestMethod]
    public void A_component_parameter_that_takes_csharp_is_coloured_as_csharp_and_one_that_takes_a_string_as_a_string()
    {
        var tokens = Tokens("""
            @foreach (var note in _notes)
            {
                <BdndDraggable TItem="string" Type="note" Item="note" Context="drag" class="note" />
            }
            <BdndDropTarget TItem="SortableCard.DragItem" Accept="note" OnDrop="Store" Disabled="true" Label="Drop here"
                            Edges="BdndEdges.Vertical" />

            @code {
                private readonly string[] _notes = ["Glass"];

                private void Store(BdndDropEventArgs<string> e) { }
            }
            """);

        // `Type` takes a string and `Item` does not: the same word is text in one and a variable in the other
        Assert.Contains("[attr:Type]=[str:\"note\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:Item]=[str:\"]note[str:\"]", tokens, "a variable is a string");
        Assert.Contains("[attr:Accept]=[str:\"note\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:Label]=[str:\"Drop here\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:class]=[str:\"note\"]", tokens, "an attribute passed on to an element is not a string");
        Assert.Contains("[attr:OnDrop]=[str:\"][fn:Store][str:\"]", tokens, "a method group is a string");
        Assert.Contains("[attr:Disabled]=[str:\"][key:true][str:\"]", tokens, "a literal is a string");
        Assert.Contains("[attr:Edges]=[str:\"][type:BdndEdges].Vertical[str:\"]", tokens, "an enum value is a string");
        Assert.Contains("[attr:TItem]=[str:\"][type:SortableCard].[type:DragItem][str:\"]", tokens, "a type argument is not a type");
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

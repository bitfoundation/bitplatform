using System.Net;
using System.Text.RegularExpressions;
using Bit.Blow.Demo.Client.Examples;

namespace Bit.Blow.Tests.Mcp;

/// <summary>
/// The demo's code viewer: every source a "View code" panel shows goes through the highlighter without its safety
/// net, so a lexer bug fails here rather than shipping as an uncoloured snippet; and the colours tell apart what a
/// reader has to - a member from a type, C# from text.
/// </summary>
[TestClass]
public class DemoCodeHighlighterTests
{
    /// <summary>Every source the demo's "View code" panels show, keyed by route.</summary>
    public static IEnumerable<object[]> Sources =>
        DemoSources.ByRoute.Select(route => new object[] { $"/{route.Key}", route.Value });

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
            @inject NavigationManager Nav

            <div>@Node.Label</div>
            <span>@Now</span>

            @code {
                private static readonly string[] Columns = ["To do", "Done"];

                [Parameter] public BlowNode Node { get; set; } = default!;
                [Parameter] public EventCallback<int> OnStep { get; set; }

                private Task Step() => OnStep.InvokeAsync(Columns.Length);
                private void Home() => Nav.NavigateTo("/");
                private BlowPosition Side() => BlowPosition.Left;
            }
            """);

        foreach (var member in new[] { "Node", "OnStep", "Columns", "Nav" })
        {
            Assert.DoesNotContain($"[type:{member}].", tokens, $"{member} is coloured as a type");
        }
        Assert.Contains("[type:BlowPosition].", tokens, "a type's static member lost the type's colour");
        Assert.Contains("[raz:@]Now[pun:</]", tokens, "the markup after an implicit expression is taken for type arguments");
    }

    [TestMethod]
    public void A_component_parameter_that_takes_csharp_is_coloured_as_csharp_and_one_that_takes_a_string_as_a_string()
    {
        var tokens = Tokens("""
            @foreach (var node in _nodes)
            {
                <BlowHandle Type="BlowHandleType.Target" Id="node" IsConnectable="true" class="handle" />
            }
            <BlowCanvas Nodes="_nodes" AriaLabel="A live approval workflow" OnConnect="Store" />
            <Virtualize TItem="BlowNode" Items="_nodes" />

            @code {
                private readonly List<BlowNode> _nodes = [];

                private void Store(BlowConnection connection) { }
            }
            """);

        // `Id` takes a string and `Nodes` does not: the loop variable is text in one, and the list a field in the other
        Assert.Contains("[attr:Id]=[str:\"node\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:Nodes]=[str:\"]_nodes[str:\"]", tokens, "a field is a string");
        Assert.Contains("[attr:AriaLabel]=[str:\"A live approval workflow\"]", tokens, "a string is not a string");
        Assert.Contains("[attr:class]=[str:\"handle\"]", tokens, "an attribute passed on to an element is not a string");
        Assert.Contains("[attr:OnConnect]=[str:\"][fn:Store][str:\"]", tokens, "a method group is a string");
        Assert.Contains("[attr:IsConnectable]=[str:\"][key:true][str:\"]", tokens, "a literal is a string");
        Assert.Contains("[attr:Type]=[str:\"][type:BlowHandleType].Target[str:\"]", tokens, "an enum value is a string");
        Assert.Contains("[attr:TItem]=[str:\"][type:BlowNode][str:\"]", tokens, "a type argument is not a type");
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
    public void A_render_fragment_template_switches_back_to_markup()
    {
        var tokens = Tokens("""
            <BlowCanvas Nodes="_nodes">
                <NodeTemplate Context="node">
                    @if (node.Type == "color")
                    {
                        <div>@node.Label</div>
                    }
                    else
                    {
                        <BlowDefaultNode Node="node" />
                    }
                </NodeTemplate>
            </BlowCanvas>

            @code {
                private List<BlowNode> _nodes = [];
            }
            """);

        Assert.Contains("[key:if] (node.Type == [str:\"color\"])", tokens, "the control statement is not C#");
        Assert.Contains("[tag:div][pun:>][raz:@]node.Label[pun:</][tag:div]", tokens, "the statement's body is not markup");
        Assert.Contains("[type:BlowDefaultNode]", tokens, "the component in the else branch is not a component");
        Assert.Contains("[key:private]", tokens, "the code block after the template is not C#");
    }

    [TestMethod]
    public void A_generic_method_is_a_method_and_its_type_arguments_are_types()
    {
        var tokens = Tokens("""
            var modes = Enum.GetValues<BlowPosition>();
            private static T Make<T>() => default!;
            """, "csharp");

        Assert.Contains("[fn:GetValues]<[type:BlowPosition]>", tokens);
        Assert.Contains("[fn:Make]<[type:T]>", tokens);
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
    /// the quotes and the text of a string are a single <c>[str:"node"]</c> - and the entities decoded.
    /// </summary>
    private static string Tokens(string code, string language = "razor")
    {
        var tokens = Regex.Replace(SyntaxHighlighter.HighlightOrThrow(code, language), "<span class=\"tok-(\\w+)\">([^<]*)</span>", "[$1:$2]");
        string merged;
        while ((merged = Regex.Replace(tokens, @"\[(\w+):([^\]]*)\]\[\1:", "[$1:$2")) != tokens) tokens = merged;
        return WebUtility.HtmlDecode(tokens);
    }
}

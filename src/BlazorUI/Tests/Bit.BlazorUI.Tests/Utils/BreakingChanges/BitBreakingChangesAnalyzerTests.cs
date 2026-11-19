using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bit.BlazorUI.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.BlazorUI.Tests.Utils.BreakingChanges;

/// <summary>
/// Runs <see cref="BitBreakingChangesAnalyzer"/> over compilations against the packages as compiled: C# written by
/// hand, and the C# the Razor compiler generates for a .razor file, written here the way it generates it (the builder
/// calls, the <c>#line</c> regions mapping the C# of the markup back to the .razor file, the hidden regions between
/// them) with the .razor file beside it as an additional file, as the Razor SDK passes it.
/// </summary>
[TestClass]
public sealed class BitBreakingChangesAnalyzerTests
{
    private const string RazorPath = @"C:\app\Pages\Page.razor";

    [TestMethod]
    public async Task AnAttributeThatIsGoneIsReportedWhereItIsWrittenInTheRazorFile()
    {
        var razor = """
                    <BitNumberField TValue="int" InputReadOnly />
                    <BitNumberField TValue="int"
                                    IsInputReadOnly />
                    <BitNumberField TValue="int" IsInputReadOnly="@flag" />
                    """;

        var generated = Page("""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitNumberField<int>>(0);
                    __builder.AddComponentParameter(1, nameof(global::Bit.BlazorUI.BitNumberField<int>.
            #nullable restore
            #line (1,30)-(1,43) "C:\app\Pages\Page.razor"
            InputReadOnly

            #line default
            #line hidden
            #nullable disable
                    ), true);
                    __builder.CloseComponent();
                    __builder.OpenComponent<global::Bit.BlazorUI.BitNumberField<int>>(2);
                    __builder.AddComponentParameter(3, "IsInputReadOnly", true);
                    __builder.CloseComponent();
                    __builder.OpenComponent<global::Bit.BlazorUI.BitNumberField<int>>(4);
                    __builder.AddComponentParameter(5, "IsInputReadOnly", (object)(
            #nullable restore
            #line (4,48)-(4,52) "C:\app\Pages\Page.razor"
            flag

            #line default
            #line hidden
            #nullable disable
                    ));
                    __builder.CloseComponent();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(2, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 3, 17);
        AssertAt(diagnostics[1], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 4, 30);
        StringAssert.Contains(diagnostics[0].GetMessage(), "InputReadOnly");
        StringAssert.Contains(diagnostics[0].GetMessage(), "11.0.0");
    }

    [TestMethod]
    public async Task ABindingIsReportedOnceAtItsNameAndAChangedCallbackWrittenOutOnItsOwn()
    {
        var razor = """
                    <BitToggleButton @bind-IsChecked="on" />
                    <BitToggleButton IsChecked="on" IsCheckedChanged="OnChanged" />
                    """;

        // @bind-IsChecked is generated as IsChecked and IsCheckedChanged, of which only IsChecked is in the markup; the
        // values are C#, mapped back to where they are written.
        var generated = Page($"""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitToggleButton>(0);
                    __builder.AddComponentParameter(1, "IsChecked", (object)({Mapped(1, 35, "on")}));
                    __builder.AddComponentParameter(2, "IsCheckedChanged", (object)null);
                    __builder.CloseComponent();
                    __builder.OpenComponent<global::Bit.BlazorUI.BitToggleButton>(3);
                    __builder.AddComponentParameter(4, "IsChecked", (object)({Mapped(2, 29, "on")}));
                    __builder.AddComponentParameter(5, "IsCheckedChanged", (object)({Mapped(2, 51, "null")}));
                    __builder.CloseComponent();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(3, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 1, 24);
        StringAssert.Contains(diagnostics[0].GetMessage(), "CheckedChanged");
        AssertAt(diagnostics[1], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 18);
        AssertAt(diagnostics[2], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 33);
        StringAssert.Contains(diagnostics[2].GetMessage(), "IsCheckedChanged is CheckedChanged");
    }

    [TestMethod]
    public async Task AParameterOfTheBaseComponentIsReportedOnEveryComponentAndOnAnAppsSubclass()
    {
        var razor = """
                    <BitButton IsEnabled="false" />
                    <MyButton IsEnabled="false" />
                    """;

        var generated = Page("""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitButton>(0);
                    __builder.AddComponentParameter(1, "IsEnabled", false);
                    __builder.CloseComponent();
                    __builder.OpenComponent<global::App.MyButton>(2);
                    __builder.AddComponentParameter(3, "IsEnabled", false);
                    __builder.CloseComponent();
            """, extra: "public class MyButton : global::Bit.BlazorUI.BitButton { }");

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(2, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 1, 12);
        AssertAt(diagnostics[1], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 11);
        StringAssert.Contains(diagnostics[0].GetMessage(), "Disabled");
    }

    [TestMethod]
    public async Task ANameTheComponentTakesNowAndTheAttributesOfAnElementAreLeftAlone()
    {
        var razor = """
                    <MyButton IsLoading="true" />
                    <div IsEnabled="false"></div>
                    """;

        // The app's own component declares a parameter of the old name; an element's attribute is no parameter at all.
        var generated = Page("""
                    __builder.OpenComponent<global::App.MyButton>(0);
                    __builder.AddComponentParameter(1, "IsLoading", true);
                    __builder.CloseComponent();
                    __builder.OpenElement(2, "div");
                    __builder.AddAttribute(3, "IsEnabled", "false");
                    __builder.CloseElement();
            """, extra: """
                        public class MyButton : global::Bit.BlazorUI.BitButton
                        {
                            [global::Microsoft.AspNetCore.Components.Parameter] public bool IsLoading { get; set; }
                        }
                        """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(0, diagnostics.Length, Describe(diagnostics));
    }

    [TestMethod]
    public async Task AChildContentTagThatIsGoneIsReportedWhetherItIsAnElementOrStaticMarkup()
    {
        var razor = """
                    <BitMediaQuery Query="BitScreenQuery.Md">
                        <Matched>@wide</Matched>
                        <NotMatched><span>narrow</span></NotMatched>
                    </BitMediaQuery>
                    """;

        var generated = Page("""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitMediaQuery>(0);
                    __builder.AddComponentParameter(1, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                        __builder2.OpenElement(2, "Matched");
                        __builder2.AddContent(3, "wide");
                        __builder2.CloseElement();
                        __builder2.AddMarkupContent(4, "<NotMatched><span>narrow</span></NotMatched>");
                    }));
                    __builder.CloseComponent();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(2, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 6);
        StringAssert.Contains(diagnostics[0].GetMessage(), "MatchedTemplate");
        AssertAt(diagnostics[1], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 3, 6);
        StringAssert.Contains(diagnostics[1].GetMessage(), "NotMatchedTemplate");
    }

    [TestMethod]
    public async Task TheTagOfAComponentThatIsGoneIsReportedUnlessItIsInACommentOrTheAppHasOneOfThatName()
    {
        var razor = """
                    @* <BitProPanel /> was the panel *@
                    <BitProModal IsOpen="true" />
                    <BitQuickGrid Items="items"></BitQuickGrid>
                    """;

        var withoutOwn = await Analyze(Page(""), razor);

        Assert.AreEqual(2, withoutOwn.Length, Describe(withoutOwn));
        AssertAt(withoutOwn[0], BitBreakingChangesAnalyzer.TypeDiagnosticId, RazorPath, 2, 2);
        StringAssert.Contains(withoutOwn[0].GetMessage(), "BitModal");
        AssertAt(withoutOwn[1], BitBreakingChangesAnalyzer.TypeDiagnosticId, RazorPath, 3, 2);

        var withOwn = await Analyze(Page("", extra: "public class BitProModal : global::Microsoft.AspNetCore.Components.ComponentBase { }"), razor);

        Assert.AreEqual(1, withOwn.Length, Describe(withOwn));
        StringAssert.Contains(withOwn[0].GetMessage(), "BitQuickGrid");
    }

    [TestMethod]
    public async Task CSharpThatNamesATypeOrAMemberThatIsGoneIsToldWhatToWriteInstead()
    {
        var source = """
                     namespace App;

                     public class Code
                     {
                         public void M(global::Bit.BlazorUI.BitButton button, global::Bit.BlazorUI.BitFullCalendarState state)
                         {
                             var placement = BitPanelPosition.End;
                             var item = new global::Bit.BlazorUI.BitDropdownItem<string> { Text = "Apple", IsEnabled = false };
                             var p = new global::Bit.BlazorUI.BitButtonParams { IsLoading = true };
                             var enabled = button.IsEnabled;
                             var disabled = button.Disabled;
                             state.GoToToday();
                             var events = state.ExpandedEvents;
                         }
                     }
                     """;

        var diagnostics = await Analyze(source);

        // GoToToday is internal now rather than removed: found, but no more usable than a removed one.
        CollectionAssert.AreEquivalent(
            new[] { "BITBC002 BitPanelPosition", "BITBC001 IsEnabled", "BITBC001 IsLoading", "BITBC001 IsEnabled", "BITBC001 GoToToday" },
            diagnostics.Select(d => $"{d.Id} {d.Location.SourceTree!.GetText().ToString(d.Location.SourceSpan)}").ToArray(),
            Describe(diagnostics));

        var itemHint = diagnostics.Single(d => d.GetMessage().Contains("BitDropdownItem")).GetMessage();
        StringAssert.Contains(itemHint, "IsDisabled");
    }

    [TestMethod]
    public async Task AProjectThatDoesNotReferenceThePackagesIsNotAnalyzed()
    {
        var source = """
                     public class BitPanelPosition { }
                     public class Code { void M() { var x = BitPanelPositio.End; } }
                     """;

        var compilation = CSharpCompilation.Create("NoBit", [CSharpSyntaxTree.ParseText(source)],
                                                   References().Where(r => r.Display?.Contains("Bit.BlazorUI") is not true),
                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = await compilation.WithAnalyzers([new BitBreakingChangesAnalyzer()]).GetAnalyzerDiagnosticsAsync();

        Assert.AreEqual(0, diagnostics.Length, Describe(diagnostics));
    }

    [TestMethod]
    public void RazorCommentsAreBlankedOutWithoutMovingAnything()
    {
        var text = "a @* <BitProModal />\n *@ b";

        var blanked = BitBreakingChangesAnalyzer.WithoutRazorComments(text);

        Assert.AreEqual(text.Length, blanked.Length);
        Assert.AreEqual("a", blanked[..1]);
        Assert.AreEqual('\n', blanked[text.IndexOf('\n')]);
        Assert.IsFalse(blanked.Contains("BitProModal"));
        Assert.IsTrue(blanked.EndsWith(" b"));
    }

    private static string Page(string body, string extra = "")
        => $$"""
             #pragma checksum "C:\app\Pages\Page.razor" "{8829d00f-11b8-4213-878b-770e8597ac16}" "00"
             // <auto-generated/>
             #pragma warning disable 1591
             namespace App
             {
             #nullable restore
             #line (1,2)-(1,20) "C:\app\_Imports.razor"
             using Bit.BlazorUI

             #nullable disable
                 ;
                 #line default
                 #line hidden
                 public partial class Page : global::Microsoft.AspNetCore.Components.ComponentBase
                 {
                     #pragma warning disable 1998
                     protected override void BuildRenderTree(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder)
                     {
             {{body}}
                     }
                     #pragma warning restore 1998
                     private bool flag = true;
                     private bool on;
                     private string wide = "";
                 }
                 {{extra}}
             }
             #pragma warning restore 1591
             """;

    /// <summary>A C# expression of the markup as the Razor compiler writes it: on lines of its own, mapped to the .razor file.</summary>
    private static string Mapped(int line, int column, string expression)
        => $"""

            #nullable restore
            #line ({line},{column})-({line},{column + expression.Length}) "{RazorPath}"
            {expression}

            #line default
            #line hidden
            #nullable disable

            """;

    private static async Task<ImmutableArray<Diagnostic>> Analyze(string source, string? razor = null)
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest), path: @"C:\app\obj\Page_razor.g.cs");
        var compilation = CSharpCompilation.Create("App", [tree], References(), new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var additional = razor is null ? ImmutableArray<AdditionalText>.Empty : ImmutableArray.Create<AdditionalText>(new InMemoryText(RazorPath, razor));
        var options = new CompilationWithAnalyzersOptions(new AnalyzerOptions(additional), onAnalyzerException: null, concurrentAnalysis: false, logAnalyzerExecutionTime: false);

        var diagnostics = await compilation.WithAnalyzers([new BitBreakingChangesAnalyzer()], options).GetAnalyzerDiagnosticsAsync();

        return [.. diagnostics.OrderBy(d => d.Location.GetMappedLineSpan().Path, StringComparer.Ordinal)
                              .ThenBy(d => d.Location.GetMappedLineSpan().StartLinePosition)];
    }

    private static IEnumerable<MetadataReference> References()
        => ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!).Split(Path.PathSeparator)
                                                                        .Where(path => path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                                                                        .Select(path => MetadataReference.CreateFromFile(path));

    private static void AssertAt(Diagnostic diagnostic, string id, string path, int line, int column)
    {
        var span = diagnostic.Location.GetMappedLineSpan();
        Assert.AreEqual(id, diagnostic.Id, diagnostic.ToString());
        Assert.AreEqual(path, span.Path, diagnostic.ToString());
        Assert.AreEqual((line, column), (span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1), diagnostic.ToString());
    }

    private static string Describe(IEnumerable<Diagnostic> diagnostics)
        => string.Join("\n", diagnostics.Select(d => d.ToString()));

    private sealed class InMemoryText(string path, string text) : AdditionalText
    {
        public override string Path { get; } = path;

        public override SourceText GetText(CancellationToken cancellationToken = default) => SourceText.From(text);
    }
}

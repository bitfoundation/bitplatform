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
    public async Task TheChangedCallbackOfABindingIsNotTakenFromTheNextTag()
    {
        var razor = """
                    <BitToggleButton @bind-IsChecked="on" />
                    <BitToggleButton IsChecked="on" IsCheckedChanged="cb" />
                    """;

        // As the Razor compiler writes it: the binding's callback is hidden code of its own, and the attributes of a name
        // that is no parameter are string literals, so nothing of the second tag is mapped.
        var generated = Page($"""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitToggleButton>(0);
                    __builder.AddComponentParameter(1, "IsChecked", {Mapped(1, 35, "on")});
                    __builder.AddComponentParameter(2, "IsCheckedChanged", global::Microsoft.AspNetCore.Components.CompilerServices.RuntimeHelpers.CreateInferredEventCallback(this, __value => on = __value, on));
                    __builder.CloseComponent();
                    __builder.AddMarkupContent(3, "\n");
                    __builder.OpenComponent<global::Bit.BlazorUI.BitToggleButton>(4);
                    __builder.AddComponentParameter(5, "IsChecked", "on");
                    __builder.AddComponentParameter(6, "IsCheckedChanged", "cb");
                    __builder.CloseComponent();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(3, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 1, 24);
        AssertAt(diagnostics[1], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 18);
        AssertAt(diagnostics[2], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 33);
        StringAssert.Contains(diagnostics[2].GetMessage(), "IsCheckedChanged is CheckedChanged");
    }

    [TestMethod]
    public async Task AnAttributeIsFoundInItsTagPastTypeArgumentsAndDirectivesWrittenOutOfOrder()
    {
        var razor = """
                    <BitDropdown TItem="BitDropdownItem<string>" TValue="string" Items="items" IsLoading="true" />
                    <BitButton @key="1" @ref="btn" isenabled="false" />
                    """;

        // The < of a type argument is no tag, and Razor maps @key and @ref after the attributes they are written before.
        var generated = Page($$"""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitDropdown<{{Mapped(1, 21, "BitDropdownItem<string>")}}, {{Mapped(1, 54, "string")}}>>(0);
                    __builder.AddComponentParameter(1, nameof(global::Bit.BlazorUI.BitDropdown<BitDropdownItem<string>, string>.Items), {{Mapped(1, 69, "items")}});
                    __builder.AddComponentParameter(2, "IsLoading", "true");
                    __builder.CloseComponent();
                    __builder.AddMarkupContent(3, "\n");
                    __builder.OpenComponent<global::Bit.BlazorUI.BitButton>(4);
                    __builder.AddAttribute(5, "isenabled", "false");
                    __builder.SetKey({{Mapped(2, 18, "1")}});
                    __builder.AddComponentReferenceCapture(6, (__value) => { {{Mapped(2, 27, "btn")}} = (global::Bit.BlazorUI.BitButton)__value; });
                    __builder.CloseComponent();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(2, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 1, 76);
        AssertAt(diagnostics[1], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 32);
    }

    [TestMethod]
    public async Task AnAttributeIsReportedWhenAValueBesideItDoesNotCompile()
    {
        var razor = """
                    <BitButton IsEnabled="@missing" />
                    """;

        // The call adding it does not bind: its argument is an error of its own (CS0103).
        var generated = Page($"""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitButton>(0);
                    __builder.AddAttribute(1, "IsEnabled", {Mapped(1, 24, "missing")});
                    __builder.CloseComponent();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(1, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 1, 12);
    }

    [TestMethod]
    public async Task AGenericComponentWhoseTypeIsCascadedIsReadAtItsOwnTag()
    {
        var razor = """
                    <MyGrid Items="names" IsEnabled="false" />
                    <MyGrid Items="names" IsEnabled="false" />
                    """;

        // A type parameter cascaded to the content has every attribute's value captured, and so mapped, before the call
        // that opens the component.
        var generated = Page($$"""
                    {
                        global::__Blazor.App.Page.TypeInference.CreateMyGrid_0_CaptureParameters({{Mapped(1, 16, "names")}}, out var __typeInferenceArg_0___arg0, "false", out var __typeInferenceArg_0___arg1);
                        global::__Blazor.App.Page.TypeInference.CreateMyGrid_0(__builder, 0, 1, __typeInferenceArg_0___arg0, 2, __typeInferenceArg_0___arg1);
                    }
                    __builder.AddMarkupContent(3, "\n");
                    {
                        global::__Blazor.App.Page.TypeInference.CreateMyGrid_1_CaptureParameters({{Mapped(2, 16, "names")}}, out var __typeInferenceArg_0___arg0, "false", out var __typeInferenceArg_0___arg1);
                        global::__Blazor.App.Page.TypeInference.CreateMyGrid_1(__builder, 4, 5, __typeInferenceArg_0___arg0, 6, __typeInferenceArg_0___arg1);
                    }
            """, extra: """
                        [global::Microsoft.AspNetCore.Components.CascadingTypeParameter("TItem")]
                        public class MyGrid<TItem> : global::Bit.BlazorUI.BitButton
                        {
                            [global::Microsoft.AspNetCore.Components.Parameter] public global::System.Collections.Generic.List<TItem>? Items { get; set; }
                        }
                        """, after: """
                        namespace __Blazor.App.Page
                        {
                            #line hidden
                            internal static class TypeInference
                            {
                                public static void CreateMyGrid_0<TItem>(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder, int seq, int __seq0, global::System.Collections.Generic.List<TItem> __arg0, int __seq1, global::System.Object __arg1)
                                {
                                __builder.OpenComponent<global::App.MyGrid<TItem>>(seq);
                                __builder.AddComponentParameter(__seq0, nameof(global::App.MyGrid<TItem>.Items), __arg0);
                                __builder.AddAttribute(__seq1, "IsEnabled", __arg1);
                                __builder.CloseComponent();
                                }

                                public static void CreateMyGrid_0_CaptureParameters<TItem>(global::System.Collections.Generic.List<TItem> __arg0, out global::System.Collections.Generic.List<TItem> __arg0_out, global::System.Object __arg1, out global::System.Object __arg1_out)
                                {
                                    __arg0_out = __arg0;
                                    __arg1_out = __arg1;
                                }

                                public static void CreateMyGrid_1<TItem>(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder, int seq, int __seq0, global::System.Collections.Generic.List<TItem> __arg0, int __seq1, global::System.Object __arg1)
                                {
                                __builder.OpenComponent<global::App.MyGrid<TItem>>(seq);
                                __builder.AddComponentParameter(__seq0, nameof(global::App.MyGrid<TItem>.Items), __arg0);
                                __builder.AddAttribute(__seq1, "IsEnabled", __arg1);
                                __builder.CloseComponent();
                                }

                                public static void CreateMyGrid_1_CaptureParameters<TItem>(global::System.Collections.Generic.List<TItem> __arg0, out global::System.Collections.Generic.List<TItem> __arg0_out, global::System.Object __arg1, out global::System.Object __arg1_out)
                                {
                                    __arg0_out = __arg0;
                                    __arg1_out = __arg1;
                                }
                            }
                        }
                        """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(2, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 1, 23);
        AssertAt(diagnostics[1], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 23);
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
    public async Task AnUpperCaseTagNestedInAnElementOfTheContentIsNoChildContentTag()
    {
        var razor = """
                    <BitMediaQuery>
                        <div><Matched>@wide</Matched></div>
                        <div><NotMatched>narrow</NotMatched></div>
                    </BitMediaQuery>
                    """;

        // Razor binds a template parameter to a tag right inside the component's content only.
        var generated = Page("""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitMediaQuery>(0);
                    __builder.AddComponentParameter(1, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                        __builder2.OpenElement(2, "div");
                        __builder2.OpenElement(3, "Matched");
                        __builder2.AddContent(4, "wide");
                        __builder2.CloseElement();
                        __builder2.CloseElement();
                        __builder2.AddMarkupContent(5, "<div><NotMatched>narrow</NotMatched></div>");
                    }));
                    __builder.CloseComponent();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(0, diagnostics.Length, Describe(diagnostics));
    }

    [TestMethod]
    public async Task AChildContentTagIsLookedForInTheContentOfItsOwnComponent()
    {
        var razor = """
                    <BitDropdown TItem="BitDropdownItem<string>" TValue="string" Items="items"><ItemTemplate><div><Matched>q</Matched></div></ItemTemplate></BitDropdown>
                    <BitMediaQuery><BitButton>in</BitButton><Matched>a</Matched></BitMediaQuery>
                    """;

        // Nothing is mapped between the first <Matched>, nested in an element of the dropdown's template, and the second,
        // right inside the query's content after a component.
        var generated = Page($$"""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitDropdown<{{Mapped(1, 21, "BitDropdownItem<string>")}}, {{Mapped(1, 54, "string")}}>>(0);
                    __builder.AddComponentParameter(1, nameof(global::Bit.BlazorUI.BitDropdown<BitDropdownItem<string>, string>.Items), {{Mapped(1, 69, "items")}});
                    __builder.AddAttribute(2, "ItemTemplate", (global::Microsoft.AspNetCore.Components.RenderFragment<BitDropdownItem<string>>)((context) => (__builder2) => {
                        __builder2.OpenElement(3, "div");
                        __builder2.OpenElement(4, "Matched");
                        __builder2.AddContent(5, "q");
                        __builder2.CloseElement();
                        __builder2.CloseElement();
                    }));
                    __builder.CloseComponent();
                    __builder.AddMarkupContent(6, "\n");
                    __builder.OpenComponent<global::Bit.BlazorUI.BitMediaQuery>(7);
                    __builder.AddAttribute(8, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                        __builder2.OpenComponent<global::Bit.BlazorUI.BitButton>(9);
                        __builder2.AddAttribute(10, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)((__builder3) => {
                            __builder3.AddContent(11, "in");
                        }));
                        __builder2.CloseComponent();
                        __builder2.OpenElement(12, "Matched");
                        __builder2.AddContent(13, "a");
                        __builder2.CloseElement();
                    }));
                    __builder.CloseComponent();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(1, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 42);
    }

    [TestMethod]
    public async Task AGenericComponentWhoseTypeArgumentsAreInferredIsReadAtItsTag()
    {
        var razor = """
                    <BitMediaQuery>
                        <BitDropdown Items="items" Value="value" IsLoading="true">
                            <Matched>x</Matched>
                        </BitDropdown>
                    </BitMediaQuery>
                    <BitDropdown Items="items" Value="value" IsLoading="true" />
                    """;

        // Razor opens such a component in a TypeInference helper at the end of the file, called where the tag is with
        // the values of its attributes; its child content is an argument of the call.
        var generated = Page($$"""
                    __builder.OpenComponent<global::Bit.BlazorUI.BitMediaQuery>(0);
                    __builder.AddComponentParameter(1, "ChildContent", (global::Microsoft.AspNetCore.Components.RenderFragment)((__builder2) => {
                        global::__Blazor.App.Page.TypeInference.CreateBitDropdown_0(__builder2, 2, 3, {{Mapped(2, 25, "items")}}, 4, {{Mapped(2, 39, "value")}}, 5, "true", 6, (__builder3) => {
                            __builder3.OpenElement(7, "Matched");
                            __builder3.AddContent(8, "x");
                            __builder3.CloseElement();
                        });
                    }));
                    __builder.CloseComponent();
                    global::__Blazor.App.Page.TypeInference.CreateBitDropdown_1(__builder, 9, 10, {{Mapped(6, 21, "items")}}, 11, {{Mapped(6, 35, "value")}}, 12, "true");
            """, after: """
                        namespace __Blazor.App.Page
                        {
                            #line hidden
                            internal static class TypeInference
                            {
                                public static void CreateBitDropdown_0<TItem, TValue>(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder, int seq, int __seq0, global::System.Collections.Generic.ICollection<TItem> __arg0, int __seq1, TValue __arg1, int __seq2, global::System.Object __arg2, int __seq3, global::Microsoft.AspNetCore.Components.RenderFragment __arg3) where TItem : class, new()
                                {
                                __builder.OpenComponent<global::Bit.BlazorUI.BitDropdown<TItem, TValue>>(seq);
                                __builder.AddComponentParameter(__seq0, nameof(global::Bit.BlazorUI.BitDropdown<TItem, TValue>.Items), __arg0);
                                __builder.AddComponentParameter(__seq1, nameof(global::Bit.BlazorUI.BitDropdown<TItem, TValue>.Value), __arg1);
                                __builder.AddAttribute(__seq2, "IsLoading", __arg2);
                                __builder.AddComponentParameter(__seq3, nameof(global::Bit.BlazorUI.BitDropdown<TItem, TValue>.ChildContent), __arg3);
                                __builder.CloseComponent();
                                }

                                public static void CreateBitDropdown_1<TItem, TValue>(global::Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder __builder, int seq, int __seq0, global::System.Collections.Generic.ICollection<TItem> __arg0, int __seq1, TValue __arg1, int __seq2, global::System.Object __arg2) where TItem : class, new()
                                {
                                __builder.OpenComponent<global::Bit.BlazorUI.BitDropdown<TItem, TValue>>(seq);
                                __builder.AddComponentParameter(__seq0, nameof(global::Bit.BlazorUI.BitDropdown<TItem, TValue>.Items), __arg0);
                                __builder.AddComponentParameter(__seq1, nameof(global::Bit.BlazorUI.BitDropdown<TItem, TValue>.Value), __arg1);
                                __builder.AddAttribute(__seq2, "IsLoading", __arg2);
                                __builder.CloseComponent();
                                }
                            }
                        }
                        """);

        var diagnostics = await Analyze(generated, razor);

        // Matched is BitMediaQuery's template that is gone, but the tag is in the dropdown's content, not the query's.
        Assert.AreEqual(2, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 2, 46);
        StringAssert.Contains(diagnostics[0].GetMessage(), "of BitDropdown");
        AssertAt(diagnostics[1], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 6, 42);
        StringAssert.Contains(diagnostics[1].GetMessage(), "Loading");
    }

    [TestMethod]
    public async Task ATagInAnHtmlCommentOrInTheCSharpOfTheFileIsNoComponentTag()
    {
        var razor = """
                    <!-- <BitProModal /> was here -->
                    <BitProPanel />
                    @code {
                        private string html = "<BitProModal>";
                        private System.Collections.Generic.List<BitProModal> modals = new();
                    }
                    """;

        // The @code block is C#, mapped back to the .razor file: a type it names is the C# analysis's to report, once.
        var generated = Page("", members: """

                    #nullable restore
                    #line (3,8)-(6,1) "C:\app\Pages\Page.razor"

                        private string html = "<BitProModal>";
                        private System.Collections.Generic.List<BitProModal> modals = new();

                    #line default
                    #line hidden
                    #nullable disable

                    """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(2, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.TypeDiagnosticId, RazorPath, 2, 2);
        StringAssert.Contains(diagnostics[0].GetMessage(), "BitProPanel");
        Assert.AreEqual(BitBreakingChangesAnalyzer.TypeDiagnosticId, diagnostics[1].Id, diagnostics[1].ToString());
        Assert.AreEqual(4, diagnostics[1].Location.GetMappedLineSpan().StartLinePosition.Line, diagnostics[1].ToString());
        StringAssert.Contains(diagnostics[1].GetMessage(), "BitProModal");
    }

    [TestMethod]
    public async Task TheTagOfAComponentThatIsGoneIsReportedUnlessItIsInACommentOrRazorBindsItToTheAppsComponent()
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

        // Razor bound the tag to the app's own component of the name: the generated code opens it.
        var withOwn = await Analyze(Page("""
                    __builder.OpenComponent<global::App.BitProModal>(0);
                    __builder.CloseComponent();
            """, extra: "public class BitProModal : global::Microsoft.AspNetCore.Components.ComponentBase { }"), razor);

        Assert.AreEqual(1, withOwn.Length, Describe(withOwn));
        StringAssert.Contains(withOwn[0].GetMessage(), "BitQuickGrid");

        // A type of the name that is no component binds no tag.
        var withOwnType = await Analyze(Page("", extra: "public class BitProModal { }"), razor);

        Assert.AreEqual(2, withOwnType.Length, Describe(withOwnType));
    }

    [TestMethod]
    public async Task TheTagOfAComponentThatIsGoneRightAfterACSharpBlockIsReported()
    {
        var razor = """
                    @if (count > 0) { <span>x</span> }
                    <BitProModal />
                    """;

        // The Razor compiler writes the end column of a mapped piece exclusive, and Roslyn reads it as written: the } of
        // the block maps to (1,34)-(2,1), whose end is the < of the tag.
        var generated = Page("""
            #nullable restore
            #line (1,2)-(1,18) "C:\app\Pages\Page.razor"
            if (count > 0) {

            #line default
            #line hidden
            #nullable disable
                    __builder.AddMarkupContent(0, "<span>x</span>");
            #nullable restore
            #line (1,34)-(2,1) "C:\app\Pages\Page.razor"
            }

            #line default
            #line hidden
            #nullable disable
                    __builder.OpenElement(1, "BitProModal");
                    __builder.CloseElement();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(1, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.TypeDiagnosticId, RazorPath, 2, 2);
    }

    [TestMethod]
    public async Task AFullyQualifiedTagOfAComponentThatIsGoneIsReportedAtItsName()
    {
        var razor = """
                    <Bit.BlazorUI.BitProModal IsOpen="true" />
                    <App.Shared.BitProModal />
                    """;

        var diagnostics = await Analyze(Page(""), razor);

        Assert.AreEqual(1, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.TypeDiagnosticId, RazorPath, 1, 15);
    }

    [TestMethod]
    public async Task AnAttributeIsFoundInItsComponentsTagNotInAnElementsValueBeforeIt()
    {
        var razor = """
                    <div title="IsEnabled is gone" data-x='IsEnabled'><BitButton IsEnabled="false" /></div>
                    """;

        var generated = Page("""
                    __builder.OpenElement(0, "div");
                    __builder.AddAttribute(1, "title", "IsEnabled is gone");
                    __builder.AddAttribute(2, "data-x", "IsEnabled");
                    __builder.OpenComponent<global::Bit.BlazorUI.BitButton>(3);
                    __builder.AddComponentParameter(4, "IsEnabled", false);
                    __builder.CloseComponent();
                    __builder.CloseElement();
            """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(1, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 1, 62);
    }

    [TestMethod]
    public async Task ANameThatIsOnlyACascadingParameterNowIsStillReported()
    {
        var razor = """
                    <MyButton IsLoading="true" />
                    """;

        var generated = Page("""
                    __builder.OpenComponent<global::App.MyButton>(0);
                    __builder.AddComponentParameter(1, "IsLoading", true);
                    __builder.CloseComponent();
            """, extra: """
                        public class MyButton : global::Bit.BlazorUI.BitButton
                        {
                            [global::Microsoft.AspNetCore.Components.CascadingParameter] public bool IsLoading { get; set; }
                        }
                        """);

        var diagnostics = await Analyze(generated, razor);

        Assert.AreEqual(1, diagnostics.Length, Describe(diagnostics));
        AssertAt(diagnostics[0], BitBreakingChangesAnalyzer.MemberDiagnosticId, RazorPath, 1, 11);
    }

    [TestMethod]
    public async Task CSharpThatNamesTheChangedCallbackOfARenamedValueIsToldItsNewName()
    {
        var source = """
                     namespace App;

                     public class Code
                     {
                         public void M(global::Bit.BlazorUI.BitToggleButton toggle)
                         {
                             var callback = toggle.IsCheckedChanged;
                         }
                     }
                     """;

        var diagnostics = await Analyze(source);

        Assert.AreEqual(1, diagnostics.Length, Describe(diagnostics));
        Assert.AreEqual(BitBreakingChangesAnalyzer.MemberDiagnosticId, diagnostics[0].Id, diagnostics[0].ToString());
        StringAssert.Contains(diagnostics[0].GetMessage(), "IsCheckedChanged is CheckedChanged");
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
    public async Task CSharpThatWritesTheNameAnyOtherWayIsToldToo()
    {
        var source = """
                     using B = Bit.BlazorUI;

                     namespace App;

                     public class Code
                     {
                         public void M(global::Bit.BlazorUI.BitButton? button, global::Bit.BlazorUI.BitMapMarker marker)
                         {
                             var qualified = Bit.BlazorUI.BitPanelPosition.End;
                             var global = global::Bit.BlazorUI.BitPanelPosition.End;
                             var aliased = B.BitPanelPosition.End;
                             var conditional = button?.IsEnabled;
                             var copy = marker with { TooltipDirection = null };
                             var texts = new global::Bit.BlazorUI.BitFullCalendarStrings { BitFcTodayButton = "Today" };
                         }
                     }
                     """;

        var diagnostics = await Analyze(source);

        // BitFcTodayButton is also the name of a component that is gone, but an initializer sets a member: one error, the member's.
        CollectionAssert.AreEquivalent(
            new[] { "BITBC002 BitPanelPosition", "BITBC002 BitPanelPosition", "BITBC002 BitPanelPosition", "BITBC001 IsEnabled", "BITBC001 TooltipDirection", "BITBC001 BitFcTodayButton" },
            diagnostics.Select(d => $"{d.Id} {d.Location.SourceTree!.GetText().ToString(d.Location.SourceSpan)}").ToArray(),
            Describe(diagnostics));

        StringAssert.Contains(diagnostics.Single(d => d.GetMessage().Contains("BitFcTodayButton")).GetMessage(), "TodayButton");
    }

    [TestMethod]
    public async Task AProjectThatDoesNotReferenceThePackagesIsNotAnalyzed()
    {
        // The very name that is reported in a project referencing the packages.
        var source = "public class Code { void M() { var x = BitPanelPosition.End; } }";

        var withPackages = await Analyze(source);

        Assert.AreEqual(1, withPackages.Length, Describe(withPackages));
        Assert.AreEqual(BitBreakingChangesAnalyzer.TypeDiagnosticId, withPackages[0].Id, withPackages[0].ToString());

        var compilation = CSharpCompilation.Create("NoBit", [CSharpSyntaxTree.ParseText(source)],
                                                   References().Where(r => r.Display?.Contains("Bit.BlazorUI") is not true),
                                                   new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var withoutPackages = await compilation.WithAnalyzers([new BitBreakingChangesAnalyzer()]).GetAnalyzerDiagnosticsAsync();

        Assert.AreEqual(0, withoutPackages.Length, Describe(withoutPackages));
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

    /// <summary>
    /// A generated .razor file: <paramref name="body"/> in BuildRenderTree, <paramref name="members"/> (an @code block) in
    /// the class, <paramref name="extra"/> beside it and <paramref name="after"/> after its namespace (the type-inference helpers).
    /// </summary>
    private static string Page(string body, string extra = "", string members = "", string after = "")
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
                     private global::System.Collections.Generic.List<global::Bit.BlazorUI.BitDropdownItem<string>> items = new();
                     private string value = "";
                     private int count;
                     private global::Bit.BlazorUI.BitButton? btn;
                     private global::System.Collections.Generic.List<string> names = new();
             {{members}}
                 }
                 {{extra}}
             }
             #pragma warning restore 1591
             {{after}}
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

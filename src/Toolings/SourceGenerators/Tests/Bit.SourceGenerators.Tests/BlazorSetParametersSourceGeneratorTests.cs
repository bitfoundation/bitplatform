using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.SourceGenerators.Tests;

[TestClass]
public class BlazorSetParametersSourceGeneratorTests
{
    private const string Usings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Threading.Tasks;
        global using Microsoft.AspNetCore.Components;
        global using Microsoft.AspNetCore.Components.Rendering;
        """;

    private const string Greeting = """
        namespace TestApp.Components
        {
            public partial class Greeting : ComponentBase
            {
                private string? title;

                [Parameter] public string? Title { get => title; set { title = value; TitleSets++; } }
                [Parameter] public int Count { get; set; }
                [Parameter] public int? Limit { get; set; }
                [Parameter] public DateTime When { get; set; }

                public int TitleSets { get; private set; }

                protected override void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.AddContent(0, $"{Title}|{Count}|{Limit}|{When:yyyy}|{TitleSets}");
                }
            }
        }
        """;

    private static GeneratorRun Run(params string[] sources)
    {
        return GeneratorTestHarness.Run(new BlazorSetParametersSourceGenerator(), [Usings, .. sources]);
    }

    private static Task<string> RenderAsync(GeneratorRun run, string componentType, Dictionary<string, object?>? parameters = null)
    {
        return BlazorTestRenderer.RenderAsync(run.Load().GetType(componentType, throwOnError: true)!, parameters);
    }

    [TestMethod]
    public async Task Parameters_are_assigned_by_the_generated_override_alone()
    {
        var run = Run(Greeting).AssertSucceeded();

        var html = await RenderAsync(run, "TestApp.Components.Greeting", new()
        {
            ["Title"] = "hi",
            ["Count"] = 3,
            ["Limit"] = null,
            ["When"] = new DateTime(2026, 1, 1)
        });

        Assert.AreEqual("hi|3||2026|1", html);
        StringAssert.Contains(run.Source("Greeting"), "case nameof(Title):");
    }

    [TestMethod]
    public async Task Null_for_a_value_type_parameter_assigns_its_default()
    {
        var run = Run(Greeting).AssertSucceeded();

        var html = await RenderAsync(run, "TestApp.Components.Greeting", new() { ["Count"] = null, ["When"] = null });

        Assert.AreEqual("|0||0001|0", html);
    }

    [TestMethod]
    public async Task Cascading_parameter_is_assigned()
    {
        var run = Run("""
            namespace TestApp.Components
            {
                public partial class Themed : ComponentBase
                {
                    [CascadingParameter] public string? Theme { get; set; }
                    [Parameter] public string? Text { get; set; }

                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.AddContent(0, $"{Theme}|{Text}");
                    }
                }
            }
            """).AssertSucceeded();

        var html = await BlazorTestRenderer.RenderWithCascadingValueAsync("dark", run.Load().GetType("TestApp.Components.Themed")!, new Dictionary<string, object?> { ["Text"] = "hello" });

        Assert.AreEqual("dark|hello", html);
    }

    [TestMethod]
    public async Task Unmatched_attributes_reach_the_CaptureUnmatchedValues_parameter()
    {
        var run = Run("""
            namespace TestApp.Components
            {
                public partial class Box : ComponentBase
                {
                    [Parameter] public string? Title { get; set; }
                    [Parameter(CaptureUnmatchedValues = true)] public Dictionary<string, object>? Attributes { get; set; }

                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.AddContent(0, $"{Title}|{string.Join(",", (Attributes ?? []).Select(a => a.Key + "=" + a.Value))}");
                    }
                }
            }
            """).AssertSucceeded();

        var html = await RenderAsync(run, "TestApp.Components.Box", new() { ["Title"] = "t", ["class"] = "big" });

        Assert.AreEqual("t|class=big", html);
    }

    [TestMethod]
    public async Task Parameter_names_are_matched_ignoring_case_like_blazor_does()
    {
        var run = Run(Greeting).AssertSucceeded();

        var html = await RenderAsync(run, "TestApp.Components.Greeting", new() { ["title"] = "hi", ["count"] = 2 });

        StringAssert.StartsWith(html, "hi|2|");
    }

    [TestMethod]
    public async Task Init_only_parameter_is_left_to_the_base_class()
    {
        var run = Run("""
            namespace TestApp.Components
            {
                public partial class Caption : ComponentBase
                {
                    [Parameter] public string? Label { get; init; }
                    [Parameter] public int Size { get; set; }

                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.AddContent(0, $"{Label}|{Size}");
                    }
                }
            }
            """).AssertSucceeded();

        var html = await RenderAsync(run, "TestApp.Components.Caption", new() { ["Label"] = "l", ["Size"] = 4 });

        Assert.AreEqual("l|4", html);
    }

    [TestMethod]
    public async Task Unknown_parameter_is_rejected_like_blazor_does()
    {
        var run = Run(Greeting).AssertSucceeded();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => RenderAsync(run, "TestApp.Components.Greeting", new() { ["Title"] = "hi", ["Nope"] = 1 }));

        StringAssert.Contains(exception.Message, "Nope");
    }

    [TestMethod]
    public async Task Derived_component_assigns_its_own_and_its_base_parameters()
    {
        var run = Run("""
            namespace TestApp.Components
            {
                public partial class LabelBase : ComponentBase
                {
                    [Parameter] public string? Label { get; set; }
                }

                public partial class FancyLabel : LabelBase
                {
                    [Parameter] public string? Icon { get; set; }

                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.AddContent(0, $"{Icon}|{Label}");
                    }
                }
            }
            """).AssertSucceeded();

        var html = await RenderAsync(run, "TestApp.Components.FancyLabel", new() { ["Icon"] = "i", ["Label"] = "l" });

        Assert.AreEqual("i|l", html);
    }

    [TestMethod]
    public void Class_with_its_own_SetParametersAsync_is_left_alone()
    {
        var run = Run("""
            namespace TestApp.Components
            {
                public partial class Manual : ComponentBase
                {
                    [Parameter] public string? Text { get; set; }

                    public override Task SetParametersAsync(ParameterView parameters) => base.SetParametersAsync(parameters);
                }
            }
            """).AssertSucceeded();

        Assert.IsEmpty(run.Result.GeneratedSources);
    }

    [TestMethod]
    public async Task Generic_component_is_supported()
    {
        var run = Run("""
            namespace TestApp.Components
            {
                public partial class ItemList<TItem> : ComponentBase
                {
                    [Parameter] public IReadOnlyList<TItem>? Items { get; set; }

                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.AddContent(0, string.Join(",", Items ?? []));
                    }
                }
            }
            """).AssertSucceeded();

        var html = await BlazorTestRenderer.RenderAsync(run.Load().GetType("TestApp.Components.ItemList`1")!.MakeGenericType(typeof(int)), new Dictionary<string, object?> { ["Items"] = new[] { 1, 2 } });

        Assert.AreEqual("1,2", html);
    }

    [TestMethod]
    public async Task Nested_component_is_generated_inside_its_containing_class()
    {
        var run = Run("""
            namespace TestApp.Components
            {
                public static partial class Widgets
                {
                    public partial class Badge : ComponentBase
                    {
                        [Parameter] public string? Text { get; set; }

                        protected override void BuildRenderTree(RenderTreeBuilder builder)
                        {
                            builder.AddContent(0, Text);
                        }
                    }
                }
            }
            """).AssertSucceeded();

        var html = await RenderAsync(run, "TestApp.Components.Widgets+Badge", new() { ["Text"] = "new" });

        Assert.AreEqual("new", html);
    }

    [TestMethod]
    public void Internal_component_keeps_its_accessibility()
    {
        var run = Run("""
            namespace TestApp.Components
            {
                internal sealed partial class Secret : ComponentBase
                {
                    [Parameter] public string? Text { get; set; }
                }
            }
            """).AssertSucceeded();

        Assert.IsFalse(run.Load().GetType("TestApp.Components.Secret")!.IsPublic);
    }

    [TestMethod]
    public void Record_with_parameters_is_left_alone()
    {
        var run = Run("""
            namespace TestApp.Components
            {
                public partial record Options
                {
                    [Parameter] public int Size { get; set; }
                }
            }
            """).AssertSucceeded();

        Assert.IsEmpty(run.Result.GeneratedSources);
    }

    [TestMethod]
    public async Task Component_in_the_global_namespace_is_supported()
    {
        var run = Run("""
            public partial class GlobalLabel : ComponentBase
            {
                [Parameter] public string? Text { get; set; }

                protected override void BuildRenderTree(RenderTreeBuilder builder)
                {
                    builder.AddContent(0, Text);
                }
            }
            """).AssertSucceeded();

        Assert.AreEqual("g", await RenderAsync(run, "GlobalLabel", new() { ["Text"] = "g" }));
    }

    [TestMethod]
    public void Obsolete_parameter_does_not_make_the_generated_code_warn()
    {
        Run("""
            namespace TestApp.Components
            {
                public partial class Legacy : ComponentBase
                {
                    [Parameter, Obsolete("Use Text")] public string? Caption { get; set; }
                    [Parameter, Obsolete] public string? Tooltip { get; set; }
                    [Parameter] public string? Text { get; set; }
                }
            }
            """).AssertSucceeded();
    }

    [TestMethod]
    public void Type_names_do_not_bind_to_namespaces_of_the_consumer()
    {
        Run("""
            namespace TestApp.System
            {
                public sealed class Marker { }
            }

            namespace TestApp.Microsoft
            {
                public sealed class Marker { }
            }

            namespace TestApp.Components
            {
                public partial class Schedule : ComponentBase
                {
                    [Parameter] public DateTime When { get; set; }
                    [Parameter] public RenderFragment? ChildContent { get; set; }
                }
            }
            """).AssertSucceeded();
    }

    [TestMethod]
    public void Unrelated_edit_keeps_the_generated_output_cached()
    {
        var first = Run(Greeting).AssertSucceeded();

        var second = first.RunAgain(first.OutputCompilation.RemoveSyntaxTrees(first.Result.GeneratedSources.Select(s => s.SyntaxTree))
            .AddSyntaxTrees(GeneratorTestHarness.ParseSource("namespace TestApp { public class Unrelated { } }", "Unrelated.cs")));

        var reasons = second.Result.TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs).Select(output => output.Reason).ToArray();
        Assert.IsNotEmpty(reasons);
        Assert.IsTrue(reasons.All(reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged), string.Join(", ", reasons));
    }
}

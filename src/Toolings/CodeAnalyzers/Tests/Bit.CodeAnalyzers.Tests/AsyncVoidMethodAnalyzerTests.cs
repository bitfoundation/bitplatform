using Bit.CodeAnalyzers.SystemAnalyzers;
using Microsoft.CodeAnalysis;
using static Bit.CodeAnalyzers.Tests.AnalyzerTestHarness;

namespace Bit.CodeAnalyzers.Tests;

[TestClass]
public class AsyncVoidMethodAnalyzerTests
{
    private static Task<string[]> Analyze(string source, OutputKind outputKind = OutputKind.DynamicallyLinkedLibrary)
    {
        return GetDiagnostics(new AsyncVoidMethodAnalyzer(), source, outputKind);
    }

    [TestMethod]
    public async Task ReportsAsyncVoidMethodWithoutTryCatchOnItsName()
    {
        var diagnostics = await Analyze("""
            using System.Threading.Tasks;

            namespace App;

            public class Page
            {
                public async void OnClick()
                {
                    await Task.Delay(1);
                }
            }
            """);

        AssertDiagnostics(diagnostics, "AsyncVoidMethodAnalyzer1: OnClick");
    }

    [TestMethod]
    public async Task AcceptsAsyncVoidMethodWhoseWholeBodyIsATryCatch()
    {
        var diagnostics = await Analyze("""
            using System;
            using System.Threading.Tasks;

            public class Page
            {
                public async void OnClick()
                {
                    try
                    {
                        await Task.Delay(1);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine(exception);
                    }
                    finally
                    {
                        Console.WriteLine("done");
                    }
                }
            }
            """);

        AssertDiagnostics(diagnostics);
    }

    [TestMethod]
    public async Task ReportsStatementsOutsideTheTryCatchWithTheSecondRule()
    {
        var diagnostics = await Analyze("""
            using System;
            using System.Threading.Tasks;

            public class Page
            {
                public async void OnClick()
                {
                    try
                    {
                        await Task.Delay(1);
                    }
                    catch (Exception)
                    {
                    }

                    await Task.Delay(1);
                }
            }
            """);

        AssertDiagnostics(diagnostics, "AsyncVoidMethodAnalyzer2: OnClick");
    }

    [TestMethod]
    public async Task ReportsATryFinallyWithoutACatch()
    {
        var diagnostics = await Analyze("""
            using System;
            using System.Threading.Tasks;

            public class Page
            {
                public async void OnClick()
                {
                    try
                    {
                        await Task.Delay(1);
                    }
                    finally
                    {
                        Console.WriteLine("done");
                    }
                }
            }
            """);

        AssertDiagnostics(diagnostics, "AsyncVoidMethodAnalyzer1: OnClick");
    }

    [TestMethod]
    public async Task ReportsMethodsWithNoBlockToWrapWithTheFirstRule()
    {
        var diagnostics = await Analyze("""
            using System.Threading.Tasks;

            public class Page
            {
                public async void OnClick() => await Task.Delay(1);

                public async void OnHover()
                {
                }
            }
            """);

        AssertDiagnostics(diagnostics, "AsyncVoidMethodAnalyzer1: OnClick", "AsyncVoidMethodAnalyzer1: OnHover");
    }

    [TestMethod]
    public async Task ReportsAsyncVoidLocalFunctions()
    {
        var diagnostics = await Analyze("""
            using System;
            using System.Threading.Tasks;

            public class Page
            {
                public event EventHandler? Clicked;

                public void Subscribe()
                {
                    Clicked += OnClicked;

                    async void OnClicked(object? sender, EventArgs e)
                    {
                        await Task.Delay(1);
                    }
                }
            }
            """);

        AssertDiagnostics(diagnostics, "AsyncVoidMethodAnalyzer1: OnClicked");
    }

    [TestMethod]
    public async Task ReportsAsyncVoidLocalFunctionsOfTopLevelStatements()
    {
        var diagnostics = await Analyze("""
            using System.Threading.Tasks;

            Run();

            async void Run()
            {
                await Task.Delay(1);
            }
            """, OutputKind.ConsoleApplication);

        AssertDiagnostics(diagnostics, "AsyncVoidMethodAnalyzer1: Run");
    }

    [TestMethod]
    public async Task IgnoresLocalFunctionsDeclaredAroundTheTryCatch()
    {
        var diagnostics = await Analyze("""
            using System;
            using System.Threading.Tasks;

            public class Page
            {
                public async void OnClick()
                {
                    static void Log(Exception exception) => Console.WriteLine(exception);

                    try
                    {
                        await Work();
                    }
                    catch (Exception exception)
                    {
                        Log(exception);
                    }

                    async Task Work()
                    {
                        await Task.Delay(1);
                    }
                }
            }
            """);

        AssertDiagnostics(diagnostics);
    }

    [TestMethod]
    public async Task IgnoresMethodsThatAreNotAsyncVoid()
    {
        var diagnostics = await Analyze("""
            using System;
            using System.Threading.Tasks;

            public class Page
            {
                public async Task Load() => await Task.Delay(1);

                public async Task<int> Count()
                {
                    await Task.Delay(1);
                    return 1;
                }

                public async ValueTask Save() => await Task.Delay(1);

                public void Render()
                {
                }

                public void Subscribe(Action action)
                {
                    Action handler = async () => await Task.Delay(1);
                    action += handler;
                }
            }
            """);

        AssertDiagnostics(diagnostics);
    }

    [TestMethod]
    public async Task CoversNestedTypesRecordsStructsAndPartialClasses()
    {
        var diagnostics = await Analyze("""
            using System.Threading.Tasks;

            namespace App;

            public partial class Outer
            {
                public record Inner
                {
                    public async void InRecord() => await Task.Delay(1);
                }

                private struct Value
                {
                    public async void InStruct() => await Task.Delay(1);
                }
            }

            public partial class Outer
            {
                private async void InPartial() => await Task.Delay(1);
            }
            """);

        AssertDiagnostics(diagnostics,
            "AsyncVoidMethodAnalyzer1: InRecord",
            "AsyncVoidMethodAnalyzer1: InStruct",
            "AsyncVoidMethodAnalyzer1: InPartial");
    }

    [TestMethod]
    public async Task ReportsInGeneratedCodeWhereRazorPutsCodeBlocks()
    {
        var diagnostics = await Analyze("""
            // <auto-generated/>
            using System.Threading.Tasks;

            public partial class Counter
            {
                private async void IncrementCount()
                {
                    await Task.Delay(1);
                }
            }
            """);

        AssertDiagnostics(diagnostics, "AsyncVoidMethodAnalyzer1: IncrementCount");
    }
}

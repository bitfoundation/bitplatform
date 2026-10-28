using System.Reflection;
using System.Runtime.Loader;
using Bunit;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.Brouter.Tests;

/// <summary>
/// Discovery over an assembly some of whose types need an assembly that can't be loaded, the way an app assembly's
/// types need a BlazorWebAssemblyLazyLoad assembly that isn't loaded yet.
/// </summary>
[TestClass]
public class UnloadableTypeDiscoveryTests : BunitTestContext
{
    private const string MissingAssemblyName = "Bit.Brouter.Tests.MissingDependency";

    private static readonly Assembly fixture = BuildFixture();

    [TestMethod]
    public void Discovery_skips_the_types_it_cant_load_and_finds_the_rest()
    {
        var routes = BrouteScanner.Discover(fixture, null);

        CollectionAssert.AreEquivalent(new[] { "/before", "/after" }, routes.Select(r => r.Template).ToArray());
    }

    [TestMethod]
    public void Discovery_reports_why_it_skipped_types_once_per_assembly()
    {
        var reports = new List<(Assembly Assembly, IReadOnlyList<Exception> Failures)>();

        BrouteScanner.Discover(fixture, [fixture, typeof(DiscoveryHost).Assembly], (assembly, failures) => reports.Add((assembly, failures)));

        var report = reports.Single();
        Assert.AreSame(fixture, report.Assembly);
        // DerivedPage and DecoratedPage.
        Assert.HasCount(2, report.Failures);
        foreach (var failure in report.Failures)
        {
            StringAssert.Contains(failure.Message, MissingAssemblyName);
        }
    }

    [TestMethod]
    public void Type_by_type_scan_skips_the_types_it_cant_load()
    {
        var loadable = Assert.ThrowsExactly<ReflectionTypeLoadException>(() => fixture.GetTypes()).Types.OfType<Type>().ToArray();
        var failures = new List<Exception>();

        CollectionAssert.AreEquivalent(loadable, BrouteScanner.GetLoadableTypes(fixture.ManifestModule, failures));
        Assert.IsFalse(loadable.Any(t => t.Name == "DerivedPage"));
        StringAssert.Contains(failures.Single().Message, MissingAssemblyName);
    }

    [TestMethod]
    public void Brouter_warns_about_the_types_it_skipped_once_however_often_it_rescans()
    {
        var logger = new RecordingLogger();
        Services.AddSingleton<ILogger<Brouter>>(logger);

        var cut = RenderComponent<Brouter>(p => p.Add(b => b.AppAssembly, fixture));
        // A new assembly set makes Brouter scan the fixture again.
        cut.Render(p => p.Add(b => b.AdditionalAssemblies, [typeof(DiscoveryHost).Assembly]));

        var warning = logger.Warnings.Single();
        StringAssert.Contains(warning, "Bit.Brouter.Tests.UnloadableTypes");
        StringAssert.Contains(warning, MissingAssemblyName);
    }

    // DerivedPage can't be loaded at all, since its base type is in the missing assembly. DecoratedPage loads, but one of
    // its attributes is in the missing assembly, so reading its attributes fails.
    private static Assembly BuildFixture()
    {
        var missing = Compile(MissingAssemblyName, """
            namespace MissingDependency;

            public class MissingBase { }

            public sealed class MissingAttribute : System.Attribute { }
            """);

        var fixture = Compile("Bit.Brouter.Tests.UnloadableTypes", """
            using System.Threading.Tasks;
            using Microsoft.AspNetCore.Components;

            namespace UnloadableTypes;

            public abstract class Page : IComponent
            {
                public void Attach(RenderHandle renderHandle) { }
                public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
            }

            [Route("/before")]
            public class BeforePage : Page { }

            [Route("/derived")]
            public class DerivedPage : MissingDependency.MissingBase, IComponent
            {
                public void Attach(RenderHandle renderHandle) { }
                public Task SetParametersAsync(ParameterView parameters) => Task.CompletedTask;
            }

            [Route("/decorated"), MissingDependency.Missing]
            public class DecoratedPage : Page { }

            [Route("/after")]
            public class AfterPage : Page { }
            """, MetadataReference.CreateFromImage(missing));

        // Only the fixture is loaded. Its other references fall back to the default context, which can't find the missing one.
        return new AssemblyLoadContext("Brouter unloadable types").LoadFromStream(new MemoryStream(fixture));
    }

    private static byte[] Compile(string assemblyName, string source, params MetadataReference[] references)
    {
        var tpa = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;
        var compilation = CSharpCompilation.Create(
            assemblyName,
            syntaxTrees: [CSharpSyntaxTree.ParseText(source)],
            references: tpa.Split(Path.PathSeparator)
                .Where(p => p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
                .Select(p => (MetadataReference)MetadataReference.CreateFromFile(p))
                .Concat(references),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using var ms = new MemoryStream();
        var emit = compilation.Emit(ms);
        if (emit.Success is false)
            throw new InvalidOperationException(string.Join(Environment.NewLine, emit.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)));

        return ms.ToArray();
    }

    private sealed class RecordingLogger : ILogger<Brouter>
    {
        public List<string> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) Warnings.Add(formatter(state, exception));
        }
    }
}

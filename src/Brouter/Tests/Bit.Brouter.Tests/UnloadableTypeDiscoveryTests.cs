using Microsoft.AspNetCore.Components;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Bunit;
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

        var (warning, exception) = logger.Warnings.Single();
        StringAssert.Contains(warning, "Bit.Brouter.Tests.UnloadableTypes");
        StringAssert.Contains(warning, MissingAssemblyName);
        // The failures themselves ride along, inner exceptions and all.
        Assert.HasCount(2, Assert.IsInstanceOfType<AggregateException>(exception).InnerExceptions);
    }

    // DerivedPage can't be loaded at all, since its base type is in the missing assembly. DecoratedPage loads, but one of
    // its attributes is in the missing assembly, so reading its attributes fails.
    private static Assembly BuildFixture()
    {
        // The missing assembly has to exist while the fixture is built, since the fixture's types refer to its types, so
        // it lives in a context of its own that the fixture's context knows nothing about.
        var missing = new AssemblyLoadContext("Brouter missing dependency").LoadFromStream(new MemoryStream(Emit(MissingAssemblyName, module =>
        {
            module.DefineType("MissingDependency.MissingBase", TypeAttributes.Public | TypeAttributes.Class).CreateType();
            module.DefineType("MissingDependency.MissingAttribute", TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Class, typeof(Attribute)).CreateType();
        })));
        var missingBase = missing.GetType("MissingDependency.MissingBase", throwOnError: true)!;
        var missingAttribute = missing.GetType("MissingDependency.MissingAttribute", throwOnError: true)!.GetConstructor(Type.EmptyTypes)!;

        var fixture = Emit("Bit.Brouter.Tests.UnloadableTypes", module =>
        {
            var page = DefineComponent(module, "UnloadableTypes.Page", typeof(object), TypeAttributes.Abstract).CreateType();
            DefineComponent(module, "UnloadableTypes.BeforePage", page, routes: "/before").CreateType();
            DefineComponent(module, "UnloadableTypes.DerivedPage", missingBase, routes: "/derived").CreateType();
            var decorated = DefineComponent(module, "UnloadableTypes.DecoratedPage", page, routes: "/decorated");
            decorated.SetCustomAttribute(new CustomAttributeBuilder(missingAttribute, []));
            decorated.CreateType();
            DefineComponent(module, "UnloadableTypes.AfterPage", page, routes: "/after").CreateType();
        });

        // Only the fixture is loaded here. Its other references fall back to the default context, which can't find the
        // missing one.
        return new AssemblyLoadContext("Brouter unloadable types").LoadFromStream(new MemoryStream(fixture));
    }

    private static byte[] Emit(string assemblyName, Action<ModuleBuilder> define)
    {
        var builder = new PersistedAssemblyBuilder(new AssemblyName(assemblyName), typeof(object).Assembly);
        define(builder.DefineDynamicModule(assemblyName));

        using var ms = new MemoryStream();
        builder.Save(ms);
        return ms.ToArray();
    }

    // A public class deriving from parent that implements IComponent itself (a base type's implementation isn't enough
    // when parent is MissingBase), with one [Route] per template.
    private static TypeBuilder DefineComponent(ModuleBuilder module, string name, Type parent, TypeAttributes attributes = default, params string[] routes)
    {
        var type = module.DefineType(name, TypeAttributes.Public | TypeAttributes.Class | attributes, parent, [typeof(IComponent)]);

        const MethodAttributes implementation = MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Final;

        var attach = type.DefineMethod(nameof(IComponent.Attach), implementation, typeof(void), [typeof(RenderHandle)]).GetILGenerator();
        attach.Emit(OpCodes.Ret);

        var setParameters = type.DefineMethod(nameof(IComponent.SetParametersAsync), implementation, typeof(Task), [typeof(ParameterView)]).GetILGenerator();
        setParameters.Emit(OpCodes.Call, typeof(Task).GetProperty(nameof(Task.CompletedTask))!.GetMethod!);
        setParameters.Emit(OpCodes.Ret);

        var route = typeof(RouteAttribute).GetConstructor([typeof(string)])!;
        foreach (var template in routes)
        {
            type.SetCustomAttribute(new CustomAttributeBuilder(route, [template]));
        }

        return type;
    }

    private sealed class RecordingLogger : ILogger<Brouter>
    {
        public List<(string Message, Exception? Exception)> Warnings { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (logLevel == LogLevel.Warning) Warnings.Add((formatter(state, exception), exception));
        }
    }
}

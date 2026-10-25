using System.Globalization;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bit.SourceGenerators.Tests;

[TestClass]
public class AutoInjectSourceGeneratorTests
{
    private const string Attribute = """
        global using System;
        global using Microsoft.Extensions.DependencyInjection;

        namespace Microsoft.Extensions.DependencyInjection
        {
            [System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
            public sealed partial class AutoInjectAttribute : System.Attribute
            {
            }
        }
        """;

    private const string Services = """
        namespace TestApp
        {
            public interface IClock { string Name { get; } }
            public sealed class Clock : IClock { public string Name => "clock"; }
            public interface ILog { string Name { get; } }
            public sealed class Log : ILog { public string Name => "log"; }
            public interface IStore<T> { string Name { get; } }
            public sealed class Store<T> : IStore<T> { public string Name => "store of " + typeof(T).Name; }
            public interface IMissing { }
        }
        """;

    private static GeneratorRun Run(params string[] sources)
    {
        return GeneratorTestHarness.Run(new AutoInjectSourceGenerator(), [Attribute, Services, .. sources]);
    }

    private static object Resolve(Assembly assembly, string typeName)
    {
        var services = new ServiceCollection();
        services.AddSingleton(assembly.GetType("TestApp.IClock")!, assembly.GetType("TestApp.Clock")!);
        services.AddSingleton(assembly.GetType("TestApp.ILog")!, assembly.GetType("TestApp.Log")!);
        services.AddSingleton(assembly.GetType("TestApp.IStore`1")!, assembly.GetType("TestApp.Store`1")!);
        using var serviceProvider = services.BuildServiceProvider();
        return ActivatorUtilities.CreateInstance(serviceProvider, assembly.GetType(typeName, throwOnError: true)!);
    }

    private static object? Member(object instance, Type declaringType, string name)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        return declaringType.GetField(name, flags)?.GetValue(instance) ?? declaringType.GetProperty(name, flags)?.GetValue(instance);
    }

    private static string? NameOf(object? service)
    {
        return (string?)service?.GetType().GetProperty("Name")!.GetValue(service);
    }

    private static string[] ConstructorParameters(Type type)
    {
        return [.. type.GetConstructors().Single().GetParameters().Select(p => p.Name!)];
    }

    [TestMethod]
    public void Normal_class_gets_a_constructor_that_assigns_every_member()
    {
        var run = Run("""
            namespace TestApp.Services
            {
                public partial class OrderService
                {
                    [AutoInject] private ILog log = default!;
                    [AutoInject] protected IClock Clock { get; set; } = default!;
                    [AutoInject] private readonly IStore<OrderService> _store = default!;

                    public object?[] Services => [log, Clock, _store];
                }
            }
            """).AssertSucceeded();

        var instance = Resolve(run.Load(), "TestApp.Services.OrderService");

        Assert.AreEqual("autoInjectedStore, autoInjectedClock, autoInjectedLog", string.Join(", ", ConstructorParameters(instance.GetType())));
        var services = (object?[])instance.GetType().GetProperty("Services")!.GetValue(instance)!;
        Assert.AreEqual("log, clock, store of OrderService", string.Join(", ", services.Select(NameOf)));
    }

    [TestMethod]
    public void Nullable_member_becomes_an_optional_constructor_parameter()
    {
        var run = Run("""
            namespace TestApp
            {
                public partial class Reporter
                {
                    [AutoInject] private IClock clock = default!;
                    [AutoInject] private IMissing? missing;

                    public bool HasMissing => missing is not null;
                    public string ClockName => clock.Name;
                }
            }
            """).AssertSucceeded();

        var instance = Resolve(run.Load(), "TestApp.Reporter");

        var missingParameter = instance.GetType().GetConstructors().Single().GetParameters().Single(p => p.Name == "autoInjectedMissing");
        Assert.IsTrue(missingParameter.HasDefaultValue);
        Assert.IsNull(missingParameter.DefaultValue);
        Assert.IsFalse((bool)instance.GetType().GetProperty("HasMissing")!.GetValue(instance)!);
        Assert.AreEqual("clock", instance.GetType().GetProperty("ClockName")!.GetValue(instance));
    }

    [TestMethod]
    public void Derived_classes_pass_the_base_members_to_the_base_constructor()
    {
        var run = Run("""
            namespace TestApp
            {
                public abstract partial class ServiceBase
                {
                    [AutoInject] protected IClock Clock = default!;
                }

                public partial class MiddleService : ServiceBase
                {
                }

                public partial class LeafService : MiddleService
                {
                    [AutoInject] private ILog log = default!;

                    public string Names => Clock.Name + "," + log.Name;
                }
            }
            """).AssertSucceeded();

        var assembly = run.Load();
        var leaf = Resolve(assembly, "TestApp.LeafService");
        var middle = Resolve(assembly, "TestApp.MiddleService");

        Assert.AreEqual("clock,log", leaf.GetType().GetProperty("Names")!.GetValue(leaf));
        Assert.AreEqual("clock", NameOf(Member(middle, assembly.GetType("TestApp.ServiceBase")!, "Clock")));
        Assert.AreEqual("autoInjectedClock", string.Join(", ", ConstructorParameters(middle.GetType())));
        Assert.AreEqual("autoInjectedClock, autoInjectedLog", string.Join(", ", ConstructorParameters(leaf.GetType())));
    }

    [TestMethod]
    public void Members_named_alike_in_a_base_and_a_derived_class_get_distinct_parameters()
    {
        var run = Run("""
            namespace TestApp
            {
                public partial class BaseHandler
                {
                    [AutoInject] private ILog log = default!;

                    public string BaseLog => log.Name;
                }

                public partial class DerivedHandler : BaseHandler
                {
                    [AutoInject] private ILog log = default!;
                    [AutoInject] private IClock _log = default!;

                    public string Logs => BaseLog + "," + log.Name + "," + _log.Name;
                }
            }
            """).AssertSucceeded();

        var instance = Resolve(run.Load(), "TestApp.DerivedHandler");

        Assert.AreEqual("log,log,clock", instance.GetType().GetProperty("Logs")!.GetValue(instance));
    }

    [TestMethod]
    public void Nullable_member_named_before_a_required_one_comes_after_it()
    {
        var run = Run("""
            namespace TestApp
            {
                public partial class Probe
                {
                    [AutoInject] private IMissing? alpha;
                    [AutoInject] private IClock clock = default!;

                    public bool HasAlpha => alpha is not null;
                    public string ClockName => clock.Name;
                }
            }
            """).AssertSucceeded();

        var instance = Resolve(run.Load(), "TestApp.Probe");

        Assert.AreEqual("autoInjectedClock, autoInjectedAlpha", string.Join(", ", ConstructorParameters(instance.GetType())));
        Assert.IsFalse((bool)instance.GetType().GetProperty("HasAlpha")!.GetValue(instance)!);
        Assert.AreEqual("clock", instance.GetType().GetProperty("ClockName")!.GetValue(instance));
    }

    [TestMethod]
    public void Nullable_base_member_named_before_a_required_one_is_passed_in_the_base_constructor_order()
    {
        var run = Run("""
            namespace TestApp
            {
                public partial class ProbeBase
                {
                    [AutoInject] protected IMissing? Alpha;
                    [AutoInject] protected IClock Clock = default!;
                }

                public partial class PassThroughProbe : ProbeBase
                {
                    public string Names => Clock.Name + "," + (Alpha is null);
                }

                public partial class LeafProbe : ProbeBase
                {
                    [AutoInject] private ILog log = default!;
                    [AutoInject] private IMissing? beta;

                    public string Names => Clock.Name + "," + log.Name + "," + (Alpha is null) + "," + (beta is null);
                }
            }
            """).AssertSucceeded();

        var assembly = run.Load();
        var passThrough = Resolve(assembly, "TestApp.PassThroughProbe");
        var leaf = Resolve(assembly, "TestApp.LeafProbe");

        Assert.AreEqual("autoInjectedClock, autoInjectedAlpha", string.Join(", ", ConstructorParameters(assembly.GetType("TestApp.ProbeBase")!)));
        Assert.AreEqual("autoInjectedClock, autoInjectedAlpha", string.Join(", ", ConstructorParameters(passThrough.GetType())));
        Assert.AreEqual("autoInjectedClock, autoInjectedLog, autoInjectedAlpha, autoInjectedBeta", string.Join(", ", ConstructorParameters(leaf.GetType())));
        Assert.AreEqual("clock,True", passThrough.GetType().GetProperty("Names")!.GetValue(passThrough));
        Assert.AreEqual("clock,log,True,True", leaf.GetType().GetProperty("Names")!.GetValue(leaf));
    }

    [TestMethod]
    public void Generic_class_gets_a_constructor()
    {
        var run = Run("""
            namespace TestApp
            {
                public partial class Repository<TEntity> where TEntity : class
                {
                    [AutoInject] private IStore<TEntity> store = default!;

                    public string StoreName => store.Name;
                }

                public sealed class Customer { }
            }
            """).AssertSucceeded();

        var assembly = run.Load();
        var services = new ServiceCollection();
        services.AddSingleton(assembly.GetType("TestApp.IStore`1")!, assembly.GetType("TestApp.Store`1")!);
        using var serviceProvider = services.BuildServiceProvider();
        var instance = ActivatorUtilities.CreateInstance(serviceProvider, assembly.GetType("TestApp.Repository`1")!.MakeGenericType(assembly.GetType("TestApp.Customer")!));

        Assert.AreEqual("store of Customer", instance.GetType().GetProperty("StoreName")!.GetValue(instance));
    }

    [TestMethod]
    public async Task Razor_component_gets_injectable_properties()
    {
        var run = Run("""
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;

            namespace TestApp.Components
            {
                public partial class ClockView : ComponentBase
                {
                    [AutoInject] private IClock clock = default!;
                    [AutoInject] protected ILog Log { get; set; } = default!;

                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.AddContent(0, clock.Name + "," + Log.Name);
                    }
                }
            }
            """).AssertSucceeded();

        var assembly = run.Load();
        var componentType = assembly.GetType("TestApp.Components.ClockView")!;

        var html = await BlazorTestRenderer.RenderAsync(componentType, configureServices: services =>
        {
            services.AddSingleton(assembly.GetType("TestApp.IClock")!, assembly.GetType("TestApp.Clock")!);
            services.AddSingleton(assembly.GetType("TestApp.ILog")!, assembly.GetType("TestApp.Log")!);
        });

        Assert.AreEqual("clock,log", html);
        Assert.IsNotNull(componentType.GetProperty("____Clock", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.IsNotNull(componentType.GetProperty("____Log", BindingFlags.Instance | BindingFlags.NonPublic));
        Assert.IsEmpty(componentType.GetConstructors().Single().GetParameters());
    }

    [TestMethod]
    public void Razor_component_deriving_from_an_auto_inject_base_needs_nothing_generated()
    {
        var run = Run("""
            using Microsoft.AspNetCore.Components;
            using Microsoft.AspNetCore.Components.Rendering;

            namespace TestApp.Components
            {
                public partial class AppComponentBase : ComponentBase
                {
                    [AutoInject] protected IClock Clock = default!;
                }

                public class ClockLabel : AppComponentBase
                {
                    protected override void BuildRenderTree(RenderTreeBuilder builder)
                    {
                        builder.AddContent(0, Clock.Name);
                    }
                }
            }
            """).AssertSucceeded();

        Assert.HasCount(1, run.Result.GeneratedSources);
    }

    [TestMethod]
    public void Non_partial_class_is_reported_at_its_name()
    {
        var run = Run("""
            namespace TestApp
            {
                public class Worker
                {
                    [AutoInject] private IClock clock = default!;
                }
            }
            """);

        var diagnostic = run.GeneratorDiagnostics.Single();
        Assert.AreEqual("BITGEN001", diagnostic.Id);
        Assert.AreEqual(DiagnosticSeverity.Error, diagnostic.Severity);
        Assert.AreEqual("Worker", run.TextAt(diagnostic));
        Assert.IsEmpty(run.Result.GeneratedSources);
    }

    [TestMethod]
    public void Nested_class_gets_a_constructor()
    {
        var run = Run("""
            namespace TestApp
            {
                public static partial class Features
                {
                    public partial class Handler
                    {
                        [AutoInject] private IClock clock = default!;

                        public string ClockName => clock.Name;
                    }
                }
            }
            """).AssertSucceeded();

        var instance = Resolve(run.Load(), "TestApp.Features+Handler");

        Assert.AreEqual("clock", instance.GetType().GetProperty("ClockName")!.GetValue(instance));
    }

    [TestMethod]
    public void Nested_class_in_a_non_partial_class_is_reported()
    {
        var run = Run("""
            namespace TestApp
            {
                public static class Features
                {
                    public partial class Handler
                    {
                        [AutoInject] private IClock clock = default!;
                    }
                }
            }
            """);

        var diagnostic = run.GeneratorDiagnostics.Single();
        Assert.AreEqual("BITGEN001", diagnostic.Id);
        Assert.AreEqual("Features", run.TextAt(diagnostic));
        Assert.IsEmpty(run.Result.GeneratedSources);
    }

    [TestMethod]
    public void Generated_part_keeps_the_accessibility_of_the_class()
    {
        var run = Run("""
            namespace TestApp
            {
                internal interface IInternalService { }

                internal sealed partial class InternalWorker
                {
                    [AutoInject] private IInternalService service = default!;
                }

                partial class DefaultWorker
                {
                    [AutoInject] private IInternalService service = default!;
                }
            }
            """).AssertSucceeded();

        var assembly = run.Load();

        Assert.IsFalse(assembly.GetType("TestApp.InternalWorker")!.IsPublic);
        Assert.IsFalse(assembly.GetType("TestApp.DefaultWorker")!.IsPublic);
    }

    [TestMethod]
    public void Class_in_the_global_namespace_gets_a_constructor()
    {
        var run = Run("""
            using TestApp;

            public partial class GlobalWorker
            {
                [AutoInject] private IClock clock = default!;

                public string ClockName => clock.Name;
            }
            """).AssertSucceeded();

        var instance = Resolve(run.Load(), "GlobalWorker");

        Assert.AreEqual("clock", instance.GetType().GetProperty("ClockName")!.GetValue(instance));
    }

    [TestMethod]
    public void Record_gets_a_constructor()
    {
        var run = Run("""
            namespace TestApp
            {
                public partial record Settings
                {
                    [AutoInject] private IClock clock = default!;

                    public string ClockName => clock.Name;
                }
            }
            """).AssertSucceeded();

        var instance = Resolve(run.Load(), "TestApp.Settings");

        Assert.AreEqual("clock", instance.GetType().GetProperty("ClockName")!.GetValue(instance));
    }

    [TestMethod]
    public void Type_names_do_not_bind_to_namespaces_of_the_consumer()
    {
        var run = Run("""
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
                using global::System;
                using global::Microsoft.AspNetCore.Components;

                public partial class Panel : ComponentBase
                {
                    [AutoInject] private IServiceProvider services = default!;
                    [AutoInject] private NavigationManager navigationManager = default!;
                }

                public partial class Worker
                {
                    [AutoInject] private IServiceProvider services = default!;
                    [AutoInject] private TimeProvider timeProvider = default!;
                }
            }
            """);

        run.AssertSucceeded();
    }

    [TestMethod]
    public void Keyword_named_member_is_escaped()
    {
        var run = Run("""
            namespace TestApp
            {
                public partial class EventSink
                {
                    [AutoInject] private IClock @event = default!;

                    public string ClockName => @event.Name;
                }
            }
            """).AssertSucceeded();

        var instance = Resolve(run.Load(), "TestApp.EventSink");

        Assert.AreEqual("clock", instance.GetType().GetProperty("ClockName")!.GetValue(instance));
    }

    [TestMethod]
    public void Incomplete_member_while_typing_does_not_fail_the_generator()
    {
        var run = Run("""
            namespace TestApp
            {
                public partial class Typing
                {
                    [AutoInject] private IClock
                }

                public partial class Done
                {
                    [AutoInject] private IClock clock = default!;
                }
            }
            """);

        Assert.IsNull(run.Result.Exception, run.Result.Exception?.ToString());
        StringAssert.Contains(run.Source("TestApp.Done"), "autoInjectedClock");
    }

    [TestMethod]
    public void Output_does_not_depend_on_the_current_culture()
    {
        const string source = """
            namespace TestApp
            {
                public partial class Ordered
                {
                    [AutoInject] private IClock ibis = default!;
                    [AutoInject] private ILog Ilex = default!;
                    [AutoInject] private IStore<Ordered> Iguana = default!;
                }
            }
            """;

        string invariant;
        string turkish;
        var currentCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            invariant = Run(source).Source("TestApp.Ordered");
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("tr-TR");
            turkish = Run(source).Source("TestApp.Ordered");
        }
        finally
        {
            CultureInfo.CurrentCulture = currentCulture;
        }

        Assert.AreEqual(invariant, turkish);
    }

    [TestMethod]
    public void Unrelated_edit_keeps_the_generated_output_cached()
    {
        var first = Run("""
            namespace TestApp
            {
                public partial class BaseWorker
                {
                    [AutoInject] protected IClock Clock = default!;
                }

                public partial class Worker : BaseWorker
                {
                    [AutoInject] private ILog log = default!;
                }

                public partial class PassThroughWorker : BaseWorker
                {
                }
            }
            """).AssertSucceeded();

        var second = first.RunAgain(first.OutputCompilation.RemoveSyntaxTrees(first.Result.GeneratedSources.Select(s => s.SyntaxTree))
            .AddSyntaxTrees(GeneratorTestHarness.ParseSource("namespace TestApp { public class Unrelated { } }", "Unrelated.cs")));

        var reasons = second.Result.TrackedOutputSteps.SelectMany(step => step.Value).SelectMany(step => step.Outputs).Select(output => output.Reason).ToArray();
        Assert.IsNotEmpty(reasons);
        Assert.IsTrue(reasons.All(reason => reason is IncrementalStepRunReason.Cached or IncrementalStepRunReason.Unchanged), string.Join(", ", reasons));
    }
}

using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Utilities.Testing;

/// <summary>
/// What keeps a library lazy loadable on WebAssembly, checked against its assembly. An app that downloads the library
/// only when a page that uses it opens has nothing of the library's in <c>Program.cs</c> - its types are not there yet
/// when it runs - so the library may have nothing for the app's startup to call, and its components may take from the
/// container only what every Blazor host registers by itself. Linked into the test project of each library whose guide
/// says it lazy loads with nothing to register, so one fix here reaches all of them.
/// </summary>
internal static class LazyLoadingAssert
{
    // Registered by every hosting model.
    private static readonly Type[] _everyHostRegisters = [typeof(IJSRuntime), typeof(NavigationManager)];

    // What an app's startup hands a library to set itself up in: the container, the provider built from it, and the
    // hosts and their builders. By name, so a library is not made to reference the hosting packages to be checked.
    private static readonly HashSet<string> _startupTypes =
    [
        "Microsoft.Extensions.DependencyInjection.IServiceCollection",
        "System.IServiceProvider",
        "Microsoft.Extensions.Hosting.IHostApplicationBuilder",
        "Microsoft.Extensions.Hosting.IHostBuilder",
        "Microsoft.Extensions.Hosting.IHost",
        "Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHostBuilder",
        "Microsoft.AspNetCore.Components.WebAssembly.Hosting.WebAssemblyHost",
    ];

    /// <summary>
    /// Every service a component of <paramref name="library"/> takes from the container, through an <c>[Inject]</c>
    /// property or its constructor, is one every Blazor host registers. <paramref name="known"/> is one it is known to
    /// take, so a reflection that finds nothing cannot pass for one that finds nothing wrong.
    /// </summary>
    public static void ComponentsInjectOnlyWhatEveryHostRegisters(Assembly library, (Type Component, Type Service) known)
    {
        var injected = library.GetTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type))
            .SelectMany(type => InjectedProperties(type).Concat(ConstructorParameters(type)).Select(service => (Component: type, Service: service)))
            .ToArray();

        Assert.Contains(known, injected);

        foreach (var (component, service) in injected)
        {
            Assert.Contains(service, _everyHostRegisters,
                $"{component.Name} takes {service.FullName} from the container, which an app that lazy loads {library.GetName().Name} cannot register: resolve it from IServiceProvider and fall back to a default instead.");
        }
    }

    /// <summary>
    /// <paramref name="library"/> has no public static method that takes what an app's startup would hand it - the
    /// container, the service provider, a host or a host builder - whether as an extension method or not: one would be
    /// called from <c>Program.cs</c>, before a lazily loaded assembly is there.
    /// </summary>
    public static void HasNothingForStartupToCall(Assembly library)
    {
        var entryPoints = library.GetExportedTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.GetParameters().Any(parameter => IsStartupType(parameter.ParameterType)))
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToArray();

        Assert.IsEmpty(entryPoints, $"{library.GetName().Name} has methods for an app's startup to call, which an app that lazy loads it cannot: {string.Join(", ", entryPoints)}.");
    }

    private static IEnumerable<Type> InjectedProperties(Type component) =>
        component.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
            .Where(property => property.IsDefined(typeof(InjectAttribute)))
            .Select(property => property.PropertyType);

    // The component activator builds a component through a public constructor, whose parameters come from the container.
    private static IEnumerable<Type> ConstructorParameters(Type component) =>
        component.IsAbstract
            ? []
            : component.GetConstructors(BindingFlags.Instance | BindingFlags.Public).SelectMany(constructor => constructor.GetParameters()).Select(parameter => parameter.ParameterType);

    // WebApplicationBuilder and WebApplication count through the interfaces they implement.
    private static bool IsStartupType(Type type)
    {
        if (type.IsByRef) type = type.GetElementType()!;

        return _startupTypes.Contains(type.FullName ?? "") || type.GetInterfaces().Any(contract => _startupTypes.Contains(contract.FullName ?? ""));
    }
}

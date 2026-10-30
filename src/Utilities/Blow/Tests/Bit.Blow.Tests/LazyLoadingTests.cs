using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Blow.Tests;

/// <summary>
/// What keeps Bit.Blow lazy loadable on WebAssembly. An app that downloads it only when a page with a canvas opens has
/// nothing of Blow's in <c>Program.cs</c> - its types are not there yet when it runs - so no component may need a
/// service of Blow's own: only what every Blazor host already registers.
/// </summary>
[TestClass]
public class LazyLoadingTests
{
    // IJSRuntime is what the canvas measures through; NavigationManager is there in every host as well.
    private static readonly Type[] _everyHostRegisters = [typeof(IJSRuntime), typeof(NavigationManager)];

    [TestMethod]
    public void ComponentsInjectOnlyWhatEveryBlazorHostRegisters()
    {
        var injected = typeof(BlowCanvas).Assembly.GetTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(property => property.IsDefined(typeof(InjectAttribute)))
                .Select(property => (Component: type, Service: property.PropertyType)))
            .ToArray();

        // The canvas's IJSRuntime, so a reflection that finds nothing cannot pass for one that finds nothing wrong.
        Assert.Contains((typeof(BlowCanvas), typeof(IJSRuntime)), injected);

        foreach (var (component, service) in injected)
        {
            Assert.Contains(service, _everyHostRegisters,
                $"{component.Name} injects {service.Name}, which an app that lazy loads Bit.Blow cannot register: resolve it from IServiceProvider and fall back to a default instead.");
        }
    }

    [TestMethod]
    public void TheLibraryHasNoRegistrationToCall()
    {
        // The guide says there is no AddBlow... method, and lazy loading depends on there being none to call.
        var registrations = typeof(BlowCanvas).Assembly.GetExportedTypes()
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .Where(method => method.GetParameters().FirstOrDefault()?.ParameterType.FullName == "Microsoft.Extensions.DependencyInjection.IServiceCollection")
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .ToArray();

        Assert.IsEmpty(registrations, string.Join(", ", registrations));
    }
}

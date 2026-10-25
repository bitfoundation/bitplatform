using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Bink.Tests.Components;

/// <summary>
/// What Bink's components take from the application's container. An app that lazy loads Bit.Bink on WebAssembly cannot
/// register anything of Bink's: <c>Program.cs</c> runs before the assembly is there. So every service a component
/// injects has to be one that every Blazor host registers by itself.
/// </summary>
[TestClass]
public sealed class ComponentServicesTests
{
    // Registered by every hosting model. A service of Bink's own here would break every app that lazy loads it.
    private static readonly Type[] FrameworkServices = [typeof(IJSRuntime), typeof(NavigationManager)];

    [TestMethod]
    public void Components_inject_only_what_every_Blazor_host_registers()
    {
        var injected = typeof(BinkCanvas).Assembly.GetTypes()
            .Where(type => typeof(IComponent).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
                .Where(property => property.IsDefined(typeof(InjectAttribute)))
                .Select(property => (Component: type.Name, Service: property.PropertyType)))
            .ToArray();

        Assert.IsNotEmpty(injected, "No component injects anything any more; this test has nothing left to guard.");

        foreach (var (component, service) in injected)
        {
            CollectionAssert.Contains(FrameworkServices, service, $"{component} injects {service.FullName}, which an app that lazy loads Bit.Bink has no way to register.");
        }
    }
}

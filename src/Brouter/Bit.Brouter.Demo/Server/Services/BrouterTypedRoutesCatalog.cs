using System.Reflection;
using Bit.Brouter.Demo.Client;
using Bit.Brouter.Demo.Server.Dtos;

namespace Bit.Brouter.Demo.Server.Services;

/// <summary>
/// Reads back the <c>BrouterRoutes</c> class that the Bit.Brouter.Generators source generator
/// produced for this demo.
/// <para>
/// The generator's output cannot be listed from the library - it is emitted into each consuming
/// project from that project's own route declarations - so the only honest way to show an agent
/// what it gets is to show a real one. This demo compiled against the generator, so its builders
/// are right here in the referenced assembly, sample URLs and all.
/// </para>
/// </summary>
public static class BrouterTypedRoutesCatalog
{
    private const string HowItWorks = """
        Reference the analyzer-only package and the very next build emits a `static partial class BrouterRoutes`
        into your project's root namespace, with one URL builder per route template found in your `.razor` files -
        both `@page` directives and `<Broute Path="...">` declarations:

            <PackageReference Include="Bit.Brouter.Generators" Version="10.6.0-pre-02" PrivateAssets="all" />

        Route parameters become method parameters typed by their constraint (`{id:int}` -> `int id`), optional and
        default-valued parameters become optional arguments, and a catch-all takes the remainder. Named routes
        (`<Broute Name="counter" ...>`) additionally get a constant under `BrouterRoutes.Names`, which is what
        `IBrouter.NavigateToName` / `ResolveUrl` take. Renaming or removing a route then breaks the call site at
        compile time instead of producing a 404 at run time:

            <BrouterLink Href="@BrouterRoutes.Counter(1234)">Counter</BrouterLink>
            brouter.NavigateToName(BrouterRoutes.Names.Counter, new Dictionary<string, object> { ["init"] = 1234 });

        The builders below are the real output for this documentation site, generated from the route table in
        Demo/Client/AppRouter.razor.
        """;

    private static readonly Lazy<BrouterTypedRoutesDto?> _typedRoutes = new(Build);

    public static BrouterTypedRoutesDto? TypedRoutes => _typedRoutes.Value;

    private static BrouterTypedRoutesDto? Build()
    {
        var assembly = typeof(DocsCatalog).Assembly;

        var type = assembly.GetTypes().FirstOrDefault(t => t.Name == "BrouterRoutes" && t.IsAbstract && t.IsSealed);
        if (type is null) return null;

        var builders = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(method => method.IsSpecialName is false && method.ReturnType == typeof(string))
            .OrderBy(method => method.Name, StringComparer.OrdinalIgnoreCase)
            .Select(method => new BrouterTypedRouteDto
            {
                Method = method.Name,
                Signature = $"({string.Join(", ", method.GetParameters().Select(Describe))})",
                ExampleUrl = TryInvoke(method)
            })
            .ToArray();

        var names = type.GetNestedType("Names", BindingFlags.Public)
                       ?.GetFields(BindingFlags.Public | BindingFlags.Static)
                        .Where(field => field.IsLiteral && field.FieldType == typeof(string))
                        .ToDictionary(field => field.Name, field => (string)field.GetRawConstantValue()!)
                    ?? [];

        return new BrouterTypedRoutesDto
        {
            GeneratedFor = assembly.GetName().Name ?? "Bit.Brouter.Demo.Client",
            HowItWorks = HowItWorks,
            Builders = builders,
            Names = names
        };
    }

    private static string Describe(ParameterInfo parameter)
    {
        var name = BrouterApiCatalog.FriendlyName(parameter.ParameterType);

        return parameter.HasDefaultValue ? $"{name} {parameter.Name} = {parameter.DefaultValue ?? "null"}" : $"{name} {parameter.Name}";
    }

    /// <summary>Calls a builder with placeholder arguments, so the listing shows the URL it produces.</summary>
    private static string? TryInvoke(MethodInfo method)
    {
        try
        {
            var arguments = method.GetParameters().Select(Sample).ToArray();

            return method.Invoke(null, arguments) as string;
        }
        catch (Exception)
        {
            // A builder that needs something this code cannot guess simply shows no example.
            return null;
        }
    }

    private static object? Sample(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue) return parameter.DefaultValue;

        var type = Nullable.GetUnderlyingType(parameter.ParameterType) ?? parameter.ParameterType;

        if (type == typeof(string)) return parameter.Name;
        if (type == typeof(Guid)) return Guid.Empty;
        if (type == typeof(DateTime)) return new DateTime(2026, 1, 1);
        if (type == typeof(bool)) return true;
        if (type.IsValueType) return Convert.ChangeType(1, type, System.Globalization.CultureInfo.InvariantCulture);

        return null;
    }
}

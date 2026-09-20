using System.Reflection;

namespace Bit.BlazorUI.Demo.Server.Services.Mcp;

/// <summary>One member a package adds to a type declared somewhere else.</summary>
/// <param name="Name">What is written after the dot - <c>BitThemePresets.MaterialDark</c> is <c>MaterialDark</c>.</param>
/// <param name="Type">What reading it gives back, or the whole signature when the member is a method.</param>
/// <param name="Value">What it returns, when that can be read without a live app - the point of a name catalog.</param>
/// <param name="Summary">The prose the member ships with, read off the declaration the compiler kept.</param>
/// <param name="IsStatic">Whether it is read off the type itself rather than off an instance of it.</param>
public sealed record BlazorUIExtensionMember(string Name, string Type, string? Value, string? Summary, bool IsStatic);

/// <summary>The members one container adds to one type, with the package the container ships in.</summary>
public sealed record BlazorUIExtensionGroup(
    Type Receiver,
    Type Container,
    BlazorUIPackage Package,
    IReadOnlyList<BlazorUIExtensionMember> Members)
{
    /// <summary>The receiver's name without the arity marker a generic type's reflected name carries.</summary>
    public string ReceiverName => Receiver.Name.Split('`')[0];
}

/// <summary>
/// The C# 14 extension members the packages add to each other's types - today the design-system
/// presets <c>Bit.BlazorUI.Extras</c> puts on core's <c>BitThemePresets</c> and <c>BitThemeName</c>.
/// <para>
/// Reflection over a type does not see them: an extension member is compiled into the container
/// that declares it, not into the type it extends, so <c>BitThemePresets.GetFields()</c> answers
/// with the seven presets the core stylesheet carries and none of the nine another package added.
/// An agent reading that answer concludes <c>MaterialDark</c> does not exist - which is exactly
/// backwards, since reaching those names from the core type is the point of the design. So they are
/// read off the containers and answered under the type they are written on.
/// </para>
/// <para>
/// What the compiler emits for <c>extension(T) { ... }</c> is the shape this reads: a nested
/// grouping type holding the members as declared, and inside it a marker type with an
/// <c>&lt;Extension&gt;$</c> method whose one parameter IS the receiver. Nothing is matched on the
/// generated names, which are hashes; the marker method is what says a nested type is an extension
/// block, and its parameter is what says which type the block extends.
/// </para>
/// </summary>
public static class BlazorUIExtensionMembers
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly;
    private const BindingFlags Nested = BindingFlags.Public | BindingFlags.NonPublic;

    /// <summary>The name the compiler gives the method that carries an extension block's receiver.</summary>
    private const string MarkerMethod = "<Extension>$";

    private static readonly Lazy<BlazorUIExtensionGroup[]> _groups = new(Build, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>Every extension block the packages declare, in the order the packages are added.</summary>
    public static BlazorUIExtensionGroup[] All => _groups.Value;

    /// <summary>What another package adds to this type, grouped by the container that declares it.</summary>
    public static BlazorUIExtensionGroup[] For(Type receiver) => [.. All.Where(g => g.Receiver == receiver)];

    /// <summary>
    /// Whether this type is nothing but a container for extension members. One is never named in
    /// code - its members are read off the type they extend - so it is answered as a pointer there
    /// rather than as the compiler-generated methods reflection finds on it.
    /// </summary>
    public static bool IsContainer(Type type) => All.Any(g => g.Container == type);

    private static BlazorUIExtensionGroup[] Build()
    {
        var groups = new List<BlazorUIExtensionGroup>();

        foreach (var package in BlazorUIAssemblies.Packages)
        {
            foreach (var container in Containers(package.Assembly))
            {
                foreach (var skeleton in container.GetNestedTypes(Nested))
                {
                    if (Receiver(skeleton) is not Type receiver) continue;

                    var members = Members(container, skeleton);

                    if (members.Length == 0) continue;

                    groups.Add(new BlazorUIExtensionGroup(receiver, container, package, members));
                }
            }
        }

        return [.. groups];
    }

    /// <summary>
    /// The static classes of an assembly - an extension block can only be declared in one. Read
    /// here rather than off <see cref="BlazorUITypeCatalog"/>, which asks this which of its types
    /// are containers while it is being built.
    /// </summary>
    private static IEnumerable<Type> Containers(Assembly assembly)
    {
        Type[] types;

        try
        {
            types = assembly.GetExportedTypes();
        }
        catch (Exception)
        {
            return [];
        }

        return types.Where(t => t.IsNested is false && t.IsAbstract && t.IsSealed);
    }

    /// <summary>The type an extension block extends, or null when the nested type is not one.</summary>
    private static Type? Receiver(Type skeleton)
    {
        foreach (var marker in skeleton.GetNestedTypes(Nested))
        {
            if (marker.GetMethods(Declared).FirstOrDefault(m => m.Name == MarkerMethod) is not { } method) continue;

            if (method.GetParameters() is [{ } parameter]) return parameter.ParameterType;
        }

        return null;
    }

    private static BlazorUIExtensionMember[] Members(Type container, Type skeleton)
    {
        var members = new List<BlazorUIExtensionMember>();

        foreach (var property in skeleton.GetProperties(Declared))
        {
            var isStatic = property.GetMethod?.IsStatic ?? false;

            members.Add(new BlazorUIExtensionMember(
                property.Name,
                BlazorUITypeNames.Of(property.PropertyType),
                isStatic ? Value(container, property) : null,
                BlazorUIXmlDocs.GetSummary(BlazorUIXmlDocs.IdOf(skeleton, property.Name)),
                isStatic));
        }

        foreach (var method in skeleton.GetMethods(Declared).Where(m => m.IsSpecialName is false && m.Name.StartsWith('<') is false))
        {
            var parameters = string.Join(", ", method.GetParameters().Select(p => $"{BlazorUITypeNames.Of(p.ParameterType)} {p.Name}"));

            members.Add(new BlazorUIExtensionMember(
                method.Name,
                $"{BlazorUITypeNames.Of(method.ReturnType)} {method.Name}({parameters})",
                null,
                BlazorUIXmlDocs.GetSummary($"M:{(skeleton.FullName ?? skeleton.Name).Replace('+', '.')}.{method.Name}"),
                method.IsStatic));
        }

        return [.. members];
    }

    /// <summary>
    /// What a static extension property returns. The declaration in the grouping type is a skeleton
    /// with no body; the implementation is a plain static method on the container, which is what is
    /// read - and only when reading it runs nothing that needs a live app.
    /// </summary>
    private static string? Value(Type container, PropertyInfo property)
    {
        var implementation = container.GetMethod($"get_{property.Name}", BindingFlags.Public | BindingFlags.Static, null, Type.EmptyTypes, null);

        if (implementation is null) return null;

        try
        {
            return Text(implementation.Invoke(null, null));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// A value as a name catalog means it. A string is itself; a type that converts implicitly to
    /// one - <c>BitThemeName</c> wrapping its <c>bit-theme</c> token - is that string, which is the
    /// whole content of such a wrapper. Anything else has no one-cell form and is left out.
    /// </summary>
    private static string? Text(object? value)
    {
        if (value is null) return null;

        if (value is string text) return text;

        var type = value.GetType();

        if (type.IsPrimitive || type.IsEnum) return value.ToString();

        var conversion = type.GetMethod("op_Implicit", BindingFlags.Public | BindingFlags.Static, null, [type], null);

        return conversion?.ReturnType == typeof(string) ? conversion.Invoke(null, [value]) as string : null;
    }
}

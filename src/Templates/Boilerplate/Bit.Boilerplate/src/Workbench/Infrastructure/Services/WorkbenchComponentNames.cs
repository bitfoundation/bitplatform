using System.Runtime.CompilerServices;

namespace Boilerplate.Workbench.Infrastructure.Services;

/// <summary>
/// Maps the root CSS class of each bit BlazorUI component to the component's name, so Inspect mode can name what is under the pointer.
/// </summary>
public static class WorkbenchComponentNames
{
    private static readonly Lazy<Dictionary<string, string>> rootClasses = new(Build, LazyThreadSafetyMode.PublicationOnly);

    // Type arguments tried first for a generic component, before the types of its own assembly.
    private static readonly Type[] commonArguments = [typeof(object), typeof(string), typeof(int)];

    public static IReadOnlyDictionary<string, string> RootClasses => rootClasses.Value;

    /// <summary>
    /// Reads RootElementClass from an uninitialized instance of each component type, so no constructor runs. A generic type is
    /// closed over arguments that meet its constraints, and skipped when none are found.
    /// </summary>
    private static Dictionary<string, string> Build()
    {
        Dictionary<string, string> result = [];
        var rootElementClass = typeof(BitComponentBase).GetProperty("RootElementClass", BindingFlags.Instance | BindingFlags.NonPublic)!;
        Assembly[] assemblies = [typeof(BitButton).Assembly, typeof(BitChart).Assembly, typeof(BitAiChat).Assembly];

        foreach (var assembly in assemblies.Distinct())
        {
            var exported = assembly.GetExportedTypes();

            foreach (var type in exported.Where(type => type.IsClass && type.IsAbstract is false && typeof(BitComponentBase).IsAssignableFrom(type)))
            {
                try
                {
                    if ((type.IsGenericTypeDefinition ? Close(type, exported) : type) is not { } closed)
                        continue;

                    var instance = RuntimeHelpers.GetUninitializedObject(closed);

                    if ((rootElementClass.GetValue(instance) as string ?? ReadInitializer(closed)) is { Length: > 0 } name)
                    {
                        result.TryAdd(name, type.Name.Split('`')[0]);
                    }
                }
                catch (Exception exp) when (exp is ArgumentException or TargetInvocationException)
                {
                    // A getter that needs the constructor's work, or arguments the checks below missed: the type goes unnamed.
                }
            }
        }

        return result;
    }

    /// <summary>Closes a generic component over arguments that meet its constraints, or returns null when one can't be found.</summary>
    private static Type? Close(Type definition, Type[] exported)
    {
        var arguments = definition.GetGenericArguments().Select(parameter => ArgumentFor(parameter, exported)).ToArray();

        return arguments.Any(argument => argument is null) ? null : definition.MakeGenericType([.. arguments.OfType<Type>()]);
    }

    /// <summary>
    /// The first type that meets a generic parameter's constraints; null when a constraint names another generic parameter
    /// or nothing fits.
    /// </summary>
    private static Type? ArgumentFor(Type parameter, Type[] exported)
    {
        var constraints = parameter.GetGenericParameterConstraints();

        if (constraints.Any(constraint => constraint.ContainsGenericParameters))
            return null;

        return commonArguments.Concat(exported).FirstOrDefault(candidate => Fits(candidate, parameter.GenericParameterAttributes, constraints));
    }

    private static bool Fits(Type candidate, GenericParameterAttributes attributes, Type[] constraints)
    {
        if (candidate.IsAbstract || candidate.ContainsGenericParameters || candidate.IsByRefLike)
            return false;

        if (attributes.HasFlag(GenericParameterAttributes.ReferenceTypeConstraint) && candidate.IsValueType)
            return false;

        if (attributes.HasFlag(GenericParameterAttributes.NotNullableValueTypeConstraint) && (candidate.IsValueType is false || Nullable.GetUnderlyingType(candidate) is not null))
            return false;

        if (attributes.HasFlag(GenericParameterAttributes.DefaultConstructorConstraint) && candidate.IsValueType is false && candidate.GetConstructor(Type.EmptyTypes) is null)
            return false;

        return constraints.All(constraint => constraint.IsAssignableFrom(candidate));
    }

    /// <summary>
    /// The class an auto-property sets in the constructor, as in RootElementClass { get; } = "bit-cal", read from the
    /// constructor's IL: a ldstr followed by a stfld of the property's backing field.
    /// </summary>
    private static string? ReadInitializer(Type type)
    {
        for (var declaring = type; declaring is not null && declaring != typeof(BitComponentBase); declaring = declaring.BaseType)
        {
            if (declaring.GetField("<RootElementClass>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly) is not { } field)
                continue;

            foreach (var constructor in declaring.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (constructor.GetMethodBody()?.GetILAsByteArray() is not { } il)
                    continue;

                for (var index = 0; index + 10 <= il.Length; index++)
                {
                    if (il[index] is 0x72 && il[index + 5] is 0x7D && BitConverter.ToInt32(il, index + 6) == field.MetadataToken)
                        return declaring.Module.ResolveString(BitConverter.ToInt32(il, index + 1));
                }
            }
        }

        return null;
    }
}

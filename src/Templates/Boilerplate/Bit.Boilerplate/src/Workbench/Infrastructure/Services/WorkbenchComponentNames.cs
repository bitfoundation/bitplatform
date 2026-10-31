using System.Runtime.CompilerServices;

namespace Boilerplate.Workbench.Infrastructure.Services;

public static class WorkbenchComponentNames
{
    private static readonly Lazy<Dictionary<string, string>> rootClasses = new(Build);

    public static IReadOnlyDictionary<string, string> RootClasses => rootClasses.Value;

    private static Dictionary<string, string> Build()
    {
        Dictionary<string, string> result = [];
        var rootElementClass = typeof(BitComponentBase).GetProperty("RootElementClass", BindingFlags.Instance | BindingFlags.NonPublic);

        var types = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => assembly.GetName().Name?.StartsWith("Bit.BlazorUI", StringComparison.Ordinal) is true)
            .SelectMany(assembly => assembly.GetExportedTypes())
            .Where(type => type.IsClass && type.IsAbstract is false && typeof(BitComponentBase).IsAssignableFrom(type));

        foreach (var type in types)
        {
            try
            {
                var concrete = type.IsGenericTypeDefinition ? type.MakeGenericType([.. type.GetGenericArguments().Select(_ => typeof(object))]) : type;
                var instance = concrete.GetConstructor(Type.EmptyTypes) is not null ? Activator.CreateInstance(concrete) : RuntimeHelpers.GetUninitializedObject(concrete);

                if (rootElementClass?.GetValue(instance) is string { Length: > 0 } name)
                {
                    result.TryAdd(name, type.Name.Split('`')[0]);
                }
            }
            catch (Exception exp) when (exp is ArgumentException or TargetInvocationException or NotSupportedException or MemberAccessException)
            {
            }
        }

        return result;
    }
}

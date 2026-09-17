using System.Text.RegularExpressions;
using Mono.Cecil;

namespace Bit.Minifier;

/// <summary>
/// Opt-in passes that trade debuggability and some compatibility for size: every non-public name in every
/// assembly is shortened. Public API names stay, so stack traces still say where things happened. A name that
/// appears as a word in any string literal of the app is kept - that is what nameof(...) and GetMethod("...")
/// compile to - and the assemblies the runtime binds to by name from native code are left out of renaming.
/// </summary>
internal sealed partial class AggressiveMinifier(Action<ModuleDefinition, string, string, string> map)
{
    // the Mono runtime and the JS interop layer look members of these up by name, from native code
    private static readonly HashSet<string> RuntimeBound = new(StringComparer.OrdinalIgnoreCase) { "System.Private.CoreLib", "System.Runtime.InteropServices.JavaScript" };

    public static readonly HashSet<string> MoreAttributes =
    [
        "System.CLSCompliantAttribute",
        "System.ObsoleteAttribute",
        "System.ParamArrayAttribute",
        "System.ComponentModel.EditorBrowsableAttribute",
        "System.CodeDom.Compiler.GeneratedCodeAttribute",
        "System.Diagnostics.DebuggerBrowsableAttribute",
        "System.Diagnostics.DebuggerDisplayAttribute",
        "System.Diagnostics.DebuggerHiddenAttribute",
        "System.Diagnostics.DebuggerNonUserCodeAttribute",
        "System.Diagnostics.DebuggerStepThroughAttribute",
        "System.Diagnostics.DebuggerStepperBoundaryAttribute",
        "System.Diagnostics.DebuggerTypeProxyAttribute",
        "System.Diagnostics.CodeAnalysis.ConstantExpectedAttribute",
        "System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute",
        "System.Diagnostics.CodeAnalysis.ExperimentalAttribute",
        "System.Diagnostics.CodeAnalysis.RequiresAssemblyFilesAttribute",
        "System.Diagnostics.CodeAnalysis.RequiresDynamicCodeAttribute",
        "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute",
        "System.Diagnostics.CodeAnalysis.StringSyntaxAttribute",
        "System.Diagnostics.CodeAnalysis.SuppressMessageAttribute",
        "System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute",
        "System.Diagnostics.CodeAnalysis.UnscopedRefAttribute",
        "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute",
        "System.Runtime.CompilerServices.CallerFilePathAttribute",
        "System.Runtime.CompilerServices.CallerLineNumberAttribute",
        "System.Runtime.CompilerServices.CallerMemberNameAttribute",
        "System.Runtime.CompilerServices.CollectionBuilderAttribute",
        "System.Runtime.CompilerServices.CompilerFeatureRequiredAttribute",
        "System.Runtime.CompilerServices.DynamicAttribute",
        "System.Runtime.CompilerServices.ExtensionAttribute",
        "System.Runtime.CompilerServices.InterpolatedStringHandlerArgumentAttribute",
        "System.Runtime.CompilerServices.InterpolatedStringHandlerAttribute",
        "System.Runtime.CompilerServices.IsReadOnlyAttribute",
        "System.Runtime.CompilerServices.IsUnmanagedAttribute",
        "System.Runtime.CompilerServices.NativeIntegerAttribute",
        "System.Runtime.CompilerServices.OverloadResolutionPriorityAttribute",
        "System.Runtime.CompilerServices.ParamCollectionAttribute",
        "System.Runtime.CompilerServices.RequiresLocationAttribute",
        "System.Runtime.CompilerServices.TupleElementNamesAttribute",
        "System.Reflection.AssemblyCompanyAttribute",
        "System.Reflection.AssemblyConfigurationAttribute",
        "System.Reflection.AssemblyCopyrightAttribute",
        "System.Reflection.AssemblyDefaultAliasAttribute",
        "System.Reflection.AssemblyDescriptionAttribute",
        "System.Reflection.AssemblyFileVersionAttribute",
        "System.Reflection.AssemblyProductAttribute",
        "System.Reflection.AssemblyTitleAttribute",
        "System.Reflection.AssemblyTrademarkAttribute",
        "System.Resources.NeutralResourcesLanguageAttribute",
    ];

    private readonly HashSet<string> literalWords = [];

    /// <summary>Whether a string literal of the app mentions the name, the way nameof(...) and GetMethod("...") do.</summary>
    public bool IsMentioned(string name) => literalWords.Contains(name);

    /// <summary>Every identifier-like word of every string literal and attribute string in the set.</summary>
    public void CollectLiterals(IEnumerable<ModuleDefinition> modules)
    {
        foreach (var module in modules)
        {
            foreach (var type in module.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody is false) continue;
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.Operand is string literal) AddWords(literal);
                    }
                }
            }
            foreach (var provider in AssemblyMinifier.Providers(module))
            {
                foreach (var attribute in provider.CustomAttributes)
                {
                    // [UnsafeAccessor] without Name reaches the member named like the accessor itself
                    if (attribute.AttributeType.FullName == "System.Runtime.CompilerServices.UnsafeAccessorAttribute" && provider is MethodDefinition accessor
                        && attribute.Properties.Any(p => p.Name == "Name") is false)
                    {
                        literalWords.Add(accessor.Name);
                    }
                    foreach (var argument in attribute.ConstructorArguments.Concat(attribute.Properties.Select(p => p.Argument)).Concat(attribute.Fields.Select(f => f.Argument)))
                    {
                        if (argument.Value is string text) AddWords(text);
                    }
                }
            }
        }
    }

    private void AddWords(string text)
    {
        literalWords.Add(text);
        foreach (Match word in Words().Matches(text)) literalWords.Add(word.Value);
    }

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex Words();

    // the JS interop source generator's registration (__GeneratedInitializer.__Register_, __Wrapper_*) is bound from native code
    private static bool IsNativeBound(TypeDefinition type)
        => type.Namespace.StartsWith("System.Runtime.InteropServices.JavaScript", StringComparison.Ordinal) || type.Name.StartsWith("__", StringComparison.Ordinal)
            || (type.DeclaringType is not null && IsNativeBound(type.DeclaringType));

    public int Run(ModuleDefinition module, bool renameInternals)
    {
        if (RuntimeBound.Contains(module.Assembly.Name.Name)) return ClearParameterNames(module, renameInternals);

        return RemovePrivateProperties(module, renameInternals)
            + RenameMembers(module, renameInternals)
            + ClearParameterNames(module, renameInternals)
            + RenameTypes(module, renameInternals);
    }

    private int RemovePrivateProperties(ModuleDefinition module, bool renameInternals)
    {
        int removed = 0;
        foreach (var type in module.GetTypes())
        {
            if (type.IsInterface || IsNativeBound(type) || IsComponent(type)) continue;
            for (int i = type.Properties.Count - 1; i >= 0; i--)
            {
                var property = type.Properties[i];
                var accessors = new[] { property.GetMethod, property.SetMethod }.Where(m => m is not null).ToList();
                if (IsBound(property) || property.HasOtherMethods || literalWords.Contains(property.Name)) continue;
                // public properties are what serializers see, whoever declares them
                if (accessors.Any(m => m!.IsPublic || m.IsVirtual || m.HasOverrides || IsBound(m) || IsRenamable(type, m, renameInternals) is false)) continue;
                foreach (var accessor in accessors) accessor!.IsSpecialName = false;
                type.Properties.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    private int RenameMembers(ModuleDefinition module, bool renameInternals)
    {
        int renamed = 0;
        foreach (var type in module.GetTypes())
        {
            if (type.IsInterface || IsNativeBound(type)) continue;
            if (type.IsEnum is false && type.IsExplicitLayout is false)
            {
                var taken = type.Fields.Select(f => f.Name).ToHashSet();
                int next = 0;
                foreach (var field in type.Fields)
                {
                    if (field.IsPublic || field.IsSpecialName || field.IsRuntimeSpecialName || IsBound(field) || literalWords.Contains(field.Name) || field.Name.StartsWith('<')) continue;
                    if (IsRenamable(type, field.IsPrivate, field.IsPublic, field.IsFamily || field.IsFamilyOrAssembly, renameInternals) is false) continue;
                    var name = ShortName.Next(taken, ref next);
                    map(module, "F", $"{type.FullName}::{field.Name}", name);
                    field.Name = name;
                    renamed++;
                }
            }

            var takenMethods = type.Methods.Select(m => m.Name).ToHashSet();
            int nextMethod = 0;
            foreach (var method in type.Methods)
            {
                if (method.IsConstructor || method.IsSpecialName || method.IsRuntimeSpecialName || method.IsVirtual || method.HasOverrides
                    || method.IsPInvokeImpl || method.IsInternalCall || IsBound(method) || literalWords.Contains(method.Name)
                    || method.Name.StartsWith("__", StringComparison.Ordinal)
                    // generated names are the other pass's, and some are looked up by convention (<Main>$ by the WebAssembly host)
                    || method.Name.StartsWith('<')) continue;
                if (IsRenamable(type, method, renameInternals) is false) continue;
                var name = ShortName.Next(takenMethods, ref nextMethod);
                map(module, "M", $"{type.FullName}::{method.Name}", name);
                method.Name = name;
                renamed++;
            }
        }
        return renamed;
    }

    private static int ClearParameterNames(ModuleDefinition module, bool renameInternals)
    {
        int cleared = 0;
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods)
            {
                // constructor parameters are what serializers bind by name; overrides don't care about names
                if (method.IsConstructor || IsBound(method)) continue;
                if (IsRenamable(type, method, renameInternals) is false && type.Name.StartsWith('<') is false) continue;
                foreach (var parameter in method.Parameters)
                {
                    if (parameter.Name.Length == 0 || parameter.HasCustomAttributes) continue;
                    parameter.Name = "";
                    cleared++;
                }
            }
        }
        return cleared;
    }

    private int RenameTypes(ModuleDefinition module, bool renameInternals)
    {
        int renamed = 0;
        var resources = module.Resources.Select(r => r.Name).ToList();
        var topLevel = module.Types.Select(t => t.FullName).ToHashSet();
        int nextTop = 0;
        foreach (var type in module.GetTypes().ToList())
        {
            if (type.Name == "<Module>" || type.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)) continue;
            if (IsVisibleOutside(type) || IsBound(type) || IsNativeBound(type)) continue;
            if (type.IsNested is false && renameInternals is false) continue;
            var plain = type.Name.Split('`')[0];
            if (literalWords.Contains(plain) || IsComponent(type)) continue;
            // ResourceManager(typeof(T)) and IStringLocalizer<T> find the embedded resource by the type's full name
            if (resources.Any(r => r.StartsWith(type.FullName + ".", StringComparison.Ordinal))) continue;

            var arity = type.Name.Contains('`') ? type.Name[type.Name.IndexOf('`')..] : "";
            string name;
            if (type.IsNested)
            {
                var taken = type.DeclaringType.NestedTypes.Select(t => t.Name).ToHashSet();
                int next = 0;
                do name = "_" + ShortName.Get(next++) + arity; while (taken.Contains(name));
            }
            else
            {
                do name = "_" + ShortName.Get(nextTop++) + arity; while (topLevel.Contains(type.Namespace + "." + name));
                topLevel.Add(type.Namespace + "." + name);
            }
            map(module, "T", type.FullName, name);
            if (type.IsNested)
            {
                type.Name = name;
            }
            else
            {
                // the module looks top-level types up through a name cache that only Add/Remove maintain
                var index = module.Types.IndexOf(type);
                module.Types.RemoveAt(index);
                type.Name = name;
                module.Types.Insert(index, type);
            }
            renamed++;
        }
        return renamed;
    }

    // an attribute left after stripping is one something reads at runtime, except the state machine markers
    private static bool IsBound(ICustomAttributeProvider provider)
        => provider.CustomAttributes.Any(a => a.AttributeType.Name.EndsWith("StateMachineAttribute", StringComparison.Ordinal) is false
            && a.AttributeType.FullName != "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    // Blazor sends component type names from the (unminified) server to the client
    private bool IsComponent(TypeDefinition type)
    {
        for (var t = type; t is not null;)
        {
            if (t.Interfaces.Any(i => i.InterfaceType.FullName == "Microsoft.AspNetCore.Components.IComponent")) return true;
            if (t.BaseType is null) return false;
            t = ReferenceIndex.Resolve(t.BaseType);
        }
        return false;
    }

    private static bool IsRenamable(TypeDefinition type, MethodDefinition method, bool renameInternals)
        => IsRenamable(type, method.IsPrivate, method.IsPublic, method.IsFamily || method.IsFamilyOrAssembly, renameInternals);

    private static bool IsRenamable(TypeDefinition type, bool isPrivate, bool isPublic, bool isProtected, bool renameInternals)
    {
        if (isPrivate) return true;
        if (renameInternals is false) return false;
        // a public member of a type nobody outside can see is as internal as the type
        if (isPublic || isProtected) return IsVisibleOutside(type) is false;
        return true;
    }

    private static bool IsVisibleOutside(TypeDefinition type)
    {
        for (var t = type; t is not null; t = t.DeclaringType)
        {
            if ((t.IsPublic || t.IsNestedPublic || t.IsNestedFamily || t.IsNestedFamilyOrAssembly) is false) return false;
        }
        return true;
    }
}

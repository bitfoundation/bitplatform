using System.Text.RegularExpressions;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Bit.Minifier;

/// <summary>How far renaming may reach into an assembly: who else can see its names decides.</summary>
internal enum RenameScope
{
    /// <summary>An InternalsVisibleTo friend isn't minified: only private names change.</summary>
    Private,

    /// <summary>Every non-public name changes, and public ones of types nobody outside can see.</summary>
    Internal,

    /// <summary>Super aggressive, and only minified assemblies reference this one: public names change too.</summary>
    Public,
}

/// <summary>
/// Opt-in passes that trade debuggability and some compatibility for size: every non-public name in every
/// assembly is shortened. Public API names stay, so stack traces still say where things happened. A name that
/// appears as a word in any string literal of the app is kept - that is what nameof(...) and GetMethod("...")
/// compile to - and the assemblies the runtime binds to by name from native code are left out of renaming.
/// Super aggressive (<see cref="RenameScope.Public"/>) renames public names too, and clears namespaces,
/// generic parameter names and event metadata.
/// </summary>
internal sealed partial class AggressiveMinifier(Action<ModuleDefinition, string, string, string> map, bool super, bool keepPublicFields = false)
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
    ];

    // super aggressive: read by the compiler, analyzers, the trimmer or a debugger only
    public static readonly HashSet<string> SuperAttributes =
    [
        "Microsoft.CodeAnalysis.EmbeddedAttribute",
        "System.Diagnostics.DebuggableAttribute",
        "System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute",
        "System.Diagnostics.CodeAnalysis.FeatureGuardAttribute",
        "System.Diagnostics.CodeAnalysis.FeatureSwitchDefinitionAttribute",
        "System.Reflection.AssemblyDelaySignAttribute",
        "System.Reflection.AssemblyKeyFileAttribute",
        "System.Reflection.AssemblyKeyNameAttribute",
        "System.Reflection.AssemblySignatureKeyAttribute",
        "System.Runtime.CompilerServices.AsyncMethodBuilderAttribute",
        "System.Runtime.CompilerServices.EnumeratorCancellationAttribute",
        "System.Runtime.CompilerServices.ExtensionMarkerAttribute",
        "System.Runtime.CompilerServices.ModuleInitializerAttribute",
        "System.Runtime.CompilerServices.SkipLocalsInitAttribute",
        "System.Runtime.InteropServices.ComVisibleAttribute",
        "System.Runtime.Versioning.ObsoletedOSPlatformAttribute",
        "System.Runtime.Versioning.RequiresPreviewFeaturesAttribute",
        "System.Runtime.Versioning.SupportedOSPlatformAttribute",
        "System.Runtime.Versioning.SupportedOSPlatformGuardAttribute",
        "System.Runtime.Versioning.TargetPlatformAttribute",
        "System.Runtime.Versioning.UnsupportedOSPlatformAttribute",
        "System.Runtime.Versioning.UnsupportedOSPlatformGuardAttribute",
    ];

    private const string Component = "Microsoft.AspNetCore.Components.IComponent";

    private readonly HashSet<string> literalWords = [];
    // every dotted prefix of every dotted word ("A.B.C" -> "A", "A.B", "A.B.C"): namespaces a string names
    private readonly HashSet<string> literalNamespaces = [];
    // methods an expression tree holds by token: Expression.Property wants their property metadata
    private readonly HashSet<MethodDefinition> tokens = [];
    // decided before anything is renamed, since renaming changes the names they are recognized by
    private readonly HashSet<TypeDefinition> components = [];
    private readonly HashSet<TypeDefinition> keptTypes = [];
    private readonly HashSet<MethodDefinition> staticImplementations = [];
    private readonly Dictionary<TypeDefinition, string> originalNames = [];

    /// <summary>Whether a string literal of the app mentions the name, the way nameof(...) and GetMethod("...") do.</summary>
    public bool IsMentioned(string name) => literalWords.Contains(name);

    /// <summary>
    /// Everything the passes need to know before the first rename: every identifier-like word of every string
    /// literal, attribute string and resource name of <paramref name="modules"/> and <paramref name="readers"/>
    /// (assemblies that are not rewritten but may reach the rewritten ones by name), the methods loaded by
    /// token, and the Blazor components. Only the words when nothing but generated names is renamed.
    /// </summary>
    public void Collect(IReadOnlyCollection<ModuleDefinition> modules, IEnumerable<ModuleDefinition> readers, bool wordsOnly)
    {
        foreach (var module in modules.Concat(readers))
        {
            foreach (var type in module.GetTypes())
            {
                foreach (var method in type.Methods)
                {
                    if (method.HasBody is false) continue;
                    foreach (var instruction in method.Body.Instructions)
                    {
                        if (instruction.Operand is string literal) AddWords(literal);
                        else if (instruction.OpCode.Code == Code.Ldtoken && instruction.Operand is MethodReference token && ReferenceIndex.Resolve(token) is MethodDefinition loaded) tokens.Add(loaded);
                    }
                }
            }
            // ResourceManager(typeof(T)) and IStringLocalizer<T> find embedded resources by the type's name
            foreach (var resource in module.Resources) AddWords(resource.Name);
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

        if (wordsOnly) return;
        foreach (var type in modules.SelectMany(m => m.GetTypes()))
        {
            originalNames[type] = type.FullName;
            CollectStaticImplementations(type);
            // a server reports its exceptions by type name, and clients map them back by it
            if (super && IsException(type)) keptTypes.Add(type);
            if (IsComponent(type) is false) continue;
            components.Add(type);
            if (super is false) continue;
            // a server that prerenders the component sends the types of its parameters by name
            foreach (var property in type.Properties)
            {
                if (property.CustomAttributes.Any(a => a.AttributeType.Name.EndsWith("ParameterAttribute", StringComparison.Ordinal))) Keep(property.PropertyType);
            }
        }
    }

    /// <summary>
    /// A static method may implement a static abstract interface member implicitly, by name alone. Roslyn writes a
    /// MethodImpl either way, which keeps the name already; IL from elsewhere need not.
    /// </summary>
    private void CollectStaticImplementations(TypeDefinition type)
    {
        if (type.IsInterface || type.Methods.Any(m => m.IsStatic && m.IsConstructor is false) is false) return;
        var names = new HashSet<string>();
        for (var t = type; t is not null; t = t.BaseType is null ? null : ReferenceIndex.Resolve(t.BaseType))
        {
            // the type lists every interface it implements, the inherited ones included
            foreach (var implementation in t.Interfaces)
            {
                if (ReferenceIndex.Resolve(implementation.InterfaceType) is not { } @interface)
                {
                    staticImplementations.UnionWith(type.Methods.Where(m => m.IsStatic));
                    return;
                }
                names.UnionWith(@interface.Methods.Where(m => m.IsStatic && m.IsVirtual).Select(m => m.Name));
            }
        }
        staticImplementations.UnionWith(type.Methods.Where(m => m.IsStatic && names.Contains(m.Name)));
    }

    private void Keep(TypeReference type)
    {
        switch (type)
        {
            case GenericInstanceType generic:
                Keep(generic.ElementType);
                foreach (var argument in generic.GenericArguments) Keep(argument);
                break;
            case TypeSpecification specification:
                Keep(specification.ElementType);
                break;
            case GenericParameter:
                break;
            default:
                for (var definition = ReferenceIndex.Resolve(type); definition is not null; definition = definition.DeclaringType)
                {
                    if (keptTypes.Add(definition) is false) break;
                }
                break;
        }
    }

    private void AddWords(string text)
    {
        literalWords.Add(text);
        foreach (Match word in Words().Matches(text)) literalWords.Add(word.Value);
        if (super is false) return;
        foreach (Match dotted in DottedWords().Matches(text))
        {
            for (int dot = dotted.Value.IndexOf('.'); dot > 0; dot = dotted.Value.IndexOf('.', dot + 1)) literalNamespaces.Add(dotted.Value[..dot]);
            literalNamespaces.Add(dotted.Value);
        }
    }

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*")]
    private static partial Regex Words();

    [GeneratedRegex(@"[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)+")]
    private static partial Regex DottedWords();

    // the JS interop source generator's registration (__GeneratedInitializer.__Register_, __Wrapper_*) is bound from native code
    private static bool IsNativeBound(TypeDefinition type)
        => type.Namespace.StartsWith("System.Runtime.InteropServices.JavaScript", StringComparison.Ordinal) || type.Name.StartsWith("__", StringComparison.Ordinal)
            || (type.DeclaringType is not null && IsNativeBound(type.DeclaringType));

    public int Run(ModuleDefinition module, RenameScope scope)
    {
        if (RuntimeBound.Contains(module.Assembly.Name.Name)) return ClearParameterNames(module, scope == RenameScope.Public ? RenameScope.Internal : scope);

        return RemovePrivateProperties(module, scope)
            + (super ? RemoveEvents(module, scope) : 0)
            + RenameMembers(module, scope)
            + ClearParameterNames(module, scope)
            + RenameTypes(module, scope)
            + (super ? RenameGenericParameters(module, scope) : 0);
    }

    private int RemovePrivateProperties(ModuleDefinition module, RenameScope scope)
    {
        int removed = 0;
        foreach (var type in module.GetTypes())
        {
            if (type.IsInterface || IsNativeBound(type) || components.Contains(type)) continue;
            for (int i = type.Properties.Count - 1; i >= 0; i--)
            {
                var property = type.Properties[i];
                var accessors = new[] { property.GetMethod, property.SetMethod }.Where(m => m is not null).ToList();
                if (IsBound(property) || property.HasOtherMethods || literalWords.Contains(property.Name)) continue;
                // public properties are what serializers see, whoever declares them
                if (accessors.Any(m => m!.IsPublic || m.IsVirtual || m.HasOverrides || IsBound(m) || tokens.Contains(m) || IsRenamable(type, m, scope) is false)) continue;
                foreach (var accessor in accessors) accessor!.IsSpecialName = false;
                type.Properties.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    // event rows only serve reflection; the accessors they list stay, as plain methods
    private int RemoveEvents(ModuleDefinition module, RenameScope scope)
    {
        int removed = 0;
        foreach (var type in module.GetTypes())
        {
            if (type.IsInterface || IsNativeBound(type) || components.Contains(type)) continue;
            for (int i = type.Events.Count - 1; i >= 0; i--)
            {
                var @event = type.Events[i];
                var accessors = new[] { @event.AddMethod, @event.RemoveMethod, @event.InvokeMethod }.Where(m => m is not null).ToList();
                if (IsBound(@event) || @event.HasOtherMethods || literalWords.Contains(@event.Name)) continue;
                if (accessors.Any(m => m!.IsVirtual || m.HasOverrides || IsBound(m) || tokens.Contains(m) || IsRenamable(type, m, scope) is false)) continue;
                foreach (var accessor in accessors) accessor!.IsSpecialName = false;
                type.Events.RemoveAt(i);
                removed++;
            }
        }
        return removed;
    }

    private int RenameMembers(ModuleDefinition module, RenameScope scope)
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
                    if (field.IsSpecialName || field.IsRuntimeSpecialName || IsBound(field) || literalWords.Contains(field.Name) || field.Name.StartsWith('<')) continue;
                    // public fields stay below super aggressive, whoever declares them: serializers may see them. Above,
                    // constants stay (code lists them by reflection), and all of them when Newtonsoft.Json may serialize them
                    if (field.IsPublic && (scope != RenameScope.Public || field.IsLiteral || keepPublicFields)) continue;
                    if (IsRenamable(type, field.IsPrivate, field.IsPublic, field.IsFamily || field.IsFamilyOrAssembly, scope) is false) continue;
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
                if (method.IsConstructor || (method.IsSpecialName && IsRenamableAccessor(method) is false) || method.IsRuntimeSpecialName || method.IsVirtual || method.HasOverrides
                    || method.IsPInvokeImpl || method.IsInternalCall || IsBound(method) || literalWords.Contains(method.Name)
                    || method.Name.StartsWith("__", StringComparison.Ordinal)
                    // generated names are the other pass's, and some are looked up by convention (<Main>$ by the WebAssembly host)
                    || method.Name.StartsWith('<')
                    || IsEntryPoint(method)
                    || IsPropertyConvention(type, method)
                    || staticImplementations.Contains(method)) continue;
                if (IsRenamable(type, method, scope) is false) continue;
                var name = ShortName.Next(takenMethods, ref nextMethod);
                map(module, "M", $"{type.FullName}::{method.Name}", name);
                method.Name = name;
                renamed++;
            }
        }
        return renamed;
    }

    /// <summary>
    /// Super aggressive: a property's or an event's accessors are tied to it by metadata (MethodSemantics), not by
    /// name, which is how reflection, serializers and Blazor find them. Operators stay: Expression and dynamic look
    /// them up by name.
    /// </summary>
    private bool IsRenamableAccessor(MethodDefinition method)
        => super && method.SemanticsAttributes != MethodSemanticsAttributes.None && method.Name.StartsWith("op_", StringComparison.Ordinal) is false;

    /// <summary>
    /// The WebAssembly host starts an async Main through the method the entry point's name points at:
    /// <c>&lt;Main&gt;</c> leads it to <c>&lt;Main&gt;$</c> or <c>Main</c>.
    /// </summary>
    private static bool IsEntryPoint(MethodDefinition method)
    {
        if (method.Module.EntryPoint is not { } entry || entry.DeclaringType != method.DeclaringType) return false;
        return method == entry || method.Name == entry.Name.Trim('<', '>') || method.Name == entry.Name + "$";
    }

    private int ClearParameterNames(ModuleDefinition module, RenameScope scope)
    {
        int cleared = 0;
        foreach (var type in module.GetTypes())
        {
            foreach (var method in type.Methods)
            {
                // constructor parameters are what serializers bind by name; overrides don't care about names
                if (method.IsConstructor || IsBound(method)) continue;
                if (IsRenamable(type, method, scope) is false && type.Name.StartsWith('<') is false) continue;
                // public parameters (super aggressive) keep the names a string mentions: a generated client may build requests
                // from them, and a reflection-based one (Refit) from any interface method's
                var exposed = IsRenamable(type, method, RenameScope.Internal) is false;
                if (exposed && type.IsInterface) continue;
                foreach (var parameter in method.Parameters)
                {
                    if (parameter.Name.Length == 0 || parameter.HasCustomAttributes || (exposed && literalWords.Contains(parameter.Name))) continue;
                    parameter.Name = "";
                    cleared++;
                }
            }
        }
        return cleared;
    }

    private int RenameTypes(ModuleDefinition module, RenameScope scope)
    {
        int renamed = 0;
        var resources = module.Resources.Select(r => r.Name).ToList();
        var topLevel = module.Types.Select(t => t.FullName).ToHashSet();
        int nextTop = 0;
        foreach (var type in module.GetTypes().ToList())
        {
            if (type.Name == "<Module>" || type.Name.StartsWith("<PrivateImplementationDetails>", StringComparison.Ordinal)) continue;
            if ((IsVisibleOutside(type) && scope != RenameScope.Public) || IsBound(type) || IsNativeBound(type)) continue;
            if (type.IsNested is false && scope == RenameScope.Private) continue;
            var plain = type.Name.Split('`')[0];
            if (literalWords.Contains(plain) || components.Contains(type) || keptTypes.Contains(type)) continue;
            // a type's FullName is cached, and stale once the type it is nested in is renamed
            var original = originalNames.GetValueOrDefault(type, type.FullName);
            if (resources.Any(r => r.StartsWith(original + ".", StringComparison.Ordinal))) continue;

            var arity = type.Name.Contains('`') ? type.Name[type.Name.IndexOf('`')..] : "";
            string name;
            if (type.IsNested)
            {
                var taken = type.DeclaringType.NestedTypes.Select(t => t.Name).ToHashSet();
                int next = 0;
                do name = "_" + ShortName.Get(next++) + arity; while (taken.Contains(name));
                type.Name = name;
            }
            else
            {
                // super aggressive: the namespace goes too, unless a string names it
                var @namespace = super && literalWords.Contains(type.Namespace) is false && literalNamespaces.Contains(type.Namespace) is false ? "" : type.Namespace;
                do name = "_" + ShortName.Get(nextTop++) + arity; while (topLevel.Contains(FullName(@namespace, name)));
                topLevel.Add(FullName(@namespace, name));
                // the module looks top-level types up through a name cache that only Add/Remove maintain
                var index = module.Types.IndexOf(type);
                module.Types.RemoveAt(index);
                type.Name = name;
                type.Namespace = @namespace;
                module.Types.Insert(index, type);
                name = type.FullName;
            }
            map(module, "T", original, name);
            renamed++;
        }
        return renamed;
    }

    private static string FullName(string @namespace, string name) => @namespace.Length == 0 ? name : @namespace + "." + name;

    // T, TKey, TValue: names only reflection and stack traces show
    private int RenameGenericParameters(ModuleDefinition module, RenameScope scope)
    {
        int renamed = 0;
        foreach (var type in module.GetTypes())
        {
            if (IsNativeBound(type)) continue;
            if (scope == RenameScope.Public || (scope == RenameScope.Internal && IsVisibleOutside(type) is false)) renamed += Rename(originalNames.GetValueOrDefault(type, type.FullName), type.GenericParameters);
            foreach (var method in type.Methods)
            {
                if (IsRenamable(type, method, scope)) renamed += Rename($"{originalNames.GetValueOrDefault(type, type.FullName)}::{method.Name}", method.GenericParameters);
            }
        }
        return renamed;

        int Rename(string owner, IList<GenericParameter> parameters)
        {
            int count = 0;
            for (int i = 0; i < parameters.Count; i++)
            {
                var parameter = parameters[i];
                var name = ShortName.Get(i);
                if (parameter.Name == name || parameter.HasCustomAttributes || literalWords.Contains(parameter.Name) || parameters.Any(p => p.Name == name)) continue;
                map(module, "G", $"{owner}<{parameter.Name}>", name);
                parameter.Name = name;
                count++;
            }
            return count;
        }
    }

    // an attribute left after stripping is one something reads at runtime, except the state machine markers
    private static bool IsBound(ICustomAttributeProvider provider)
        => provider.CustomAttributes.Any(a => a.AttributeType.Name.EndsWith("StateMachineAttribute", StringComparison.Ordinal) is false
            && a.AttributeType.FullName != "System.Runtime.CompilerServices.CompilerGeneratedAttribute");

    // Blazor sends component type names from the (unminified) server to the client
    private static bool IsComponent(TypeDefinition type)
    {
        for (var t = type; t is not null;)
        {
            if (t.Interfaces.Any(i => i.InterfaceType.FullName == Component)) return true;
            if (t.BaseType is null) return false;
            t = ReferenceIndex.Resolve(t.BaseType);
        }
        return false;
    }

    private static bool IsException(TypeDefinition type)
    {
        for (var t = type; t is not null; t = t.BaseType is null ? null : ReferenceIndex.Resolve(t.BaseType))
        {
            if (t.FullName == "System.Exception") return true;
        }
        return false;
    }

    // Newtonsoft.Json and component models find ShouldSerializeX() and ResetX() by the name of property X
    private static bool IsPropertyConvention(TypeDefinition type, MethodDefinition method)
    {
        foreach (var prefix in new[] { "ShouldSerialize", "Reset" })
        {
            if (method.Name.StartsWith(prefix, StringComparison.Ordinal) && type.Properties.Any(p => p.Name.Length == method.Name.Length - prefix.Length && method.Name.EndsWith(p.Name, StringComparison.Ordinal))) return true;
        }
        return false;
    }

    private static bool IsRenamable(TypeDefinition type, MethodDefinition method, RenameScope scope)
        => IsRenamable(type, method.IsPrivate, method.IsPublic, method.IsFamily || method.IsFamilyOrAssembly, scope);

    private static bool IsRenamable(TypeDefinition type, bool isPrivate, bool isPublic, bool isProtected, RenameScope scope)
    {
        if (isPrivate) return true;
        return scope switch
        {
            RenameScope.Private => false,
            RenameScope.Public => true,
            // a public member of a type nobody outside can see is as internal as the type
            _ => (isPublic || isProtected) is false || IsVisibleOutside(type) is false,
        };
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

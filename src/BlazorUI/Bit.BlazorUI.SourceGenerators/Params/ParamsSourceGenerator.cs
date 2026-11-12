using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bit.BlazorUI.SourceGenerators.Params;

/// <summary>
/// Writes the snapshot and the merge BitParamsScope takes of every partial params type of the package (one implementing
/// Bit.BlazorUI.IBitComponentParams): property by property, as plain code, instead of read off the type by reflection
/// while a WebAssembly app's first page is being built. What it writes is the reflection of BitParamsScope spelled out -
/// the same properties, taken apart to the same depth into the same snapshots - so the two never disagree on whether a
/// params object has changed, and BitParamsGeneratedContractTests compares them for every params type.
/// </summary>
[Generator]
public class ParamsSourceGenerator : IIncrementalGenerator
{
    private const string ParamsInterfaceName = "Bit.BlazorUI.IBitComponentParams";
    private const string GeneratedInterfaceName = "Bit.BlazorUI.IBitGeneratedParams";
    private const string Snapshot = "global::Bit.BlazorUI.BitParamsSnapshot";

    private static readonly SymbolDisplayFormat _typeFormat = SymbolDisplayFormat.FullyQualifiedFormat;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var types = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax { BaseList: not null } c
                                               && c.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                transform: static (ctx, ct) => ctx.SemanticModel.GetDeclaredSymbol(ctx.Node, ct) as INamedTypeSymbol)
            .Where(static t => t is not null && t.ContainingType is null)
            .Select(static (t, _) => t!.ContainingNamespace.IsGlobalNamespace ? t.MetadataName : t.ContainingNamespace.ToDisplayString() + "." + t.MetadataName)
            .Collect();

        context.RegisterSourceOutput(types.Combine(context.CompilationProvider), static (spc, pair) => Execute(spc, pair.Left, pair.Right));
    }

    private static void Execute(SourceProductionContext context, ImmutableArray<string> names, Compilation compilation)
    {
        var paramsInterface = compilation.GetTypeByMetadataName(ParamsInterfaceName);
        var generatedInterface = compilation.GetTypeByMetadataName(GeneratedInterfaceName);

        if (paramsInterface is null || generatedInterface is null) return;
        if (compilation.IsSymbolAccessibleWithin(generatedInterface, compilation.Assembly) is false) return;

        var dictionaryType = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
        var objectType = compilation.GetSpecialType(SpecialType.System_Object);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        var attributesType = dictionaryType?.Construct(stringType, objectType);

        var writer = new Writer(compilation, attributesType);

        foreach (var name in names.Distinct().OrderBy(n => n, System.StringComparer.Ordinal))
        {
            var type = compilation.Assembly.GetTypeByMetadataName(name);

            if (type is null || type.IsAbstract || type.IsStatic) continue;
            if (type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, paramsInterface)) is false) continue;
            if (HasPublicParameterlessConstructor(type) is false) continue;

            writer.WriteParams(type);
        }

        if (writer.IsEmpty) return;

        context.AddSource("BitParams.Generated.g.cs", SourceText.From(writer.ToString(), Encoding.UTF8));
    }

    internal static bool HasPublicParameterlessConstructor(INamedTypeSymbol type)
    {
        return type.InstanceConstructors.Any(c => c.Parameters.Length == 0 && c.DeclaredAccessibility == Accessibility.Public);
    }



    /// <summary>
    /// One property the snapshot or the merge reads: the public read-write ones, and the public read-only ones holding a
    /// collection - exactly the properties BitParamsScope.GetSnapshotProperties reflects.
    /// </summary>
    private readonly struct SnapshotProperty
    {
        public SnapshotProperty(IPropertySymbol property, bool isCopyable)
        {
            Property = property;
            IsCopyable = isCopyable;
        }

        public IPropertySymbol Property { get; }

        public bool IsCopyable { get; }
    }

    /// <summary>
    /// The properties a value of the type is taken apart by, most derived first, one per name.
    /// </summary>
    /// <returns>The properties, or null where one of them cannot be read in code (obsolete as an error).</returns>
    private static List<SnapshotProperty>? GetSnapshotProperties(INamedTypeSymbol type)
    {
        var result = new List<SnapshotProperty>();
        var seen = new HashSet<string>();

        for (var current = type; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (property.IsStatic || property.IsIndexer) continue;
                if (property.DeclaredAccessibility != Accessibility.Public) continue;
                if (property.ExplicitInterfaceImplementations.Length > 0) continue;
                if (property.GetMethod is not { DeclaredAccessibility: Accessibility.Public }) continue;
                if (seen.Add(property.Name) is false) continue;
                if (IsObsoleteAsError(property)) return null;

                var isCopyable = property.SetMethod is { DeclaredAccessibility: Accessibility.Public };

                if (isCopyable || IsCollectionType(property.Type))
                {
                    result.Add(new SnapshotProperty(property, isCopyable));
                }
            }
        }

        return result;
    }

    private static bool IsObsoleteAsError(IPropertySymbol property)
    {
        return property.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.ObsoleteAttribute"
                                              && a.ConstructorArguments.Length > 1
                                              && a.ConstructorArguments[1].Value is true);
    }

    private static bool IsCollectionType(ITypeSymbol type)
    {
        if (type is IArrayTypeSymbol) return true;

        return type.AllInterfaces.Append(type as INamedTypeSymbol)
                   .OfType<INamedTypeSymbol>()
                   .Any(i => i.ToDisplayString() == "System.Collections.ICollection"
                          || (i.IsGenericType && (i.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.ICollection<T>"
                                               || i.ConstructedFrom.ToDisplayString() == "System.Collections.Generic.IReadOnlyCollection<T>")));
    }

    private static bool IsEnumerable(ITypeSymbol type)
    {
        return type is IArrayTypeSymbol
            || type.SpecialType == SpecialType.System_Collections_IEnumerable
            || type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable);
    }



    private sealed class Writer
    {
        private readonly Compilation _compilation;
        private readonly INamedTypeSymbol? _attributesType;
        private readonly StringBuilder _params = new();
        private readonly StringBuilder _nested = new();
        private readonly Dictionary<string, string> _nestedMethods = new();
        private readonly Queue<INamedTypeSymbol> _pendingNested = new();

        public Writer(Compilation compilation, INamedTypeSymbol? attributesType)
        {
            _compilation = compilation;
            _attributesType = attributesType;
        }

        public bool IsEmpty => _params.Length == 0;

        public void WriteParams(INamedTypeSymbol type)
        {
            var typeName = type.ToDisplayString(_typeFormat);
            var declaration = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var properties = GetSnapshotProperties(type);

            if (properties is null) return;

            var ns = type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString();

            var sb = _params;

            if (ns is not null) sb.Append("namespace ").Append(ns).AppendLine().AppendLine("{");

            sb.Append("    partial class ").Append(declaration).Append(" : global::").Append(GeneratedInterfaceName).AppendLine();
            sb.AppendLine("    {");

            // The snapshot.
            sb.Append("        object global::").Append(GeneratedInterfaceName).AppendLine(".TakeSnapshot()");
            sb.AppendLine("        {");
            sb.Append("            if (GetType() != typeof(").Append(typeName).AppendLine(")) return null;");

            if (properties.Count == 0)
            {
                sb.AppendLine("            return this;");
            }
            else
            {
                sb.Append("            return ").Append(Snapshot).Append(".Object(typeof(").Append(typeName).AppendLine("), new object[]");
                sb.AppendLine("            {");

                foreach (var p in properties)
                {
                    sb.Append("                ").Append(SnapshotOf("this." + p.Property.Name, p.Property.Type, "1")).AppendLine(",");
                }

                sb.AppendLine("            });");
            }

            sb.AppendLine("        }");
            sb.AppendLine();

            // The merge.
            sb.Append("        global::").Append(ParamsInterfaceName).Append(" global::").Append(GeneratedInterfaceName)
              .Append(".MergeOver(global::").Append(ParamsInterfaceName).AppendLine(" basis)");
            sb.AppendLine("        {");
            sb.Append("            if (GetType() != typeof(").Append(typeName).AppendLine(")) return null;");
            sb.AppendLine();
            sb.Append("            var b = basis as ").Append(typeName).AppendLine(";");
            sb.Append("            if (b is not null && b.GetType() != typeof(").Append(typeName).AppendLine(")) b = null;");
            sb.AppendLine();
            sb.Append("            return new ").Append(typeName).AppendLine();
            sb.AppendLine("            {");

            foreach (var p in properties.Where(p => p.IsCopyable))
            {
                var name = p.Property.Name;
                var propertyType = p.Property.Type;

                sb.Append("                ").Append(name).Append(" = ");

                if (IsAttributesTarget(propertyType))
                {
                    sb.Append('(').Append(propertyType.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString(_typeFormat))
                      .Append(')').Append(Snapshot).Append(".MergeAttributes(this.").Append(name).Append(", b?.").Append(name).Append(')');
                }
                else if (propertyType.IsValueType && propertyType.OriginalDefinition.SpecialType != SpecialType.System_Nullable_T)
                {
                    sb.Append("this.").Append(name);
                }
                else
                {
                    sb.Append("this.").Append(name).Append(" ?? b?.").Append(name);
                }

                sb.AppendLine(",");
            }

            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine("    }");

            if (ns is not null) sb.AppendLine("}");

            sb.AppendLine();

            DrainNested();
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            sb.AppendLine("// <auto-generated/>");
            sb.AppendLine("#nullable disable");
            sb.AppendLine("#pragma warning disable");
            sb.AppendLine();
            sb.Append(_params);

            if (_nested.Length > 0)
            {
                sb.AppendLine("namespace Bit.BlazorUI.Generated");
                sb.AppendLine("{");
                sb.Append(_nested);
                sb.AppendLine("}");
            }

            return sb.ToString();
        }

        // Whether a dictionary of attributes can be assigned to the property, which is what makes BitParamsScope merge
        // the dictionary it holds entry by entry.
        private bool IsAttributesTarget(ITypeSymbol propertyType)
        {
            if (_attributesType is null) return false;

            var conversion = _compilation.ClassifyConversion(_attributesType, propertyType);

            return conversion.IsIdentity || (conversion.IsImplicit && conversion.IsReference);
        }

        /// <summary>
        /// The expression of the snapshot of a value of the given declared type, as BitParamsScope.TakeSnapshot takes it.
        /// </summary>
        private string SnapshotOf(string expression, ITypeSymbol type, string depth)
        {
            // A value type, a string, a delegate and a Type are leaves, compared as they are.
            if (type.IsValueType) return expression;
            if (type.SpecialType == SpecialType.System_String) return expression;
            if (type.TypeKind == TypeKind.Delegate) return expression;
            if (type.ToDisplayString() is "System.Type" or "System.Delegate" or "System.MulticastDelegate") return expression;

            if (type is INamedTypeSymbol { TypeKind: TypeKind.Class, IsGenericType: false } named && IsOwnType(named) && IsEnumerable(named) is false
            && _compilation.IsSymbolAccessibleWithin(named, _compilation.Assembly))
            {
                // An abstract type is never what the value is, so what it is decides.
                if (named.IsAbstract is false)
                {
                    return NestedMethod(named) + "(" + expression + ", " + depth + ")";
                }
            }

            return Snapshot + ".Of(" + expression + ", " + depth + ")";
        }

        // A type of the packages, whose public properties the generated code may read: the types of any other library are
        // taken apart by reflection, as they are now.
        private bool IsOwnType(INamedTypeSymbol type)
        {
            var assembly = type.ContainingAssembly?.Name ?? "";

            return SymbolEqualityComparer.Default.Equals(type.ContainingAssembly, _compilation.Assembly)
                || assembly == "Bit.BlazorUI" || assembly.StartsWith("Bit.BlazorUI.", System.StringComparison.Ordinal);
        }

        private string NestedMethod(INamedTypeSymbol type)
        {
            var key = type.ToDisplayString(_typeFormat);

            if (_nestedMethods.TryGetValue(key, out var method)) return method;

            // A class of its own per type, rather than one for all of them: what an app's publish keeps of the scripts and
            // the stylesheets is read off the types its code reaches, and one class reaching every type a params object can
            // hold would have every params type - and so every component - reach all of them.
            method = "global::Bit.BlazorUI.Generated.BitParamsSnapshotOf_" + Sanitize(type.WithNullableAnnotation(NullableAnnotation.NotAnnotated).ToDisplayString()) + ".Take";
            _nestedMethods.Add(key, method);
            _pendingNested.Enqueue(type);

            return method;
        }

        private void DrainNested()
        {
            while (_pendingNested.Count > 0)
            {
                WriteNested(_pendingNested.Dequeue());
            }
        }

        private void WriteNested(INamedTypeSymbol type)
        {
            var typeName = type.ToDisplayString(_typeFormat);
            var method = _nestedMethods[typeName];
            var className = method.Substring("global::Bit.BlazorUI.Generated.".Length, method.Length - "global::Bit.BlazorUI.Generated.".Length - ".Take".Length);
            var sb = _nested;

            sb.Append("    /// <summary>The snapshot of a ").Append(type.Name).AppendLine(" a params object holds, written out like the params object's.</summary>");
            sb.Append("    file static class ").AppendLine(className);
            sb.AppendLine("    {");
            sb.Append("        public static object Take(").Append(typeName).AppendLine(" value, int depth)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (value is null) return null;");
            sb.Append("            if (value.GetType() != typeof(").Append(typeName).Append(")) return ").Append(Snapshot).AppendLine(".Of(value, depth);");
            sb.Append("            if (depth >= ").Append(Snapshot).AppendLine(".MaxDepth) return value;");

            // A type that cannot be copied is compared as it is, the same object or not.
            var properties = HasPublicParameterlessConstructor(type) ? GetSnapshotProperties(type) : [];

            if (properties is null)
            {
                sb.Append("            return ").Append(Snapshot).AppendLine(".Of(value, depth);");
            }
            else if (properties.Count == 0)
            {
                sb.AppendLine("            return value;");
            }
            else
            {
                sb.Append("            return ").Append(Snapshot).Append(".Object(typeof(").Append(typeName).AppendLine("), new object[]");
                sb.AppendLine("            {");

                foreach (var p in properties)
                {
                    sb.Append("                ").Append(SnapshotOf("value." + p.Property.Name, p.Property.Type, "depth + 1")).AppendLine(",");
                }

                sb.AppendLine("            });");
            }

            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static string Sanitize(string name)
        {
            var sb = new StringBuilder(name.Length);

            foreach (var c in name)
            {
                sb.Append(char.IsLetterOrDigit(c) ? c : '_');
            }

            return sb.ToString();
        }
    }
}

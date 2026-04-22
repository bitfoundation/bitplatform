using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bit.BlazorUI.SourceGenerators.AutoInject;

[Generator]
public class AutoInjectSourceGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor NonPartialClassError = new(
        id: "BITGEN001",
        title: "The class needs to be partial",
        messageFormat: "{0} is not partial. The AutoInject attribute needs to be used only in partial classes.",
        category: "Bit.SourceGenerators",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // Provider 1: fields and properties directly annotated with [AutoInject]
        var directMemberProvider = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                AutoInjectHelper.AutoInjectAttributeFullName,
                predicate: static (node, _) => node is FieldDeclarationSyntax or PropertyDeclarationSyntax,
                transform: static (ctx, ct) => TransformDirectMember(ctx, ct))
            .Where(static e => e is not null)
            .Select(static (e, _) => e!.Value);

        // Provider 2: partial classes whose base type uses [AutoInject] but they don't
        var derivedClassProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax cls &&
                    cls.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)),
                transform: static (ctx, ct) => TransformDerivedClass(ctx, ct))
            .Where(static e => e is not null)
            .Select(static (e, _) => e!.Value);

        var combined = directMemberProvider.Collect()
            .Combine(derivedClassProvider.Collect());

        context.RegisterSourceOutput(combined, static (spc, inputs) => Execute(spc, inputs.Left, inputs.Right));
    }

    // ── Data models ──────────────────────────────────────────────────────────

    private readonly record struct DirectEntry(
        string ContainingTypeFullName,
        string ClassName,
        string ClassNameForCode,
        string ClassNamespace,
        AutoInjectClassType ClassType,
        bool IsPartial,
        AutoInjectMember Member,
        // Base class members encoded as "F:name:type|P:name:type" for structural equality
        string EncodedBaseMembers);

    private readonly record struct DerivedEntry(
        string ContainingTypeFullName,
        string ClassName,
        string ClassNameForCode,
        string ClassNamespace,
        AutoInjectClassType ClassType,
        bool IsPartial,
        string EncodedBaseMembers);

    // ── Transforms ───────────────────────────────────────────────────────────

    private static DirectEntry? TransformDirectMember(GeneratorAttributeSyntaxContext ctx, CancellationToken ct)
    {
        var symbol = ctx.TargetSymbol;
        if (symbol is not (IFieldSymbol or IPropertySymbol)) return null;

        var containingType = symbol.ContainingType;
        if (containingType is null) return null;

        // Filter out nested types
        if (!containingType.ContainingSymbol.Equals(containingType.ContainingNamespace, SymbolEqualityComparer.Default))
            return null;

        var attrSymbol = ctx.SemanticModel.Compilation.GetTypeByMetadataName(AutoInjectHelper.AutoInjectAttributeFullName);

        var member = symbol is IFieldSymbol f
            ? new AutoInjectMember(f.Name, f.Type.ToDisplayString(), IsField: true)
            : new AutoInjectMember(((IPropertySymbol)symbol).Name, ((IPropertySymbol)symbol).Type.ToDisplayString(), IsField: false);

        var baseMembers = attrSymbol is null
            ? (IReadOnlyCollection<ISymbol>)new List<ISymbol>()
            : AutoInjectHelper.GetBaseClassEligibleMembers(containingType, attrSymbol);

        var isPartial = IsSymbolPartial(containingType);
        var classType = IsRazorComponent(containingType) ? AutoInjectClassType.RazorComponent : AutoInjectClassType.NormalClass;

        return new DirectEntry(
            ContainingTypeFullName: containingType.ToDisplayString(),
            ClassName: containingType.Name,
            ClassNameForCode: AutoInjectHelper.GenerateClassName(containingType),
            ClassNamespace: containingType.ContainingNamespace.ToDisplayString(),
            ClassType: classType,
            IsPartial: isPartial,
            Member: member,
            EncodedBaseMembers: EncodeMembers(baseMembers));
    }

    private static DerivedEntry? TransformDerivedClass(GeneratorSyntaxContext ctx, CancellationToken ct)
    {
        var classDecl = (ClassDeclarationSyntax)ctx.Node;
        var classSymbol = ctx.SemanticModel.GetDeclaredSymbol(classDecl, ct);
        if (classSymbol is null) return null;

        if (classSymbol.BaseType is null) return null;
        if (classSymbol.BaseType.ToDisplayString() == "System.Object") return null;

        // Filter out nested types
        if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            return null;

        var attrFqn = AutoInjectHelper.AutoInjectAttributeFullName;

        var isBaseTypeUseAutoInject = classSymbol.BaseType
            .GetMembers()
            .Any(m => (m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property) &&
                       m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attrFqn));

        if (!isBaseTypeUseAutoInject) return null;

        var isCurrentClassUseAutoInject = classSymbol
            .GetMembers()
            .Any(m => (m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property) &&
                       m.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == attrFqn));

        // Let the direct-member provider handle classes that have their own [AutoInject] members
        if (isCurrentClassUseAutoInject) return null;

        var attrSymbol = ctx.SemanticModel.Compilation.GetTypeByMetadataName(attrFqn);
        if (attrSymbol is null) return null;

        var baseMembers = AutoInjectHelper.GetBaseClassEligibleMembers(classSymbol, attrSymbol);
        var classType = IsRazorComponent(classSymbol) ? AutoInjectClassType.RazorComponent : AutoInjectClassType.NormalClass;

        return new DerivedEntry(
            ContainingTypeFullName: classSymbol.ToDisplayString(),
            ClassName: classSymbol.Name,
            ClassNameForCode: AutoInjectHelper.GenerateClassName(classSymbol),
            ClassNamespace: classSymbol.ContainingNamespace.ToDisplayString(),
            ClassType: classType,
            IsPartial: true, // predicate already checked for partial keyword
            EncodedBaseMembers: EncodeMembers(baseMembers));
    }

    // ── Code generation ───────────────────────────────────────────────────────

    private static void Execute(
        SourceProductionContext spc,
        ImmutableArray<DirectEntry> directEntries,
        ImmutableArray<DerivedEntry> derivedEntries)
    {
        // Group direct entries by class
        var directGroups = directEntries
            .GroupBy(e => e.ContainingTypeFullName)
            .ToDictionary(g => g.Key, g => g.ToList());

        // Emit one file per class that has direct [AutoInject] members
        foreach (var kvp in directGroups)
        {
            var fullName = kvp.Key;
            var entries = kvp.Value;
            var first = entries[0];

            if (!first.IsPartial)
            {
                spc.ReportDiagnostic(Diagnostic.Create(NonPartialClassError, Location.None, first.ClassName));
                continue;
            }

            var directMembers = entries.Select(e => e.Member).OrderBy(m => m.Name).ToList();
            var baseMembers = DecodeMembers(first.EncodedBaseMembers);

            string? source = first.ClassType == AutoInjectClassType.RazorComponent
                ? AutoInjectRazorComponentHandler.Generate(first.ClassNamespace, first.ClassNameForCode, directMembers)
                : AutoInjectNormalClassHandler.Generate(first.ClassNamespace, first.ClassNameForCode, first.ClassName, directMembers, baseMembers);

            if (!string.IsNullOrEmpty(source))
            {
                var hintName = $"{EscapeForHint(fullName)}_autoInject.g.cs";
                spc.AddSource(hintName, SourceText.From(source!, Encoding.UTF8));
            }
        }

        // Emit one file per derived class (pass-through constructor / empty inject list)
        foreach (var entry in derivedEntries)
        {
            // Skip if already handled by the direct provider
            if (directGroups.ContainsKey(entry.ContainingTypeFullName)) continue;

            if (!entry.IsPartial)
            {
                spc.ReportDiagnostic(Diagnostic.Create(NonPartialClassError, Location.None, entry.ClassName));
                continue;
            }

            var baseMembers = DecodeMembers(entry.EncodedBaseMembers);
            var empty = new List<AutoInjectMember>();

            string? source = entry.ClassType == AutoInjectClassType.RazorComponent
                ? AutoInjectRazorComponentHandler.Generate(entry.ClassNamespace, entry.ClassNameForCode, empty)
                : AutoInjectNormalClassHandler.Generate(entry.ClassNamespace, entry.ClassNameForCode, entry.ClassName, empty, baseMembers);

            if (!string.IsNullOrEmpty(source))
            {
                var hintName = $"{EscapeForHint(entry.ContainingTypeFullName)}_autoInject.g.cs";
                spc.AddSource(hintName, SourceText.From(source!, Encoding.UTF8));
            }
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static bool IsRazorComponent(INamedTypeSymbol @class)
    {
        // Use interface check only — avoids File.Exists() I/O which is forbidden in incremental transforms
        return @class.AllInterfaces.Any(o => o.ToDisplayString() == "Microsoft.AspNetCore.Components.IComponent");
    }

    private static bool IsSymbolPartial(INamedTypeSymbol classSymbol)
    {
        foreach (var syntaxRef in classSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is ClassDeclarationSyntax cls &&
                cls.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return true;
        }
        return false;
    }

    private static string EncodeMembers(IEnumerable<ISymbol> members)
    {
        var sb = new StringBuilder();
        foreach (var m in members)
        {
            if (sb.Length > 0) sb.Append('|');
            if (m is IFieldSymbol f)
                sb.Append('F').Append(':').Append(f.Name).Append(':').Append(f.Type.ToDisplayString());
            else if (m is IPropertySymbol p)
                sb.Append('P').Append(':').Append(p.Name).Append(':').Append(p.Type.ToDisplayString());
        }
        return sb.ToString();
    }

    private static List<AutoInjectMember> DecodeMembers(string encoded)
    {
        var result = new List<AutoInjectMember>();
        if (string.IsNullOrEmpty(encoded)) return result;

        foreach (var part in encoded.Split('|'))
        {
            // format: "F:name:type" or "P:name:type" (type may itself contain ':')
            var colonIdx = part.IndexOf(':');
            if (colonIdx < 0) continue;
            var kind = part[0];
            var rest = part.Substring(colonIdx + 1);
            var secondColon = rest.IndexOf(':');
            if (secondColon < 0) continue;
            var name = rest.Substring(0, secondColon);
            var typeDisplay = rest.Substring(secondColon + 1);
            result.Add(new AutoInjectMember(name, typeDisplay, IsField: kind == 'F'));
        }

        return result;
    }

    private static string EscapeForHint(string fullyQualifiedName)
        => fullyQualifiedName.Replace('<', '[').Replace('>', ']').Replace(' ', '_');
}


using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Bit.SourceGenerators;

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
                // Field [AutoInject] targets VariableDeclaratorSyntax in this API; property targets PropertyDeclarationSyntax.
                predicate: static (node, _) => node is FieldDeclarationSyntax or PropertyDeclarationSyntax or VariableDeclaratorSyntax,
                transform: static (ctx, ct) => TransformDirectMember(ctx, ct))
            .Where(static e => e is not null)
            .Select(static (e, _) => e!.Value);

        // Provider 2: classes whose base type uses [AutoInject] but they don't (including non-partial, to report diagnostic)
        var derivedClassProvider = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => TransformDerivedClass(ctx, ct))
            .Where(static e => e is not null)
            .Select(static (e, _) => e!.Value);

        var combined = directMemberProvider.Collect()
            .Combine(derivedClassProvider.Collect());

        context.RegisterSourceOutput(combined, static (spc, inputs) => Execute(spc, inputs.Left, inputs.Right));
    }

    // ── Data models ──────────────────────────────────────────────────────────

    private readonly record struct LocationInfo(
        string FilePath,
        int SpanStart,
        int SpanLength,
        int StartLine,
        int StartChar,
        int EndLine,
        int EndChar);

    private readonly record struct DirectEntry(
        string ContainingTypeFullName,
        string ClassName,
        string ClassNameForCode,
        string ClassNamespace,
        AutoInjectClassType ClassType,
        bool IsPartial,
        AutoInjectMember Member,
        // Base class members encoded as "F\tname\ttype\tnullable|..." for structural equality
        string EncodedBaseMembers,
        LocationInfo? ClassLocation);

    private readonly record struct DerivedEntry(
        string ContainingTypeFullName,
        string ClassName,
        string ClassNameForCode,
        string ClassNamespace,
        AutoInjectClassType ClassType,
        bool IsPartial,
        string EncodedBaseMembers,
        LocationInfo? ClassLocation);

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

        AutoInjectMember member;
        if (symbol is IFieldSymbol f)
            member = new AutoInjectMember(f.Name, f.Type.ToDisplayString(), IsField: true, IsNullable: f.NullableAnnotation is NullableAnnotation.Annotated);
        else
        {
            var p = (IPropertySymbol)symbol;
            member = new AutoInjectMember(p.Name, p.Type.ToDisplayString(), IsField: false, IsNullable: p.NullableAnnotation is NullableAnnotation.Annotated);
        }

        var baseMembers = attrSymbol is null
            ? (IReadOnlyCollection<ISymbol>)new List<ISymbol>()
            : AutoInjectHelper.GetBaseClassEligibleMembers(containingType, attrSymbol);

        var isPartial = IsSymbolPartial(containingType);
        var classType = IsRazorComponent(containingType) ? AutoInjectClassType.RazorComponent : AutoInjectClassType.NormalClass;

        LocationInfo? classLocation = null;
        foreach (var syntaxRef in containingType.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is ClassDeclarationSyntax classDecl)
            {
                classLocation = GetLocationInfo(classDecl.Identifier);
                break;
            }
        }

        return new DirectEntry(
            ContainingTypeFullName: containingType.ToDisplayString(),
            ClassName: containingType.Name,
            ClassNameForCode: AutoInjectHelper.GenerateClassName(containingType),
            ClassNamespace: containingType.ContainingNamespace.ToDisplayString(),
            ClassType: classType,
            IsPartial: isPartial,
            Member: member,
            EncodedBaseMembers: EncodeMembers(baseMembers),
            ClassLocation: classLocation);
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

        var attrSymbol = ctx.SemanticModel.Compilation.GetTypeByMetadataName(attrFqn);
        if (attrSymbol is null) return null;

        var baseMembers = AutoInjectHelper.GetBaseClassEligibleMembers(classSymbol, attrSymbol);
        if (baseMembers.Count == 0) return null;

        var isCurrentClassUseAutoInject = classSymbol
            .GetMembers()
            .Any(m => (m.Kind == SymbolKind.Field || m.Kind == SymbolKind.Property) &&
                       m.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, attrSymbol)));

        // Let the direct-member provider handle classes that have their own [AutoInject] members
        if (isCurrentClassUseAutoInject) return null;
        var classType = IsRazorComponent(classSymbol) ? AutoInjectClassType.RazorComponent : AutoInjectClassType.NormalClass;

        LocationInfo? classLocation = null;
        foreach (var syntaxRef in classSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxRef.GetSyntax() is ClassDeclarationSyntax classDecl2)
            {
                classLocation = GetLocationInfo(classDecl2.Identifier);
                break;
            }
        }

        return new DerivedEntry(
            ContainingTypeFullName: classSymbol.ToDisplayString(),
            ClassName: classSymbol.Name,
            ClassNameForCode: AutoInjectHelper.GenerateClassName(classSymbol),
            ClassNamespace: classSymbol.ContainingNamespace.ToDisplayString(),
            ClassType: classType,
            IsPartial: IsSymbolPartial(classSymbol),
            EncodedBaseMembers: EncodeMembers(baseMembers),
            ClassLocation: classLocation);
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
                var loc = first.ClassLocation.HasValue ? ToLocation(first.ClassLocation.Value) : Location.None;
                spc.ReportDiagnostic(Diagnostic.Create(NonPartialClassError, loc, first.ClassName));
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
        // Group by ContainingTypeFullName to collapse multi-file partial declarations that
        // produce duplicate DerivedEntry records (same full name, different ClassLocation).
        foreach (var group in derivedEntries.GroupBy(e => e.ContainingTypeFullName))
        {
            var entry = group.First();

            // Skip if already handled by the direct provider
            if (directGroups.ContainsKey(entry.ContainingTypeFullName)) continue;

            if (!entry.IsPartial)
            {
                var loc = entry.ClassLocation.HasValue ? ToLocation(entry.ClassLocation.Value) : Location.None;
                spc.ReportDiagnostic(Diagnostic.Create(NonPartialClassError, loc, entry.ClassName));
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
        // Use interface check only - avoids File.Exists() I/O which is forbidden in incremental transforms
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

    // Format per member: "F\tname\ttype\t0" or "P\tname\ttype\t1", separated by "|"
    // Tab (\t) is used to separate fields; type display strings never contain tabs.
    private static string EncodeMembers(IEnumerable<ISymbol> members)
    {
        var sb = new StringBuilder();
        foreach (var m in members)
        {
            if (sb.Length > 0) sb.Append('|');
            if (m is IFieldSymbol f)
                sb.Append('F').Append('\t').Append(f.Name).Append('\t').Append(f.Type.ToDisplayString()).Append('\t').Append(f.NullableAnnotation is NullableAnnotation.Annotated ? '1' : '0');
            else if (m is IPropertySymbol p)
                sb.Append('P').Append('\t').Append(p.Name).Append('\t').Append(p.Type.ToDisplayString()).Append('\t').Append(p.NullableAnnotation is NullableAnnotation.Annotated ? '1' : '0');
        }
        return sb.ToString();
    }

    private static List<AutoInjectMember> DecodeMembers(string encoded)
    {
        var result = new List<AutoInjectMember>();
        if (string.IsNullOrEmpty(encoded)) return result;

        foreach (var part in encoded.Split('|'))
        {
            // format: "F\tname\ttype\tnullable" (4 tab-separated fields)
            var fields = part.Split('\t');
            if (fields.Length < 4) continue;
            var kind = fields[0][0];
            var name = fields[1];
            var typeDisplay = fields[2];
            var isNullable = fields[3] == "1";
            result.Add(new AutoInjectMember(name, typeDisplay, IsField: kind == 'F', IsNullable: isNullable));
        }

        return result;
    }

    private static string EscapeForHint(string fullyQualifiedName)
        => fullyQualifiedName.Replace('<', '[').Replace('>', ']').Replace(' ', '_');

    private static LocationInfo? GetLocationInfo(SyntaxToken token)
    {
        var location = token.GetLocation();

        if (location.SourceTree is null) return null;

        var lineSpan = location.GetLineSpan();

        return new LocationInfo(
            FilePath: location.SourceTree.FilePath,
            SpanStart: location.SourceSpan.Start,
            SpanLength: location.SourceSpan.Length,
            StartLine: lineSpan.StartLinePosition.Line,
            StartChar: lineSpan.StartLinePosition.Character,
            EndLine: lineSpan.EndLinePosition.Line,
            EndChar: lineSpan.EndLinePosition.Character);
    }

    private static Location ToLocation(LocationInfo info)
        => Location.Create(
            info.FilePath,
            new TextSpan(info.SpanStart, info.SpanLength),
            new LinePositionSpan(
                new LinePosition(info.StartLine, info.StartChar),
                new LinePosition(info.EndLine, info.EndChar)));
}

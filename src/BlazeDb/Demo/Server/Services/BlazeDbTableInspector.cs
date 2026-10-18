using System.Collections.Immutable;
using Bit.BlazeDb.Generators;
using Bit.BlazeDb.Demo.Server.Dtos;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Bit.BlazeDb.Demo.Server.Services;

/// <summary>
/// Runs the real BlazeDb source generator over a piece of table code and reports what it made of it -
/// the diagnostics a build would report, and the table the generated partial describes: its name, its
/// key, the field number and wire type of every stored property, and its indexes.
/// <para>
/// This is the one question about BlazeDb that documentation cannot answer: "what does the generator do
/// with THIS class?". A table type is plain C# with attributes on it, and the rules that decide whether
/// it generates - partial, one key, supported types, indexable members, valid index names - are easy to
/// get slightly wrong in ways that only show up as a build error, or worse, as a field number nobody
/// meant to depend on. So instead of describing the rules, the server applies them: the verdict is the
/// generator's own, from the same assembly the Bit.BlazeDb.Generators package ships.
/// </para>
/// <para>
/// The submitted source is compiled and never run: everything reported is read off the compilation and
/// the generated syntax, so nothing a caller sends can execute on this server.
/// </para>
/// </summary>
public static class BlazeDbTableInspector
{
    /// <summary>A table type is a few dozen lines; anything far past that was pasted by mistake.</summary>
    public const int MaxSourceLength = 50_000;

    private const string TableAttribute = "Bit.BlazeDb.BlazeDbTableAttribute";
    private const string FieldAttribute = "Bit.BlazeDb.BlazeDbFieldAttribute";
    private const string KeyAttribute = "Bit.BlazeDb.BlazeDbKeyAttribute";

    // The usings a model file starts with in practice, so a pasted class without its header still
    // compiles - every sample on the site and in the README is written without them.
    private const string ImplicitUsings = """
        global using System;
        global using System.Collections.Generic;
        global using System.Linq;
        global using System.Text.Json;
        global using System.Threading;
        global using System.Threading.Tasks;
        global using Bit.BlazeDb;
        """;

    private static readonly Lazy<ImmutableArray<MetadataReference>> _references = new(BuildReferences);

    private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.Latest);

    private static readonly SymbolDisplayFormat _typeFormat = SymbolDisplayFormat.MinimallyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private static ImmutableArray<MetadataReference> BuildReferences()
    {
        var references = ImmutableArray.CreateBuilder<MetadataReference>();

        // The framework this server runs on - the BCL, System.Text.Json and the rest a model can use.
        var tpa = (string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty;

        foreach (var path in tpa.Split(Path.PathSeparator))
        {
            if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) references.Add(MetadataReference.CreateFromFile(path));
        }

        // The engine: the attributes the generator reads and the descriptor types it emits.
        references.Add(MetadataReference.CreateFromFile(typeof(BlazeDbDatabase).Assembly.Location));

        return references.ToImmutable();
    }

    public static BlazeDbTableInspectionDto Inspect(string? source, bool includeGeneratedCode)
    {
        source ??= string.Empty;

        var notes = new List<string>();

        if (source.Length > MaxSourceLength)
        {
            notes.Add($"INCOMPLETE ANALYSIS: the source is {source.Length} characters and only the first {MaxSourceLength} were inspected. " +
                      "Send one table type at a time.");
            source = source[..MaxSourceLength];
        }

        var userTree = CSharpSyntaxTree.ParseText(source, _parseOptions, path: "Model.cs");
        var usingsTree = CSharpSyntaxTree.ParseText(ImplicitUsings, _parseOptions, path: "GlobalUsings.cs");

        var compilation = CSharpCompilation.Create(
            "BlazeDbTableInspection",
            [userTree, usingsTree],
            _references.Value,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BlazeDbTableGenerator()).WithUpdatedParseOptions(_parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _);

        var run = driver.GetRunResult();
        var generated = run.Results.SelectMany(result => result.GeneratedSources).ToArray();

        var generatorDiagnostics = run.Results.SelectMany(result => result.Diagnostics).ToArray();

        // Only what the compiler says is broken: a pasted fragment trips over nullable and unused-variable
        // warnings that say nothing about BlazeDb. Generator diagnostics are kept whatever their severity.
        var compilerErrors = updated.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();

        var diagnostics = generatorDiagnostics.Select(diagnostic => ToDto(diagnostic, "generator", userTree))
            .Concat(compilerErrors.Select(diagnostic => ToDto(diagnostic, "compiler", userTree)))
            .ToArray();

        var tables = FindTables(updated, userTree, generated);

        if (tables.Length == 0)
        {
            notes.Add("No type marked [BlazeDbTable] was found, so the generator had nothing to do. A table is a top-level " +
                      "`partial class` (or record, or struct) marked [BlazeDbTable(\"name\")] with exactly one [BlazeDbKey] property.");
        }

        foreach (var table in tables.Where(table => table.Generated is false))
        {
            notes.Add($"'{table.TypeName}' was rejected: the generator emits nothing for a table type with a BLZ error, so its " +
                      "`Table` descriptor and `Indexes` class are missing too. Fix the BLZ diagnostic first; the compiler errors " +
                      "that mention them go away with it. GetBlazeDbDiagnostics(code) has the cause and the fix for each id.");
        }

        foreach (var table in tables.Where(table => table.Generated && table.Fields.Length > 0 && table.Fields.All(field => field.Explicit is false)))
        {
            notes.Add($"Every field number of '{table.TypeName}' follows declaration order. That is fine while nothing is stored, but " +
                      "adding, removing or reordering a property then renumbers the ones after it, and rows already on disk decode " +
                      "into the wrong properties without an error: pin each number with [BlazeDbField(n)] before the type persists " +
                      "real data - see GetBlazeDbGuideSection(heading: \"Schema evolution\").");
        }

        if (generatorDiagnostics.Length == 0 && compilerErrors.Length > 0)
        {
            notes.Add("The generator accepted every table type, but the C# around them does not compile - see the compiler diagnostics.");
        }

        string? generatedCode = null;

        if (includeGeneratedCode)
        {
            generatedCode = string.Join("\n\n", generated.Select(source => $"// {source.HintName}\n{source.SourceText}"));
        }

        return new BlazeDbTableInspectionDto
        {
            IsValid = diagnostics.Any(diagnostic => diagnostic.Severity == nameof(DiagnosticSeverity.Error)) is false,
            Tables = tables,
            Diagnostics = diagnostics,
            Notes = [.. notes],
            GeneratedCode = string.IsNullOrEmpty(generatedCode) ? null : generatedCode
        };
    }

    private static BlazeDbInspectedTableDto[] FindTables(Compilation compilation, SyntaxTree userTree, GeneratedSourceResult[] generated)
    {
        var model = compilation.GetSemanticModel(userTree);
        var tables = new List<BlazeDbInspectedTableDto>();

        foreach (var declaration in userTree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            if (model.GetDeclaredSymbol(declaration) is not INamedTypeSymbol type) continue;

            var isTable = type.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == TableAttribute);
            if (isTable is false || tables.Any(table => table.TypeName == type.Name)) continue;

            // The generator names its output after the type: "<namespace>.<TypeName>.BlazeDb.g.cs".
            var hintName = $"{(type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString() + ".")}{type.Name}.BlazeDb.g.cs";
            var output = generated.FirstOrDefault(source => source.HintName == hintName);

            tables.Add(output.SyntaxTree is null
                ? new BlazeDbInspectedTableDto { TypeName = type.Name, Generated = false, Fields = [], Indexes = [], GeneratedMembers = [] }
                : Describe(type, output.SyntaxTree.GetRoot()));
        }

        return [.. tables];
    }

    /// <summary>The table the generated partial of <paramref name="type"/> describes, read off its syntax.</summary>
    private static BlazeDbInspectedTableDto Describe(INamedTypeSymbol type, SyntaxNode generated)
    {
        var tableField = generated.DescendantNodes().OfType<VariableDeclaratorSyntax>().FirstOrDefault(v => v.Identifier.ValueText == "Table");
        var tableArguments = (tableField?.Initializer?.Value as BaseObjectCreationExpressionSyntax)?.ArgumentList?.Arguments;

        var tableName = tableArguments is { Count: > 0 } ? (tableArguments.Value[0].Expression as LiteralExpressionSyntax)?.Token.ValueText : null;
        var keyName = tableArguments?.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "keyMember")?.Expression is LiteralExpressionSyntax keyLiteral
            ? keyLiteral.Token.ValueText
            : null;

        var keyProperty = keyName is null ? null : type.GetMembers(keyName).OfType<IPropertySymbol>().FirstOrDefault();

        var fields = ReadFields(type, generated);
        var indexes = ReadIndexes(type, generated);

        string[] members =
        [
            $"{type.Name}.Table : BlazeDbTableDescriptor<{keyProperty?.Type.ToDisplayString(_typeFormat) ?? "?"}, {type.Name}>",
            .. indexes.Select(index => $"{index.Reference} : BlazeDb{index.Kind}IndexDefinition<{type.Name}, {index.KeyType}>")
        ];

        return new BlazeDbInspectedTableDto
        {
            TypeName = type.Name,
            Generated = true,
            TableName = tableName,
            Key = keyName,
            KeyType = keyProperty?.Type.ToDisplayString(_typeFormat),
            Fields = fields,
            Indexes = indexes,
            GeneratedMembers = members
        };
    }

    /// <summary>
    /// Every stored property with its field number and wire type, from the generated row reader: its
    /// object initializer assigns each property from the local named after its field number
    /// (<c>Title = __v2</c>), and its switch decodes each field number only under the wire type the
    /// property is written with (<c>case 2 when __wire == ...BlazeDbWireType.LengthDelimited:</c>).
    /// </summary>
    private static BlazeDbInspectedFieldDto[] ReadFields(INamedTypeSymbol type, SyntaxNode generated)
    {
        var reader = generated.DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.ValueText == "__BlazeReadRow");
        if (reader is null) return [];

        var wireTypes = new Dictionary<int, string>();

        foreach (var label in reader.DescendantNodes().OfType<CasePatternSwitchLabelSyntax>())
        {
            if (label.Pattern is not ConstantPatternSyntax { Expression: LiteralExpressionSyntax { Token.Value: int number } }) continue;
            if (label.WhenClause?.Condition is not BinaryExpressionSyntax { Right: MemberAccessExpressionSyntax wire }) continue;

            wireTypes[number] = wire.Name.Identifier.ValueText;
        }

        var initializer = reader.DescendantNodes().OfType<InitializerExpressionSyntax>().LastOrDefault(i => i.IsKind(SyntaxKind.ObjectInitializerExpression));
        if (initializer is null) return [];

        var fields = new List<BlazeDbInspectedFieldDto>();

        foreach (var assignment in initializer.Expressions.OfType<AssignmentExpressionSyntax>())
        {
            if (assignment.Left is not IdentifierNameSyntax target) continue;

            var local = assignment.Right.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Select(identifier => identifier.Identifier.ValueText)
                .FirstOrDefault(name => name.StartsWith("__v", StringComparison.Ordinal));

            if (local is null || int.TryParse(local.AsSpan(3), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) is false) continue;

            var property = type.GetMembers(target.Identifier.ValueText).OfType<IPropertySymbol>().FirstOrDefault();
            var attributes = property?.GetAttributes() ?? [];

            fields.Add(new BlazeDbInspectedFieldDto
            {
                Property = target.Identifier.ValueText,
                Type = property?.Type.ToDisplayString(_typeFormat) ?? "?",
                FieldNumber = number,
                Explicit = attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() == FieldAttribute),
                WireType = wireTypes.GetValueOrDefault(number, "?"),
                IsKey = attributes.Any(attribute => attribute.AttributeClass?.ToDisplayString() == KeyAttribute)
            });
        }

        return [.. fields.OrderBy(field => field.FieldNumber)];
    }

    /// <summary>
    /// The indexes of the generated <c>Indexes</c> class. Each member is declared as
    /// <c>BlazeDbHashIndexDefinition&lt;TRow, TKey&gt;</c> or <c>BlazeDbOrderedIndexDefinition&lt;TRow, TKey&gt;</c> and
    /// constructed as <c>new("Name", selector, comparer, unique, new string[] { members })</c>.
    /// </summary>
    private static BlazeDbInspectedIndexDto[] ReadIndexes(INamedTypeSymbol type, SyntaxNode generated)
    {
        var indexesClass = generated.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault(c => c.Identifier.ValueText == "Indexes");
        if (indexesClass is null) return [];

        var indexes = new List<BlazeDbInspectedIndexDto>();

        foreach (var field in indexesClass.Members.OfType<FieldDeclarationSyntax>())
        {
            if (field.Declaration.Type is not QualifiedNameSyntax { Right: GenericNameSyntax definition } || definition.TypeArgumentList.Arguments.Count != 2) continue;

            var kind = definition.Identifier.ValueText switch
            {
                "BlazeDbHashIndexDefinition" => "Hash",
                "BlazeDbOrderedIndexDefinition" => "Ordered",
                _ => null
            };
            if (kind is null) continue;

            foreach (var variable in field.Declaration.Variables)
            {
                var arguments = (variable.Initializer?.Value as BaseObjectCreationExpressionSyntax)?.ArgumentList?.Arguments;
                if (arguments is not { Count: >= 5 } args) continue;

                var unique = args[3].Expression.IsKind(SyntaxKind.TrueLiteralExpression);
                var members = args[4].Expression.DescendantNodes().OfType<LiteralExpressionSyntax>()
                    .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
                    .Select(literal => literal.Token.ValueText)
                    .ToArray();

                indexes.Add(new BlazeDbInspectedIndexDto
                {
                    Name = variable.Identifier.ValueText,
                    Kind = kind,
                    Unique = unique,
                    Members = members,
                    KeyType = KeyTypeOf(type, members, definition.TypeArgumentList.Arguments[1]),
                    Reference = $"{type.Name}.Indexes.{variable.Identifier.ValueText}"
                });
            }
        }

        return [.. indexes];
    }

    /// <summary>
    /// The C# spelling of an index key: the member's own type, or the tuple of a compound index's member
    /// types - spelled from the model's symbols, since the generated code writes every type fully qualified.
    /// </summary>
    private static string KeyTypeOf(INamedTypeSymbol type, string[] members, TypeSyntax generatedKeyType)
    {
        var types = members.Select(member => type.GetMembers(member).OfType<IPropertySymbol>().FirstOrDefault()?.Type.ToDisplayString(_typeFormat)).ToArray();

        if (types.Any(t => t is null)) return generatedKeyType.ToString();

        return members.Length == 1 ? types[0]! : $"({string.Join(", ", members.Select((member, i) => $"{types[i]} {member}"))})";
    }

    private static BlazeDbInspectionDiagnosticDto ToDto(Diagnostic diagnostic, string source, SyntaxTree userTree)
    {
        // The generator reports through locations it rebuilt from a file path and a span (an incremental
        // generator may not hold on to syntax trees), so they need not carry a SourceTree - the path is
        // what says whether a diagnostic points into the submitted source.
        var span = diagnostic.Location.GetLineSpan();

        return new BlazeDbInspectionDiagnosticDto
        {
            Id = diagnostic.Id,
            Severity = diagnostic.Severity.ToString(),
            Message = diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture),
            Source = source,
            Line = span.IsValid && span.Path == userTree.FilePath ? span.StartLinePosition.Line + 1 : null
        };
    }
}

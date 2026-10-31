using System.Collections.Immutable;
using Bit.BlazeDb.Generators;
using Bit.BlazeDb.Demo.Server.Dtos;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ModelContextProtocol;

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
/// generator's own, from the same assembly the Bit.BlazeDb package ships as its analyzer.
/// </para>
/// <para>
/// The submitted source is compiled and never run: everything reported is read off the compilation and
/// the generated syntax, so nothing a caller sends can execute on this server. Nor can what it costs to
/// compile reach the server: the compile happens in a worker process with a capped heap, killed when it
/// runs out of time - see <see cref="BlazeDbInspectionWorker"/>.
/// </para>
/// </summary>
public static class BlazeDbTableInspector
{
    /// <summary>A table type is a few dozen lines; anything far past that was pasted by mistake.</summary>
    public const int MaxSourceLength = 50_000;

    public const int MaxNameLength = 256;

    public const int MaxAttributeTokens = 2_000;

    /// <summary>Far more members than a table type has; every one is work for the generator and the compiler.</summary>
    public const int MaxMembers = 500;

    private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(20);

    private static readonly SemaphoreSlim _gate = new(2, 2);

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

    public static async Task<BlazeDbTableInspectionDto> InspectAsync(string? source, bool includeGeneratedCode, CancellationToken cancellationToken)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_timeout);

        try
        {
            // The gate bounds the workers busy at once, and so the memory they can take between them.
            await _gate.WaitAsync(timeout.Token);
            try
            {
                return await BlazeDbInspectionWorker.InspectAsync(source, includeGeneratedCode, timeout.Token);
            }
            finally
            {
                _gate.Release();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested is false)
        {
            throw new McpException($"The source was not inspected: it did not finish within {_timeout.TotalSeconds} seconds. " +
                                   "Send one table type at a time, or try again if the server is busy.");
        }
    }

    /// <summary>The inspection itself, run inside a <see cref="BlazeDbInspectionWorker"/>.</summary>
    internal static BlazeDbTableInspectionDto Inspect(string? source, bool includeGeneratedCode, CancellationToken cancellationToken)
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

        if (ExceededLimit(compilation.GetSemanticModel(userTree), cancellationToken) is { } limit)
        {
            // An McpException is the one error the SDK hands a client with its message; any other arrives
            // as a bare "an error occurred", which tells the caller nothing about what to change.
            throw new McpException($"The source was not inspected: {limit}. A table type needs nothing near that - send the type on its own.");
        }

        GeneratorDriver driver = CSharpGeneratorDriver.Create(new BlazeDbTableGenerator()).WithUpdatedParseOptions(_parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var updated, out _, cancellationToken);

        var run = driver.GetRunResult();
        var generated = run.Results.SelectMany(result => result.GeneratedSources).ToArray();

        var generatorDiagnostics = run.Results.SelectMany(result => result.Diagnostics).ToArray();

        // Only what the compiler says is broken: a pasted fragment trips over nullable and unused-variable
        // warnings that say nothing about BlazeDb. Generator diagnostics are kept whatever their severity.
        var compilerErrors = updated.GetDiagnostics(cancellationToken)
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

    /// <summary>
    /// The limit the source goes past, or null. The limits bound what the generator emits and what a report
    /// carries; what compiling the source costs is bounded by the worker it runs in.
    /// </summary>
    private static string? ExceededLimit(SemanticModel model, CancellationToken cancellationToken)
    {
        var root = model.SyntaxTree.GetRoot(cancellationToken);

        // One walk over the syntax for every limit it can tell on its own, picking up what the checks that
        // need the compiler look at.
        var members = 0;
        var attributeTokens = 0;
        var attributeEnd = -1;
        var types = new List<BaseTypeDeclarationSyntax>();
        var arguments = new List<AttributeArgumentSyntax>();

        foreach (var item in root.DescendantNodesAndTokens())
        {
            if (item.AsNode() is { } node)
            {
                if (node is MemberDeclarationSyntax && ++members > MaxMembers) return $"it declares more than {MaxMembers} members";

                switch (node)
                {
                    case BaseTypeDeclarationSyntax type:
                        types.Add(type);
                        break;
                    case AttributeListSyntax:
                        attributeEnd = Math.Max(attributeEnd, node.Span.End);
                        break;
                    case AttributeArgumentSyntax argument:
                        arguments.Add(argument);
                        break;
                }

                continue;
            }

            var token = item.AsToken();

            if (token.IsKind(SyntaxKind.IdentifierToken) && token.ValueText.Length > MaxNameLength) return $"a name is longer than {MaxNameLength} characters";

            // A node comes before its tokens, so every token short of where the last attribute list ends is that list's.
            if (token.SpanStart < attributeEnd && ++attributeTokens > MaxAttributeTokens) return $"its attributes run to more than {MaxAttributeTokens} tokens";
        }

        foreach (var type in types)
        {
            if (model.GetDeclaredSymbol(type, cancellationToken)?.ToDisplayString().Length > MaxNameLength) return $"a type's full name is longer than {MaxNameLength} characters";
        }

        foreach (var argument in arguments)
        {
            if (model.GetConstantValue(argument.Expression, cancellationToken) is { HasValue: true, Value: string { Length: > MaxNameLength } }) return $"an attribute argument is longer than {MaxNameLength} characters";
        }

        return null;
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
                : Describe(type, compilation, output.SyntaxTree));
        }

        return [.. tables];
    }

    /// <summary>The table the generated partial of <paramref name="type"/> describes, read off its syntax.</summary>
    private static BlazeDbInspectedTableDto Describe(INamedTypeSymbol type, Compilation compilation, SyntaxTree generatedTree)
    {
        // The descriptor and the index definitions are fields of a nested class the generator emits; the type's
        // Table and its Indexes class only expose them as properties. Following Table to the field it returns finds
        // that class without naming anything the generator keeps to itself.
        var descriptor = DescriptorField(type, compilation, generatedTree);
        var holder = descriptor?.FirstAncestorOrSelf<ClassDeclarationSyntax>();
        var tableArguments = (descriptor?.Initializer?.Value as BaseObjectCreationExpressionSyntax)?.ArgumentList?.Arguments;

        var tableName = tableArguments is { Count: > 0 } ? (tableArguments.Value[0].Expression as LiteralExpressionSyntax)?.Token.ValueText : null;
        var keyName = tableArguments?.FirstOrDefault(argument => argument.NameColon?.Name.Identifier.ValueText == "keyMember")?.Expression is LiteralExpressionSyntax keyLiteral
            ? keyLiteral.Token.ValueText
            : null;

        var keyProperty = keyName is null ? null : type.GetMembers(keyName).OfType<IPropertySymbol>().FirstOrDefault();

        var fields = ReadFields(type, compilation, generatedTree);
        var indexes = ReadIndexes(type, holder);

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
    /// The field the generated <c>Table</c> property of <paramref name="type"/> returns, as the compiler binds it -
    /// not the <c>Table</c> index an <c>Indexes</c> class may declare.
    /// </summary>
    private static VariableDeclaratorSyntax? DescriptorField(INamedTypeSymbol type, Compilation compilation, SyntaxTree generatedTree)
    {
        if (compilation.ContainsSyntaxTree(generatedTree) is false) return null;

        var table = generatedTree.GetRoot().DescendantNodes().OfType<PropertyDeclarationSyntax>()
            .FirstOrDefault(property => property.Identifier.ValueText == "Table" && property.Parent is TypeDeclarationSyntax parent && parent.Identifier.ValueText == type.Name);
        if (table?.ExpressionBody is null) return null;

        var field = compilation.GetSemanticModel(generatedTree).GetSymbolInfo(table.ExpressionBody.Expression).Symbol as IFieldSymbol;

        return field?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() as VariableDeclaratorSyntax;
    }

    /// <summary>
    /// Every stored property with its field number and wire type, from the generated row reader: its
    /// object initializer assigns each property from the local named after its field number
    /// (<c>Title = __v2</c>), and its switch decodes each field number only under the wire type the
    /// property is written with (<c>case 2 when __wire == ...BlazeDbWireType.LengthDelimited:</c>).
    /// </summary>
    private static BlazeDbInspectedFieldDto[] ReadFields(INamedTypeSymbol type, Compilation compilation, SyntaxTree generatedTree)
    {
        var reader = generatedTree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().FirstOrDefault(m => m.Identifier.ValueText == "__BlazeReadRow");
        if (reader is null) return [];

        var wireTypes = new Dictionary<int, string>();

        foreach (var label in reader.DescendantNodes().OfType<CasePatternSwitchLabelSyntax>())
        {
            if (label.Pattern is not ConstantPatternSyntax { Expression: LiteralExpressionSyntax { Token.Value: int number } }) continue;
            if (label.WhenClause?.Condition is not BinaryExpressionSyntax { Right: MemberAccessExpressionSyntax wire }) continue;

            wireTypes[number] = wire.Name.Identifier.ValueText;
        }

        // The reader ends in `return new T(...) { ... }`: properties set in the initializer, and - for a
        // type built through a constructor, a positional record say - the rest passed as its arguments.
        if (reader.Body?.Statements.OfType<ReturnStatementSyntax>().LastOrDefault()?.Expression is not ObjectCreationExpressionSyntax creation) return [];

        var assigned = new List<(string Property, ExpressionSyntax Value)>();

        if (creation.ArgumentList is { Arguments.Count: > 0 } arguments)
        {
            // The constructor the reader actually calls, as the compiler binds it - not one picked by its
            // parameter count, which a record's copy constructor can share with a one-parameter primary one.
            var count = arguments.Arguments.Count;
            var constructor = (compilation.ContainsSyntaxTree(generatedTree)
                    ? compilation.GetSemanticModel(generatedTree).GetSymbolInfo(creation).Symbol as IMethodSymbol
                    : null)
                ?? type.InstanceConstructors.FirstOrDefault(c => c.Parameters.Length == count &&
                    !(count == 1 && SymbolEqualityComparer.Default.Equals(c.Parameters[0].Type, type)));
            for (var i = 0; constructor is not null && i < arguments.Arguments.Count && i < constructor.Parameters.Length; i++)
            {
                var parameter = constructor.Parameters[i];
                var property = type.GetMembers().OfType<IPropertySymbol>()
                    .FirstOrDefault(p => string.Equals(p.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
                if (property is not null) assigned.Add((property.Name, arguments.Arguments[i].Expression));
            }
        }

        foreach (var assignment in creation.Initializer?.Expressions.OfType<AssignmentExpressionSyntax>() ?? [])
        {
            // A required property the constructor takes is set again in the initializer, from the same local;
            // it is one field, reported once.
            if (assignment.Left is IdentifierNameSyntax target && assigned.All(existing => existing.Property != target.Identifier.ValueText))
            {
                assigned.Add((target.Identifier.ValueText, assignment.Right));
            }
        }

        var fields = new List<BlazeDbInspectedFieldDto>();

        foreach (var (name, value) in assigned)
        {
            var local = value.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>()
                .Select(identifier => identifier.Identifier.ValueText)
                .FirstOrDefault(name => name.StartsWith("__v", StringComparison.Ordinal));

            if (local is null || int.TryParse(local.AsSpan(3), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var number) is false) continue;

            var property = type.GetMembers(name).OfType<IPropertySymbol>().FirstOrDefault();
            var attributes = property?.GetAttributes() ?? [];

            fields.Add(new BlazeDbInspectedFieldDto
            {
                Property = name,
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
    /// The indexes the generated <c>Indexes</c> class exposes, read off the holder's fields behind it. Each is declared as
    /// <c>BlazeDbHashIndexDefinition&lt;TRow, TKey&gt;</c> or <c>BlazeDbOrderedIndexDefinition&lt;TRow, TKey&gt;</c> and
    /// constructed as <c>new("Name", selector, comparer, unique, new string[] { members })</c>; the field's own name
    /// carries a prefix, so the index's name is the first argument.
    /// </summary>
    private static BlazeDbInspectedIndexDto[] ReadIndexes(INamedTypeSymbol type, ClassDeclarationSyntax? holder)
    {
        if (holder is null) return [];

        var indexes = new List<BlazeDbInspectedIndexDto>();

        foreach (var field in holder.Members.OfType<FieldDeclarationSyntax>())
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
                if (arguments is not { Count: >= 5 } args || args[0].Expression is not LiteralExpressionSyntax name) continue;

                var unique = args[3].Expression.IsKind(SyntaxKind.TrueLiteralExpression);
                var members = args[4].Expression.DescendantNodes().OfType<LiteralExpressionSyntax>()
                    .Where(literal => literal.IsKind(SyntaxKind.StringLiteralExpression))
                    .Select(literal => literal.Token.ValueText)
                    .ToArray();

                indexes.Add(new BlazeDbInspectedIndexDto
                {
                    Name = name.Token.ValueText,
                    Kind = kind,
                    Unique = unique,
                    Members = members,
                    KeyType = KeyTypeOf(type, members, definition.TypeArgumentList.Arguments[1]),
                    // An index named after a keyword is a member the generator escapes: Indexes.@event.
                    Reference = $"{type.Name}.Indexes.{(SyntaxFacts.GetKeywordKind(name.Token.ValueText) is SyntaxKind.None ? "" : "@")}{name.Token.ValueText}"
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

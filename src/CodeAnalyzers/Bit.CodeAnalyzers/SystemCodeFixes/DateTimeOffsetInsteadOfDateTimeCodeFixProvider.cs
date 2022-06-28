using System.Linq;
using System.Threading;
using System.Composition;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Bit.CodeAnalyzers.SystemAnalyzers;

namespace Bit.CodeAnalyzers.SystemCodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DateTimeOffsetInsteadOfDateTimeCodeFixProvider)), Shared]
public class DateTimeOffsetInsteadOfDateTimeCodeFixProvider : CodeFixProvider
{
    private const string Title = "Use DateTimeOffset";

    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DateTimeOffsetInsteadOfDateTimeAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        Diagnostic diagnostic = context.Diagnostics.First();

        SyntaxNode node = root.FindNode(context.Span);

        if (node is IdentifierNameSyntax == false)
            return;

        context.RegisterCodeFix(CodeAction.Create(title: Title, createChangedDocument: c => ReplaceDateTimeWithDateTimeOffsetAsync(context.Document, node, c), equivalenceKey: Title), diagnostic);
    }

    private async Task<Document> ReplaceDateTimeWithDateTimeOffsetAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        IdentifierNameSyntax convertedNode = (IdentifierNameSyntax)node;

        IdentifierNameSyntax? newNode = convertedNode?.WithIdentifier(SyntaxFactory.ParseToken("DateTimeOffset")).WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());

        SyntaxNode newRoot = root.ReplaceNode(node, newNode);

        Document newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument;
    }
}

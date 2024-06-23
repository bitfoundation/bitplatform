﻿using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Bit.CodeAnalyzers.SystemAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();

        var node = root.FindNode(context.Span);

        if (node is IdentifierNameSyntax is false)
            return;

        context.RegisterCodeFix(CodeAction.Create(title: Title, createChangedDocument: c => ReplaceDateTimeWithDateTimeOffsetAsync(context.Document, node, c), equivalenceKey: Title), diagnostic);
    }

    private async Task<Document> ReplaceDateTimeWithDateTimeOffsetAsync(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var convertedNode = (IdentifierNameSyntax)node;

        var newNode = convertedNode?.WithIdentifier(SyntaxFactory.ParseToken("DateTimeOffset")).WithLeadingTrivia(node.GetLeadingTrivia()).WithTrailingTrivia(node.GetTrailingTrivia());

        var newRoot = root.ReplaceNode(node, newNode);

        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument;
    }
}

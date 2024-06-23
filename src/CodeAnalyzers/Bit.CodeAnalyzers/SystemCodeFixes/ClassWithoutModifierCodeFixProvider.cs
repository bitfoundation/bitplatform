using System.Collections.Immutable;
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

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ClassWithoutModifierAnalyzer)), Shared]
public class ClassWithoutModifierCodeFixProvider : CodeFixProvider
{
    private const string Title = "Add public modifier";

    public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ClassWithoutModifierAnalyzer.DiagnosticId);

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        var diagnostic = context.Diagnostics.First();

        var node = root.FindNode(context.Span);

        if (node is ClassDeclarationSyntax is false)
            return;

        context.RegisterCodeFix(CodeAction.Create(title: Title, createChangedDocument: c => AddPublicModifierToClass(context.Document, node, c), equivalenceKey: Title), diagnostic);
    }

    private async Task<Document> AddPublicModifierToClass(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        var convertedNode = (ClassDeclarationSyntax)node;

        var newNode = convertedNode?.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        var newRoot = root.ReplaceNode(node, newNode);

        var newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument;
    }
}

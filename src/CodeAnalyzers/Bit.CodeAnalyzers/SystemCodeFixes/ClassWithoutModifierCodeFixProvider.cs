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
        SyntaxNode root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        Diagnostic diagnostic = context.Diagnostics.First();

        SyntaxNode node = root.FindNode(context.Span);

        if (node is ClassDeclarationSyntax == false)
            return;

        context.RegisterCodeFix(CodeAction.Create(title: Title, createChangedDocument: c => AddPublicModifierToClass(context.Document, node, c), equivalenceKey: Title), diagnostic);
    }

    private async Task<Document> AddPublicModifierToClass(Document document, SyntaxNode node, CancellationToken cancellationToken)
    {
        SyntaxNode root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

        ClassDeclarationSyntax convertedNode = (ClassDeclarationSyntax)node;

        ClassDeclarationSyntax? newNode = convertedNode?.AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));

        SyntaxNode newRoot = root.ReplaceNode(node, newNode);

        Document newDocument = document.WithSyntaxRoot(newRoot);

        return newDocument;
    }
}

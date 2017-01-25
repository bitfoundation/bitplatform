using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace BitCodeAnalyzer.SystemAnalyzers
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ClassWithoutModifierAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(ClassWithoutModifierAnalyzer);

        public const string Title = "Add modifier to your class.";
        public const string Message = "Your class must have at least one modifier. (Public, Private, etc)";
        public const string Description = Message;
        private const string Category = "System";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ClassDeclaration);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is ClassDeclarationSyntax))
                return;

            ClassDeclarationSyntax classDec = (ClassDeclarationSyntax)context.Node;

            if (!classDec.Modifiers.Any())
            {
                Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);
                context.ReportDiagnostic(diagn);
            }
        }
    }
}

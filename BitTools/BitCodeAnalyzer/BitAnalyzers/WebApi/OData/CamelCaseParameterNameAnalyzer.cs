using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace BitCodeAnalyzer.BitAnalyzers.WebApi.OData
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CamelCaseParameterNameAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(CamelCaseParameterNameAnalyzer);

        public const string Title = "Use camel case naming for your parameter name";
        public const string Message = "Web API OData Action | Function parameter names must be camel case";
        public const string Description = Message;
        private const string Category = "Bit";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.Attribute);
        }

        private void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode root = context.Node;

            if (!(root is AttributeSyntax))
                return;

            AttributeSyntax attributeSyntax = (AttributeSyntax)context.Node;

            ISymbol attributeUsage = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;

            if (attributeUsage?.ContainingType?.ToString() == "Foundation.Api.ApiControllers.ParameterAttribute")
            {
                LiteralExpressionSyntax expression = (LiteralExpressionSyntax)(attributeSyntax.DescendantNodes().OfType<AttributeArgumentSyntax>().First()).Expression;

                string parameterNameValue = expression.Token.ValueText;

                if (!char.IsLower(parameterNameValue[0]))
                {
                    Diagnostic diagn = Diagnostic.Create(Rule, root.GetLocation(), Message);

                    context.ReportDiagnostic(diagn);
                }
            }
        }
    }
}

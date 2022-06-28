using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace Bit.Tooling.CodeAnalyzer.BitAnalyzers.Dto
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ODataOperationsCanNotReturnADtoWithoutSingleResultAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = nameof(ODataOperationsCanNotReturnADtoWithoutSingleResultAnalyzer);

        public const string Title = "OData operations (Create|Update|PartialUpdate|Action|Function|Get) may not return a dto without SingleResult.";
        public const string Message = "Return SingleResult<{0}> or Task<SingleResult<{0}>> instead of {0}.";
        public const string Description = "OData operations (Create|Update|PartialUpdate|Action|Function|Get) may not return a dto without SingleResult.";
        private const string Category = "Bit";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, Message, Category, DiagnosticSeverity.Error, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeSyntaxAsync, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeSyntaxAsync(SyntaxNodeAnalysisContext context)
        {
            SyntaxNode? methodDec = context.Node as MethodDeclarationSyntax;

            if (methodDec == null)
                return;

            IMethodSymbol method = (IMethodSymbol)context.SemanticModel.GetDeclaredSymbol(methodDec);

            if (method.ContainingType.IsDtoController() && method.IsOperation(out AttributeData _))
            {
                ITypeSymbol returnType = method.ReturnType.GetUnderlyingTypeSymbol();

                if (returnType.IsDto() && !returnType.IsCollectionType() && !returnType.IsQueryableType())
                {
                    Diagnostic diagn = Diagnostic.Create(Rule, methodDec.GetLocation(), returnType.Name);
                    context.ReportDiagnostic(diagn);
                }
            }
        }
    }
}

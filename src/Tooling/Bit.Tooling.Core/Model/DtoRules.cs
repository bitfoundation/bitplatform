using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace BitTools.Core.Model
{
    public class DtoRules
    {
        public virtual ClassDeclarationSyntax ClassDeclaration { get; set; } = default!;

        public virtual SyntaxTree ClassSyntaxTree { get; set; } = default!;

        public virtual CompilationUnitSyntax ClassRootNode { get; set; } = default!;

        public virtual string Name { get; set; } = default!;

        public virtual INamedTypeSymbol DtoRulesSymbol { get; set; } = default!;

        public virtual Document DtoRulesDocument { get; set; } = default!;

        public virtual ITypeSymbol DtoSymbol { get; set; } = default!;

        public virtual SemanticModel SemanticModel { get; set; } = default!;
    }
}

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Foundation.CodeGenerators.Model
{
    public class DtoRules
    {
        public virtual ClassDeclarationSyntax ClassDeclaration { get; set; }

        public virtual SyntaxTree ClassSyntaxTree { get; set; }

        public virtual CompilationUnitSyntax ClassRootNode { get; set; }

        public virtual string Name { get; set; }

        public virtual INamedTypeSymbol DtoRulesSymbol { get; set; }

        public virtual Document DtoRulesDocument { get; set; }

        public virtual ITypeSymbol DtoSymbol { get; set; }

        public SemanticModel SemanticModel { get; internal set; }
    }
}

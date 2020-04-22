using Bit.Tooling.Core.Contracts;
using Bit.Tooling.Core.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Bit.Tooling.CodeGenerator.Implementations
{
    public class DefaultDtoRulesValidator : IDtoRulesValidator
    {
        public virtual void Validate(DtoRules dtoRules, IList<Project>? allSourceProjects = null)
        {
            if (dtoRules == null)
                throw new ArgumentNullException(nameof(dtoRules));

            if (dtoRules.DtoRulesSymbol
                .Constructors.Length > 1)
            {
                throw new NotSupportedException("Multiple constructor is not supported");
            }

            if (dtoRules.ClassDeclaration.Members.OfType<MethodDeclarationSyntax>()
                .GroupBy(m => m.Identifier.ValueText)
                .Any(mOverloads => mOverloads.Count() > 1))
            {
                throw new NotSupportedException("Method overloading is not supported");
            }

            DtoRulesNotSupportedCSharpFeaturesVisitor dtoRulesNotSupportedCSharpFeaturesVisitor = new DtoRulesNotSupportedCSharpFeaturesVisitor(dtoRules.SemanticModel);
            dtoRulesNotSupportedCSharpFeaturesVisitor.Visit(dtoRules.ClassRootNode);
        }
    }

    public class DtoRulesNotSupportedCSharpFeaturesVisitor : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;

        public DtoRulesNotSupportedCSharpFeaturesVisitor(SemanticModel semanticModel)
            : base(SyntaxWalkerDepth.StructuredTrivia)
        {
            if (semanticModel == null)
                throw new ArgumentNullException(nameof(semanticModel));

            _semanticModel = semanticModel;
        }

        public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
        {
            throw new NotSupportedException("Null-Conditional operator expression is not supported");
        }

        public override void VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node)
        {
            throw new NotSupportedException("Anonymous method expression is not supported");
        }

        public override void VisitCastExpression(CastExpressionSyntax node)
        {
            throw new NotSupportedException("Cast expression is not supported");
        }

        public override void VisitCheckedStatement(CheckedStatementSyntax node)
        {
            throw new NotSupportedException("Checked statement is not supported");
        }

        public override void VisitDestructorDeclaration(DestructorDeclarationSyntax node)
        {
            throw new NotSupportedException("Destructor declaration is not supported");
        }

        public override void VisitEventFieldDeclaration(EventFieldDeclarationSyntax node)
        {
            throw new NotSupportedException("Event field declaration is not supported");
        }

        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (node.Modifiers.Any(m => m.ValueText == "volatile"))
                throw new NotSupportedException("Volatile keyword is not supported");

            base.VisitFieldDeclaration(node);
        }

        public override void VisitGotoStatement(GotoStatementSyntax node)
        {
            throw new NotSupportedException("Go to is not supported");
        }

        public override void VisitIndexerDeclaration(IndexerDeclarationSyntax node)
        {
            throw new NotSupportedException("Indexer declaration is not supported");
        }

        public override void VisitIndexerMemberCref(IndexerMemberCrefSyntax node)
        {
            throw new NotSupportedException("Indexer Member is not supported");
        }

        public override void VisitLabeledStatement(LabeledStatementSyntax node)
        {
            throw new NotSupportedException("Labeled statements is not supported");
        }

        public override void VisitLockStatement(LockStatementSyntax node)
        {
            throw new NotSupportedException("Lock statement is not supported");
        }

        public override void VisitQueryExpression(QueryExpressionSyntax node)
        {
            throw new NotSupportedException("Query syntax linq is not supported");
        }

        public override void VisitRegionDirectiveTrivia(RegionDirectiveTriviaSyntax node)
        {
            throw new NotSupportedException("Region directive is not supported");
        }

        public override void VisitSizeOfExpression(SizeOfExpressionSyntax node)
        {
            throw new NotSupportedException("SizeOf is not supported");
        }

        public override void VisitTypeOfExpression(TypeOfExpressionSyntax node)
        {
            throw new NotSupportedException("Type Of expression is not supported");
        }

        public override void VisitUnsafeStatement(UnsafeStatementSyntax node)
        {
            throw new NotSupportedException("Unsafe statement is not supported");
        }

        public override void VisitUsingStatement(UsingStatementSyntax node)
        {
            throw new NotSupportedException("Using statement is not supported");
        }

        public override void VisitYieldStatement(YieldStatementSyntax node)
        {
            throw new NotSupportedException("Yield statement is not supported");
        }

        public override void VisitAwaitExpression(AwaitExpressionSyntax node)
        {
            throw new NotSupportedException("Await expression is not supported");
        }

        public override void VisitParameter(ParameterSyntax node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            if (node.Modifiers.Any(m => m.ValueText == "ref"))
                throw new NotSupportedException("Ref parameter is not supported");

            if (node.Modifiers.Any(m => m.ValueText == "out"))
                throw new NotSupportedException("Out parameter is not supported");

            base.VisitParameter(node);
        }

        public override void VisitCatchDeclaration(CatchDeclarationSyntax node)
        {
            if (node == null)
                throw new ArgumentNullException(nameof(node));

            ISymbol exceptionSymbol = _semanticModel.GetSymbolInfo(node.Type).Symbol;

            if (exceptionSymbol.ToString() != "System.Exception")
                throw new NotSupportedException("Catch block for inherited exception types is not supported");

            base.VisitCatchDeclaration(node);
        }
    }
}

using Microsoft.OData.UriParser;

namespace Bit.OData.Visitors
{
    public class EmptyQueryNodeVisitor : QueryNodeVisitor<object>
    {
        public override object Visit(SingleValuePropertyAccessNode nodeIn)
        {
            return null;
        }

        public override object Visit(AllNode nodeIn)
        {
            return null;
        }

        public override object Visit(AnyNode nodeIn)
        {
            return null;
        }

        public override object Visit(CollectionFunctionCallNode nodeIn)
        {
            return null;
        }

        public override object Visit(CollectionNavigationNode nodeIn)
        {
            return null;
        }

        public override object Visit(CollectionOpenPropertyAccessNode nodeIn)
        {
            return null;
        }

        public override object Visit(CollectionPropertyAccessNode nodeIn)
        {
            return null;
        }

        public override object Visit(ConstantNode nodeIn)
        {
            return null;
        }

        public override object Visit(ConvertNode nodeIn)
        {
            return null;
        }

        public override object Visit(CountNode nodeIn)
        {
            return null;
        }

        public override object Visit(NamedFunctionParameterNode nodeIn)
        {
            return null;
        }

        public override object Visit(ParameterAliasNode nodeIn)
        {
            return null;
        }

        public override object Visit(SearchTermNode nodeIn)
        {
            return null;
        }

        public override object Visit(SingleNavigationNode nodeIn)
        {
            return null;
        }

        public override object Visit(SingleValueFunctionCallNode nodeIn)
        {
            return null;
        }

        public override object Visit(SingleValueOpenPropertyAccessNode nodeIn)
        {
            return null;
        }

        public override object Visit(UnaryOperatorNode nodeIn)
        {
            return null;
        }

        public override object Visit(BinaryOperatorNode nodeIn)
        {
            return null;
        }
    }
}

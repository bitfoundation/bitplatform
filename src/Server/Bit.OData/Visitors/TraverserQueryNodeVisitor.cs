using Microsoft.OData.UriParser;

namespace Bit.OData.Visitors
{
    public class TraverserQueryNodeVisitor : EmptyQueryNodeVisitor
    {
        public override object Visit(BinaryOperatorNode nodeIn)
        {
            nodeIn.Left.Accept(this);
            nodeIn.Right.Accept(this);

            return base.Visit(nodeIn);
        }
    }
}

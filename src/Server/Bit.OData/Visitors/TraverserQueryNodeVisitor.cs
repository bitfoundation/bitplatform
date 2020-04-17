using Microsoft.OData.UriParser;
using System;

namespace Bit.OData.Visitors
{
    public class TraverserQueryNodeVisitor : EmptyQueryNodeVisitor
    {
        public override object? Visit(BinaryOperatorNode nodeIn)
        {
            if (nodeIn == null)
                throw new ArgumentNullException(nameof(nodeIn));

            nodeIn.Left.Accept(this);
            nodeIn.Right.Accept(this);

            return base.Visit(nodeIn);
        }
    }
}

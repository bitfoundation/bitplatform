using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace BitTools.Core.Model
{
    public class EnumType
    {
        public virtual ITypeSymbol EnumTypeSymbol { get; set; }

        public virtual IList<EnumMember> Members { get; set; }
    }

    public class EnumMember
    {
        public virtual string Name { get; set; }

        public virtual ISymbol Symbol { get; set; }

        public virtual int Index { get; set; }
    
        public virtual int Value { get; set; }
    }
}

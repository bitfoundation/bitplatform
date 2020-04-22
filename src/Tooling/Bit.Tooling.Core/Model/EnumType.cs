using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Bit.Tooling.Core.Model
{
    public class EnumType
    {
        public virtual ITypeSymbol EnumTypeSymbol { get; set; } = default!;

        public virtual IList<EnumMember> Members { get; set; } = default!;
    }

    public class EnumMember
    {
        public virtual string Name { get; set; } = default!;

        public virtual ISymbol Symbol { get; set; } = default!;

        public virtual int Index { get; set; }

        public virtual int Value { get; set; }
    }
}

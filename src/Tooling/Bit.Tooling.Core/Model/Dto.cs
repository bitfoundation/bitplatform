using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Bit.Tooling.Core.Model
{
    public class Dto
    {
        public virtual INamedTypeSymbol? DtoSymbol { get; set; }

        public virtual INamedTypeSymbol? BaseDtoSymbol { get; set; }

        public virtual IList<IPropertySymbol> Properties { get; set; } = default!;

        public override string ToString()
        {
            if (DtoSymbol != null)
                return DtoSymbol.Name;
            return base.ToString();
        }
    }
}

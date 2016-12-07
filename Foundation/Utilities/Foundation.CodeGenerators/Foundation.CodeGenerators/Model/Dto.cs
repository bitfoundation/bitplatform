using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Model
{
    public class Dto
    {
        public virtual INamedTypeSymbol DtoSymbol { get; set; }

        public virtual INamedTypeSymbol BaseDtoSymbol { get; set; }

        public virtual IList<IPropertySymbol> Properties { get; set; }

        public override string ToString()
        {
            if (DtoSymbol != null)
                return DtoSymbol.Name;
            return base.ToString();
        }
    }
}

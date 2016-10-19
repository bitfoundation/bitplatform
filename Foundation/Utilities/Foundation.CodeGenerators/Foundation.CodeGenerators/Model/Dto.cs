using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace Foundation.CodeGenerators.Model
{
    public class Dto
    {
        public virtual INamedTypeSymbol DtoSymbol { get; set; }

        public virtual IList<IPropertySymbol> Properties { get; set; }
    }
}

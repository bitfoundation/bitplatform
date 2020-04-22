using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace Bit.Tooling.Core.Model
{
    public class DtoController
    {
        public virtual string Name { get; set; } = default!;

        public virtual IList<ODataOperation> Operations { get; set; } = default!;

        public virtual INamedTypeSymbol ControllerSymbol { get; set; } = default!;

        public virtual ITypeSymbol ModelSymbol { get; set; } = default!;

        public override string ToString()
        {
            if (ControllerSymbol != null)
                return ControllerSymbol.Name;
            else
                return base.ToString();
        }
    }
}

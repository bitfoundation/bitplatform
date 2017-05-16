using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BitTools.Core.Model
{
    public class ODataOperationParameter
    {
        public string Name { get; set; }

        public ITypeSymbol Type { get; set; }
    }

    public enum ODataOperationKind
    {
        Function, Action
    }

    public class ODataOperation
    {
        public virtual ODataOperationKind Kind { get; set; }

        public virtual IMethodSymbol Method { get; set; }

        public virtual ITypeSymbol ReturnType { get; set; }

        public virtual ICollection<ODataOperationParameter> Parameters { get; set; } = new Collection<ODataOperationParameter>();
    }
}

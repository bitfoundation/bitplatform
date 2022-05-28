using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Bit.Tooling.Core.Model
{
    public class ODataOperationParameter
    {
        public string Name { get; set; } = default!;

        public ITypeSymbol Type { get; set; } = default!;
    }

    public enum ODataOperationKind
    {
        Function, Action
    }

    public class ODataOperation
    {
        public virtual ODataOperationKind Kind { get; set; }

        public virtual IMethodSymbol Method { get; set; } = default!;

        public virtual ITypeSymbol ReturnType { get; set; } = default!;

        public virtual DtoController Controller { get; set; } = default!;

        public virtual ICollection<ODataOperationParameter> Parameters { get; set; } = new Collection<ODataOperationParameter>();

        public virtual string ParametersUri
        {
            get
            {
                if (Kind == ODataOperationKind.Action)
                    return string.Empty;
                return string.Join(",", Parameters.Select(p => $"{p.Name}={{({p.Name} == null ? \"null\" : $\"{GetParameterValue(p)}\")}}"));
            }
        }

        string GetParameterValue(ODataOperationParameter parameter)
        {
            if (parameter.Type.IsEnum())
                return $"{parameter.Type.GetCSharpTypeName()}'{{{parameter.Name}}}'";
            if (parameter.Type.Name == nameof(String))
                return $"'{{{parameter.Name}}}'";
            if (parameter.Type.Name == nameof(Boolean))
                return $"{{{parameter.Name}.ToString().ToLowerInvariant()}}";
            return $"{{{parameter.Name}}}";
        }

        public ITypeSymbol GetODataClientFunctionReturnType()
        {
            var returnType = ReturnType.IsCollectionType() ? ReturnType.GetElementType() : ReturnType.GetUnderlyingTypeSymbol();
            if (returnType.IsComplexType() || returnType.IsDto())
                return returnType;
            else
                return Controller.ModelSymbol;
        }
    }
}

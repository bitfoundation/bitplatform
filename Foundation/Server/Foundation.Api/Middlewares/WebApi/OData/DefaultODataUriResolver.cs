using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;

namespace Foundation.Api.Middlewares.WebApi.OData
{
    /// <summary>
    /// See https://github.com/OData/WebApi/blob/368278e40296e87a420f1bbffef038c3c4a852d2/OData/src/System.Web.OData/OData/UnqualifiedCallAndEnumPrefixFreeResolver.cs
    /// </summary>
    public class DefaultODataUriResolver : UnqualifiedODataUriResolver
    {
        private readonly StringAsEnumResolver _stringAsEnumResolver = new StringAsEnumResolver
        {
            EnableCaseInsensitive = true
        };

        public override bool EnableCaseInsensitive
        {
            get { return true; }
            set { }
        }

        public override void PromoteBinaryOperandTypes(BinaryOperatorKind binaryOperatorKind,
            ref SingleValueNode leftNode, ref SingleValueNode rightNode, out IEdmTypeReference typeReference)
        {
            _stringAsEnumResolver.PromoteBinaryOperandTypes(binaryOperatorKind, ref leftNode, ref rightNode, out typeReference);
        }

        public override IEnumerable<KeyValuePair<string, object>> ResolveKeys(IEdmEntityType type,
            IDictionary<string, string> namedValues, Func<IEdmTypeReference, string, object> convertFunc)
        {
            return _stringAsEnumResolver.ResolveKeys(type, namedValues, convertFunc);
        }

        public override IEnumerable<KeyValuePair<string, object>> ResolveKeys(IEdmEntityType type,
            IList<string> positionalValues, Func<IEdmTypeReference, string, object> convertFunc)
        {
            return _stringAsEnumResolver.ResolveKeys(type, positionalValues, convertFunc);
        }

        public override IDictionary<IEdmOperationParameter, SingleValueNode> ResolveOperationParameters(
            IEdmOperation operation, IDictionary<string, SingleValueNode> input)
        {
            return _stringAsEnumResolver.ResolveOperationParameters(operation, input);
        }
    }
}

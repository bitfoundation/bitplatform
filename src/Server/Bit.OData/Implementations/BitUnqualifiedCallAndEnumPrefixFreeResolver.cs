using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using System;
using System.Collections.Generic;

namespace Bit.OData.Implementations
{
    /// <summary>
    /// The OData uri resolver wrapper for Enum prefix free and unqualified function call.
    /// https://github.com/OData/WebApi/blob/master/src/Microsoft.AspNet.OData.Shared/UnqualifiedCallAndEnumPrefixFreeResolver.cs
    /// </summary>
    public class BitUnqualifiedCallAndEnumPrefixFreeResolver : ODataUriResolver
    {
        private readonly StringAsEnumResolver _stringAsEnum = new StringAsEnumResolver { EnableCaseInsensitive = true };
        private readonly UnqualifiedODataUriResolver _unqualified = new UnqualifiedODataUriResolver { EnableCaseInsensitive = true };

        /// <inheritdoc/>
        public override bool EnableCaseInsensitive
        {
            get => true;
            set
            {

            }
        }

        /// <inheritdoc/>
        public override IEnumerable<IEdmOperation> ResolveUnboundOperations(IEdmModel model, string identifier)
        {
            try
            {
                return _unqualified.ResolveUnboundOperations(model, identifier);
            }
            catch
            {
                return _stringAsEnum.ResolveUnboundOperations(model, identifier);
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<IEdmOperation> ResolveBoundOperations(IEdmModel model, string identifier,
            IEdmType bindingType)
        {
            try
            {
                return _unqualified.ResolveBoundOperations(model, identifier, bindingType);
            }
            catch
            {
                return _stringAsEnum.ResolveBoundOperations(model, identifier, bindingType);
            }
        }

        /// <inheritdoc/>
        public override void PromoteBinaryOperandTypes(BinaryOperatorKind binaryOperatorKind,
            ref SingleValueNode leftNode, ref SingleValueNode rightNode, out IEdmTypeReference typeReference)
        {
            try
            {
                _stringAsEnum.PromoteBinaryOperandTypes(binaryOperatorKind, ref leftNode, ref rightNode, out typeReference);
            }
            catch
            {
                _unqualified.PromoteBinaryOperandTypes(binaryOperatorKind, ref leftNode, ref rightNode, out typeReference);
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<KeyValuePair<string, object>> ResolveKeys(IEdmEntityType type,
            IDictionary<string, string> namedValues, Func<IEdmTypeReference, string, object> convertFunc)
        {
            try
            {
                return _stringAsEnum.ResolveKeys(type, namedValues, convertFunc);
            }
            catch
            {
                return _unqualified.ResolveKeys(type, namedValues, convertFunc);
            }
        }

        /// <inheritdoc/>
        public override IEnumerable<KeyValuePair<string, object>> ResolveKeys(IEdmEntityType type,
            IList<string> positionalValues, Func<IEdmTypeReference, string, object> convertFunc)
        {
            try
            {
                return _stringAsEnum.ResolveKeys(type, positionalValues, convertFunc);
            }
            catch
            {
                return _unqualified.ResolveKeys(type, positionalValues, convertFunc);
            }
        }

        /// <inheritdoc/>
        public override IDictionary<IEdmOperationParameter, SingleValueNode> ResolveOperationParameters(
            IEdmOperation operation, IDictionary<string, SingleValueNode> input)
        {
            try
            {
                return _stringAsEnum.ResolveOperationParameters(operation, input);
            }
            catch
            {
                return _unqualified.ResolveOperationParameters(operation, input);
            }
        }
    }
}

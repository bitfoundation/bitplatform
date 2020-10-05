using Simple.OData.Client;
using Simple.OData.Client.V4.Adapter;
using System;

namespace Bit.OData.Implementations
{
    public class DefaultODataAdapterFactory : ODataAdapterFactory
    {
        public override Func<ISession, IODataAdapter> CreateAdapterLoader(string metadataString, ITypeCache typeCache)
        {
            return (session) => new ODataAdapter(session, new ODataModelAdapter(ODataProtocolVersion.V4, metadataString));
        }
    }
}

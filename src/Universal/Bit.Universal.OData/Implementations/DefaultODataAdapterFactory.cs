using Microsoft.OData.Edm;
using Simple.OData.Client;
using Simple.OData.Client.V4.Adapter;
using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Bit.OData.Implementations
{
    public class DefaultODataAdapterFactory : ODataAdapterFactory
    {
        public override Func<ISession, IODataAdapter> CreateAdapterLoader(string metadataString, ITypeCache typeCache)
        {
            return (session) => new DefaultODataAdapter(session, new ODataModelAdapter(ODataProtocolVersion.V4, metadataString));
        }
    }

    public class DefaultODataAdapter : ODataAdapter
    {
        private readonly ISession _session;

        public DefaultODataAdapter(ISession session, IODataModelAdapter modelAdapter)
            : base(session, modelAdapter)
        {
            _session = session;
        }

        public override IResponseReader GetResponseReader()
        {
            return new DefaultResponseReader(_session, Model);
        }
    }

    public class DefaultResponseReader : ResponseReader
    {
        public DefaultResponseReader(ISession session, IEdmModel model)
            : base(session, model)
        {
        }

        private static readonly FieldInfo _reasonPhraseField = typeof(WebRequestException).GetField("<ReasonPhrase>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance) ?? throw new InvalidOperationException("ReasonPharse backing field could not be found");

        public async override Task<ODataResponse> GetResponseAsync(HttpResponseMessage responseMessage)
        {
            var result = await base.GetResponseAsync(responseMessage).ConfigureAwait(false);

            var exp = result.Batch?.Select(b => b.Exception).FirstOrDefault();

            if (exp is WebRequestException webExp)
            {
                var reasonPhrase = result.Batch?.SelectMany(r => r.Headers).Where(h => h.Key == "Reason-Phrase").FirstOrDefault();
                if (reasonPhrase != null)
                {
                    _reasonPhraseField.SetValue(webExp, reasonPhrase.Value.Value);
                }
            }

            if (exp != null)
                throw exp;

            return result;
        }

        protected override void ConvertEntry(ResponseNode entryNode, object entry)
        {
            base.ConvertEntry(entryNode, entry);
        }
    }
}

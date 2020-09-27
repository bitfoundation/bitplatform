using Microsoft.OData;
using Microsoft.OData.Edm;
using Simple.OData.Client;
using Simple.OData.Client.V4.Adapter;
using System;
using System.Collections.Generic;
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

        public override IBatchWriter GetBatchWriter(IDictionary<object, IDictionary<string, object>> batchEntries)
        {
            return new DefaultBatchWriter(_session, batchEntries);
        }

        public override IResponseReader GetResponseReader()
        {
            return new DefaultResponseReader(_session, Model);
        }
    }

    delegate Task<ODataResponse> ReadResponseDelegate(ODataBatchReader odataReader);
    delegate ODataMessageReaderSettings ToReaderSettingsDelegate(ISession session);

    public class DefaultResponseReader : ResponseReader
    {
        private static readonly Type _odataResponseMessageType;
        private static readonly ToReaderSettingsDelegate ToReaderSettings;

        static DefaultResponseReader()
        {
            _odataResponseMessageType = Assembly.Load("Simple.OData.Client.V4.Adapter").GetType("Simple.OData.Client.V4.Adapter.ODataResponseMessage", throwOnError: true);
            ToReaderSettings = (ToReaderSettingsDelegate)Delegate.CreateDelegate(typeof(ToReaderSettingsDelegate), Assembly.Load("Simple.OData.Client.V4.Adapter").GetType("Simple.OData.Client.V4.Adapter.ODataExtensions", throwOnError: true).GetMethods().Single(m => m.Name == "ToReaderSettings" && m.GetParameters().Single().ParameterType == typeof(ISession)));
        }

        private readonly IEdmModel _model;
        private readonly ISession _session;
        private readonly ReadResponseDelegate ReadResponse;

        public DefaultResponseReader(ISession session, IEdmModel model)
            : base(session, model)
        {
            _session = session;
            _model = model;
            ReadResponse = (ReadResponseDelegate)Delegate.CreateDelegate(typeof(ReadResponseDelegate), target: this, "ReadResponse");
        }

        public async override Task<ODataResponse> GetResponseAsync(HttpResponseMessage responseMessage)
        {
            IODataResponseMessageAsync oDataResponseMessage = (IODataResponseMessageAsync)Activator.CreateInstance(_odataResponseMessageType, args: new object[] { responseMessage });

            if (responseMessage?.RequestMessage?.RequestUri?.ToString()?.Contains("$batch") == true)
            {
                ODataMessageReaderSettings messageReaderSettings = ToReaderSettings(_session);
                using ODataMessageReader messageReader = new ODataMessageReader(oDataResponseMessage, messageReaderSettings, _model);
                ODataBatchReader oDataBatchReader = messageReader.CreateODataBatchReader();
                return await ReadResponse(oDataBatchReader).ConfigureAwait(false);
            }

            return await GetResponseAsync(oDataResponseMessage).ConfigureAwait(false);
        }
    }

    public class DefaultBatchWriter : BatchWriter
    {
        private static readonly Type _odataRequestMessageType;
        private static readonly FieldInfo _requestMessageField;
        private static readonly FieldInfo _messageWriterField;
        private static readonly FieldInfo _batchWriterField;

        static DefaultBatchWriter()
        {
            _odataRequestMessageType = Assembly.Load("Simple.OData.Client.V4.Adapter").GetType("Simple.OData.Client.V4.Adapter.ODataRequestMessage", throwOnError: true);
            _requestMessageField = typeof(BatchWriter).GetField("_requestMessage", BindingFlags.NonPublic | BindingFlags.Instance);
            _messageWriterField = typeof(BatchWriter).GetField("_messageWriter", BindingFlags.NonPublic | BindingFlags.Instance);
            _batchWriterField = typeof(BatchWriter).GetField("_batchWriter", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public DefaultBatchWriter(ISession session, IDictionary<object, IDictionary<string, object>> batchEntries)
            : base(session, batchEntries)
        {

        }

        public override async Task StartBatchAsync()
        {
            // https://docs.microsoft.com/en-us/odata/webapi/disable-instance-annotation
            // https://docs.microsoft.com/en-us/odata/odatalib/json-batch
            // https://docs.microsoft.com/en-us/odata/odatalib/json-batch#sample-batch-request-in-json-format

            var _requestMessage = (IODataRequestMessageAsync)Activator.CreateInstance(_odataRequestMessageType);
            _requestMessage.Url = _session.Settings.BaseUri;
            _requestMessage.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            _requestMessage.SetHeader("Accept", "application/json;odata.metadata=minimal");
            _requestMessage.SetHeader("Prefer", "odata.include-annotations=-*");
            var _messageWriter = new ODataMessageWriter(_requestMessage);
            var _batchWriter = await _messageWriter.CreateODataBatchWriterAsync().ConfigureAwait(false);
            await _batchWriter.WriteStartBatchAsync().ConfigureAwait(false);
            HasOperations = true;
            _requestMessageField.SetValue(this, _requestMessage);
            _messageWriterField.SetValue(this, _messageWriter);
            _batchWriterField.SetValue(this, _batchWriter);
        }

        protected async override Task<object> CreateOperationMessageAsync(Uri uri, string method, string collection, string contentId, bool resultRequired)
        {
            ODataBatchOperationRequestMessage message = (ODataBatchOperationRequestMessage)await base.CreateOperationMessageAsync(uri, method, collection, contentId, resultRequired).ConfigureAwait(false);

            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.SetHeader("Accept", "application/json;odata.metadata=minimal");
            message.SetHeader("Prefer", "odata.include-annotations=-*");

            return message;
        }
    }
}

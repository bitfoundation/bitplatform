using Bit.Core.Implementations;
using Bit.Http.Contracts;
using Bit.Model.Contracts;
using Bit.Model.Implementations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Http.Implementations
{
    public class ODataHttpClient<TDto>
    {
        public virtual string ODataRoute { get; }
        public virtual string ControllerName { get; }
        public virtual HttpClient HttpClient { get; }

        public ODataHttpClient(HttpClient httpClient, string controllerName, string odataRoute)
        {
            ODataRoute = odataRoute;
            ControllerName = controllerName;
            HttpClient = httpClient;
        }

        protected virtual async Task<TDto> SendAsync(object[] keys, object dto, HttpMethod method, ODataContext? oDataContext, CancellationToken cancellationToken)
        {
            if (dto is null)
                throw new ArgumentNullException(nameof(dto));

            string qs = oDataContext?.Query is not null ? $"?{oDataContext.Query}" : string.Empty;

            using StringContent content = new StringContent(DefaultJsonContentFormatter.Current.Serialize(dto), Encoding.UTF8, DefaultJsonContentFormatter.Current.ContentType);

            using HttpRequestMessage request = new HttpRequestMessage(method, $"odata/{ODataRoute}/{ControllerName}({string.Join(",", keys)}){qs}");

            request.Content = content;

            using Stream responseStream = await (await HttpClient.SendAsync(request, cancellationToken).ConfigureAwait(false))
                .EnsureSuccessStatusCode()
                .Content.
#if DotNetStandard2_0 || UWP || Android || iOS  || DotNetStandard2_1
            ReadAsStreamAsync()
#else
            ReadAsStreamAsync(cancellationToken)
#endif
                .ConfigureAwait(false);

            return await DeserializeAsync<TDto>(responseStream, null, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<TDto> Create(TDto dto, ODataContext? oDataContext = default, CancellationToken cancellationToken = default)
        {
            return await SendAsync(Array.Empty<object>(), dto, HttpMethod.Post, oDataContext, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<TDto> Update(TDto dto, ODataContext? oDataContext = default, CancellationToken cancellationToken = default)
        {
            return await SendAsync(DtoMetadataWorkspace.Current.GetKeys(dto), dto, HttpMethod.Put, oDataContext, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<TDto> PartialUpdate(object[] keys, object dto, ODataContext? oDataContext = default, CancellationToken cancellationToken = default)
        {
            return await SendAsync(keys, dto, new HttpMethod("Patch"), oDataContext, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<TDto> PartialUpdate(TDto dto, ODataContext? oDataContext = default, CancellationToken cancellationToken = default)
        {
            return await SendAsync(DtoMetadataWorkspace.Current.GetKeys(dto), dto, new HttpMethod("Patch"), oDataContext, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task Delete(object[] keys, ODataContext? oDataContext = default, CancellationToken cancellationToken = default)
        {
            if (keys is null)
                throw new ArgumentNullException(nameof(keys));

            string qs = oDataContext?.Query is not null ? $"?{oDataContext.Query}" : string.Empty;

            (await HttpClient.DeleteAsync($"odata/{ODataRoute}/{ControllerName}({string.Join(",", keys)}){qs}", cancellationToken).ConfigureAwait(false))
                .EnsureSuccessStatusCode();
        }

        public virtual async Task<TDto> Get(object[] keys, ODataContext? oDataContext = default, CancellationToken cancellationToken = default)
        {
            if (keys is null)
                throw new ArgumentNullException(nameof(keys));

            string qs = oDataContext?.Query is not null ? $"?{oDataContext.Query}" : string.Empty;

            using Stream responseStream = await (await HttpClient.GetAsync($"odata/{ODataRoute}/{ControllerName}({string.Join(",", keys)}){qs}", cancellationToken).ConfigureAwait(false))
                .EnsureSuccessStatusCode()
                .Content.
#if DotNetStandard2_0 || UWP || Android || iOS || DotNetStandard2_1
            ReadAsStreamAsync()
#else
            ReadAsStreamAsync(cancellationToken)
#endif
                .ConfigureAwait(false);

            return await DeserializeAsync<TDto>(responseStream, null, cancellationToken).ConfigureAwait(false);
        }

        public virtual async Task<List<TDto>> Get(ODataContext? oDataContext = default, CancellationToken cancellationToken = default)
        {
            string qs = oDataContext?.Query is not null ? $"?{oDataContext.Query}" : string.Empty;

            using Stream responseStream = await (await HttpClient.GetAsync($"odata/{ODataRoute}/{ControllerName}(){qs}", cancellationToken).ConfigureAwait(false))
                .EnsureSuccessStatusCode()
                .Content
                .ReadAsStreamAsync().ConfigureAwait(false);

            List<TDto> odataResponse = await DeserializeAsync<List<TDto>>(responseStream, oDataContext, cancellationToken).ConfigureAwait(false);

            return odataResponse;
        }

        public virtual async Task<T> DeserializeAsync<T>(Stream responseStream, ODataContext? context, CancellationToken cancellationToken)
        {
            JsonSerializer serializer = JsonSerializer.CreateDefault(DefaultJsonContentFormatter.DeserializeSettings());
            using StreamReader streamReader = new StreamReader(responseStream);
            using JsonTextReader jsonReader = new JsonTextReader(streamReader);
            JToken json = await JToken.LoadAsync(jsonReader, cancellationToken).ConfigureAwait(false);

            if (json["value"] == null)
                return json.ToObject<T>(serializer);

            ODataResponse<T> oDataResponse = json.ToObject<ODataResponse<T>>(serializer);

            if (context != null)
                context.TotalCount = oDataResponse.TotalCount;

            return oDataResponse.Value;
        }
    }
}

using Bit.Core.Implementations;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Refit;
using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Http.Implementations
{
    public class BitRefitJsonContentSerializer : IHttpContentSerializer
    {
        private readonly JsonSerializerSettings _jsonSerializeSettings;
        private readonly JsonSerializerSettings _jsonDeserializeSettings;

        public BitRefitJsonContentSerializer() :
            this(DefaultJsonContentFormatter.SerializeSettings(), DefaultJsonContentFormatter.DeserializeSettings())
        {

        }

        public BitRefitJsonContentSerializer(JsonSerializerSettings jsonSerializeSettings, JsonSerializerSettings jsonDeserializeSettings)
        {
            _jsonSerializeSettings = jsonSerializeSettings;
            _jsonDeserializeSettings = jsonDeserializeSettings;
        }

        public async Task<T?> FromHttpContentAsync<T>(HttpContent content, CancellationToken cancellationToken = default)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

#if DotNetStandard2_0 || UWP
            using Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
#else
            await using Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
            using StreamReader reader = new StreamReader(stream);
            using JsonTextReader jsonTextReader = new JsonTextReader(reader);
            return (await JToken.LoadAsync(jsonTextReader, cancellationToken).ConfigureAwait(false)).ToObject<T>(JsonSerializer.Create(_jsonDeserializeSettings))!;
        }

        public string? GetFieldNameForProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetCustomAttribute<JsonPropertyAttribute>()?.PropertyName ?? propertyInfo.Name;
        }

        public HttpContent ToHttpContent<T>(T item)
        {
            return new StringContent(DefaultJsonContentFormatter.Current.Serialize(item), Encoding.UTF8, DefaultJsonContentFormatter.Current.ContentType);
        }
    }
}

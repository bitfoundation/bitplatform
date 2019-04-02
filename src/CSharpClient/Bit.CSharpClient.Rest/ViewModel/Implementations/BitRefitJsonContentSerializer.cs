using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Refit;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Bit.ViewModel.Implementations
{
    public class BitRefitJsonContentSerializer : IContentSerializer
    {
        private readonly JsonSerializerSettings _jsonSerializeSettings;
        private readonly JsonSerializerSettings _jsonDeserializeSettings;

        public BitRefitJsonContentSerializer() :
            this(DefaultJsonContentFormatter.SerializeSettings(), DefaultJsonContentFormatter.DeSerializeSettings())
        {

        }

        public BitRefitJsonContentSerializer(JsonSerializerSettings jsonSerializeSettings, JsonSerializerSettings jsonDeserializeSettings)
        {
            _jsonSerializeSettings = jsonSerializeSettings;
            _jsonDeserializeSettings = jsonDeserializeSettings;
        }

        public virtual async Task<T> DeserializeAsync<T>(HttpContent content)
        {
            using (Stream stream = await content.ReadAsStreamAsync().ConfigureAwait(false))
            using (StreamReader reader = new StreamReader(stream))
            using (JsonTextReader jsonTextReader = new JsonTextReader(reader))
                return (await JToken.LoadAsync(jsonTextReader).ConfigureAwait(false)).ToObject<T>(JsonSerializer.Create(_jsonDeserializeSettings));
        }

        public virtual Task<HttpContent> SerializeAsync<T>(T item)
        {
            StringContent content = new StringContent(JsonConvert.SerializeObject(item, _jsonSerializeSettings), Encoding.UTF8, "application/json");
            return Task.FromResult<HttpContent>(content);
        }
    }
}

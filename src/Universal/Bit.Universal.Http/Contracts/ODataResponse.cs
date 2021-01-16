using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace Bit.Http.Contracts
{
    public class ODataResponse<T>
    {
        [JsonProperty("value")]
        [JsonPropertyName("value")]
        public virtual T Value { get; set; } = default!;

        [JsonProperty("@odata.context")]
        [JsonPropertyName("@odata.context")]
        public virtual string? Context { get; set; }

        /// <summary>
        /// It can be requested by $count=true in query string of your request.
        /// </summary>
        [JsonProperty("@odata.count")]
        [JsonPropertyName("@odata.count")]
        public virtual long? TotalCount { get; set; }
    }
}

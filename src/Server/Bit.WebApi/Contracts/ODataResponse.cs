using Newtonsoft.Json;

namespace Bit.WebApi.Contracts
{
    public class ODataResponse<T>
    {
        [JsonProperty("value")]
        public virtual T Value { get; set; }

        [JsonProperty("@odata.context")]
        public virtual string Context { get; set; }

        /// <summary>
        /// It can be requested by $count=true in query string of your request.
        /// </summary>
        [JsonProperty("@odata.count")]
        public virtual long? TotalCount { get; set; }
    }
}

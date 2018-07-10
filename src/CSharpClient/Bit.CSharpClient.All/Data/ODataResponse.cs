using Newtonsoft.Json;

namespace Bit.Data
{
    public class ODataResponse<T>
    {
        [JsonProperty("value")]
        public virtual T Value { get; set; }
    }
}

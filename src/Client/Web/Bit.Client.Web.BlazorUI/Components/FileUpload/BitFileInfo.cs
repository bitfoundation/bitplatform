using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Bit.Client.Web.BlazorUI
{
    public class BitFileInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")]
        public long Size { get; set; }

        [JsonPropertyName("type")]
        public string ContentType { get; set; } = string.Empty;

        [JsonIgnore]
        public BitUploadStatus UploadStatus { get; set; }

        public int ChunkCount { get; set; }
        public IList<long> ChunkesUpLoadedSize { get; } = new List<long>();
        public bool RequestToPause { get; set; }
        public bool RequestToCancell { get; set; }
    }
}

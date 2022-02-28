using System;
using System.Text.Json.Serialization;

namespace Bit.Client.Web.BlazorUI
{
    public class BitFileInfo
    {
        [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

        [JsonPropertyName("size")] public long Size { get; set; }

        [JsonPropertyName("type")] public string ContentType { get; set; } = string.Empty;

        public long TotalSizeOfUploaded { get; set; }

        internal bool RequestToPause { get; set; }
        internal bool RequestToCancel { get; set; }
        internal long SizeOfLastChunkUploaded { get; set; }

        [JsonIgnore] public string? Message { get; set; }
        [JsonIgnore] public BitFileUploadStatus Status { get; set; }
        [JsonIgnore] internal DateTime? StartTimeUpload { get; set; }
    }
}

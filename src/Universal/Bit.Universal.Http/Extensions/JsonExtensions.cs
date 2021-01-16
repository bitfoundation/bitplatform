using System.Buffers;
using System.Threading.Tasks;

namespace System.Text.Json
{
    public static partial class JsonExtensions
    {
#if DotNetStandard2_0 || UWP
        public static async Task<T> ToObjectAsync<T>(this JsonElement element, JsonSerializerOptions? options = null)
        {
            string json = element.GetRawText();

            return JsonSerializer.Deserialize<T>(json, options)!;
        }

        public static async Task<T> ToObjectAsync<T>(this JsonDocument document, JsonSerializerOptions? options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            string json = document.RootElement.GetRawText();

            return await document.RootElement.ToObjectAsync<T>(options).ConfigureAwait(false);
        }
#else
        public static async Task<T> ToObjectAsync<T>(this JsonElement element, JsonSerializerOptions? options = null)
        {
            ArrayBufferWriter<byte>? bufferWriter = new ArrayBufferWriter<byte>();

            await using (Utf8JsonWriter writer = new Utf8JsonWriter(bufferWriter))
                element.WriteTo(writer);

            return JsonSerializer.Deserialize<T>(bufferWriter.WrittenSpan, options)!;
        }

        public static async Task<T> ToObjectAsync<T>(this JsonDocument document, JsonSerializerOptions? options = null)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            return await document.RootElement.ToObjectAsync<T>(options).ConfigureAwait(false);
        }
#endif
    }
}

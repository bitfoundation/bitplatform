using Bit.Core.Contracts;
using Newtonsoft.Json;
using System;

namespace Bit.Core.Implementations
{
    public class DefaultJsonContentFormatter : IContentFormatter
    {
        private static IContentFormatter _current;

        public static IContentFormatter Current
        {
            get => _current ?? (_current = new DefaultJsonContentFormatter());
            set => _current = value;
        }

        public virtual T DeSerialize<T>(string objAsStr)
        {
            return JsonConvert.DeserializeObject<T>(objAsStr, DeSerializeSettings());
        }

        public static Func<JsonSerializerSettings> DeSerializeSettings { get; set; } = () => new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.All
        };

        public static Func<JsonSerializerSettings> SerializeSettings { get; set; } = () => new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat
        };

        public virtual string Serialize<T>(T obj)
        {
            JsonSerializerSettings jsonSerializerSettings = SerializeSettings();

            jsonSerializerSettings.Formatting = Formatting.Indented;

            return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
        }
    }
}

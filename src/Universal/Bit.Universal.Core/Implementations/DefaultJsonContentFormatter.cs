using Bit.Core.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Core.Implementations
{
    public class BitCamelCasePropertyNamesContractResolver : CamelCasePropertyNamesContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            bool hasNotMappedAttribute = member.GetCustomAttribute<NotMappedAttribute>() != null;
            if (hasNotMappedAttribute == true)
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }

    public class BitContractResolver : DefaultContractResolver
    {
        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty property = base.CreateProperty(member, memberSerialization);
            bool hasNotMappedAttribute = member.GetCustomAttribute<NotMappedAttribute>() != null;
            if (hasNotMappedAttribute == true)
            {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }

    public class DefaultJsonContentFormatter : IContentFormatter
    {
        private static IContentFormatter _current = default!;

        public static IContentFormatter Current
        {
            get
            {
                if (_current == null)
                    _current = new DefaultJsonContentFormatter();

                return _current;
            }
            set => _current = value;
        }

        public virtual T Deserialize<T>(string objAsStr)
        {
            return JsonConvert.DeserializeObject<T>(objAsStr, DeserializeSettings())!;
        }

        public static Func<JsonSerializerSettings> DeserializeSettings { get; set; } = () => new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.All,
            ContractResolver = BitContractResolver,
            Converters = new List<JsonConverter> { new StringEnumConverter { } }
        };

        private static readonly BitContractResolver BitContractResolver = new BitContractResolver();

        public static Func<JsonSerializerSettings> SerializeSettings { get; set; } = () => new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ContractResolver = BitContractResolver,
            Converters = new List<JsonConverter> { new StringEnumConverter { } }
        };

        public virtual string Serialize<T>([AllowNull] T obj)
        {
            JsonSerializerSettings jsonSerializerSettings = SerializeSettings();

            jsonSerializerSettings.Formatting = Formatting.Indented;

            return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
        }

        public virtual async Task<T> DeserializeAsync<T>(Stream input, CancellationToken cancellationToken)
        {
            JsonSerializer serializer = JsonSerializer.CreateDefault(SerializeSettings());
            using StreamReader streamReader = new StreamReader(input);
            using JsonTextReader jsonReader = new JsonTextReader(streamReader);
            JToken json = await JToken.LoadAsync(jsonReader, cancellationToken).ConfigureAwait(false);
            return json.ToObject<T>(serializer);
        }

        public virtual string ContentType => "application/json";
    }
}
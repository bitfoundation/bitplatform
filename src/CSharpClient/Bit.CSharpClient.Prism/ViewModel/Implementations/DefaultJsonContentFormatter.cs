using Bit.ViewModel.Contracts;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;

namespace Bit.ViewModel.Implementations
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
        private static IContentFormatter _current;

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

        public virtual T DeSerialize<T>(string objAsStr)
        {
            return JsonConvert.DeserializeObject<T>(objAsStr, DeSerializeSettings());
        }

        public static Func<JsonSerializerSettings> DeSerializeSettings { get; set; } = () => new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            TypeNameHandling = TypeNameHandling.All,
            ContractResolver = BitContractResolver
        };

        private static readonly BitContractResolver BitContractResolver = new BitContractResolver();

        public static Func<JsonSerializerSettings> SerializeSettings { get; set; } = () => new JsonSerializerSettings
        {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            ContractResolver = BitContractResolver
        };

        public virtual string Serialize<T>(T obj)
        {
            JsonSerializerSettings jsonSerializerSettings = SerializeSettings();

            jsonSerializerSettings.Formatting = Formatting.Indented;

            return JsonConvert.SerializeObject(obj, jsonSerializerSettings);
        }
    }
}

using Bit.Model.Contracts;
using Prism.Navigation;
using System.Reflection;

namespace System.Collections.Generic
{
    public static class IDictionaryExtensions
    {
        public static NavigationParameters ToNavParams(this IDictionary<string, object> parameters)
        {
            NavigationParameters result = new NavigationParameters();

            foreach (KeyValuePair<string, object> param in parameters)
            {
                result.Add(param.Key, param.Value);
            }

            return result;
        }

        public static IDto ToDto(this IDictionary<string, object> unTypedDto, TypeInfo dtoType)
        {
            if (unTypedDto == null)
                throw new ArgumentNullException(nameof(unTypedDto));

            if (dtoType == null)
                throw new ArgumentNullException(nameof(dtoType));

            IDto dto = (IDto)Activator.CreateInstance(dtoType);

            Dictionary<string, object> extraProps = null;

            if (dto is IOpenType openTypeDto)
                extraProps = openTypeDto.Properties = (openTypeDto.Properties ?? new Dictionary<string, object>());

            foreach (KeyValuePair<string, object> KeyVal in unTypedDto)
            {
                PropertyInfo prop = dtoType.GetProperty(KeyVal.Key);

                object val = KeyVal.Value;

                if (prop != null)
                {
                    if (prop.PropertyType.IsEnum && val != null)
                        val = Enum.Parse(prop.PropertyType, KeyVal.Value.ToString());
                    prop.SetValue(dto, val);
                }
                else
                {
                    extraProps?.Add(KeyVal.Key, val);
                }
            }

            return dto;
        }

        public static TDto ToDto<TDto>(this IDictionary<string, object> unTypedDto)
            where TDto : class, IDto
        {
            return (TDto)unTypedDto.ToDto(typeof(TDto).GetTypeInfo());
        }
    }
}

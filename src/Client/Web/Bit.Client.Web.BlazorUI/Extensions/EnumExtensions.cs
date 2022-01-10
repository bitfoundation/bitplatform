using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace Bit.Client.Web.BlazorUI
{
    public static class EnumExtensions
    {
        public static string? GetDisplayName(this Enum enumValue, bool showNameIfHasNoDisplayName = true)
        {
            if (enumValue is null)
            {
                return null;
            }

            var name = enumValue.GetType()
              .GetMember(enumValue.ToString())
              .FirstOrDefault()?
              .GetCustomAttribute<DisplayAttribute>()
              ?.GetName();

            if (name.HasValue())
            {
                return name!;
            }

            if (showNameIfHasNoDisplayName)
            {
                return enumValue.ToString();
            }

            return null;
        }

        public static string? GetName(this BitIconName? bitIconName, bool ignoreDefaultValue = true)
        {
            if (!bitIconName.HasValue)
            {
                return null;
            }

            return GetIconName(bitIconName.Value, ignoreDefaultValue);
        }

        public static string? GetName(this BitIconName bitIconName, bool ignoreDefaultValue = true)
        {
            return GetIconName(bitIconName, ignoreDefaultValue);
        }

        private static string? GetIconName(this BitIconName bitIconName, bool ignoreDefaultValue = true)
        {
            if (ignoreDefaultValue)
            {
                var attributes = (DefaultValueAttribute[])bitIconName.GetType().GetCustomAttributes(typeof(DefaultValueAttribute), false);

                if (attributes is not null && attributes.Length > 0 && attributes[0].Value is not null)
                {
                    if ((BitIconName)attributes[0].Value! == bitIconName)
                    {
                        return null;
                    }
                }
                else if (bitIconName == default)
                {
                    return null;
                }
            }

            return bitIconName.GetDisplayName();
        }
    }
}

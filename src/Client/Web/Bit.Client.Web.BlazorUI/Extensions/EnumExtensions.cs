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

        public static string? GetName(this BitIcon? bitIcon, bool ignoreDefaultValue = true)
        {
            if (!bitIcon.HasValue)
            {
                return null;
            }

            return GetIconName(bitIcon.Value, ignoreDefaultValue);
        }

        public static string? GetName(this BitIcon bitIcon, bool ignoreDefaultValue = true)
        {
            return GetIconName(bitIcon, ignoreDefaultValue);
        }

        private static string? GetIconName(this BitIcon bitIcon, bool ignoreDefaultValue = true)
        {
            if (ignoreDefaultValue)
            {
                var attributes = (DefaultValueAttribute[])bitIcon.GetType().GetCustomAttributes(typeof(DefaultValueAttribute), false);

                if (attributes is not null && attributes.Length > 0 && attributes[0].Value is not null)
                {
                    if ((BitIcon)attributes[0].Value! == bitIcon)
                    {
                        return null;
                    }
                }
                else if (bitIcon == default)
                {
                    return null;
                }
            }

            return bitIcon.GetDisplayName();
        }
    }
}

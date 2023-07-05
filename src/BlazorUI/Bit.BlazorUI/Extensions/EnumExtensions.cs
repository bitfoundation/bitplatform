using System.Reflection;
using System.Globalization;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.DataAnnotations;

[assembly: InternalsVisibleTo("Bit.BlazorUI.Demo.Client.Core")]
namespace Bit.BlazorUI;

internal static class EnumExtensions
{
    internal static string? GetDisplayName(this Enum enumValue, bool showNameIfHasNoDisplayName = true, bool toLowerDisplayName = false)
    {
        if (enumValue is null) return null;

        var name = enumValue.GetType()
                            .GetMember(enumValue.ToString())
                            .FirstOrDefault()?
                            .GetCustomAttribute<DisplayAttribute>()?
                            .GetName();

        string? displayName = null;

        if (name.HasValue())
        {
            displayName = name!;
        }
        else if (showNameIfHasNoDisplayName)
        {
            displayName = enumValue.ToString();
        }

        if (displayName.HasValue() && toLowerDisplayName)
        {
            displayName = displayName!.ToLower(CultureInfo.CurrentUICulture);
        }

        return displayName;
    }

    internal static string? GetName(this BitIconName? bitIconName, bool ignoreDefaultValue = true)
    {
        if (bitIconName.HasValue is false) return null;

        return GetIconName(bitIconName.Value, ignoreDefaultValue);
    }

    internal static string? GetName(this BitIconName bitIconName, bool ignoreDefaultValue = true) => GetIconName(bitIconName, ignoreDefaultValue);

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

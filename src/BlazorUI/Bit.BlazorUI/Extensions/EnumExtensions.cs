using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace Bit.BlazorUI;

internal static class EnumExtensions
{
    [SuppressMessage("Trimming", "IL2075:'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.", Justification = "<Pending>")]
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
}

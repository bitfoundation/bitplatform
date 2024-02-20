﻿using System.Reflection;
using System.Globalization;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace Bit.BlazorUI;

[EditorBrowsable(EditorBrowsableState.Never)]
public static class EnumExtensions
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static string? GetDisplayName(this Enum enumValue, bool showNameIfHasNoDisplayName = true, bool toLowerDisplayName = false)
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

namespace Bit.BlazorUI;

/// <summary>
/// Represents icon information for rendering icons in Bit BlazorUI components.
/// Supports both built-in Fluent UI icons and custom/external icon libraries.
/// </summary>
public class BitIconInfo
{
    /// <summary>
    /// Creates a new instance of <see cref="BitIconInfo"/>.
    /// </summary>
    public BitIconInfo() { }

    /// <summary>
    /// Creates a new instance of <see cref="BitIconInfo"/> with the specified icon name.
    /// </summary>
    /// <param name="name">The name of the icon. For built-in icons, use values from <c>BitIconName</c>.</param>
    public BitIconInfo(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitIconInfo"/> with full customization.
    /// </summary>
    /// <param name="name">The name of the icon or the CSS class for external icons.</param>
    /// <param name="baseClass">
    /// The base CSS class for the icon. 
    /// Set to empty string for external icon libraries that don't need a base class.
    /// When null, defaults to "bit-icon" for built-in icons.
    /// </param>
    /// <param name="prefix">
    /// The CSS class prefix used before the icon name.
    /// When null, defaults to "bit-icon--" for built-in icons.
    /// Set to empty string for external icons that don't use a prefix.
    /// </param>
    public BitIconInfo(string name, string? baseClass, string? prefix = null)
    {
        Name = name;
        BaseClass = baseClass;
        Prefix = prefix;
    }

    /// <summary>
    /// Gets or sets the name of the icon.
    /// For built-in icons, use values from <c>BitIconName</c> (e.g., "Add", "Delete").
    /// For external icons, this can be the full CSS class name if <see cref="BaseClass"/> and <see cref="Prefix"/> are empty.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the base CSS class for the icon.
    /// For built-in Fluent UI icons, this defaults to "bit-icon".
    /// For external icon libraries like FontAwesome, you might set this to "fa" or leave empty.
    /// </summary>
    /// <example>
    /// For FontAwesome: BaseClass = "fa" or BaseClass = ""
    /// For Material Icons: BaseClass = "material-icons" or BaseClass = ""
    /// </example>
    public string? BaseClass { get; set; }

    /// <summary>
    /// Gets or sets the CSS class prefix used before the icon name.
    /// For built-in Fluent UI icons, this defaults to "bit-icon--".
    /// For external icon libraries, you might set this to "fa-" or leave empty.
    /// </summary>
    /// <example>
    /// For FontAwesome 6: Prefix = "" and Name = "fa-solid fa-house"
    /// For FontAwesome 5: Prefix = "fa-" and Name = "house"
    /// </example>
    public string? Prefix { get; set; }

    /// <summary>
    /// Determines whether the icon has a valid name set.
    /// </summary>
    /// <returns><c>true</c> if the icon has a non-empty name; otherwise, <c>false</c>.</returns>
    public bool HasValue() => Name.HasValue();

    /// <summary>
    /// Gets the CSS classes to render the icon.
    /// </summary>
    /// <returns>The complete CSS class string for the icon.</returns>
    public string GetCssClasses()
    {
        if (Name.HasNoValue()) return string.Empty;

        if (BaseClass.HasNoValue() && Prefix.HasNoValue())
        {
            return Name!;
        }

        if (BaseClass.HasNoValue())
        {
            return $"{Prefix}{Name}";
        }

        if (Prefix.HasNoValue())
        {
            return $"{BaseClass} {Name}";
        }

        return $"{BaseClass} {Prefix}{Name}";
    }

    /// <summary>
    /// Implicit conversion from string to <see cref="BitIconInfo"/>.
    /// This maintains backward compatibility with the existing string-based IconName parameter.
    /// </summary>
    /// <param name="iconName">The icon name string.</param>
    public static implicit operator BitIconInfo?(string? iconName)
    {
        if (iconName is null) return null;

        return new BitIconInfo(iconName);
    }

    /// <summary>
    /// Implicit conversion from <see cref="BitIconInfo"/> to string.
    /// Returns the icon name for simple scenarios.
    /// </summary>
    /// <param name="iconInfo">The icon info instance.</param>
    public static implicit operator string?(BitIconInfo? iconInfo)
    {
        return iconInfo?.Name;
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for an external/custom icon using the provided CSS classes directly.
    /// </summary>
    /// <param name="cssClasses">The complete CSS class(es) to render the icon (e.g., "fa-solid fa-house" for FontAwesome 6).</param>
    /// <returns>A new <see cref="BitIconInfo"/> instance configured for external icons.</returns>
    /// <example>
    /// FontAwesome 6: BitIconInfo.Css("fa-solid fa-house")
    /// Material Icons: BitIconInfo.Css("material-icons home")
    /// Bootstrap Icons: BitIconInfo.Css("bi bi-house")
    /// </example>
    public static BitIconInfo Css(string cssClasses)
    {
        return new BitIconInfo(cssClasses, baseClass: "", prefix: "");
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for a built-in bit BlazorUI icon.
    /// </summary>
    /// <param name="name">The bit BlazorUI icon name (e.g., "add").</param>
    /// <returns>A new <see cref="BitIconInfo"/> instance configured for bit BlazorUI icons.</returns>
    /// <example>
    /// BitIconInfo.Bit("add")
    /// </example>
    public static BitIconInfo Bit(string name)
    {
        return new BitIconInfo(name, baseClass: "bit-icon", prefix: "bit-icon--");
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for a FontAwesome icon.
    /// </summary>
    /// <param name="icons">The FontAwesome icon classes (e.g., "fa-solid fa-house" or just "fa-house" with style parameter).</param>
    /// <returns>A new <see cref="BitIconInfo"/> instance configured for FontAwesome icons.</returns>
    /// <example>
    /// BitIconInfo.Fa("solid house")
    /// BitIconInfo.Fa("fa-brands fa-github")
    /// </example>
    public static BitIconInfo Fa(string icons)
    {
        var cssClasses = string.Join(' ', icons.Split(' ').Select(i => i.StartsWith("fa-") ? i : $"fa-{i}"));

        return new BitIconInfo(cssClasses, baseClass: "", prefix: "");
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for a Material Icons icon.
    /// </summary>
    /// <param name="iconName">The Material Icon name (e.g., "home", "search", "menu").</param>
    /// <returns>A new <see cref="BitIconInfo"/> instance configured for Material Icons.</returns>
    /// <example>
    /// BitIconInfo.Material("home")
    /// BitIconInfo.Material("search")
    /// </example>
    public static BitIconInfo Material(string iconName)
    {
        return new BitIconInfo(iconName, baseClass: "material-icons", prefix: "");
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for a Bootstrap Icons icon.
    /// </summary>
    /// <param name="iconName">The Bootstrap Icon name without the "bi-" prefix (e.g., "house", "search").</param>
    /// <returns>A new <see cref="BitIconInfo"/> instance configured for Bootstrap Icons.</returns>
    /// <example>
    /// BitIconInfo.Bootstrap("house")
    /// BitIconInfo.Bootstrap("search")
    /// </example>
    public static BitIconInfo Bootstrap(string iconName)
    {
        return new BitIconInfo(iconName, baseClass: "bi", prefix: "bi-");
    }

    /// <summary>
    /// Resolves the effective icon from either a <see cref="BitIconInfo"/> or an icon name string.
    /// The <paramref name="icon"/> parameter takes precedence when both are provided.
    /// </summary>
    /// <param name="icon">The icon info instance, if provided.</param>
    /// <param name="iconName">The icon name string for built-in icons, if provided.</param>
    /// <returns>A <see cref="BitIconInfo"/> instance if either parameter has a value; otherwise, <c>null</c>.</returns>
    /// <remarks>
    /// This method is intended for internal use by components to unify the handling of
    /// the <c>Icon</c> and <c>IconName</c> parameters.
    /// </remarks>
    public static BitIconInfo? From(BitIconInfo? icon, string? iconName)
    {
        if (icon is not null) return icon;

        if (iconName.HasNoValue()) return null;

        return Bit(iconName!);
    }
}

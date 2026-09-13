namespace Bit.BlazorUI;

/// <summary>
/// Represents icon information for rendering icons in Bit BlazorUI components.
/// Supports both built-in Fluent UI icons and custom/external icon libraries.
/// </summary>
/// <remarks>
/// An icon set names its glyphs in one of two ways, and this type carries both: with CSS classes
/// (<see cref="BaseClass"/>, <see cref="Prefix"/> and <see cref="Name"/>, which is how Fabric MDL2,
/// FontAwesome and Bootstrap Icons work), or with a ligature written as the element's own text
/// (<see cref="Content"/>, which is how Material Icons and Material Symbols work). The two combine:
/// a ligature set still needs the class that selects its font family.
/// </remarks>
public class BitIconInfo : IEquatable<BitIconInfo>
{
    /// <summary>
    /// Creates a new instance of <see cref="BitIconInfo"/>.
    /// </summary>
    public BitIconInfo() { }

    /// <summary>
    /// Creates a new instance of <see cref="BitIconInfo"/> with the specified icon name.
    /// </summary>
    /// <param name="name">
    /// The name of the icon.
    /// </param>
    public BitIconInfo(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Creates a new instance of <see cref="BitIconInfo"/> with full customization.
    /// </summary>
    /// <param name="name">
    /// The name of the icon or the CSS class for external icons.
    /// </param>
    /// <param name="baseClass">
    /// The base CSS class for the icon.
    /// Set to null or empty string for external icon libraries that don't need a base class.
    /// </param>
    /// <param name="prefix">
    /// The CSS class prefix used before the icon name.
    /// Set to null or empty string for external icons that don't use a prefix.
    /// </param>
    public BitIconInfo(string name, string? baseClass, string? prefix = null)
    {
        Name = name;
        BaseClass = baseClass;
        Prefix = prefix;
    }

    /// <summary>
    /// Gets or sets the name of the icon.
    /// For external icons, this can be the full CSS class name if <see cref="BaseClass"/> and <see cref="Prefix"/> are empty.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the base CSS class for the icon.
    /// For external icon libraries like FontAwesome, you might set this to "fa" or leave empty.
    /// </summary>
    public string? BaseClass { get; set; }

    /// <summary>
    /// Gets or sets the CSS class prefix used before the icon name.
    /// For external icon libraries, you might set this to "fa-" or leave empty.
    /// </summary>
    public string? Prefix { get; set; }

    /// <summary>
    /// Gets or sets the text rendered inside the icon element - the ligature of a ligature-based icon set.
    /// </summary>
    /// <remarks>
    /// Material Icons and Material Symbols name each glyph with a ligature written as the element's
    /// text rather than with a class of its own, so those sets need this alongside the
    /// <see cref="BaseClass"/> that selects the font family. Class-based sets leave it null.
    /// <br />
    /// Only a component that renders the icon's content puts this on the page - <see cref="BitIcon"/>
    /// does. Everywhere the library draws a glyph of its own inside another control, only the classes
    /// are rendered, so a ligature-based set has to be given to a <see cref="BitIcon"/>.
    /// </remarks>
    public string? Content { get; set; }

    /// <summary>
    /// Gets a value indicating whether this instance names no glyph at all - nothing to put in a class
    /// attribute, and nothing to write as the element's text.
    /// </summary>
    public bool IsEmpty => Name.HasNoValue() && BaseClass.HasNoValue() && Content.HasNoValue();



    /// <summary>
    /// Gets the CSS classes to render the icon.
    /// </summary>
    /// <returns>The complete CSS class string for the icon.</returns>
    public string GetCssClasses()
    {
        // A ligature set names its glyph in the element's text, so its family class stands on its own
        // with no name behind it. Anything else with no name has no class to give.
        if (Name.HasNoValue()) return BaseClass.HasValue() ? BaseClass! : string.Empty;

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
    /// <param name="iconName">
    /// The icon name string.
    /// </param>
    public static implicit operator BitIconInfo?(string? iconName)
    {
        if (iconName is null) return null;

        return new BitIconInfo(iconName);
    }

    /// <summary>
    /// Implicit conversion from <see cref="BitIconInfo"/> to string.
    /// Returns the icon name for simple scenarios.
    /// </summary>
    /// <param name="iconInfo">
    /// The icon info instance.
    /// </param>
    public static implicit operator string?(BitIconInfo? iconInfo)
    {
        return iconInfo?.Name;
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for an external/custom icon using the provided CSS classes directly.
    /// </summary>
    /// <param name="cssClasses">
    /// The complete CSS class(es) to render the icon (e.g., "fa-solid fa-house" for FontAwesome).
    /// </param>
    /// <returns>
    /// A new <see cref="BitIconInfo"/> instance configured for external icons.
    /// </returns>
    public static BitIconInfo Css(string cssClasses)
    {
        return new BitIconInfo(cssClasses, baseClass: "", prefix: "");
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for a built-in bit BlazorUI icon.
    /// </summary>
    /// <param name="name">
    /// The bit BlazorUI icon name (e.g., "add").
    /// </param>
    /// <returns>
    /// A new <see cref="BitIconInfo"/> instance configured for bit BlazorUI icons.
    /// </returns>
    public static BitIconInfo Bit(string name)
    {
        return new BitIconInfo(name, baseClass: "bit-icon", prefix: "bit-icon--");
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for a FontAwesome icon.
    /// </summary>
    /// <param name="icons">
    /// The FontAwesome icon classes (e.g., "fa-solid fa-house", "solid house", or just "house" with style parameter).
    /// </param>
    /// <returns>
    /// A new <see cref="BitIconInfo"/> instance configured for FontAwesome icons.
    /// </returns>
    public static BitIconInfo Fa(string icons)
    {
        var cssClasses = string.Join(' ', icons.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                                               .Select(i => i.StartsWith("fa-") ? i : $"fa-{i}"));

        return new BitIconInfo(cssClasses, baseClass: "", prefix: "");
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for a Bootstrap Icons icon.
    /// </summary>
    /// <param name="iconName">
    /// The Bootstrap Icon name without the "bi-" prefix (e.g., "house", "search").
    /// </param>
    /// <returns>
    /// A new <see cref="BitIconInfo"/> instance configured for Bootstrap Icons.
    /// </returns>
    public static BitIconInfo Bi(string iconName)
    {
        return new BitIconInfo(iconName, baseClass: "bi", prefix: "bi-");
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for a Material Icons glyph.
    /// </summary>
    /// <param name="iconName">
    /// The Material Icons ligature (e.g., "home", "arrow_forward").
    /// </param>
    /// <param name="style">
    /// The Material Icons style - "outlined", "round", "sharp" or "two-tone".
    /// Leave it null for the filled default.
    /// </param>
    /// <returns>
    /// A new <see cref="BitIconInfo"/> instance configured for Material Icons.
    /// </returns>
    /// <remarks>
    /// Material Icons names each glyph with a ligature rather than with a class of its own, so the
    /// name lands on <see cref="Content"/> and the classes carry nothing but the font family.
    /// </remarks>
    public static BitIconInfo Mi(string iconName, string? style = null)
    {
        return new BitIconInfo
        {
            Content = iconName,
            BaseClass = style.HasValue() ? $"material-icons-{style}" : "material-icons"
        };
    }

    /// <summary>
    /// Creates a <see cref="BitIconInfo"/> for a Material Symbols glyph.
    /// </summary>
    /// <param name="iconName">
    /// The Material Symbols ligature (e.g., "home", "arrow_forward").
    /// </param>
    /// <param name="style">
    /// The Material Symbols style - "outlined" (the default), "rounded" or "sharp".
    /// </param>
    /// <returns>
    /// A new <see cref="BitIconInfo"/> instance configured for Material Symbols.
    /// </returns>
    /// <remarks>
    /// Material Symbols names each glyph with a ligature rather than with a class of its own, so the
    /// name lands on <see cref="Content"/> and the classes carry nothing but the font family.
    /// </remarks>
    public static BitIconInfo Ms(string iconName, string style = "outlined")
    {
        return new BitIconInfo
        {
            Content = iconName,
            BaseClass = style.HasValue() ? $"material-symbols-{style}" : "material-symbols-outlined"
        };
    }

    /// <summary>
    /// Resolves the effective icon from either a <see cref="BitIconInfo"/> or an icon name string.
    /// The <paramref name="icon"/> parameter takes precedence when both are provided.
    /// </summary>
    /// <param name="icon">
    /// The icon info instance, if provided.
    /// </param>
    /// <param name="iconName">
    /// The icon name string for built-in icons, if provided.
    /// </param>
    /// <returns>
    /// A <see cref="BitIconInfo"/> instance if either parameter has a value; otherwise, <c>null</c>.
    /// </returns>
    /// <remarks>
    /// This method is useful for components to unify the handling of
    /// the <c>Icon</c> and <c>IconName</c> parameters.
    /// <br />
    /// Precedence only means something while there is something to prefer: an instance that names no
    /// glyph at all (<see cref="IsEmpty"/>) is no icon, so a name given beside it is still used
    /// rather than silently dropped.
    /// </remarks>
    public static BitIconInfo? From(BitIconInfo? icon, string? iconName)
    {
        if (icon is not null && icon.IsEmpty is false) return icon;

        if (iconName.HasNoValue()) return icon;

        return Bit(iconName!);
    }



    /// <summary>
    /// Determines whether the specified instance describes the same glyph as this one.
    /// </summary>
    /// <remarks>
    /// Two descriptions of the same glyph are the same icon. Without this, every render that builds
    /// an icon inline - the implicit conversion from a string, a call to one of the factories - hands
    /// the component a brand new instance and makes it rebuild its class attribute for a value that
    /// never changed.
    /// </remarks>
    /// <param name="other">
    /// The instance to compare with this one.
    /// </param>
    /// <returns>
    /// true when both describe the same glyph; otherwise, false.
    /// </returns>
    public bool Equals(BitIconInfo? other)
    {
        if (other is null) return false;

        if (ReferenceEquals(this, other)) return true;

        return string.Equals(Name, other.Name, StringComparison.Ordinal)
            && string.Equals(BaseClass, other.BaseClass, StringComparison.Ordinal)
            && string.Equals(Prefix, other.Prefix, StringComparison.Ordinal)
            && string.Equals(Content, other.Content, StringComparison.Ordinal);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj) => Equals(obj as BitIconInfo);

    /// <inheritdoc />
    public override int GetHashCode() => HashCode.Combine(Name, BaseClass, Prefix, Content);

    /// <summary>
    /// Returns the CSS classes this instance renders with.
    /// </summary>
    /// <returns>The value of <see cref="GetCssClasses"/>.</returns>
    public override string ToString() => GetCssClasses();
}

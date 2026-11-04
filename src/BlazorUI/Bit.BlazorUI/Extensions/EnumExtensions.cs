using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: System.Reflection.Metadata.MetadataUpdateHandler(typeof(Bit.BlazorUI.EnumExtensions))]

namespace Bit.BlazorUI;

internal static class EnumExtensions
{
    internal static string? GetDisplayName(this Enum enumValue, bool showNameIfHasNoDisplayName = true, bool toLowerDisplayName = false)
    {
        if (enumValue is null) return null;

        var displayName = enumValue.FindDisplayName();

        if (displayName.HasValue() is false && showNameIfHasNoDisplayName)
        {
            displayName = enumValue.ToString();
        }

        if (displayName.HasValue() && toLowerDisplayName)
        {
            displayName = displayName!.ToLower(CultureInfo.CurrentUICulture);
        }

        return displayName;
    }

    /// <summary>
    /// The name the <see cref="DisplayAttribute"/> of an enum value gives it, or <c>null</c> when it has none: a value no
    /// member stands for (a number no member has), a member without the attribute, an empty name, or a name its
    /// <c>ResourceType</c> cannot give. A combination of flags is written the way <see cref="Enum.ToString()"/> writes it,
    /// with each member that has a name written by it.
    /// </summary>
    internal static string? FindDisplayName(this Enum value)
    {
        var info = EnumInfo.Of(value.GetType());
        var text = value.ToString();

        if (info.ByName.TryGetValue(text, out var member)) return member.DisplayName;

        // Enum writes a combination of flags as the names of its members joined by ", ", and a value it cannot write
        // that way as a number. A name has no comma or space in it, so splitting the text gives back the members.
        if (info.IsFlags is false || text.Contains(',') is false) return null;

        var parts = text.Split(", ");
        var named = false;
        for (var i = 0; i < parts.Length; i++)
        {
            if (info.ByName.TryGetValue(parts[i], out var part) && part.DisplayName is { } name)
            {
                parts[i] = name;
                named = true;
            }
        }

        return named ? string.Join(", ", parts) : null;
    }

    /// <summary>
    /// The members of an enum type, in the order <see cref="Enum.GetNames(Type)"/> lists them, each with its value and
    /// the name its <see cref="DisplayAttribute"/> gives it. Read once per type and shared by everything that asks.
    /// </summary>
    internal static IReadOnlyList<EnumMember> GetEnumMembers(Type enumType) => EnumInfo.Of(enumType).Members;

    // Hot reload can change the [Display] attributes of an enum, or add and remove its members; the next read builds the
    // members of every type again.
    internal static void ClearCache(Type[]? updatedTypes) => EnumInfo.Clear();

    /// <summary>A member of an enum type: its own name, its value, and the name its <see cref="DisplayAttribute"/> gives it.</summary>
    internal sealed class EnumMember
    {
        private readonly DisplayAttribute? _display;

        internal EnumMember(string name, Enum value, DisplayAttribute? display)
        {
            Name = name;
            Value = value;
            _display = display;
        }

        /// <summary>The member's own name, which <see cref="Enum.Parse(Type, string)"/> reads back.</summary>
        public string Name { get; }

        public Enum Value { get; }

        /// <summary>
        /// The name the member's <see cref="DisplayAttribute"/> gives it, or <c>null</c> when it has none. Read on every
        /// call: a name read off a <c>ResourceType</c> follows the UI culture of the moment.
        /// </summary>
        public string? DisplayName
        {
            get
            {
                if (_display is null) return null;

                try
                {
                    var name = _display.GetName();
                    return string.IsNullOrEmpty(name) ? null : name;
                }
                catch (Exception e) when (e is InvalidOperationException or System.Resources.MissingManifestResourceException)
                {
                    // A resource that cannot be read leaves the member to its own name rather than fail whatever
                    // writes it.
                    return null;
                }
            }
        }
    }

    private sealed class EnumInfo
    {
        // Keyed by the type itself and held weakly, so the members of an enum of a collectible assembly go with it.
        private static readonly ConditionalWeakTable<Type, EnumInfo> Infos = new();

        internal static EnumInfo Of(Type enumType) => Infos.GetValue(enumType, static t => new EnumInfo(t));

        internal static void Clear() => Infos.Clear();

        internal IReadOnlyList<EnumMember> Members { get; }

        internal Dictionary<string, EnumMember> ByName { get; }

        internal bool IsFlags { get; }

        [UnconditionalSuppressMessage("Trimming", "IL2070", Justification = "The trimmer keeps every field of an enum it keeps, and the attributes on them.")]
        private EnumInfo(Type enumType)
        {
            var names = Enum.GetNames(enumType);
            var values = Enum.GetValuesAsUnderlyingType(enumType);
            var members = new EnumMember[names.Length];
            ByName = new Dictionary<string, EnumMember>(names.Length, StringComparer.Ordinal);

            for (var i = 0; i < names.Length; i++)
            {
                var display = enumType.GetField(names[i], BindingFlags.Public | BindingFlags.Static)?.GetCustomAttribute<DisplayAttribute>();

                // A ResourceType that is not public, or has no public static string property of the name, can never
                // give a name: that is how the type is written, not the culture of the moment. Such an attribute is
                // dropped here rather than failing on every read.
                if (display?.ResourceType is not null)
                {
                    try
                    {
                        display.GetName();
                    }
                    catch (InvalidOperationException)
                    {
                        display = null;
                    }
                }

                members[i] = new EnumMember(names[i], (Enum)Enum.ToObject(enumType, values.GetValue(i)!), display);
                ByName[names[i]] = members[i];
            }

            Members = members;
            IsFlags = enumType.IsDefined(typeof(FlagsAttribute), inherit: false);
        }
    }
}

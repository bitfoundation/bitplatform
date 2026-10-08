using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Library = Bit.BlazorUI;

namespace Bit.BlazorUI.Demo.Client.Core.Models;

/// <summary>
/// The enum tables of the types the whole library shares - <c>BitColor</c>, <c>BitSize</c>,
/// <c>BitVariant</c> and the like - written once, for every demo page whose API section lists them.
/// <para>
/// Each page used to carry its own copy of these tables, and dozens of copies of the same table drift:
/// some lost members the enum gained, some kept placeholder descriptions, and the same type was
/// anchored under three different ids. A page now names the table in its <c>componentSubEnums</c>
/// list instead (<c>DemoSharedEnums.BitColor()</c>), which is also all the MCP server reads it from.
/// </para>
/// <para>
/// Nothing in a table is written here but its anchor id. The members and their values are read off the
/// enum itself, in the order it declares them, and the prose is the enum's own XML documentation - what
/// IntelliSense and the MCP server's <c>GetBitBlazorUIType</c> say about the same member - which
/// <c>MSBuild/DemoSharedEnumDocs.targets</c> writes into the generated half of this class before every
/// compile, since a WebAssembly page has no XML documentation to read at runtime. A shared table names its
/// enum in that file too.
/// </para>
/// <para>
/// A table is only shared while what it says holds on every page that shows it. A page whose members
/// mean something of their own there - BitLoading's sizes are pixel sizes, BitNavBar's alignments
/// spread its own items - keeps its own table, under the same anchor id. What a page may still adjust
/// is the line above the table, through <c>description</c> (every factory takes one), and the members
/// it actually supports, through <see cref="Only"/>.
/// </para>
/// <para>
/// Every call builds a new instance, so nothing a page does to its list reaches another page.
/// </para>
/// </summary>
public static partial class DemoSharedEnums
{
    public static ComponentSubEnum BitAlignment(string? description = null) => Table<Library.BitAlignment>("alignment-enum", description);

    public static ComponentSubEnum BitButtonType(string? description = null) => Table<Library.BitButtonType>("button-type-enum", description);

    public static ComponentSubEnum BitColor(string? description = null) => Table<Library.BitColor>("color-enum", description);

    public static ComponentSubEnum BitColorKind(string? description = null) => Table<Library.BitColorKind>("color-kind-enum", description);

    public static ComponentSubEnum BitDropDirection(string? description = null) => Table<Library.BitDropDirection>("drop-direction-enum", description);

    public static ComponentSubEnum BitEnterKeyHint(string? description = null) => Table<Library.BitEnterKeyHint>("enter-key-hint-enum", description);

    public static ComponentSubEnum BitImageLoading(string? description = null) => Table<Library.BitImageLoading>("image-loading-enum", description);

    public static ComponentSubEnum BitInputMode(string? description = null) => Table<Library.BitInputMode>("input-mode-enum", description);

    public static ComponentSubEnum BitInputType(string? description = null) => Table<Library.BitInputType>("input-type-enum", description);

    public static ComponentSubEnum BitLineStyle(string? description = null) => Table<Library.BitLineStyle>("line-style-enum", description);

    public static ComponentSubEnum BitLinkRels(string? description = null) => Table<Library.BitLinkRels>("link-rels-enum", description);

    public static ComponentSubEnum BitNavAriaCurrent(string? description = null) => Table<Library.BitNavAriaCurrent>("nav-aria-current-enum", description);

    public static ComponentSubEnum BitNavItemTemplateRenderMode(string? description = null) => Table<Library.BitNavItemTemplateRenderMode>("nav-item-template-render-mode-enum", description);

    public static ComponentSubEnum BitNavMatch(string? description = null) => Table<Library.BitNavMatch>("nav-match-enum", description);

    public static ComponentSubEnum BitNavMode(string? description = null) => Table<Library.BitNavMode>("nav-mode-enum", description);

    public static ComponentSubEnum BitNavRenderType(string? description = null) => Table<Library.BitNavRenderType>("nav-render-type-enum", description);

    public static ComponentSubEnum BitPlacement(string? description = null) => Table<Library.BitPlacement>("placement-enum", description);

    public static ComponentSubEnum BitPoliteness(string? description = null) => Table<Library.BitPoliteness>("politeness-enum", description);

    public static ComponentSubEnum BitPosition(string? description = null) => Table<Library.BitPosition>("position-enum", description);

    public static ComponentSubEnum BitSelectionMode(string? description = null) => Table<Library.BitSelectionMode>("selection-mode-enum", description);

    public static ComponentSubEnum BitShape(string? description = null) => Table<Library.BitShape>("shape-enum", description);

    public static ComponentSubEnum BitSize(string? description = null) => Table<Library.BitSize>("size-enum", description);

    public static ComponentSubEnum BitTimeFormat(string? description = null) => Table<Library.BitTimeFormat>("time-format-enum", description);

    public static ComponentSubEnum BitVariant(string? description = null) => Table<Library.BitVariant>("variant-enum", description);

    /// <summary>
    /// Narrows a shared table down to the members a component actually supports, in the table's own
    /// order - BitPagination renders only the eight general colors of <c>BitColor</c>. A name that is not
    /// a member of the table throws, and so does naming none at all, so a typo, a renamed member or an
    /// empty list cannot quietly empty a page's table.
    /// </summary>
    public static ComponentSubEnum Only(this ComponentSubEnum subEnum, params string[] names)
    {
        if (names.Length == 0)
            throw new ArgumentException($"Name at least one member of {subEnum.Name} to keep.", nameof(names));

        var unknown = names.Except(subEnum.Items.Select(i => i.Name)).ToArray();

        if (unknown.Length > 0)
            throw new ArgumentException($"{string.Join(", ", unknown)} is not a member of {subEnum.Name}.", nameof(names));

        subEnum.Items = [.. subEnum.Items.Where(i => names.Contains(i.Name))];

        return subEnum;
    }

    /// <summary>
    /// Builds the table of <typeparamref name="TEnum"/>: one row per member, in the order the enum declares
    /// them and with its own value, each described by its XML documentation. A summary missing from the
    /// documentation leaves its cell empty rather than throwing, so a doc gap costs a page one sentence
    /// instead of the whole page; <c>DemoSharedEnumsTests</c> is what fails on it.
    /// </summary>
    private static ComponentSubEnum Table<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TEnum>(string id, string? description) where TEnum : struct, Enum
    {
        var name = typeof(TEnum).Name;

        return new()
        {
            Id = id,
            Name = name,
            Description = description ?? Summary(name),
            Items =
            [
                .. typeof(TEnum).GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => new ComponentEnumItem
                {
                    Name = f.Name,
                    Value = Convert.ToInt64(f.GetValue(null), CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
                    Description = Summary($"{name}.{f.Name}"),
                })
            ]
        };
    }

    private static string Summary(string key) => Summaries.TryGetValue(key, out var summary) ? summary : string.Empty;
}

using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>The built-in block types, and the props each one reads.</summary>
public static class BitBlockTypes
{
    /// <summary>A paragraph of formatted text. No props.</summary>
    public static BitBlockType Paragraph { get; } = new()
    {
        Name = "paragraph",
        Kind = BitBlockKind.Text,
        SlashLabel = "Text",
        SlashKeywords = "paragraph plain"
    };

    /// <summary>
    /// A heading. Prop <c>level</c>: <c>1</c> to <c>6</c>; any other value reads as <c>2</c>.
    /// </summary>
    public static BitBlockType Heading { get; } = new()
    {
        Name = "heading",
        Kind = BitBlockKind.Text,
        SlashLabel = "Heading",
        SlashKeywords = "title h1 h2 h3 h4 h5 h6"
    };

    /// <summary>
    /// One item of a list. Consecutive items of one style are one list, and an item nests the items of a list inside it.
    /// Props: <c>style</c> - <c>bullet</c> (the default), <c>ordered</c> or <c>task</c>; <c>checked</c> - <c>true</c> for a
    /// ticked task.
    /// </summary>
    public static BitBlockType ListItem { get; } = new()
    {
        Name = "listItem",
        Kind = BitBlockKind.Text,
        Nests = true,
        SlashLabel = "List",
        SlashKeywords = "bullet ordered numbered task todo checklist"
    };

    /// <summary>A quoted paragraph. No props.</summary>
    public static BitBlockType Quote { get; } = new()
    {
        Name = "quote",
        Kind = BitBlockKind.Text,
        SlashLabel = "Quote",
        SlashKeywords = "blockquote citation"
    };

    /// <summary>A block of code, as plain text. Prop <c>language</c>: the language it is written in, if any.</summary>
    public static BitBlockType Code { get; } = new()
    {
        Name = "code",
        Kind = BitBlockKind.Code,
        SlashLabel = "Code",
        SlashKeywords = "pre snippet fence"
    };

    /// <summary>
    /// An image. Props: <c>src</c>, <c>alt</c>, <c>width</c> (in pixels; HTML and JSON only, since Markdown has no way to
    /// say it) and <c>caption</c>.
    /// </summary>
    public static BitBlockType Image { get; } = new()
    {
        Name = "image",
        Kind = BitBlockKind.Atom,
        SlashLabel = "Image",
        SlashKeywords = "picture photo"
    };

    /// <summary>A horizontal rule between sections. No props.</summary>
    public static BitBlockType Divider { get; } = new()
    {
        Name = "divider",
        Kind = BitBlockKind.Atom,
        SlashLabel = "Divider",
        SlashKeywords = "separator rule hr line"
    };

    /// <summary>Every built-in type, in the order the slash menu offers them.</summary>
    public static IReadOnlyList<BitBlockType> All { get; } = [Paragraph, Heading, ListItem, Quote, Code, Image, Divider];

    /// <summary>
    /// The built-in types with <paramref name="types"/> added after them, by name: a later type replaces an earlier one of
    /// the same name, so an app's type can stand in for a built-in.
    /// </summary>
    internal static Dictionary<string, BitBlockType> Resolve(IEnumerable<BitBlockType>? types)
    {
        var registry = new Dictionary<string, BitBlockType>(StringComparer.Ordinal);
        foreach (var type in All) registry[type.Name] = type;

        if (types is not null)
        {
            foreach (var type in types)
            {
                if (type is not null) registry[type.Name] = type;
            }
        }

        return registry;
    }

    /// <summary>
    /// The type a block of <paramref name="name"/> is exported as: the registered one when it exports itself in that format, else the
    /// built-in of that name (an app's type that only replaces a built-in's view still exports as the built-in), else the
    /// registered one, else null for a type nobody registered.
    /// </summary>
    internal static BitBlockType? ExportType(string name, Dictionary<string, BitBlockType> registry, Func<BitBlockType, BitBlockExport?> export)
    {
        registry.TryGetValue(name, out var type);
        if (type is not null && export(type) is not null) return type;

        return All.FirstOrDefault(t => t.Name == name) ?? type;
    }

    // A heading's level, read leniently: anything but 1-6 is 2.
    internal static int HeadingLevel(BitBlock block)
        => int.TryParse(block.GetProp("level"), NumberStyles.None, CultureInfo.InvariantCulture, out var level) && level is >= 1 and <= 6
            ? level
            : 2;

    // A list item's style, read leniently: anything but ordered or task is bullet.
    internal static string ListStyle(BitBlock block)
        => block.GetProp("style") switch
        {
            "ordered" => "ordered",
            "task" => "task",
            _ => "bullet"
        };

    internal static bool IsChecked(BitBlock block) => block.GetProp("checked") == "true";
}

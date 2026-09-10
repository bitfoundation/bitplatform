// Optional-content (/OCProperties) enumeration: the layers a document declares and
// whether its default configuration shows each of them.

namespace Bit.BlazorUI;

internal static class BitPdfOptionalContent
{
    public static IReadOnlyList<BitPdfLayer> Build(IBitPdfXRef xref, BitPdfDict catalog)
    {
        if (xref.FetchIfRef(catalog.GetRaw("OCProperties")) is not BitPdfDict properties
            || xref.FetchIfRef(properties.GetRaw("OCGs")) is not List<object?> groups)
        {
            return Array.Empty<BitPdfLayer>();
        }

        // The default configuration's /OFF lists the groups that start hidden; a
        // /BaseState of /OFF inverts that, with /ON naming the exceptions.
        var off = new HashSet<string>(StringComparer.Ordinal);
        var on = new HashSet<string>(StringComparer.Ordinal);
        bool hiddenByDefault = false;
        if (xref.FetchIfRef(properties.GetRaw("D")) is BitPdfDict config)
        {
            hiddenByDefault = BitPdfPrimitives.IsName(config.Get("BaseState"), "OFF");
            Collect(xref, config.GetRaw("OFF"), off);
            Collect(xref, config.GetRaw("ON"), on);
        }

        var layers = new List<BitPdfLayer>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in groups)
        {
            if (item is not BitPdfRef reference) continue;

            string id = reference.ToRefString();
            if (seen.Add(id) is false) continue;

            string? name = xref.FetchIfRef(item) is BitPdfDict group
                ? (xref.FetchIfRef(group.GetRaw("Name")) as BitPdfString)?.AsText()
                : null;

            layers.Add(new BitPdfLayer
            {
                Id = id,
                Name = string.IsNullOrWhiteSpace(name) ? id : name,
                VisibleByDefault = hiddenByDefault ? on.Contains(id) : off.Contains(id) is false,
            });
        }
        return layers;
    }

    private static void Collect(IBitPdfXRef xref, object? raw, HashSet<string> into)
    {
        if (xref.FetchIfRef(raw) is not List<object?> list) return;

        foreach (var item in list)
        {
            if (item is BitPdfRef reference)
            {
                into.Add(reference.ToRefString());
            }
        }
    }
}

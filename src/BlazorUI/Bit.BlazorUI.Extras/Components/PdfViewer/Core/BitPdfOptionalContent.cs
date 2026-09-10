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

        var hidden = DefaultHiddenIds(xref, catalog);

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
                VisibleByDefault = hidden.Contains(id) is false,
            });
        }
        return layers;
    }

    /// <summary>
    /// The groups the document's default configuration starts hidden, by reference
    /// string. This is the one place that reading of <c>/OCProperties /D</c> lives:
    /// the renderer asks it too, so a page rendered before the layer list exists
    /// hides exactly what the layer list will say is hidden.
    /// </summary>
    public static HashSet<string> DefaultHiddenIds(IBitPdfXRef xref, BitPdfDict catalog)
    {
        var hidden = new HashSet<string>(StringComparer.Ordinal);

        if (xref.FetchIfRef(catalog.GetRaw("OCProperties")) is not BitPdfDict properties
            || xref.FetchIfRef(properties.GetRaw("D")) is not BitPdfDict config)
        {
            return hidden;
        }

        // The default configuration's /OFF lists the groups that start hidden; a
        // /BaseState of /OFF inverts that, with /ON naming the exceptions.
        if (BitPdfPrimitives.IsName(config.Get("BaseState"), "OFF"))
        {
            var on = new HashSet<string>(StringComparer.Ordinal);
            Collect(xref, config.GetRaw("ON"), on);
            foreach (var item in xref.FetchIfRef(properties.GetRaw("OCGs")) as List<object?> ?? [])
            {
                if (item is BitPdfRef reference && on.Contains(reference.ToRefString()) is false)
                {
                    hidden.Add(reference.ToRefString());
                }
            }
        }
        else
        {
            Collect(xref, config.GetRaw("OFF"), hidden);
        }

        ApplyViewUsage(xref, config, hidden);
        return hidden;
    }

    /// <summary>
    /// Applies the configuration's <c>/AS</c> usage-application rules for the /View
    /// event: a group whose <c>/Usage /View /ViewState</c> says OFF is hidden on
    /// screen whatever /BaseState, /ON and /OFF said, which is how a document ships
    /// e.g. a print-only watermark layer.
    /// </summary>
    private static void ApplyViewUsage(IBitPdfXRef xref, BitPdfDict config, HashSet<string> hidden)
    {
        if (xref.FetchIfRef(config.GetRaw("AS")) is not List<object?> applications) return;

        foreach (var entry in applications)
        {
            if (xref.FetchIfRef(entry) is not BitPdfDict application) continue;

            // /Event names the medium the rule speaks for; only the on-screen one
            // decides what this renderer draws. /Category names which usage-dict
            // entries to consult - a rule that does not consult /View has nothing
            // to say about viewing.
            if (BitPdfPrimitives.IsName(application.Get("Event"), "View") is false) continue;
            if (xref.FetchIfRef(application.GetRaw("Category")) is not List<object?> categories
                || categories.Any(c => BitPdfPrimitives.IsName(xref.FetchIfRef(c), "View")) is false)
            {
                continue;
            }

            foreach (var item in xref.FetchIfRef(application.GetRaw("OCGs")) as List<object?> ?? [])
            {
                if (item is not BitPdfRef reference
                    || xref.FetchIfRef(item) is not BitPdfDict group
                    || xref.FetchIfRef(group.GetRaw("Usage")) is not BitPdfDict usage
                    || xref.FetchIfRef(usage.GetRaw("View")) is not BitPdfDict view)
                {
                    continue;
                }

                object? state = view.Get("ViewState");
                if (BitPdfPrimitives.IsName(state, "OFF"))
                {
                    hidden.Add(reference.ToRefString());
                }
                else if (BitPdfPrimitives.IsName(state, "ON"))
                {
                    hidden.Remove(reference.ToRefString());
                }
            }
        }
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

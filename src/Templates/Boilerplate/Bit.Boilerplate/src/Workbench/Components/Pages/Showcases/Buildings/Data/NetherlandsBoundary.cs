namespace Boilerplate.Workbench.Components.Pages.Showcases.Buildings.Data;

/// <summary>The outline of the Netherlands from the embedded GeoJSON file, drawn on the map and used to keep buildings inside.</summary>
public static class NetherlandsBoundary
{
    private static readonly Lazy<(string GeoJson, double[][][][] Polygons)> boundary = new(Load);

    /// <summary>The embedded file's text, a GeoJSON feature of the country.</summary>
    public static string GeoJson => boundary.Value.GeoJson;

    /// <summary>Whether a point lies inside the country, counting edge crossings over every ring of each polygon.</summary>
    public static bool Contains(double latitude, double longitude)
    {
        foreach (var polygon in boundary.Value.Polygons)
        {
            var crossings = 0;

            foreach (var ring in polygon)
            {
                for (var i = 0; i < ring.Length - 1; i++)
                {
                    var (x1, y1) = (ring[i][0], ring[i][1]);
                    var (x2, y2) = (ring[i + 1][0], ring[i + 1][1]);

                    if ((y1 > latitude) != (y2 > latitude) && longitude < x1 + (latitude - y1) * (x2 - x1) / (y2 - y1))
                    {
                        crossings++;
                    }
                }
            }

            if (crossings % 2 == 1)
                return true;
        }

        return false;
    }

    /// <summary>A GeoJSON polygon over the region around the country with holes for it, to dim what lies outside.</summary>
    public static string BuildOutsideMask()
    {
        var holes = boundary.Value.Polygons.Select(polygon => polygon[0]);
        double[][] world = [[-30, 30], [40, 30], [40, 70], [-30, 70], [-30, 30]];

        return JsonSerializer.Serialize(new Dictionary<string, object>
        {
            ["type"] = "Feature",
            ["properties"] = new Dictionary<string, object>(),
            ["geometry"] = new Dictionary<string, object> { ["type"] = "Polygon", ["coordinates"] = new[] { world }.Concat(holes).ToArray() }
        });
    }

    /// <summary>Reads the embedded file once: its text, and its polygons as rings of longitude and latitude pairs.</summary>
    private static (string, double[][][][]) Load()
    {
        using var stream = typeof(NetherlandsBoundary).Assembly.GetManifestResourceStream("Boilerplate.Workbench.Components.Pages.Showcases.Buildings.Data.netherlands.geojson")!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();

        using var document = JsonDocument.Parse(json);
        var coordinates = document.RootElement.GetProperty("geometry").GetProperty("coordinates");

        var polygons = coordinates.EnumerateArray()
            .Select(polygon => polygon.EnumerateArray()
                .Select(ring => ring.EnumerateArray()
                    .Select(point => new[] { point[0].GetDouble(), point[1].GetDouble() })
                    .ToArray())
                .ToArray())
            .ToArray();

        return (json, polygons);
    }
}

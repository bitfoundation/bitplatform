namespace Boilerplate.Workbench.Components.Pages.Showcase.Buildings.Data;

public static class NetherlandsBoundary
{
    private static readonly Lazy<(string GeoJson, double[][][][] Polygons)> boundary = new(Load);

    public static string GeoJson => boundary.Value.GeoJson;

    public static (double South, double West, double North, double East) Bounds { get; } = (50.70, 3.30, 53.60, 7.25);

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

    private static (string, double[][][][]) Load()
    {
        using var stream = typeof(NetherlandsBoundary).Assembly.GetManifestResourceStream("Boilerplate.Workbench.Components.Pages.Showcase.Buildings.Data.netherlands.geojson")!;
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

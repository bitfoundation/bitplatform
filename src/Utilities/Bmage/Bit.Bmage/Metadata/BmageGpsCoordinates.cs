namespace Bit.Bmage;

/// <summary>GPS coordinates decoded from EXIF.</summary>
public readonly record struct BmageGpsCoordinates(double Latitude, double Longitude, double? Altitude)
{
    public override string ToString() => string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{Latitude:0.######}, {Longitude:0.######}{(Altitude is { } a ? $", {a:0.#}m" : "")}");
}

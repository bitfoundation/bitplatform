namespace Boilerplate.Workbench.Components.Pages.Showcases.Buildings.Data;

public sealed class Building
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Name { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public DutchProvince Province { get; set; }

    public BuildingType Type { get; set; }

    public int? Year { get; set; }

    public double? Height { get; set; }

    public int? Floors { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public Building Clone() => (Building)MemberwiseClone();
}

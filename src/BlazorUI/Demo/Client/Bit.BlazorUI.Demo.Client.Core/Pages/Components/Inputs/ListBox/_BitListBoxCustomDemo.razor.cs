using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ListBox;

public partial class _BitListBoxCustomDemo
{
    public class ListBoxValidationModel
    {
        [Required(ErrorMessage = "Pick a fruit.")]
        public string? Fruit { get; set; }

        [MinLength(2, ErrorMessage = "Pick at least two fruits.")]
        public IEnumerable<string?>? Fruits { get; set; } = [];

        public override string ToString() => $"{Fruit} / {string.Join(", ", Fruits ?? [])}";
    }



    private string? basicValue;
    private IEnumerable<string?>? multiValues;
    private string? countryValue;
    private string? eventsLog;

    private readonly ListBoxValidationModel validationModel = new();
    private string? validationResult;

    private void HandleOnSelectItem(Produce item) => eventsLog = $"Selected {item.Name}";

    private void HandleOnDeselectItem(Produce item) => eventsLog = $"Deselected {item.Name}";

    private static readonly BitListBoxNameSelectors<Produce, string> nameSelectors = new()
    {
        Text = { Name = nameof(Produce.Name) },
        Value = { Name = nameof(Produce.Code) },
        Description = { Name = nameof(Produce.Note) },
        IconName = { Name = nameof(Produce.Symbol) },
        Icon = { Selector = p => p.Glyph },
        IsDisabled = { Name = nameof(Produce.OutOfSeason) },
        Class = { Name = nameof(Produce.Css) },
        Style = { Name = nameof(Produce.Inline) },
    };

    private readonly List<Produce> basicItems =
    [
        new() { Name = "Apple", Code = "apple" },
        new() { Name = "Banana", Code = "banana" },
        new() { Name = "Cherry", Code = "cherry" },
        new() { Name = "Grape", Code = "grape" },
        new() { Name = "Orange", Code = "orange" }
    ];

    private readonly List<Produce> iconItems =
    [
        new() { Name = "Standard", Note = "3 to 5 days, free", Symbol = BitIconName.Car, Code = "standard" },
        new() { Name = "Express", Note = "Next day, $9.99", Symbol = BitIconName.Airplane, Code = "express" },
        new() { Name = "Pick up", Note = "From a store near you", Symbol = BitIconName.Shop, Code = "pickup" }
    ];

    private readonly List<Produce> disabledItems =
    [
        new() { Name = "Apple", Code = "apple" },
        new() { Name = "Banana", Code = "banana", OutOfSeason = true },
        new() { Name = "Cherry", Code = "cherry" },
        new() { Name = "Grape", Code = "grape", OutOfSeason = true },
        new() { Name = "Orange", Code = "orange" }
    ];

    private readonly List<Produce> countryItems =
    [
        .. new[]
        {
            "Argentina", "Australia", "Austria", "Belgium", "Brazil", "Canada", "Chile", "China", "Denmark", "Egypt",
            "Finland", "France", "Germany", "Greece", "India", "Indonesia", "Iran", "Ireland", "Italy", "Japan",
            "Mexico", "Netherlands", "Norway", "Poland", "Portugal", "Spain", "Sweden", "Turkey"
        }.Select(name => new Produce { Name = name, Code = name })
    ];

    private readonly List<Produce> externalIconItems1 =
    [
        new() { Name = "Car", Glyph = "fa-solid fa-car", Code = "car" },
        new() { Name = "Train", Glyph = BitIconInfo.Fa("solid train"), Code = "train" },
        new() { Name = "Plane", Glyph = BitIconInfo.Fa("solid plane"), Code = "plane" }
    ];

    private readonly List<Produce> externalIconItems2 =
    [
        new() { Name = "Bike", Glyph = BitIconInfo.Bi("bicycle"), Code = "bike" },
        new() { Name = "Bus", Glyph = BitIconInfo.Bi("bus-front"), Code = "bus" },
        new() { Name = "Taxi", Glyph = BitIconInfo.Bi("taxi-front"), Code = "taxi" }
    ];

    private readonly List<Produce> styleClassItems =
    [
        new() { Name = "Apple", Code = "apple", Inline = "color: seagreen;" },
        new() { Name = "Banana", Code = "banana", Css = "custom-item" },
        new() { Name = "Cherry", Code = "cherry" }
    ];

    private readonly List<Produce> rtlItems =
    [
        new() { Name = "سیب", Code = "apple" },
        new() { Name = "موز", Code = "banana" },
        new() { Name = "گیلاس", Code = "cherry" }
    ];
}

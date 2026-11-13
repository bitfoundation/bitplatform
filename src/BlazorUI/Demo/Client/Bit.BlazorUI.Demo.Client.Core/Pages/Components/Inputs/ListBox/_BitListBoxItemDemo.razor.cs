using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ListBox;

public partial class _BitListBoxItemDemo
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

    private void HandleOnSelectItem(BitListBoxItem<string> item) => eventsLog = $"Selected {item.Text}";

    private void HandleOnDeselectItem(BitListBoxItem<string> item) => eventsLog = $"Deselected {item.Text}";

    private readonly List<BitListBoxItem<string>> basicItems =
    [
        new() { Text = "Apple", Value = "apple" },
        new() { Text = "Banana", Value = "banana" },
        new() { Text = "Cherry", Value = "cherry" },
        new() { Text = "Grape", Value = "grape" },
        new() { Text = "Orange", Value = "orange" }
    ];

    private readonly List<BitListBoxItem<string>> iconItems =
    [
        new() { Text = "Standard", Description = "3 to 5 days, free", IconName = BitIconName.Car, Value = "standard" },
        new() { Text = "Express", Description = "Next day, $9.99", IconName = BitIconName.Airplane, Value = "express" },
        new() { Text = "Pick up", Description = "From a store near you", IconName = BitIconName.Shop, Value = "pickup" }
    ];

    private readonly List<BitListBoxItem<string>> disabledItems =
    [
        new() { Text = "Apple", Value = "apple" },
        new() { Text = "Banana", Value = "banana", IsDisabled = true },
        new() { Text = "Cherry", Value = "cherry" },
        new() { Text = "Grape", Value = "grape", IsDisabled = true },
        new() { Text = "Orange", Value = "orange" }
    ];

    private readonly List<BitListBoxItem<string>> countryItems =
    [
        .. new[]
        {
            "Argentina", "Australia", "Austria", "Belgium", "Brazil", "Canada", "Chile", "China", "Denmark", "Egypt",
            "Finland", "France", "Germany", "Greece", "India", "Indonesia", "Iran", "Ireland", "Italy", "Japan",
            "Mexico", "Netherlands", "Norway", "Poland", "Portugal", "Spain", "Sweden", "Turkey"
        }.Select(name => new BitListBoxItem<string> { Text = name, Value = name })
    ];

    private readonly List<BitListBoxItem<string>> externalIconItems1 =
    [
        new() { Text = "Car", Icon = "fa-solid fa-car", Value = "car" },
        new() { Text = "Train", Icon = BitIconInfo.Fa("solid train"), Value = "train" },
        new() { Text = "Plane", Icon = BitIconInfo.Fa("solid plane"), Value = "plane" }
    ];

    private readonly List<BitListBoxItem<string>> externalIconItems2 =
    [
        new() { Text = "Bike", Icon = BitIconInfo.Bi("bicycle"), Value = "bike" },
        new() { Text = "Bus", Icon = BitIconInfo.Bi("bus-front"), Value = "bus" },
        new() { Text = "Taxi", Icon = BitIconInfo.Bi("taxi-front"), Value = "taxi" }
    ];

    private readonly List<BitListBoxItem<string>> styleClassItems =
    [
        new() { Text = "Apple", Value = "apple", Style = "color: seagreen;" },
        new() { Text = "Banana", Value = "banana", Class = "custom-item" },
        new() { Text = "Cherry", Value = "cherry" }
    ];

    private readonly List<BitListBoxItem<string>> rtlItems =
    [
        new() { Text = "سیب", Value = "apple" },
        new() { Text = "موز", Value = "banana" },
        new() { Text = "گیلاس", Value = "cherry" }
    ];
}

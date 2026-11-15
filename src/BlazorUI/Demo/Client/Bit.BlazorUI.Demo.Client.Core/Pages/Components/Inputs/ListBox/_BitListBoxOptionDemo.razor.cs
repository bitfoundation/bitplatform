using System.ComponentModel.DataAnnotations;

namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ListBox;

public partial class _BitListBoxOptionDemo
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

    private void HandleOnSelectItem(BitListBoxOption<string> item) => eventsLog = $"Selected {item.Text}";

    private void HandleOnDeselectItem(BitListBoxOption<string> item) => eventsLog = $"Deselected {item.Text}";

    private static readonly string[] countries =
    [
        "Argentina", "Australia", "Austria", "Belgium", "Brazil", "Canada", "Chile", "China", "Denmark", "Egypt",
        "Finland", "France", "Germany", "Greece", "India", "Indonesia", "Iran", "Ireland", "Italy", "Japan",
        "Mexico", "Netherlands", "Norway", "Poland", "Portugal", "Spain", "Sweden", "Turkey"
    ];
}

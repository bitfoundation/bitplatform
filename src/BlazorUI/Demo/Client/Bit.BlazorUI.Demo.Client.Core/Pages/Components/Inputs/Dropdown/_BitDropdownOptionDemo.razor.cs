namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownOptionDemo
{
    private readonly List<BitDropdownItem<string>> basicItems = new()
    {
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits" },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { Text = "Orange", Value = "f-ora", IsEnabled = false },
        new() { Text = "Grape", Value = "f-gra" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "Vegetables" },
        new() { Text = "Broccoli", Value = "v-bro" },
        new() { Text = "Carrot", Value = "v-car" },
        new() { Text = "Lettuce", Value = "v-let" }
    };
    private readonly List<BitDropdownItem<string>> dataItems = new()
    {
        new() { ItemType = BitDropdownItemType.Header, Text = "Items" },
        new() { Text = "Item a", Value = "A", Data = new DropdownItemData { IconName = "Memo" } },
        new() { Text = "Item b", Value = "B", Data = new DropdownItemData { IconName = "Print" } },
        new() { Text = "Item c", Value = "C", Data = new DropdownItemData { IconName = "ShoppingCart" } },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "More Items" },
        new() { Text = "Item d", Value = "D", Data = new DropdownItemData { IconName = "Train" } },
        new() { Text = "Item e", Value = "E", Data = new DropdownItemData { IconName = "Repair" } },
        new() { Text = "Item f", Value = "F", Data = new DropdownItemData { IconName = "Running" } }
    };
    private readonly List<BitDropdownItem<string>> rtlItems = new()
    {
        new() { ItemType = BitDropdownItemType.Header, Text = "میوه ها" },
        new() { Text = "سیب", Value = "f-app" },
        new() { Text = "موز", Value = "f-ban" },
        new() { Text = "پرتقال", Value = "f-ora", IsEnabled = false },
        new() { Text = "انگور", Value = "f-gra" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "سیزیجات" },
        new() { Text = "کلم بروكلی", Value = "v-bro" },
        new() { Text = "هویج", Value = "v-car" },
        new() { Text = "کاهو", Value = "v-let" }
    };
    private ICollection<BitDropdownItem<string>> dropDirectionItems = default!;
    private readonly List<BitDropdownItem<string>> styleClassItems = new()
    {
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits", Style = "background-color:darkred" },
        new() { Text = "Apple", Value = "f-app", Class = "custom-fruit" },
        new() { Text = "Banana", Value = "f-ban", Class = "custom-fruit" },
        new() { Text = "Orange", Value = "f-ora", IsEnabled = false, Class = "custom-fruit" },
        new() { Text = "Grape", Value = "f-gra", Class = "custom-fruit" },
        new() { ItemType = BitDropdownItemType.Divider, Style = "padding:5px; background:darkgreen" },
        new() { ItemType = BitDropdownItemType.Header, Text = "Vegetables", Style = "background-color:darkblue" },
        new() { Text = "Broccoli", Value = "v-bro", Class = "custom-veg" },
        new() { Text = "Carrot", Value = "v-car", Class = "custom-veg" },
        new() { Text = "Lettuce", Value = "v-let", Class = "custom-veg" }
    };
    private readonly List<BitDropdownItem<string>> comboBoxItems = new()
    {
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits" },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { Text = "Orange", Value = "f-ora", IsEnabled = false },
        new() { Text = "Grape", Value = "f-gra" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "Vegetables" },
        new() { Text = "Broccoli", Value = "v-bro" },
        new() { Text = "Carrot", Value = "v-car" },
        new() { Text = "Lettuce", Value = "v-let" }
    };


    private string controlledValue = "f-app";
    private ICollection<string?> controlledValues = ["f-app", "f-ban"];

    private BitDropdownOption<string>? changedItem;
    private BitDropdownOption<string>[] changedItems = Array.Empty<BitDropdownOption<string>>();

    private BitDropdownOption<string>? selectedItem1;
    private BitDropdownOption<string>? selectedItem2;

    private string? clearValue = "f-app";
    private ICollection<string?> clearValues = ["f-app", "f-ban"];

    private string successMessage = string.Empty;
    private FormValidationDropdownModel validationModel = new();

    private string comboBoxValueSample1 = default!;
    private string comboBoxValueSample2 = default!;
    private string comboBoxValueSample3 = default!;
    private string comboBoxValueSample4 = default!;
    private ICollection<string> comboBoxValues = [];


    protected override void OnInitialized()
    {
        dropDirectionItems = Enumerable.Range(1, 15)
                                       .Select(c => new BitDropdownItem<string> { Value = c.ToString(), Text = $"Category {c}" })
                                       .ToArray();

        base.OnInitialized();
    }


    private async Task HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
        await Task.Delay(3000);
        successMessage = string.Empty;
        validationModel = new();
        StateHasChanged();
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }

    private void HandleOnDynamicAdd(BitDropdownOption<string> item)
    {
        comboBoxItems.Add(new() { Text = item.Text, Value = item.Value });
    }
}

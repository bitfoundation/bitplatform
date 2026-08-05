namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.Dropdown;

public partial class _BitDropdownOptionDemo
{
    private readonly List<BitDropdownItem<string>> basicItems =
    [
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
    ];
    private readonly List<BitDropdownItem<string>> dataItems =
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Items", Data = new DropdownItemData { IconName = "BulletedList2" }  },
        new() { Text = "Item a", Value = "A", Data = new DropdownItemData { IconName = "Memo" } },
        new() { Text = "Item b", Value = "B", Data = new DropdownItemData { IconName = "Print" } },
        new() { Text = "Item c", Value = "C", Data = new DropdownItemData { IconName = "ShoppingCart" } },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "More Items", Data = new DropdownItemData { IconName = "BulletedTreeList" }  },
        new() { Text = "Item d", Value = "D", Data = new DropdownItemData { IconName = "Train" } },
        new() { Text = "Item e", Value = "E", Data = new DropdownItemData { IconName = "Repair" } },
        new() { Text = "Item f", Value = "F", Data = new DropdownItemData { IconName = "Running" } }
    ];
    private readonly List<BitDropdownItem<string>> accentedItems =
    [
        new() { Text = "José", Value = "n-jos" },
        new() { Text = "Renée", Value = "n-ren" },
        new() { Text = "Müller", Value = "n-mul" },
        new() { Text = "Ångström", Value = "n-ang" },
        new() { Text = "Zoë", Value = "n-zoe" },
        new() { Text = "Smith", Value = "n-smi" }
    ];
    private readonly List<BitDropdownItem<string>> rtlItems =
    [
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
    ];
    private ICollection<BitDropdownItem<string>> dropDirectionItems = default!;
    private readonly List<BitDropdownItem<string>> styleClassItems =
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits", Style = "text-align: center;" },
        new() { Text = "Apple", Value = "f-app", Class = "custom-fruit" },
        new() { Text = "Banana", Value = "f-ban", Class = "custom-fruit" },
        new() { Text = "Orange", Value = "f-ora", IsEnabled = false, Class = "custom-fruit" },
        new() { Text = "Grape", Value = "f-gra", Class = "custom-fruit" },
        new() { ItemType = BitDropdownItemType.Divider, Style = "padding: 0 0.25rem;" },
        new() { ItemType = BitDropdownItemType.Header, Text = "Vegetables", Style = "text-align: center;" },
        new() { Text = "Broccoli", Value = "v-bro", Class = "custom-veg" },
        new() { Text = "Carrot", Value = "v-car", Class = "custom-veg" },
        new() { Text = "Lettuce", Value = "v-let", Class = "custom-veg" }
    ];
    private readonly List<BitDropdownItem<string>> comboBoxItems =
    [
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
    ];


    private readonly List<BitDropdownItem<string>> groupedItems =
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Fruits" },
        new() { Text = "Apple", Value = "f-app" },
        new() { Text = "Banana", Value = "f-ban" },
        new() { Text = "Orange", Value = "f-ora" },
        new() { Text = "Grape", Value = "f-gra" },
        new() { Text = "Mango", Value = "f-man" },
        new() { Text = "Peach", Value = "f-pea" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "Vegetables" },
        new() { Text = "Broccoli", Value = "v-bro" },
        new() { Text = "Carrot", Value = "v-car" },
        new() { Text = "Lettuce", Value = "v-let" },
        new() { Text = "Potato", Value = "v-pot" },
        new() { Text = "Tomato", Value = "v-tom" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "Grains" },
        new() { Text = "Barley", Value = "g-bar" },
        new() { Text = "Oat", Value = "g-oat" },
        new() { Text = "Rice", Value = "g-ric" },
        new() { Text = "Wheat", Value = "g-whe" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "Nuts" },
        new() { Text = "Almond", Value = "n-alm" },
        new() { Text = "Cashew", Value = "n-cas" },
        new() { Text = "Walnut", Value = "n-wal" }
    ];

    private readonly List<BitDropdownItem<string>> localizedItems =
    [
        new() { ItemType = BitDropdownItemType.Header, Text = "Früchte" },
        new() { Text = "Apfel", Value = "f-app" },
        new() { Text = "Banane", Value = "f-ban" },
        new() { Text = "Orange", Value = "f-ora" },
        new() { Text = "Traube", Value = "f-gra" },
        new() { ItemType = BitDropdownItemType.Divider },
        new() { ItemType = BitDropdownItemType.Header, Text = "Gemüse" },
        new() { Text = "Brokkoli", Value = "v-bro" },
        new() { Text = "Karotte", Value = "v-car" }
    ];

    private IEnumerable<string?> localizationValues = ["f-app", "f-ban", "v-bro"];

    private IEnumerable<string?> selectAllValues = [];
    private IEnumerable<string?> maxSelectedValues = [];

    private bool isDropdownOpen;
    private int selectItemCounter;

    private string controlledValue = "f-app";
    private IEnumerable<string?> controlledValues = ["f-app", "f-ban"];

    private string? changedValue;
    private IEnumerable<string> changedValues = [];

    private BitDropdownOption<string>? selectedItem1;
    private BitDropdownOption<string>? selectedItem2;
    private BitDropdownOption<string>? pickedItem;
    private BitDropdownOption<string>? deselectedItem;

    private string? clearValue = "f-app";
    private IEnumerable<string?> clearValues = ["f-app", "f-ban"];
    private int clearCounter;

    private string calloutState = "closed";
    private string focusState = "blurred";

    private string? comparerValue = "F-APP";

    private bool isLoadingItems;
    private List<BitDropdownItem<string>> delayedItems = [];

    private string? immediateSearchValue;
    private string? debouncedSearchValue;

    private string successMessage = string.Empty;
    private FormValidationDropdownModel validationModel = new();

    private string? closeOnSelectValue;
    private IEnumerable<string?> closeOnSelectValues = [];

    private char[] tokenSeparators = [',', ';'];
    private IEnumerable<string?> tokenSeparatorValues = [];

    private string? openOnFocusValue;

    private string? autoSelectValue;

    private string comboBoxValueSample1 = default!;
    private string comboBoxValueSample2 = default!;
    private string comboBoxValueSample3 = default!;
    private string comboBoxValueSample4 = default!;
    private IEnumerable<string> comboBoxValues1 = [];
    private IEnumerable<string> comboBoxValues2 = [];
    private IEnumerable<string> comboBoxValues3 = [];


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

    private void HandleOnCalloutOpen() => calloutState = "opened";

    private void HandleOnCalloutClose() => calloutState = "closed";

    private void HandleOnFocusIn() => focusState = "focused";

    private void HandleOnFocusOut() => focusState = "blurred";

    private async Task LoadDelayedItems()
    {
        isLoadingItems = true;
        delayedItems = [];

        await Task.Delay(2000);

        delayedItems = [.. basicItems];
        isLoadingItems = false;
    }
}

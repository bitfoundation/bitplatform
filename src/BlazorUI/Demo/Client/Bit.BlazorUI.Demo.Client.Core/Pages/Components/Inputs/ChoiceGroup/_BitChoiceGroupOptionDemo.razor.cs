namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Inputs.ChoiceGroup;

public partial class _BitChoiceGroupOptionDemo
{
    private string oneWayValue = "A";
    private string twoWayValue = "A";
    private string readOnlyValue = "A";

    private string? changedValue;
    private string? clickedOption;
    private string? focusedOption;
    private string? blurredOption;

    private int dynamicCounter = 3;
    private string? dynamicValue = "1";
    private List<(string Text, string Value)> dynamicOptions =
    [
        ("Option 1", "1"),
        ("Option 2", "2"),
        ("Option 3", "3")
    ];
    private string itemTemplateValue = "Day";
    private string itemTemplateValue2 = "Day";
    private string itemLabelTemplateValue = "Day";
    public ChoiceGroupValidationModel validationModel = new();
    public string? successMessage;


    private void AddDynamicOption()
    {
        dynamicCounter++;
        dynamicOptions = [.. dynamicOptions, ($"Option {dynamicCounter}", $"{dynamicCounter}")];
    }

    private void RemoveDynamicOption()
    {
        if (dynamicOptions.Count <= 1) return;

        dynamicOptions = [.. dynamicOptions.Take(dynamicOptions.Count - 1)];
    }

    private void ReverseDynamicOptions()
    {
        dynamicOptions = [.. Enumerable.Reverse(dynamicOptions)];
    }

    private void HandleValidSubmit()
    {
        successMessage = "Form Submitted Successfully!";
    }

    private void HandleInvalidSubmit()
    {
        successMessage = string.Empty;
    }
}

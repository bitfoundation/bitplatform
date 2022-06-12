using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bit.Client.Web.BlazorUI;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Bit.Client.Web.BlazorUI;

public partial class BitChoiceGroup
{
    private bool isRequired;

    /// <summary>
    /// If true, an option must be selected in the ChoiceGroup.
    /// </summary>
    [Parameter]
    public bool IsRequired
    {
        get => isRequired;
        set
        {
            isRequired = value;
            ClassBuilder.Reset();
        }
    }

    /// <summary>
    /// List of options, each of which is a selection in the ChoiceGroup.
    /// </summary>
#pragma warning disable CA1002 // Do not expose generic lists
#pragma warning disable CA2227 // Collection properties should be read only
    [Parameter] public List<BitChoiceGroupOption> Options { get; set; } = new();
#pragma warning restore CA2227 // Collection properties should be read only
#pragma warning restore CA1002 // Do not expose generic lists

    /// <summary>
    /// Name of ChoiceGroup, this name is used to group each option into the same logical ChoiceGroup.
    /// </summary>
    [Parameter] public string Name { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Default selected key for ChoiceGroup.
    /// </summary>
    [Parameter] public string? DefaultValue { get; set; }

    /// <summary>
    /// ID of an element to use as the aria label for this ChoiceGroup.
    /// </summary>
    [Parameter] public string? AriaLabelledBy { get; set; }

    /// <summary>
    /// Descriptive label for the ChoiceGroup.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// Used to customize the label for the ChoiceGroup.
    /// </summary>
    [Parameter] public RenderFragment? LabelFragment { get; set; }

    protected override string RootElementClass => "bit-chg";

    protected override void RegisterComponentClasses()
    {
        ClassBuilder.Register(() => IsEnabled && Options.Any(o => o.IsEnabled) && IsRequired
                                   ? $"{RootElementClass}-required-{VisualClassRegistrar()}" : string.Empty);

        ClassBuilder.Register(() => ValueInvalid is true
                                   ? $"{RootElementClass}-invalid-{VisualClassRegistrar()}" : string.Empty);
    }

    protected override async Task OnInitializedAsync()
    {
        if (DefaultValue.HasValue() && Options.Any(o => o.Value == DefaultValue))
        {
            CurrentValue = DefaultValue;
        }

        await base.OnInitializedAsync().ConfigureAwait(false);
    }

    private string GetGroupLabelId() => $"ChoiceGroupLabel{UniqueId}";

    private string GetGroupAriaLabelledBy() => AriaLabelledBy ?? GetGroupLabelId();

    private string GetOptionInputId(BitChoiceGroupOption option) =>
        option.Id ?? $"ChoiceGroupOptionInput{UniqueId}-{option.Value}";

    private string GetOptionLabelId(BitChoiceGroupOption option) =>
        option.LabelId ?? $"ChoiceGroupOptionLabel{UniqueId}-{option.Value}";

    private string GetOptionAriaLabel(BitChoiceGroupOption option) =>
      option.AriaLabel ?? AriaLabel ?? string.Empty;

    private bool GetOptionIsChecked(BitChoiceGroupOption option) =>
        CurrentValue.HasValue() && CurrentValue == option.Value;

    private string? GetOptionImageSrc(BitChoiceGroupOption option) =>
        GetOptionIsChecked(option) && option.SelectedImageSrc.HasValue()
        ? option.SelectedImageSrc
        : option.ImageSrc;

    private static string GetOptionLabelClassName(BitChoiceGroupOption option) =>
        option.ImageSrc.HasValue() || option.IconName is not null
        ? "bit-chgo-lbl-with-img"
        : "bit-chgo-lbl";

    private static string GetOptionImageSizeStyle(BitChoiceGroupOption option) => option.ImageSize is not null
            ? $"width:{option.ImageSize.Value.Width}px; height:{option.ImageSize.Value.Height}px;"
            : string.Empty;

    private string GetOptionDivClassName(BitChoiceGroupOption option)
    {
        const string itemRootElementClass = "bit-chgo";
        StringBuilder cssClass = new(itemRootElementClass);

        if (option.IsEnabled is false || IsEnabled is false)
        {
            cssClass
               .Append(' ')
               .Append(itemRootElementClass)
               .Append("-disabled-")
               .Append(VisualClassRegistrar());
        }

        if (option.ImageSrc.HasValue() || option.IconName is not null)
        {
            cssClass
                .Append(' ')
                .Append(itemRootElementClass)
                .Append("-with-img-")
                .Append(VisualClassRegistrar());
        }

        if (GetOptionIsChecked(option))
        {
            cssClass
                .Append(' ')
                .Append(itemRootElementClass)
                .Append("-checked-")
                .Append(VisualClassRegistrar());
        }

        return cssClass.ToString();
    }

    private void HandleOptionChange(BitChoiceGroupOption option)
    {
        if (option.IsEnabled is false || IsEnabled is false) return;

        CurrentValue = option.Value;

        if (option.OnChange is not null)
        {
            option.OnChange.Invoke();
        }
    }

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, [MaybeNullWhen(false)] out string? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = value;
        validationErrorMessage = null;
        return true;
    }
}

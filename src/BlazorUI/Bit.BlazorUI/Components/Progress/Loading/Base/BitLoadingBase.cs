using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// The original loading css came from https://loading.io/css/
/// </summary>
public abstract class BitLoadingBase : BitComponentBase
{
    /// <summary>
    /// Custom CSS classes for different parts of the loading component.
    /// </summary>
    [Parameter] public BitLoadingClassStyles? Classes { get; set; }

    /// <summary>
    /// The general color of the loading component.
    /// </summary>
    [Parameter] public BitColor? Color { get; set; }

    /// <summary>
    /// The custom css color of the loading component.
    /// </summary>
    [Parameter] public string? CustomColor { get; set; }

    /// <summary>
    /// The custom size of the loading component in px.
    /// </summary>
    [Parameter] public int? CustomSize { get; set; }

    /// <summary>
    /// The text content of the label of the loading component.
    /// </summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>
    /// The position of the label of the loading component.
    /// </summary>
    [Parameter] public BitLabelPosition? LabelPosition { get; set; }

    /// <summary>
    /// The custom content of the label of the loading component.
    /// </summary>
    [Parameter] public RenderFragment? LabelTemplate { get; set; }

    /// <summary>
    /// The Size of the loading component.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }

    /// <summary>
    /// Custom CSS styles for different parts of the loading component.
    /// </summary>
    [Parameter] public BitLoadingClassStyles? Styles { get; set; }



    public override Task SetParametersAsync(ParameterView parameters)
    {
        var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;

        foreach (var parameter in parametersDictionary!)
        {
            switch (parameter.Key)
            {
                case nameof(Color):
                    var color = (BitColor?)parameter.Value;
                    if (Color != color) StyleBuilder.Reset();
                    Color = color;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(CustomColor):
                    var customColor = (string?)parameter.Value;
                    if (CustomColor != customColor) StyleBuilder.Reset();
                    CustomColor = customColor;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(CustomSize):
                    var customSize = (int?)parameter.Value;
                    if (CustomSize != customSize) StyleBuilder.Reset();
                    CustomSize = customSize;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Label):
                    var label = (string?)parameter.Value;
                    Label = label;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(LabelPosition):
                    var labelPosition = (BitLabelPosition?)parameter.Value;
                    if (LabelPosition != labelPosition) ClassBuilder.Reset();
                    LabelPosition = labelPosition;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(LabelTemplate):
                    var labelTemplate = (RenderFragment?)parameter.Value;
                    LabelTemplate = labelTemplate;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(Size):
                    var size = (BitSize?)parameter.Value;
                    if (Size != size) StyleBuilder.Reset();
                    Size = size;
                    parametersDictionary.Remove(parameter.Key);
                    break;
            }
        }

        // For derived components, retain the usual lifecycle with OnInit/OnParametersSet/etc.
        return base.SetParametersAsync(ParameterView.FromDictionary(parametersDictionary!));
    }



    internal new ElementClassBuilder ClassBuilder => base.ClassBuilder;

    internal new ElementStyleBuilder StyleBuilder => base.StyleBuilder;



    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => "bit-ldn");

        ClassBuilder.Register(() => LabelPosition switch
        {
            BitLabelPosition.Top => "bit-ldn-ltp",
            BitLabelPosition.Bottom => "bit-ldn-lbm",
            BitLabelPosition.Start => "bit-ldn-lst",
            BitLabelPosition.End => "bit-ldn-led",
            _ => "bit-ldn-ltp"
        });
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() =>
        {
            var color = Color switch
            {
                BitColor.Primary => "var(--bit-clr-pri)",
                BitColor.Secondary => "var(--bit-clr-sec)",
                BitColor.Tertiary => "var(--bit-clr-ter)",
                BitColor.Info => "var(--bit-clr-inf)",
                BitColor.Success => "var(--bit-clr-suc)",
                BitColor.Warning => "var(--bit-clr-wrn)",
                BitColor.SevereWarning => "var(--bit-clr-swr)",
                BitColor.Error => "var(--bit-clr-err)",
                _ => CustomColor ?? "var(--bit-clr-pri)"
            };

            return $"--bit-ldn-color: {color}";
        });

        StyleBuilder.Register(() => $"--bit-ldn-size:{GetSize()}px");
        StyleBuilder.Register(() => $"--bit-ldn-font-size:{GetFontSize()}px");
    }

    protected virtual int OriginalSize => 80;

    protected string Convert(double value)
    {
        return (value * GetSize() / OriginalSize).ToString(CultureInfo.InvariantCulture);
    }



    private int GetSize()
    {
        return Size switch
        {
            BitSize.Small => 40,
            BitSize.Medium => 64,
            BitSize.Large => 88,
            _ => CustomSize ?? 64
        };
    }

    private int GetFontSize()
    {
        return Size switch
        {
            BitSize.Small => 10,
            BitSize.Medium => 14,
            BitSize.Large => 18,
            _ => ((CustomSize / 64) ?? 1) * 14
        };
    }
}

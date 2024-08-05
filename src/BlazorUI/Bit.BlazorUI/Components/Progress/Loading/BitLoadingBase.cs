using System.Globalization;

namespace Bit.BlazorUI;

public abstract class BitLoadingBase : BitComponentBase
{
    /// <summary>
    /// Custom CSS class for the child element(s) of the loading component.
    /// </summary>
    [Parameter] public string? ChildClass { get; set; }

    /// <summary>
    /// Custom CSS style for the child element(s) of the loading component.
    /// </summary>
    [Parameter] public string? ChildStyle { get; set; }

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
    /// The Size of the loading component.
    /// </summary>
    [Parameter] public BitSize? Size { get; set; }



    public override Task SetParametersAsync(ParameterView parameters)
    {
        var parametersDictionary = parameters.ToDictionary() as Dictionary<string, object>;

        foreach (var parameter in parametersDictionary!)
        {
            switch (parameter.Key)
            {
                case nameof(ChildClass):
                    ChildClass = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
                case nameof(ChildStyle):
                    ChildStyle = (string?)parameter.Value;
                    parametersDictionary.Remove(parameter.Key);
                    break;
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
                    var dim = (int?)parameter.Value;
                    if (CustomSize != dim) StyleBuilder.Reset();
                    CustomSize = dim;
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
    }

    protected virtual int OriginalSize { get; set; } = 80;

    protected string Convert(double value)
    {
        return (value * GetSize() / OriginalSize).ToString(CultureInfo.InvariantCulture);
    }

    private int GetSize()
    {
        return Size switch
        {
            BitSize.Small => 32,
            BitSize.Medium => 64,
            BitSize.Large => 96,
            _ => CustomSize ?? 64
        };
    }
}

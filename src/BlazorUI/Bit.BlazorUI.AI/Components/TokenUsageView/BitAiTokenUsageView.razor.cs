using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Bit.BlazorUI;

/// <summary>
/// BitAiTokenUsageView is a compact readout of what a generation cost: its tokens, its money and its speed.
/// </summary>
/// <remarks>
/// Every field is optional, because providers report different subsets of them and a row of blanks is worse than a
/// short row: a figure the <see cref="Usage"/> or the <see cref="Metadata"/> does not carry is left out rather than
/// shown empty. The breakdown of the tokens (input, output, cached and reasoning) is the native tooltip of the readout,
/// and visually hidden text of it for the keyboard, touch screens and screen readers, which a tooltip never reaches.
/// </remarks>
public partial class BitAiTokenUsageView : BitAiComponentBase
{
    /// <summary>
    /// Gets or sets the cascading parameters for the token usage view component.
    /// </summary>
    /// <remarks>
    /// This property receives its value from an ancestor component via Blazor's cascading parameter mechanism.
    /// <br />
    /// The intended use is to allow shared configuration or settings (which figures are shown, and their look) to be
    /// applied to multiple token usage view components through the <see cref="BitParams"/> component.
    /// </remarks>
    [CascadingParameter(Name = BitAiTokenUsageViewParams.ParamName)]
    public BitAiTokenUsageViewParams? CascadingParameters { get; set; }



    /// <summary>
    /// Custom CSS classes for different parts of the token usage view.
    /// </summary>
    [Parameter, ResetClassBuilder]
    public BitAiTokenUsageViewClassStyles? Classes { get; set; }

    /// <summary>
    /// The generation metadata, which supplies the speed and the latency.
    /// </summary>
    [Parameter] public BitAiGenerationMetadata? Metadata { get; set; }

    /// <summary>
    /// Shows the time to the first token, when the <see cref="Metadata"/> carries it.
    /// <br />
    /// The default value is <strong>false</strong>.
    /// </summary>
    [Parameter] public bool ShowLatency { get; set; }

    /// <summary>
    /// Shows the throughput in tokens per second, when the <see cref="Metadata"/> carries it.
    /// <br />
    /// The default value is <strong>true</strong>.
    /// </summary>
    [Parameter] public bool ShowSpeed { get; set; } = true;

    /// <summary>
    /// Custom CSS styles for different parts of the token usage view.
    /// </summary>
    [Parameter, ResetStyleBuilder]
    public BitAiTokenUsageViewClassStyles? Styles { get; set; }

    /// <summary>
    /// The usage to show.
    /// </summary>
    [Parameter, EditorRequired]
    public BitAiTokenUsage Usage { get; set; }



    protected override string RootElementClass => "bit-aituv";

    protected override void RegisterCssClasses()
    {
        ClassBuilder.Register(() => Classes?.Root);
    }

    protected override void RegisterCssStyles()
    {
        StyleBuilder.Register(() => Styles?.Root);
    }

    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BitAiTokenUsageViewParams))]
    protected override void OnParametersSet()
    {
        CascadingParameters?.UpdateParameters(this);

        base.OnParametersSet();
    }



    // One line per kind of token the provider reported, for the tooltip of the readout and its hidden text.
    private List<string> _Breakdown
    {
        get
        {
            var parts = new List<string>(4);
            if (Usage.InputTokens is { } input) parts.Add(Strings.UsageLine(Strings.InputTokens, input));
            if (Usage.OutputTokens is { } output) parts.Add(Strings.UsageLine(Strings.OutputTokens, output));
            if (Usage.CachedInputTokens is { } cached) parts.Add(Strings.UsageLine(Strings.CachedTokens, cached));
            if (Usage.ReasoningTokens is { } reasoning) parts.Add(Strings.UsageLine(Strings.ReasoningTokens, reasoning));
            return parts;
        }
    }

    private string FormatCost(decimal cost)
    {
        var symbol = Usage.Currency switch
        {
            null or "USD" => "$",
            "EUR" => "€",
            "GBP" => "£",
            _ => Usage.Currency + " ",
        };

        // A cost is written to the minor unit of its currency - the cent, none for the yen, the fils for the dinar - and a
        // fraction of that unit, which is what most single requests cost, with two decimals more: at the precision of the
        // currency it would print as nothing.
        var decimals = MinorUnitDigits(Usage.Currency);
        if (cost != 0 && Math.Abs(cost) < MinorUnit(decimals)) decimals += 2;

        return symbol + cost.ToString("F" + decimals.ToString(CultureInfo.InvariantCulture), CultureInfo.CurrentCulture);
    }

    // The ISO 4217 minor unit of the currencies that do not count in hundredths.
    private static int MinorUnitDigits(string? currency) => currency?.ToUpperInvariant() switch
    {
        "BIF" or "CLP" or "DJF" or "GNF" or "ISK" or "JPY" or "KMF" or "KRW" or "PYG" or "RWF" or "UGX" or "UYI" or "VND"
            or "VUV" or "XAF" or "XOF" or "XPF" => 0,
        "BHD" or "IQD" or "JOD" or "KWD" or "LYD" or "OMR" or "TND" => 3,
        "CLF" or "UYW" => 4,
        _ => 2,
    };

    private static decimal MinorUnit(int digits) => digits switch { 0 => 1m, 3 => 0.001m, 4 => 0.0001m, _ => 0.01m };
}

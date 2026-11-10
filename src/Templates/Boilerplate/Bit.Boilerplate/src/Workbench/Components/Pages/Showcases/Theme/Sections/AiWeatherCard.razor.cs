namespace Boilerplate.Workbench.Components.Pages.Showcases.Theme.Sections;

/// <summary>
/// The card the AI section draws through generative UI: a city's weather now and for the day. Its parameters are bound
/// from the props of the reply's JSON, under the name <see cref="AiSamples.WeatherCardName"/>.
/// </summary>
public partial class AiWeatherCard
{
    [Parameter] public string? City { get; set; }

    /// <summary>The temperature now, in degrees Celsius.</summary>
    [Parameter] public int Temperature { get; set; }

    [Parameter] public string? Condition { get; set; }

    /// <summary>The sky the icon draws: sunny, partly_cloudy or rain.</summary>
    [Parameter] public string? Sky { get; set; }

    [Parameter] public int High { get; set; }

    [Parameter] public int Low { get; set; }

    /// <summary>The chance of rain, in percent.</summary>
    [Parameter] public int Rain { get; set; }

    private string SkyIcon => Sky switch
    {
        "sunny" => BitIconName.Sunny,
        "rain" => BitIconName.RainShowersDay,
        _ => BitIconName.PartlyCloudyDay
    };

    private BitColor SkyColor => Sky is "rain" ? BitColor.Info : BitColor.Warning;

    private static string Degrees(int value) => $"{value.ToString(CultureInfo.CurrentCulture)}°";
}

namespace Boilerplate.Client.Web;

/// <summary>
/// https://github.com/adessoTurkey-dotNET/AdsPush
/// </summary>
public class AdsPushVapidOptions
{
    /// <summary>
    /// Web push's vapid. More info at https://d3v.one/vapid-key-generator/
    /// </summary>
    [Required]
    public string PublicKey { get; set; } = default!;
}

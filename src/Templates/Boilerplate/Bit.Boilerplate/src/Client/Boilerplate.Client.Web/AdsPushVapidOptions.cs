namespace Boilerplate.Client.Web;

/// <summary>
/// https://github.com/adessoTurkey-dotNET/AdsPush
/// </summary>
public class AdsPushVapidOptions
{
    /// <summary>
    /// Web push's VAPID key pair. Create one with `bit keys vapid`.
    /// </summary>
    [Required]
    public string PublicKey { get; set; } = default!;
}

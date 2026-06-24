namespace Bit.Butil;

/// <summary>
/// Combined user/screen state from <see href="https://developer.mozilla.org/en-US/docs/Web/API/IdleDetector">IdleDetector</see>.
/// </summary>
public class IdleState
{
    /// <summary><c>"active"</c> or <c>"idle"</c>.</summary>
    public string UserState { get; set; } = "active";

    /// <summary><c>"locked"</c> or <c>"unlocked"</c>.</summary>
    public string ScreenState { get; set; } = "unlocked";
}

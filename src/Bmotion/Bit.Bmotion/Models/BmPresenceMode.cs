namespace Bit.Bmotion;

/// <summary>How presence components sequence exit and enter animations.</summary>
public enum BmPresenceMode
{
    /// <summary>Exiting and entering content animate at the same time.</summary>
    Sync,
    /// <summary>The exit animation finishes before the new content enters.</summary>
    Wait,
}

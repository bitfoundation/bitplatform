namespace Bit.Bmage;

/// <summary>What the coordinates of a geometric mask are measured in.</summary>
public enum BmageMaskUnits
{
    /// <summary>Pixels of the image the mask is applied to. Rescaled with the edit when it is previewed on a proxy.</summary>
    Pixels = 0,
    /// <summary>
    /// Fractions of the image's width and height, so (0,0) is the top left corner and (1,1) the bottom right. The same
    /// mask then fits every image, which suits a pipeline reused across uploads of different sizes.
    /// </summary>
    Relative,
}

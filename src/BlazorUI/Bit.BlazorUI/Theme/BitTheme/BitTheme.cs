namespace Bit.BlazorUI;

public class BitTheme
{
    public BitThemeColors Color { get; private set; } = new();

    public BitThemeBoxShadows BoxShadow { get; private set; } = new();

    public BitThemeSpacings Spacing { get; private set; } = new();

    public BitThemeZIndices ZIndex { get; private set; } = new();

    public BitThemeShapes Shape { get; private set; } = new();

    public BitThemeTypography Typography { get; private set; } = new();
}

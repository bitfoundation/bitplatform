namespace Bit.Bmotion;

/// <summary>DOM bounding rect returned by <c>getBoundingRect</c> in JS.</summary>
public sealed class BmotionBoundingRect
{
    public double X      { get; set; }
    public double Y      { get; set; }
    public double Width  { get; set; }
    public double Height { get; set; }
    public double Top    { get; set; }
    public double Left   { get; set; }
}

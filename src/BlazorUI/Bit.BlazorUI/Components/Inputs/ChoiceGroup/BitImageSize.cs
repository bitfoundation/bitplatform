namespace Bit.BlazorUI;

public struct BitImageSize(int width, int height)
{
    public int Width { get; set; } = width; 

    public int Height { get; set;} = height;
}

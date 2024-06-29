namespace Bit.BlazorUI;

public struct BitSize(int width, int height)
{
    public int Width { get; set; } = width; 

    public int Height { get; set;} = height;
}

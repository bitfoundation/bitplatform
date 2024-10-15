namespace Bit.BlazorUI;

internal class BitSnackBarItem
{
    public string Title { get; set; } = default!;
    
    public string? Body { get; set; }
    
    public BitColor? Color { get; set; }
    
    public string? CssClass { get; set; }
    
    public string? CssStyle { get; set; }
}

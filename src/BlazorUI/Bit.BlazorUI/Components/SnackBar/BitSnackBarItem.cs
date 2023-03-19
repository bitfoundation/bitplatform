namespace Bit.BlazorUI;

internal class BitSnackBarItem
{
    public string Title { get; set; } = string.Empty;
    
    public string? Body { get; set; }
    
    public BitSnackBarType? Type { get; set; }
}

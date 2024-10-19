namespace Bit.BlazorUI;

internal class BitSnackBarItem
{
    public readonly Guid Id = Guid.NewGuid();

    public string Title { get; set; } = default!;

    public string? Body { get; set; }

    public BitColor? Color { get; set; }

    public string? CssClass { get; set; }

    public string? CssStyle { get; set; }
}

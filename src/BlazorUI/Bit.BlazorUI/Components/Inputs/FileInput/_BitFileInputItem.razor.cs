namespace Bit.BlazorUI;

public partial class _BitFileInputItem : ComponentBase
{
    private const string ROOT_ELEMENT_CLASS = "bit-fin";

    [Parameter] public BitFileInput FileInput { get; set; } = default!;
    [Parameter] public BitFileInputInfo Item { get; set; } = default!;

    private string GetFileElClass(bool isValid)
        => isValid ? $"{ROOT_ELEMENT_CLASS}-vld" : $"{ROOT_ELEMENT_CLASS}-inv";
}

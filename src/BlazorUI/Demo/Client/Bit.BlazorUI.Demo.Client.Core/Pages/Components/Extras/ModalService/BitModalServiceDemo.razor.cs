namespace Bit.BlazorUI.Demo.Client.Core.Pages.Components.Extras.ModalService;

public partial class BitModalServiceDemo
{
    private readonly List<ComponentParameter> componentParameters =
    [
    ];


    [AutoInject] private BitModalService modalService = default!;

    private async Task ShowModal()
    {
        await modalService.Show<ModalContent>();
    }


    private readonly string example1RazorCode = @"
<BitPdfReader Config=""basicConfig"" />";
    private readonly string example1CsharpCode = @"
private readonly BitPdfReaderConfig basicConfig = new() { Url = ""url-to-the-pdf-file.pdf"" };";
}

using Microsoft.AspNetCore.Components.Web;

namespace Boilerplate.Client.Core.Services;

public partial class PromptService
{
    [AutoInject] private BitModalService modalService = default!;

    public async Task<string?> Show(string message, string title = "", bool otpInput = false)
    {
        TaskCompletionSource<string?> tcs = new();
        BitModalReference? modalReference = null;
        Dictionary<string, object> promptParameters = new()
        {
            { nameof(Prompt.Title), title },
            { nameof(Prompt.Body), message },
            { nameof(Prompt.OtpInput), otpInput },
            { nameof(Prompt.OnCancel), () => { tcs.SetResult(null); modalReference?.Close(); } },
            { nameof(Prompt.OnOk), (string value) => { tcs.SetResult(value); modalReference?.Close(); } }
        };
        var modalParameters = new BitModalParameters()
        {
            OnOverlayClick = EventCallback.Factory.Create<MouseEventArgs>(this, () => tcs.SetResult(null))
        };
        modalReference = await modalService.Show<Prompt>(promptParameters, modalParameters);
        return await tcs.Task;
    }
}

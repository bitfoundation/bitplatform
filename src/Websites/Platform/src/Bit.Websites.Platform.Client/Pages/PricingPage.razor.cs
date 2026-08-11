using Bit.Websites.Platform.Shared.Dtos.SupportPackage;

namespace Bit.Websites.Platform.Client.Pages;

public partial class PricingPage
{
    private bool isSent;
    private bool isSending;
    private bool isBuyModalOpen;
    private BuyPackageDto buyPackageModel = new();


    private void ShowBuyModal()
    {
        isBuyModalOpen = true;
    }

    private void CloseModal()
    {
        isSent = false;
        isBuyModalOpen = false;
        buyPackageModel = new();
    }

    private async Task SendMessage()
    {
        if (isSending) return;

        isSending = true;

        try
        {
            var response = await HttpClient.PostAsJsonAsync("api/SupportPackage/BuyPackage", buyPackageModel, AppJsonContext.Default.BuyPackageDto);
            response.EnsureSuccessStatusCode();
            isSent = true;
        }
        finally
        {
            isSending = false;
        }
    }
}

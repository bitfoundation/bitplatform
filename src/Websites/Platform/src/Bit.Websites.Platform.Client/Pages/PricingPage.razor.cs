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
            await HttpClient.PostAsJsonAsync("SupportPackage/BuyPackage", buyPackageModel, AppJsonContext.Default.BuyPackageDto);
        }
        finally
        {
            isSending = false;
            isSent = true;
        }
    }
}

using Bit.Websites.Platform.Shared.Dtos.SupportPackage;

namespace Bit.Websites.Platform.Client.Pages;

public partial class PricingPage
{
    private bool isSent;
    private bool isSending;
    private bool isBuyModalOpen;
    private string selectedPackageTitle = string.Empty;
    private string selectedPackagePrice = string.Empty;

    private BuyPackageDto buyPackageModel = new();



    private void ShowBuyModal(string title, string price)
    {
        selectedPackageTitle = title;
        selectedPackagePrice = price;
        buyPackageModel.SalePackageTitle = title;
        isBuyModalOpen = true;
    }

    private void CloseModal()
    {
        isBuyModalOpen = false;
        isSent = false;

        buyPackageModel.Email = "";
        buyPackageModel.Message = "";
        buyPackageModel.SalePackageTitle = "";
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

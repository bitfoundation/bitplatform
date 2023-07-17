using Bit.Websites.Platform.Shared.Dtos.SupportPackage;

namespace Bit.Websites.Platform.Web.Pages;

public partial class PricingPage
{
    private string _selectedPackageTitle = string.Empty;
    private string _selectedPackagePrice = string.Empty;

    private BuyPackageDto _buyPackageModel { get; set; } = new();

    private bool _isBuyModalOpen { get; set; }

    private bool _isSending { get; set; }

    private void ShowBuyModal(string title, string price)
    {
        _selectedPackageTitle = title;
        _selectedPackagePrice = price;
        _buyPackageModel.SalePackageTitle = title;
        _isBuyModalOpen = true;
    }

    private void CloseModal()
    {
        _isBuyModalOpen = false;
    }

    private async Task SendMessage()
    {
        if (_isSending) return;

        _isSending = true;

        try
        {
            await HttpClient.PostAsJsonAsync("SupportPackage/BuyPackage", _buyPackageModel, AppJsonContext.Default.BuyPackageDto);

            _buyPackageModel.Email = "";
            _buyPackageModel.Message = "";
            _buyPackageModel.SalePackageTitle = "";
        }
        finally
        {
            _isSending = false;
            CloseModal();
        }
    }
}

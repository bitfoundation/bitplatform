using System.Collections.Generic;
using System.Net.Http;
using Bit.Websites.Platform.Shared.Dtos.SupportPackage;
using Microsoft.Extensions.DependencyInjection;
using Bit.Websites.Platform.Shared.Dtos;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace Bit.Websites.Platform.Web.Pages;

public partial class PricingPage
{
    [AutoInject] protected HttpClient HttpClient = default!;

    private string _selectedPackageTitle = string.Empty;
    private string _selectedPackagePrice = string.Empty;

    public BuyPackageDto BuyPackageModel { get; set; } = new();

    public bool IsBuyModalOpen { get; set; }

    public bool IsLoading { get; set; }

    public bool IsSubmitButtonEnabled => IsLoading is false;

    private void ShowBuyModal(string title, string price) {
        _selectedPackageTitle = title;
        _selectedPackagePrice = price;
        BuyPackageModel.SalePackageTitle = title;
        IsBuyModalOpen = true;
    }

    private void CloseModal()
    {
        IsBuyModalOpen = false;
    }

    private async Task SendMessage()
    {
        if (IsLoading)
        {
            return;
        }

        IsLoading = true;

        try
        {
            await HttpClient.PostAsJsonAsync("SupportPackage/BuyPackage", BuyPackageModel, AppJsonContext.Default.BuyPackageDto);
            BuyPackageModel.Email = "";
            BuyPackageModel.Message = "";
            BuyPackageModel.SalePackageTitle = "";
        }
        finally
        {
            IsLoading = false;
            CloseModal();
        }
    }
}

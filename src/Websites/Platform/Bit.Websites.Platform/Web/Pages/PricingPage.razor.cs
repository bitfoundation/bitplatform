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

    public List<SupportPackageDto> SupportPackageList = new List<SupportPackageDto>()
    {
        new SupportPackageDto()
        {
            Title = "Free",
            Price = 0,
            Period = "forever",
            Description = "This is a description"
        },
        new SupportPackageDto()
        {
            Title = "Professtional",
            Price = 300,
            Period = "month",
            Description = "This is a description"
        },
        new SupportPackageDto()
        {
            Title = "Ultimate",
            Price = 1200,
            Period = "month",
            Description = "This is a description"
        },
        new SupportPackageDto()
        {
            Title = "Custom",
            Period = "",
            Description = "This is a description"
        }
    };

    public SupportPackageDto SelectedPackage { get; set; } = new();

    public BuyPackageDto BuyPackageModel { get; set; } = new();

    public bool IsBuyModalOpen { get; set; }

    public bool IsLoading { get; set; }

    public bool IsSubmitButtonEnabled => IsLoading is false;

    private void SelectPackage(SupportPackageDto package) { 
        SelectedPackage = package;
        BuyPackageModel.SalePackageTitle = package.Title;
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

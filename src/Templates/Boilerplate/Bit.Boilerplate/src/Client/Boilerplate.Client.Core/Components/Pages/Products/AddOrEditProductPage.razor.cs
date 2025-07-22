using System.IO;
using Boilerplate.Shared.Controllers;
using Boilerplate.Shared.Controllers.Categories;
using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Dtos.Products;

namespace Boilerplate.Client.Core.Components.Pages.Products;

public partial class AddOrEditProductPage
{
    [AutoInject] IProductController productController = default!;
    [AutoInject] ICategoryController categoryController = default!;
    [AutoInject] IAttachmentController attachmentController = default!;

    private bool isSaving;
    private bool isManagingFile;
    private bool isLoading = true;
    private ProductDto product = new() { Id = Guid.NewGuid() };
    private BitFileUpload fileUploadRef = default!;
    private string selectedCategoryId = string.Empty;
    private BitRichTextEditor richTextEditorRef = default!;
    private List<BitDropdownItem<string>> allCategoryList = [];
    private AppDataAnnotationsValidator validatorRef = default!;

    [Parameter] public Guid? Id { get; set; }

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        try
        {
            var categoryList = await categoryController.Get(CurrentCancellationToken);

            allCategoryList = [.. categoryList.Select(c => new BitDropdownItem<string>()
                                                           {
                                                               ItemType = BitDropdownItemType.Normal,
                                                               Text = c.Name ?? string.Empty,
                                                               Value = c.Id.ToString()
                                                           })];

            if (Id is null) return;

            product = await productController.Get(Id.Value, CurrentCancellationToken);
            selectedCategoryId = (product.CategoryId ?? default).ToString();
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task Save()
    {
        if (isLoading || isSaving) return;

        isSaving = true;

        product.DescriptionHTML = await richTextEditorRef.GetHtml();
        product.DescriptionText = await richTextEditorRef.GetText();

        try
        {
            if (Id == default)
            {
                await productController.Create(product, CurrentCancellationToken);
            }
            else
            {
                await productController.Update(product, CurrentCancellationToken);
            }

            GoBack();
        }
        catch (ResourceValidationException exp)
        {
            validatorRef.DisplayErrors(exp);
        }
        finally
        {
            isSaving = false;
        }
    }

    private void GoBack()
    {
        NavigationManager.NavigateTo(PageUrls.ProductsPage);
    }

    private async Task HandleOnUploadComplete(BitFileInfo fileInfo)
    {
        product.HasPrimaryImage = true;
        product.PrimaryImageAltText = fileInfo.Message;
        isManagingFile = false;
    }

    private async Task HandleOnUploadFailed(BitFileInfo fileInfo)
    {
        isManagingFile = false;
        SnackBarService.Error(string.IsNullOrEmpty(fileInfo.Message) ? Localizer[nameof(AppStrings.FileUploadFailed)] : fileInfo.Message);
    }

    private async Task RemoveProductImage()
    {
        if (isManagingFile) return;
        isManagingFile = true;

        try
        {
            await attachmentController.DeleteProductPrimaryImage(product.Id, CurrentCancellationToken);
            if (Id is not null)
            {
                product = await productController.Get(Id.Value, CurrentCancellationToken); // To get latest concurrency stamp and other properties modified by the server.
            }
        }
        catch (KnownException e)
        {
            SnackBarService.Error(e.Message);
        }
        finally
        {
            isManagingFile = false;
        }
    }

    private async Task<string> GetUploadUrl()
    {
        var uploadUrl = new Uri(AbsoluteServerAddress, $"/api/Attachment/UploadProductPrimaryImage/{Id ?? product.Id}").ToString();

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            uploadUrl += $"?culture={CultureInfo.CurrentUICulture.Name}"; // To have localized error messages from the server (if any).
        }

        return uploadUrl;
    }

    private async Task<Dictionary<string, string>> GetUploadRequestHeaders()
    {
        var accessToken = await AuthManager.GetFreshAccessToken(requestedBy: nameof(BitFileUpload));

        return new() { { "Authorization", $"Bearer {accessToken}" } };
    }
}

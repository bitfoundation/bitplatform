using Boilerplate.Shared.Controllers;
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;
using Boilerplate.Shared.Controllers.Categories;

namespace Boilerplate.Client.Core.Components.Pages.Authorized.Products;

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
        NavigationManager.NavigateTo(Urls.ProductsPage);
    }

    private async Task HandleOnUploadComplete()
    {
        product.HasPrimaryImage = true;
        product.ConcurrencyStamp = Guid.NewGuid().ToByteArray(); // To update the product image's url when user changes product image multiple time within the same page.
        isManagingFile = false;
    }

    private async Task HandleOnUploadFailed()
    {
        isManagingFile = false;
        SnackBarService.Error(Localizer[nameof(AppStrings.FileUploadFailed)]);
    }

    private async Task RemoveProductImage()
    {
        if (isManagingFile) return;
        isManagingFile = true;

        try
        {
            await attachmentController.DeleteProductPrimaryImage(product.Id, CurrentCancellationToken);
            product.HasPrimaryImage = false;
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
        var accessToken = await AuthManager.ObtainFreshAccessToken(requestedBy: nameof(BitFileUpload));

        return new Uri(AbsoluteServerAddress, $"/api/Attachment/UploadProductPrimaryImage/{Id ?? product.Id}?access_token={accessToken}").ToString();
    }
}

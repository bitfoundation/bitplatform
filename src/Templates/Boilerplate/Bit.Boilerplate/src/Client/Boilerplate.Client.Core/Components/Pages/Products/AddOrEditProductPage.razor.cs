using Boilerplate.Shared.Features.Products;
using Boilerplate.Shared.Features.Categories;
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Client.Core.Components.Pages.Products;

public partial class AddOrEditProductPage
{
    [AutoInject] IProductController productController = default!;
    [AutoInject] ICategoryController categoryController = default!;
    [AutoInject] IAttachmentController attachmentController = default!;

    private bool isSaving;
    private bool loadFailed;
    private bool isManagingFile;
    private bool isLoading = true;
    private ProductDto product = new() { Id = Guid.CreateSequentialGuid() };
    private BitFileUpload fileUploadRef = default!;
    private BitRichTextEditor bitRichTextEditor = default!;
    private string selectedCategoryId = string.Empty;
    private List<BitDropdownItem<string>> allCategoryList = [];
    private AppDataAnnotationsValidator validatorRef = default!;

    [Parameter] public Guid? Id { get; set; }

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();

        await LoadAsync();
    }

    private async Task LoadAsync()
    {
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
        catch (Exception exp)
        {
            loadFailed = true;
            ExceptionHandler.Handle(exp);
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

        try
        {
            product.DescriptionText = await bitRichTextEditor.GetTextAsync();

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
        NavigationManager.NavigateTo(PageUrls.Products);
    }

    private async Task Reload()
    {
        loadFailed = false;
        isLoading = true;
        product = new() { Id = Guid.CreateSequentialGuid() };
        selectedCategoryId = string.Empty;

        await LoadAsync();
    }

    private async Task HandleOnUploadComplete(BitFileInfo fileInfo)
    {
        try
        {
            if (Id is not null)
            {
                await RefreshImageState();
            }
            else
            {
                product.HasPrimaryImage = true;
                product.PrimaryImageAltText = fileInfo.Message;
            }
        }
        finally
        {
            isManagingFile = false;
        }
    }

    private async Task HandleOnUploadFailed(BitFileInfo fileInfo)
    {
        isManagingFile = false;
        SnackBarService.Error(string.IsNullOrWhiteSpace(fileInfo.Message) ? Localizer[nameof(AppStrings.FileUploadFailed)] : fileInfo.Message);
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
                await RefreshImageState();
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

    /// <summary>
    /// Uploading or deleting the primary image writes to the Product row server-side, so the form has to pick up the
    /// new concurrency stamp or the next save conflicts. Only the image fields and that stamp are copied over -
    /// replacing the whole instance would throw away whatever the user has typed but not saved yet.
    /// </summary>
    private async Task RefreshImageState()
    {
        var fresh = await productController.Get(Id!.Value, CurrentCancellationToken);

        product.Version = fresh.Version;
        product.HasPrimaryImage = fresh.HasPrimaryImage;
        product.PrimaryImageAltText = fresh.PrimaryImageAltText;
    }

    private async Task<string> GetUploadUrl()
    {
        var uploadUrl = new Uri(AbsoluteServerAddress, $"/api/v1/Attachment/UploadProductPrimaryImage/{Id ?? product.Id}").ToString();

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

//+:cnd:noEmit
using ImageMagick;
using System.Net.Http.Headers;
using Boilerplate.Shared.Features.Products;
using Boilerplate.Shared.Features.Categories;
using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Tests.Features.Products;

/// <summary>
/// The product image is uploaded on file-pick, which means it is uploaded <b>before</b> the product row exists on the
/// Add path and <b>behind the client's back</b> on the Edit path. Making <c>HasPrimaryImage</c> server-owned - the
/// right call, and what <c>[MapperIgnoreTarget]</c> on <c>ProductsMapper</c> enforces - broke both of those:
/// <list type="bullet">
/// <item>On <b>Add</b>, <c>UploadAttachment</c>'s flag write is guarded by <c>if (product is not null)</c> and the row
/// does not exist yet, while the mapper drops the client's copy of the flag. The blob and both <c>Attachment</c> rows
/// were written and then referenced by nothing: the product showed a placeholder forever.</item>
/// <item>On <b>Edit</b>, the upload's own <c>SaveChangesAsync</c> advances <c>Version</c>. The page held the
/// pre-upload DTO, so the next PUT carried a stale token and was rejected with a conflict - permanently, since
/// retrying resent the same value.</item>
/// <item>Deleting the product removed neither the <c>Attachment</c> rows nor the blobs. <c>Attachment</c> has no
/// foreign key to <c>Product</c> (its key is <c>{ Id, Kind }</c>), so nothing cascades, and the image stayed
/// downloadable from the anonymous <c>GetAttachment</c> endpoint after the product was gone.</item>
/// </list>
/// All three are invisible to a happy-path test that never uploads an image, and all three need the real
/// upload endpoint rather than a database write, because the whole defect lives in what that endpoint does or
/// does not stamp on the product row.
/// </summary>
/// <remarks>
/// <c>DoNotParallelize</c> because these tests create products through the real <c>ProductController.Create</c>, and on
/// a provider without the ShortId sequence that value comes from a coarse clock reading (See <c>Product.ShortId</c>) -
/// two creates close enough together collide on its unique index. Serialising the class puts a server boot between
/// them. The demo-grade generator is deliberate; the tests must not be what forces it to be more than that.
/// </remarks>
[TestClass, TestCategory("IntegrationTest"), DoNotParallelize]
public partial class ProductImageLifecycleTests
{
    // Seeded tenant-admin of the default (fallback) tenant; holds ProductCatalog_Manage. See UserConfiguration.
    private const string TenantAdminEmail = "store-admin@bitplatform.dev";
    private const string Password = "123456";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// The Add path, in the order the page actually performs it: upload first, create second. The product must come
    /// back with <c>HasPrimaryImage</c> true and its image must be served - proving the server adopted the attachment
    /// rather than trusting (or dropping) the client's flag.
    /// </summary>
    [TestMethod]
    public async Task CreatingAProductAfterUploadingItsImage_Should_AdoptTheImage()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await SignIn(scope);

        var products = scope.ServiceProvider.GetRequiredService<IProductController>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        // Exactly what AddOrEditProductPage does: the id is minted client-side and the upload is posted against it
        // while no such product row exists.
        var productId = Guid.CreateSequentialGuid();
        var name = $"adopt-image-{Guid.NewGuid():N}";

        await UploadProductImage(httpClient, productId, SolidImage(MagickColors.Red));

        var created = await products.Create(await NewProductDto(scope, productId, name), TestContext.CancellationToken);

        try
        {
            Assert.IsTrue(created.HasPrimaryImage,
                "The image was uploaded before the row existed, so nothing but Create can adopt it. False here means " +
                "the blob and both Attachment rows are referenced by nothing and the product renders a placeholder.");

            Assert.IsNotNull(created.GetPrimaryMediumImageUrl(server.WebAppServerAddress),
                "HasPrimaryImage is the only thing gating the image URL, so a false flag hides an image that is really there.");

            var served = await httpClient.GetByteArrayAsync(
                $"api/v1/Attachment/GetAttachment/{productId}/{AttachmentKind.ProductPrimaryImageMedium}", TestContext.CancellationToken);
            Assert.IsGreaterThan(0, served.Length, "The adopted image must actually be downloadable.");

            // The response is projected from the database rather than mapped off the tracked entity; a mapped one
            // reads Category, a navigation Create never loads, and reports null.
            Assert.IsNotNull(created.CategoryName, "Create's response must carry the category name it was given an id for.");
        }
        finally
        {
            await DeleteProduct(products, created);
        }
    }

    /// <summary>
    /// The Edit path. Uploading an image onto an existing product writes the flag server-side, which moves the row's
    /// concurrency token; a client that kept its pre-upload copy can then never save again. The assertions pin both
    /// halves - that the stale token really is rejected (so the page must re-read), and that re-reading makes the save
    /// work - because a fix that skipped the re-read would still pass the second one alone.
    /// </summary>
    [TestMethod]
    public async Task UploadingAnImageOntoAnExistingProduct_Should_MoveTheConcurrencyStamp()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await SignIn(scope);

        var products = scope.ServiceProvider.GetRequiredService<IProductController>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var name = $"stale-version-{Guid.NewGuid():N}";
        var created = await products.Create(await NewProductDto(scope, Guid.CreateSequentialGuid(), name), TestContext.CancellationToken);

        try
        {
            Assert.IsFalse(created.HasPrimaryImage, "This product starts without an image, which is what makes the upload below an UPDATE.");

            // The DTO the edit form is holding at this moment.
            var staleDto = await products.Get(created.Id, TestContext.CancellationToken);

            await UploadProductImage(httpClient, created.Id, SolidImage(MagickColors.Blue));

            staleDto.Price = 51_000M;

            await Assert.ThrowsExactlyAsync<ConflictException>(
                () => products.Update(staleDto, TestContext.CancellationToken),
                "The upload flipped HasPrimaryImage and therefore advanced Version. A PUT carrying the pre-upload " +
                "Version must be rejected - if it is accepted, the optimistic-concurrency contract is not being enforced.");

            // What HandleOnUploadComplete now does: re-read, exactly as RemoveProductImage always did.
            var fresh = await products.Get(created.Id, TestContext.CancellationToken);
            Assert.IsTrue(fresh.HasPrimaryImage, "The upload must have set the flag server-side.");
            Assert.AreNotEqual(staleDto.Version, fresh.Version, "A stamp that does not move cannot reject anything.");

            fresh.Price = 51_000M;
            var updated = await products.Update(fresh, TestContext.CancellationToken);

            Assert.AreEqual(51_000M, updated.Price, "After re-reading, the very same edit must go through.");
            Assert.IsTrue(updated.HasPrimaryImage, "Updating an unrelated field must not drop the image.");
        }
        finally
        {
            await DeleteProduct(products, await products.Get(created.Id, TestContext.CancellationToken));
        }
    }

    /// <summary>
    /// Deleting a product must take its image with it. <c>Attachment</c> has no foreign key to <c>Product</c>, so
    /// there is no cascade to rely on and the only proof is that the anonymous endpoint stops serving the blob.
    /// </summary>
    [TestMethod]
    public async Task DeletingAProduct_Should_TakeItsImageWithIt()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await SignIn(scope);

        var products = scope.ServiceProvider.GetRequiredService<IProductController>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();

        var productId = Guid.CreateSequentialGuid();
        await UploadProductImage(httpClient, productId, SolidImage(MagickColors.Green));

        var created = await products.Create(
            await NewProductDto(scope, productId, $"delete-image-{Guid.NewGuid():N}"), TestContext.CancellationToken);

        // Precondition: without this the assertions below would pass against a product that never had an image.
        Assert.AreEqual(2, await CountAttachments(server, productId), "One upload writes the medium and the original kind.");

        await products.Delete(created.Id, created.Version, TestContext.CancellationToken);

        Assert.AreEqual(0, await CountAttachments(server, productId),
            "The Attachment rows outlive the product unless Delete removes them - nothing cascades, the key is { Id, Kind }.");

        // The DI HttpClient translates the server's ProblemDetails back into the original exception type
        // (See ExceptionDelegatingHandler), so a 404 from GetAttachment arrives as ResourceNotFoundException.
        await Assert.ThrowsExactlyAsync<ResourceNotFoundException>(
            () => httpClient.GetByteArrayAsync(
                $"api/v1/Attachment/GetAttachment/{productId}/{AttachmentKind.ProductPrimaryImageMedium}", TestContext.CancellationToken),
            "GetAttachment is AllowAnonymous and gates only on the blob existing, so a blob left behind stays " +
            "publicly downloadable at a URL the storefront already handed out.");
    }

    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();
        await server.Build(services => services.AddIntegrationApiOnlyTestsServices()).Start(TestContext.CancellationToken);
        return server;
    }

    /// <summary>
    /// Signs the tenant-admin in within <paramref name="scope"/>, so every typed API client resolved from that scope
    /// calls the server as her. ProductController demands a privileged session, a selected tenant and
    /// ProductCatalog_Manage; a fresh password sign-in as a t-admin of the fallback tenant covers all three.
    /// </summary>
    private async Task SignIn(AsyncServiceScope scope)
    {
        var authManager = scope.ServiceProvider.GetRequiredService<AuthManager>();

        var requiresTwoFactor = await authManager.SignIn(new()
        {
            Email = TenantAdminEmail,
            Password = Password
        }, TestContext.CancellationToken);

        Assert.IsFalse(requiresTwoFactor, $"'{TenantAdminEmail}' is not expected to have two factor authentication enabled.");
    }

    private async Task<ProductDto> NewProductDto(AsyncServiceScope scope, Guid id, string name)
    {
        var categories = scope.ServiceProvider.GetRequiredService<ICategoryController>();
        var categoryId = (await categories.Get(TestContext.CancellationToken)).First().Id;

        return new ProductDto
        {
            Id = id,
            Name = name,
            Price = 12_345M,
            CategoryId = categoryId,
            DescriptionText = "description",
            DescriptionHTML = "<p>description</p>"
        };
    }

    /// <summary>A solid-colour PNG at least 512x512, which is the minimum the medium kind's resize accepts.</summary>
    private static byte[] SolidImage(MagickColor color, uint size = 512)
    {
        using var image = new MagickImage(color, size, size);
        return image.ToByteArray(MagickFormat.Png);
    }

    private async Task UploadProductImage(HttpClient httpClient, Guid productId, byte[] imageBytes)
    {
        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(imageBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        form.Add(fileContent, "file", "product.png"); // The endpoint binds an IFormFile parameter named "file".

        using var response = await httpClient.PostAsync(
            $"api/v1/Attachment/UploadProductPrimaryImage/{productId}", form, TestContext.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    private async Task<int> CountAttachments(AppTestServer server, Guid attachmentId)
    {
        await using var scope = server.WebApp.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        return await dbContext.Attachments.CountAsync(att => att.Id == attachmentId, TestContext.CancellationToken);
    }

    /// <summary>Best effort: a test that failed early must not take the cleanup down with it.</summary>
    private async Task DeleteProduct(IProductController products, ProductDto product)
    {
        try
        {
            await products.Delete(product.Id, product.Version, CancellationToken.None);
        }
        catch (Exception) { }
    }
}

//+:cnd:noEmit
using ImageMagick;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.OutputCaching;
using Boilerplate.Shared.Features.Products;
using Boilerplate.Shared.Features.Categories;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Server.Api.Infrastructure.Services;
using Boilerplate.Server.Shared.Infrastructure.Services;

namespace Boilerplate.Tests.Features.Caching;

/// <summary>
/// A replaced or deleted product image keeps its blob key (See <c>AttachmentController.GetFilePath</c>), so only
/// purging the attachment's own cache tag invalidates the copies already cached under the previous <c>?v=</c>.
/// <c>GetAttachment</c> is tagged by <see cref="ResponseCacheService.AttachmentCacheTagTemplate"/>, not by url,
/// so one tag covers every kind and every query string of that id. Observed through
/// <see cref="IOutputCacheStore.EvictByTagAsync"/>, which receives the same tag string sent to the CDN - the CDN
/// itself is not reachable from a test.
/// </summary>
/// <remarks>
/// <c>DoNotParallelize</c> for the same reason as <c>ProductImageLifecycleTests</c>: products are created through the
/// real <c>ProductController.Create</c>, whose ShortId comes from a coarse clock reading on a provider without the
/// sequence, and two creates close together collide on its unique index.
/// </remarks>
[TestClass, TestCategory("IntegrationTest"), TestCategory("Caching"), DoNotParallelize]
public class ProductImageCacheInvalidationTests
{
    // Seeded tenant-admin of the default (fallback) tenant; holds ProductCatalog_Manage. See UserConfiguration.
    private const string TenantAdminEmail = "store-admin@bitplatform.dev";
    private const string Password = "123456";

    public TestContext TestContext { get; set; } = default!;

    /// <summary>
    /// Replacing the image purges the attachment's tag. The tag is compared with the <c>Cache-Tag</c> the endpoint
    /// really stamps: a purge naming a tag the response is not stored under would clear nothing.
    /// </summary>
    [TestMethod]
    public async Task ReplacingAProductImage_Should_PurgeThePreviousVersionsAttachmentUrl()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await SignIn(scope);

        var products = scope.ServiceProvider.GetRequiredService<IProductController>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        var recorder = server.WebApp.Services.GetRequiredService<RecordingOutputCacheStore>();

        var productId = Guid.CreateSequentialGuid();
        await UploadProductImage(httpClient, productId, SolidImage(MagickColors.Red));

        var created = await products.Create(
            await NewProductDto(scope, productId, $"image-purge-{Guid.NewGuid():N}"), TestContext.CancellationToken);

        try
        {
            // The url a visitor is holding at this point, and the tag the edge stored it under. Anonymous on purpose:
            // an authenticated caller has a tenant claim, and AppResponseCachePolicy switches the edge off for those,
            // so the response would carry no Cache-Tag to compare with.
            using var visitorHttpClient = new HttpClient { BaseAddress = server.WebAppServerAddress };
            var imageUrl = $"/api/v1/Attachment/GetAttachment/{productId}/{AttachmentKind.ProductPrimaryImageMedium}";

            using var beforeResponse = await visitorHttpClient.GetAsync($"{imageUrl}?v={created.Version}", TestContext.CancellationToken);
            beforeResponse.EnsureSuccessStatusCode();

            Assert.IsTrue(beforeResponse.Headers.TryGetValues("App-Cache-Response", out var cacheDecision));
            Assert.DoesNotContain("Edge:-1", string.Concat(cacheDecision!),
                "The attachment has to be edge cacheable, or there would be nothing on the edge to go stale in the first place.");

            Assert.IsTrue(beforeResponse.Headers.TryGetValues(AppResponseCachePolicy.CacheTagHeaderName, out var servedTag),
                "An edge cacheable attachment must tell the CDN which tag to store it under, otherwise it could never be purged.");

            // Everything above is setup; only what the replacement itself purges counts.
            recorder.Reset();

            await UploadProductImage(httpClient, productId, SolidImage(MagickColors.Blue));

            var attachmentTag = AttachmentCacheTag(productId);

            Assert.Contains(attachmentTag, recorder.EvictedTags,
                "Replacing the image left the previous version on the edge, where it is served for the whole seven day " +
                "max-age. Moving the product's Version only re-points the documents at a new ?v=; the old one still resolves. " +
                $"Actual tags: [{string.Join(", ", recorder.EvictedTags)}]");

            Assert.AreEqual(string.Concat(servedTag!), attachmentTag,
                "The tag the purge names must be the tag the response was stored under. The template does not include " +
                "kind or query string, which is what makes one purge clear every kind and every ?v= of this attachment.");

            // The bytes really did change; without this the assertions above could hold for an upload that did nothing.
            using var afterResponse = await visitorHttpClient.GetAsync($"{imageUrl}?cb={Guid.NewGuid():N}", TestContext.CancellationToken);
            afterResponse.EnsureSuccessStatusCode();
            Assert.AreNotEqual(
                Convert.ToBase64String(await beforeResponse.Content.ReadAsByteArrayAsync(TestContext.CancellationToken)),
                Convert.ToBase64String(await afterResponse.Content.ReadAsByteArrayAsync(TestContext.CancellationToken)),
                "The second upload has to have replaced the stored bytes, or there is no stale copy to invalidate.");
        }
        finally
        {
            await DeleteProduct(products, created.Id);
        }
    }

    /// <summary>
    /// Deleting is the worse half: the page falls back to the placeholder while the edge still serves the picture.
    /// </summary>
    [TestMethod]
    public async Task DeletingAProductImage_Should_PurgeItsAttachmentUrl()
    {
        await using var server = await StartServer();
        await using var scope = server.WebApp.Services.CreateAsyncScope();

        await SignIn(scope);

        var products = scope.ServiceProvider.GetRequiredService<IProductController>();
        var attachments = scope.ServiceProvider.GetRequiredService<IAttachmentController>();
        var httpClient = scope.ServiceProvider.GetRequiredService<HttpClient>();
        var recorder = server.WebApp.Services.GetRequiredService<RecordingOutputCacheStore>();

        var productId = Guid.CreateSequentialGuid();
        await UploadProductImage(httpClient, productId, SolidImage(MagickColors.Green));

        var created = await products.Create(
            await NewProductDto(scope, productId, $"image-delete-purge-{Guid.NewGuid():N}"), TestContext.CancellationToken);

        try
        {
            Assert.IsTrue(created.HasPrimaryImage, "Without an adopted image the delete below would have nothing to purge.");

            recorder.Reset();

            await attachments.DeleteProductPrimaryImage(productId, TestContext.CancellationToken);

            Assert.Contains(AttachmentCacheTag(productId), recorder.EvictedTags,
                "The deleted image is still served from the edge for the rest of its seven day max-age. " +
                $"Actual tags: [{string.Join(", ", recorder.EvictedTags)}]");
        }
        finally
        {
            await DeleteProduct(products, created.Id);
        }
    }

    /// <summary>
    /// Records evicted tags and stores nothing, so one test's entry cannot answer the next one's request.
    /// </summary>
    private sealed class RecordingOutputCacheStore : IOutputCacheStore
    {
        private readonly ConcurrentBag<string> evictedTags = [];

        public string[] EvictedTags => [.. evictedTags];

        public void Reset() => evictedTags.Clear();

        public ValueTask EvictByTagAsync(string tag, CancellationToken cancellationToken)
        {
            evictedTags.Add(tag);
            return ValueTask.CompletedTask;
        }

        public ValueTask<byte[]?> GetAsync(string key, CancellationToken cancellationToken) => ValueTask.FromResult<byte[]?>(null);

        public ValueTask SetAsync(string key, byte[] value, string[]? tags, TimeSpan validFor, CancellationToken cancellationToken) => ValueTask.CompletedTask;
    }

    /// <summary>
    /// <c>AI:OpenAI:ChatApiKey</c> is cleared for the same reason as in <c>ProductImageLifecycleTests</c>: with a key
    /// configured, <c>AnalyzeProductImageAgent</c> rejects a solid colour square as "not a car" and the whole class
    /// fails on a developer's machine while passing on CI. Edge caching is turned on because the <c>Cache-Tag</c>
    /// header these tests compare the purge against is only written for a response the edge may keep.
    /// </summary>
    private async Task<AppTestServer> StartServer()
    {
        var server = new AppTestServer();

        await server.Build(
            configureTestServices: services =>
            {
                services.AddIntegrationApiOnlyTestsServices();

                var recorder = new RecordingOutputCacheStore();
                services.AddSingleton(recorder);
                services.RemoveAll<IOutputCacheStore>();
                services.AddSingleton<IOutputCacheStore>(recorder);
            },
            configureTestConfigurations: configuration =>
            {
                configuration["AI:OpenAI:ChatApiKey"] = null;
                configuration["ResponseCaching:EnableCdnEdgeCaching"] = "true";
            }).Start(TestContext.CancellationToken);

        return server;
    }

    /// <summary>
    /// Signs the tenant-admin in within <paramref name="scope"/>, so every typed API client resolved from that scope
    /// calls the server as her - which is what satisfies the upload endpoint's privileged session, selected tenant and
    /// ProductCatalog_Manage policies.
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
            DescriptionText = "image cache probe",
            DescriptionHTML = "<p>image cache probe</p>"
        };
    }

    /// <summary>
    /// The tag <c>GetAttachment</c> stamps and <c>PurgeAttachmentCache</c> evicts: one per attachment id, covering
    /// every kind and every <c>?v=</c>.
    /// </summary>
    private static string AttachmentCacheTag(Guid attachmentId) =>
        ResponseCacheService.AttachmentCacheTagTemplate.Replace("{attachmentId}", attachmentId.ToString()).ToLowerInvariant();

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

    /// <summary>Best effort: a test that failed early must not take the cleanup down with it.</summary>
    private async Task DeleteProduct(IProductController products, Guid productId)
    {
        try
        {
            var product = await products.Get(productId, CancellationToken.None);
            await products.Delete(product.Id, product.Version, CancellationToken.None);
        }
        catch (Exception) { }
    }
}

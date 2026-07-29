//+:cnd:noEmit
using ImageMagick;
using FluentStorage.Storage;
using System.Diagnostics.Metrics;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.Infrastructure.SignalR;
//#endif
using Boilerplate.Server.Api.Features.Identity;
using Boilerplate.Server.Api.Features.Identity.Models;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Server.Api.Infrastructure.Services;

namespace Boilerplate.Server.Api.Features.Attachments;

[ApiController]
[ApiVersion(1)]
[Route("api/v{v:apiVersion}/[controller]/[action]")]
public partial class AttachmentController : AppControllerBase, IAttachmentController
{
    [AutoInject] private IStore blobStorage = default!;
    [AutoInject] private UserManager<User> userManager = default!;

    //#if (signalR == true)
    [AutoInject] private IServiceProvider serviceProvider = default!;
    [AutoInject] private ILogger<AttachmentController> logger = default!;
    //#endif

    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif

    //#if (module == "Sales" || module == "Admin")
    [AutoInject] private ResponseCacheService responseCacheService = default!;
    //#endif

    [AutoInject] private IConfiguration configuration = default!;

    // For open telemetry metrics
    private static readonly Histogram<double> updateResizeDurationHistogram = Meter.Current.CreateHistogram<double>("attachment.resize_duration", "ms", "Elapsed time to resize and persist an uploaded image");

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024 /*11MB*/)]
    public async Task<IActionResult> UploadUserProfilePicture(IFormFile? file, CancellationToken cancellationToken)
    {
        return await UploadAttachment(
             User.GetUserId(),
             [AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal],
             file,
             cancellationToken);
    }

    //#if (module == "Sales" || module == "Admin")
    [HttpPost("{productId}")]
    [RequestSizeLimit(11 * 1024 * 1024 /*11MB*/)]
    [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
    [Authorize(Policy = AppFeatures.AdminPanel.ProductCatalog_Manage)]
    //#if (multitenant == true)
    [Authorize(Policy = AuthPolicies.TENANT_SELECTED)]
    //#endif
    public async Task<IActionResult> UploadProductPrimaryImage(Guid productId, IFormFile? file, CancellationToken cancellationToken)
    {
        //#if (multitenant == true)
        await EnsureProductIsInCurrentTenant(productId, cancellationToken);
        //#endif

        return await UploadAttachment(
            productId,
            [AttachmentKind.ProductPrimaryImageMedium, AttachmentKind.ProductPrimaryImageOriginal],
            file,
            cancellationToken);
    }
    //#endif

    [AllowAnonymous]
    [HttpGet("{attachmentId}/{kind}")]
    [AppResponseCache(MaxAge = 3600 * 24 * 7, UserAgnostic = true)]
    public async Task<IActionResult> GetAttachment(Guid attachmentId, AttachmentKind kind, CancellationToken cancellationToken = default)
    {
        // If the backend is hosted behind a CDN (which is recommended for production), the GetAttachment method's returned stream will be cached on CDN edge servers.
        // Alternatively, you can generate URLs that allow clients to download files directly from the file storage, further reducing the load on the backend.
        // If security is a concern, you can generate short-lived signed URLs for the file storage. These signed URLs can be validated either at the CDN edge or on the file storage server, ensuring that only authorized users can access the files.

        var filePath = GetFilePath(attachmentId, kind);

        if (await blobStorage.ObjectExists(filePath, cancellationToken) is false)
            throw new ResourceNotFoundException().WithData("Reason", "The attachment does not exist.");

        var mimeType = kind switch
        {
            //#if (module == "Sales" || module == "Admin")
            AttachmentKind.ProductPrimaryImageMedium => "image/webp",
            //#endif
            AttachmentKind.UserProfileImageSmall => "image/webp",
            _ => "application/octet-stream" // The *Original kinds keep the uploaded bytes verbatim.
        };

        return File(await blobStorage.OpenRead(filePath, cancellationToken), mimeType, enableRangeProcessing: true);
    }

    [HttpDelete]
    public async Task DeleteUserProfilePicture(CancellationToken cancellationToken)
    {
        await DeleteAttachment(User.GetUserId(), [AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal], cancellationToken);
    }

    //#if (module == "Sales" || module == "Admin")
    [HttpDelete("{productId}")]
    [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
    [Authorize(Policy = AppFeatures.AdminPanel.ProductCatalog_Manage)]
    //#if (multitenant == true)
    [Authorize(Policy = AuthPolicies.TENANT_SELECTED)]
    //#endif
    public async Task DeleteProductPrimaryImage(Guid productId, CancellationToken cancellationToken)
    {
        //#if (multitenant == true)
        await EnsureProductIsInCurrentTenant(productId, cancellationToken);
        //#endif

        await DeleteAttachment(productId, [AttachmentKind.ProductPrimaryImageMedium, AttachmentKind.ProductPrimaryImageOriginal], cancellationToken);
    }

    //#if (multitenant == true)
    /// <summary>
    /// Attachments aren't tenant-aware, so before creating/updating/deleting a product's images, the product must belong
    /// to the current tenant. Products that are being added aren't in the database yet, so they get a pass here.
    /// </summary>
    private async Task EnsureProductIsInCurrentTenant(Guid productId, CancellationToken cancellationToken)
    {
        var productTenantId = await DbContext.Products
            .IgnoreQueryFilters()
            .Where(p => p.Id == productId)
            .Select(p => (Guid?)p.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (productTenantId is not null && productTenantId != TenantProvider.GetCurrentTenantId())
            throw new ResourceNotFoundException().WithData("Reason", "The product belongs to another tenant.");
    }
    //#endif
    //#endif

    //#if (signalR == true)
    private async Task PublishUserProfileUpdated(User user, CancellationToken cancellationToken)
    {
        // Notify other sessions of the user that user's info has been updated, so they'll update their UI.
        var currentUserSessionId = User.GetSessionId();
        var userSessionIdsExceptCurrentUserSessionId = await DbContext.UserSessions
            .Where(us => us.UserId == user.Id && us.Id != currentUserSessionId && us.SignalRConnectionId != null)
            .Select(us => us.SignalRConnectionId!)
            .ToArrayAsync(cancellationToken);
        await appHubContext.Clients.Clients(userSessionIdsExceptCurrentUserSessionId).Publish(SharedAppMessages.PROFILE_UPDATED, user.Map(), cancellationToken);
    }
    //#endif

    private async Task DeleteAttachment(Guid attachmentId, AttachmentKind[] kinds, CancellationToken cancellationToken)
    {
        var attachments = await DbContext.Attachments.Where(p => p.Id == attachmentId && kinds.Contains(p.Kind)).ToArrayAsync(cancellationToken);

        foreach (var attachment in attachments)
        {
            var filePath = attachment.Path;

            if (await blobStorage.ObjectExists(filePath, cancellationToken) is false)
                throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ImageCouldNotBeFound)]);

            await blobStorage.DeleteObject(filePath, cancellationToken);

            //#if (module == "Sales" || module == "Admin")
            if (attachment.Kind is AttachmentKind.ProductPrimaryImageOriginal)
            {
                var product = await DbContext.Products.FindAsync([attachment.Id], cancellationToken);
                if (product is not null) // else means product is being added to the database.
                {
                    product.HasPrimaryImage = false;
                    product.PrimaryImageAltText = null;
                    await DbContext.SaveChangesAsync(cancellationToken);
                    await responseCacheService.PurgeProductCache(product.ShortId);
                }
            }
            //#endif

            if (attachment.Kind is AttachmentKind.UserProfileImageOriginal)
            {
                var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
                user!.HasProfilePicture = false;

                var result = await userManager.UpdateAsync(user);
                if (!result.Succeeded)
                    throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

                //#if (signalR == true)
                await PublishUserProfileUpdated(user, cancellationToken);
                //#endif
            }

            DbContext.Attachments.Remove(attachment);
            await DbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task<IActionResult> UploadAttachment(Guid attachmentId, AttachmentKind[] kinds, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new BadRequestException().WithData("Reason", "No file provided.");

        string? altText = null; // AI-generated alt text, when the analysis agent is configured.

        var preparedUploads = new List<(Attachment Attachment, byte[]? ResizedBytes)>();

        foreach (var kind in kinds)
        {
            var attachment = new Attachment
            {
                Id = attachmentId,
                Kind = kind,
                Path = GetFilePath(attachmentId, kind),
            };

            (bool NeedsResize, uint Width, uint Height) imageResizeContext = kind switch
            {
                AttachmentKind.UserProfileImageSmall => (true, 256, 256),
                //#if (module == "Sales" || module == "Admin")
                AttachmentKind.ProductPrimaryImageMedium => (true, 512, 512),
                //#endif
                _ => (false, 0, 0)
            };

            if (imageResizeContext.NeedsResize is false)
            {
                preparedUploads.Add((attachment, null));
                continue;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Process-wide ImageMagick ResourceLimits are configured at startup (Program.Services.cs), so what a
            // decode of an untrusted upload can cost is bounded, and anything Magick.NET cannot decode throws here.
            using MagickImage sourceImage = new(file.OpenReadStream());

            if (sourceImage.Width < imageResizeContext.Width || sourceImage.Height < imageResizeContext.Height)
                return BadRequest(Localizer[nameof(AppStrings.ImageTooSmall), imageResizeContext.Width, imageResizeContext.Height, sourceImage.Width, sourceImage.Height].ToString());

            sourceImage.Resize(new MagickGeometry(imageResizeContext.Width, imageResizeContext.Height));

            var resizedBytes = sourceImage.ToByteArray(MagickFormat.WebP);

            updateResizeDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("kind", kind.ToString()));

            //#if (module == "Sales" || module == "Admin")
            if (kind is AttachmentKind.ProductPrimaryImageMedium)
            {
                //#if (signalR == true)
                if (serviceProvider.GetKeyedService<Microsoft.Agents.AI.AIAgent>("AnalyzeProductImageAgent") is Microsoft.Agents.AI.AIAgent analyzeProductImageAgent)
                {
                    ChatOptions chatOptions = new()
                    {
                        ResponseFormat = ChatResponseFormat.Json,
                        AdditionalProperties = new()
                        {
                            ["response_format"] = new { type = "json_object" }
                        }
                    };

                    configuration.GetRequiredSection("AI:ChatOptions").Bind(chatOptions);

                    var response = await analyzeProductImageAgent.RunAsync<AIImageReviewResponse>(
                        messages: [
                            new ChatMessage(ChatRole.User,
                                "Analyze this product image for our car catalog. Is this a valid car product image that meets our quality and content standards?")
                            {
                                Contents = [new DataContent(resizedBytes, "image/webp")]
                            }
                        ],
                        cancellationToken: cancellationToken,
                        options: new Microsoft.Agents.AI.ChatClientAgentRunOptions(chatOptions));

                    if (response.Result.IsCar is false)
                    {
                        logger.LogWarning(
                            "Image validation failed - Not a car product. Confidence: {Confidence}, Reasoning: {Reasoning}",
                            response.Result.Confidence,
                            response.Result.Reasoning);
                        // Nothing has been written or deleted yet, so this rejection is real.
                        return BadRequest(Localizer[nameof(AppStrings.ImageNotCarError)].ToString());
                    }

                    if (response.Result.Confidence < 0.85)
                    {
                        logger.LogWarning(
                            "Image analysis low confidence ({Confidence}). Reasoning: {Reasoning}. Alt text: {AltText}",
                            response.Result.Confidence,
                            response.Result.Reasoning,
                            response.Result.Alt);
                    }

                    altText = response.Result.Alt;
                }
                //#endif
            }
            //#endif

            preparedUploads.Add((attachment, resizedBytes));
        }

        //  ---------------------------------------------------------------------------------------------
        //  PHASE 2 - everything validated; now mutate. Old blobs are deleted by the PATH STORED ON THE ROW,
        //  not by recomputing the key: the *Original key embeds the uploaded file's extension, so a
        //  png -> jpg re-upload used to compute a different key, leaving the old blob referenced by no row
        //  and outside every deletion path the app offers.
        //  ---------------------------------------------------------------------------------------------
        var staleAttachments = await DbContext.Attachments.Where(att => att.Id == attachmentId).ToArrayAsync(cancellationToken);

        var executionStrategy = DbContext.Database.CreateExecutionStrategy();
        await executionStrategy.ExecuteAsync(async () =>
        {
            await using var transaction = await DbContext.Database.BeginTransactionAsync(cancellationToken);

            DbContext.Attachments.RemoveRange(staleAttachments);
            await DbContext.Attachments.AddRangeAsync(preparedUploads.Select(u => u.Attachment), cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        });

        foreach (var stalePath in staleAttachments.Select(att => att.Path)
                                                  .Except(preparedUploads.Select(u => u.Attachment.Path)))
        {
            if (await blobStorage.ObjectExists(stalePath, cancellationToken))
            {
                await blobStorage.DeleteObject(stalePath, cancellationToken);
            }
        }

        foreach (var (attachment, resizedBytes) in preparedUploads)
        {
            if (resizedBytes is not null)
            {
                await blobStorage.SetBytes(attachment.Path, resizedBytes, cancellationToken: cancellationToken);
            }
            else
            {
                await blobStorage.SetObject(attachment.Path, file.OpenReadStream(), cancellationToken: cancellationToken);
            }
        }

        //#if (module == "Sales" || module == "Admin")
        if (kinds.Contains(AttachmentKind.ProductPrimaryImageMedium))
        {
            // Written server-side, mirroring DeleteAttachment. Previously the upload path returned Ok(altText)
            // and left the client to PUT HasPrimaryImage/PrimaryImageAltText back on a later Update. On an
            // image-only replacement that changed no property, EF issued no UPDATE, Product.Version never
            // moved, and the ?v={Version} cache-buster on the attachment URL stayed byte-identical - so
            // anyone holding the old copy kept it for the full 7-day max-age.
            var product = await DbContext.Products.FindAsync([attachmentId], cancellationToken);
            if (product is not null) // else means product is being added to the database.
            {
                product.HasPrimaryImage = true;
                product.PrimaryImageAltText = altText;
                await DbContext.SaveChangesAsync(cancellationToken);
                await responseCacheService.PurgeProductCache(product.ShortId);
            }
        }
        //#endif

        if (kinds.Contains(AttachmentKind.UserProfileImageSmall))
        {
            var user = await userManager.FindByIdAsync(User.GetUserId().ToString());
            user!.HasProfilePicture = true;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
                throw new ResourceValidationException(result.Errors.Select(err => new LocalizedString(err.Code, err.Description)).ToArray());

            //#if (signalR == true)
            await PublishUserProfileUpdated(user, cancellationToken);
            //#endif
        }

        return Ok(altText);
    }

    /// <summary>
    /// Deterministic for every kind: no part of the key comes from the uploaded file name. The *Original kinds
    /// deliberately carry NO extension - deriving one from the upload meant a png -> jpg re-upload computed a
    /// different key, so the old blob was left referenced by no row and outside every deletion path, and
    /// <see cref="GetAttachment"/> (which has no file name) computed a key that could never match what was stored.
    /// <br/>
    /// Environment variables are expanded over the CONFIGURED directory prefix only, never over anything the
    /// client influenced - the file name used to flow into ExpandEnvironmentVariables, so a name ending
    /// ".%TEMP%" expanded a server environment value straight into the storage key.
    /// </summary>
    private string GetFilePath(Guid attachmentId, AttachmentKind kind)
    {
        var directory = kind switch
        {
            //#if (module == "Sales" || module == "Admin")
            AttachmentKind.ProductPrimaryImageMedium or AttachmentKind.ProductPrimaryImageOriginal => AppSettings.ProductImagesDir,
            //#endif
            AttachmentKind.UserProfileImageSmall or AttachmentKind.UserProfileImageOriginal => AppSettings.UserProfileImagesDir,
            _ => throw new NotImplementedException()
        };

        directory = Environment.ExpandEnvironmentVariables(directory);

        return kind switch
        {
            //#if (module == "Sales" || module == "Admin")
            AttachmentKind.ProductPrimaryImageMedium => $"{directory}{attachmentId}_{kind}.webp",
            //#endif
            AttachmentKind.UserProfileImageSmall => $"{directory}{attachmentId}_{kind}.webp",
            _ => $"{directory}{attachmentId}_{kind}"
        };
    }

    //#if (signalR == true)
    public record AIImageReviewResponse(bool IsCar, double Confidence, string? Alt, string? Reasoning);
    //#endif
}

//+:cnd:noEmit
using ImageMagick;
using FluentStorage.Storage;
using System.Diagnostics.Metrics;
using Boilerplate.Server.Api.Features.Identity;
using Boilerplate.Shared.Features.Attachments;
using Boilerplate.Server.Shared.Infrastructure.Services;

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

    [AutoInject] private ResponseCacheService responseCacheService = default!;

    [AutoInject] private IConfiguration configuration = default!;

    /// <summary>The largest upload every endpoint here accepts; the Dev MCP reports this value rather than a copy of it.</summary>
    public const int MaxUploadSizeBytes = 11 * 1024 * 1024;

    // For open telemetry metrics
    private static readonly Histogram<double> updateResizeDurationHistogram = Meter.Current.CreateHistogram<double>("attachment.resize_duration", "ms", "Elapsed time to resize and persist an uploaded image");

    [HttpPost]
    [RequestSizeLimit(MaxUploadSizeBytes)]
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
    [RequestSizeLimit(MaxUploadSizeBytes)]
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

    //#if (signalR == true)
    /// <summary>
    /// Takes an image the user attached to an AI chat message and answers with the id it was stored under, which is
    /// all the client needs to build the attachment url it puts in the message.
    /// <para>
    /// The id is minted here rather than accepted from the client: an attachment id is the whole address of a blob
    /// (See <see cref="GetFilePath(Guid, AttachmentKind)"/>), so a caller-chosen one would let a user overwrite somebody else's attachment -
    /// their profile picture included - by naming its id.
    /// </para>
    /// </summary>
    [HttpPost]
    [RequestSizeLimit(MaxUploadSizeBytes)]
    public async Task<IActionResult> UploadAiChatImage(IFormFile? file, CancellationToken cancellationToken)
    {
        var attachmentId = Guid.CreateSequentialGuid();

        var result = await UploadAttachment(attachmentId, [AttachmentKind.AiChatImage], file, cancellationToken);

        return result is OkObjectResult ? Ok(attachmentId.ToString()) : result;
    }
    //#endif

    [AllowAnonymous]
    [HttpGet("{attachmentId}/{kind}")]
    [AppResponseCache(MaxAge = 3600 * 24 * 7, UserAgnostic = true, SkipOutputCache = true, CacheTagTemplate = ResponseCacheService.AttachmentCacheTagTemplate)]
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
            //#if (signalR == true)
            AttachmentKind.AiChatImage => "image/webp",
            //#endif
            AttachmentKind.UserProfileImageSmall => "image/webp",
            _ => "application/octet-stream" // The *Original kinds keep the uploaded format, whatever it was.
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

        // After the loop: every kind shares one tag, and by now no blob is left for a racing read to re-cache.
        await responseCacheService.PurgeAttachmentCache(attachmentId);
    }

    private async Task<IActionResult> UploadAttachment(Guid attachmentId, AttachmentKind[] kinds, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new BadRequestException().WithData("Reason", "No file provided.");

        string? altText = null; // AI-generated alt text, when the analysis agent is configured.

        var preparedUploads = new List<(Attachment Attachment, byte[] Bytes)>();

        foreach (var kind in kinds)
        {
            var attachment = new Attachment
            {
                Id = attachmentId,
                Kind = kind,
                Path = GetFilePath(attachmentId, kind),
                CreatedOn = TimeProvider.GetUtcNow(),
            };

            // ShrinkOnly makes the size a ceiling instead of a floor: the picture is only scaled down to it, and one
            // that is already smaller is stored exactly as it arrived rather than rejected or blown up.
            (bool NeedsResize, uint Width, uint Height, bool ShrinkOnly) imageResizeContext = kind switch
            {
                AttachmentKind.UserProfileImageSmall => (true, 256, 256, false),
                //#if (module == "Sales" || module == "Admin")
                AttachmentKind.ProductPrimaryImageMedium => (true, 512, 512, false),
                //#endif
                //#if (signalR == true)
                // Whatever the user had on screen when they attached it - a crop, a screenshot, a phone photo - so
                // there is no size below which it is not worth showing the model.
                AttachmentKind.AiChatImage => (true, 512, 512, true),
                //#endif
                _ => (false, 0, 0, false)
            };

            Stopwatch stopwatch = Stopwatch.StartNew();

            // Every kind is decoded, including the ones that are not resized: stripping the metadata means re-encoding,
            // so the *Original kinds keep the uploaded FORMAT rather than the uploaded bytes.
            // Process-wide ImageMagick ResourceLimits are configured at startup (Program.Services.cs), so what a
            // decode of an untrusted upload can cost is bounded, and anything Magick.NET cannot decode throws here.
            // OpenReadStream hands out a NEW stream per call and MagickImage does not take ownership of it.
            using var sourceStream = file.OpenReadStream();
            MagickImage? decodedImage;
            try
            {
                decodedImage = new(sourceStream);
            }
            catch (MagickException)
            {
                // An undecodable upload is bad input, not a server fault - a 400 like the endpoint's other rejections,
                // instead of a 500 with a Critical log per attempt. Only the decode is caught: a failure in the resize
                // or encode below would be a server problem and stays loud.
                return BadRequest(Localizer[nameof(AppStrings.UnsupportedImageFormat)].ToString());
            }
            using MagickImage sourceImage = decodedImage;

            if (imageResizeContext.NeedsResize)
            {
                if (imageResizeContext.ShrinkOnly is false &&
                    (sourceImage.Width < imageResizeContext.Width || sourceImage.Height < imageResizeContext.Height))
                    return BadRequest(Localizer[nameof(AppStrings.ImageTooSmall), imageResizeContext.Width, imageResizeContext.Height, sourceImage.Width, sourceImage.Height].ToString());

                sourceImage.Resize(new MagickGeometry(imageResizeContext.Width, imageResizeContext.Height) { Greater = imageResizeContext.ShrinkOnly });
            }

            // Drops EXIF - GPS coordinates, capture time, camera serial - along with XMP, IPTC, comments and the ICC
            // profile. A phone photo carries all of it, and the WebP re-encode below does NOT drop it on its own.
            sourceImage.Strip();

            var storedBytes = imageResizeContext.NeedsResize
                ? sourceImage.ToByteArray(MagickFormat.WebP)
                : sourceImage.ToByteArray();

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
                                Contents = [new DataContent(storedBytes, "image/webp")]
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

            preparedUploads.Add((attachment, storedBytes));
        }

        //  ---------------------------------------------------------------------------------------------
        //  PHASE 2 - everything validated; now mutate.
        //
        //  A re-upload does NOT delete and re-insert the rows. Attachment's key is composite - { Id, Kind }
        //  (AttachmentConfiguration) - and GetFilePath is deterministic over exactly those two values, so a
        //  re-upload produces rows whose keys AND Path are byte-identical to the existing ones. Removing and
        //  re-adding them in one SaveChangesAsync would also throw: EF cannot track a second instance with a
        //  key it already tracks, even when the tracked one is marked Deleted.
        //
        //  For the same reason there is no stale blob to clean up: the new key IS the old key, so the writes
        //  below overwrite in place.
        //  ---------------------------------------------------------------------------------------------
        var existingKinds = await DbContext.Attachments
            .Where(att => att.Id == attachmentId)
            .Select(att => att.Kind)
            .ToArrayAsync(cancellationToken);

        var newAttachments = preparedUploads.Select(u => u.Attachment)
                                            .Where(att => existingKinds.Contains(att.Kind) is false)
                                            .ToArray();

        if (newAttachments.Length > 0)
        {
            await DbContext.Attachments.AddRangeAsync(newAttachments, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);
        }

        var wroteAnyBlob = false;

        try
        {
            foreach (var (attachment, storedBytes) in preparedUploads)
            {
                await blobStorage.SetBytes(attachment.Path, storedBytes, cancellationToken: cancellationToken);
                wroteAnyBlob = true;
            }
        }
        finally
        {
            // The replacement is stored under the very key the old bytes were (See GetFilePath), so nothing else
            // invalidates the copies already on the edge and in browsers. In the finally because a second kind that
            // fails still leaves the first one replaced, and the edge still holding what it replaced.
            if (wroteAnyBlob)
            {
                await responseCacheService.PurgeAttachmentCache(attachmentId);
            }
        }

        //#if (module == "Sales" || module == "Admin")
        if (kinds.Contains(AttachmentKind.ProductPrimaryImageMedium))
        {
            // Written server-side: an image-only replacement changes no property, so without this EF issues no
            // UPDATE, Product.Version never moves, and every document keeps naming the same ?v= url.
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
    private string GetFilePath(Guid attachmentId, AttachmentKind kind) => GetFilePath(AppSettings, attachmentId, kind);

    /// <inheritdoc cref="GetFilePath(Guid, AttachmentKind)"/>
    /// <remarks>
    /// Static so that whoever needs a blob can work out where it is without asking this controller or the database
    /// for it - <c>AppChatbot</c> reads an attached image straight off storage this way.
    /// </remarks>
    public static string GetFilePath(ServerApiSettings appSettings, Guid attachmentId, AttachmentKind kind)
    {
        var directory = kind switch
        {
            //#if (module == "Sales" || module == "Admin")
            AttachmentKind.ProductPrimaryImageMedium or AttachmentKind.ProductPrimaryImageOriginal => appSettings.ProductImagesDir,
            //#endif
            //#if (signalR == true)
            AttachmentKind.AiChatImage => appSettings.AiChatImagesDir,
            //#endif
            AttachmentKind.UserProfileImageSmall or AttachmentKind.UserProfileImageOriginal => appSettings.UserProfileImagesDir,
            _ => throw new NotImplementedException()
        };

        directory = Environment.ExpandEnvironmentVariables(directory);

        return kind switch
        {
            //#if (module == "Sales" || module == "Admin")
            AttachmentKind.ProductPrimaryImageMedium => $"{directory}{attachmentId}_{kind}.webp",
            //#endif
            //#if (signalR == true)
            AttachmentKind.AiChatImage => $"{directory}{attachmentId}_{kind}.webp",
            //#endif
            AttachmentKind.UserProfileImageSmall => $"{directory}{attachmentId}_{kind}.webp",
            _ => $"{directory}{attachmentId}_{kind}"
        };
    }

    //#if (signalR == true)
    public record AIImageReviewResponse(bool IsCar, double Confidence, string? Alt, string? Reasoning);
    //#endif
}

//+:cnd:noEmit
using ImageMagick;
using FluentStorage.Blobs;
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

[Route("api/[controller]/[action]")]
[ApiController]
public partial class AttachmentController : AppControllerBase, IAttachmentController
{
    [AutoInject] private IBlobStorage blobStorage = default!;
    [AutoInject] private UserManager<User> userManager = default!;

    //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
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
    public async Task<IActionResult> UploadProductPrimaryImage(Guid productId, IFormFile? file, CancellationToken cancellationToken)
    {
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
        var filePath = GetFilePath(attachmentId, kind);

        if (await blobStorage.ExistsAsync(filePath, cancellationToken) is false)
            throw new ResourceNotFoundException();

        var mimeType = kind switch
        {
            _ => "image/webp" // Currently, all attachment types are images.
        };

        return File(await blobStorage.OpenReadAsync(filePath, cancellationToken), mimeType, enableRangeProcessing: true);
    }

    [HttpDelete]
    public async Task DeleteUserProfilePicture(CancellationToken cancellationToken)
    {
        await DeleteAttachment(User.GetUserId(), [AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal], cancellationToken);
    }

    //#if (module == "Sales" || module == "Admin")
    [HttpDelete("{productId}"), Authorize(Policy = AppFeatures.AdminPanel.ManageProductCatalog)]
    public async Task DeleteProductPrimaryImage(Guid productId, CancellationToken cancellationToken)
    {
        await DeleteAttachment(productId, [AttachmentKind.ProductPrimaryImageMedium, AttachmentKind.ProductPrimaryImageOriginal], cancellationToken);
    }
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

            if (await blobStorage.ExistsAsync(filePath, cancellationToken) is false)
                throw new ResourceNotFoundException(Localizer[nameof(AppStrings.ImageCouldNotBeFound)]);

            await blobStorage.DeleteAsync(filePath, cancellationToken);

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
            throw new BadRequestException();

        string? altText = null; // For future use, e.g., AI-generated alt text.

        await DbContext.Attachments.Where(att => att.Id == attachmentId).ExecuteDeleteAsync(cancellationToken);

        foreach (var kind in kinds)
        {
            var attachment = new Attachment
            {
                Id = attachmentId,
                Kind = kind,
                Path = GetFilePath(attachmentId, kind, file.FileName),
            };

            if (await blobStorage.ExistsAsync(attachment.Path, cancellationToken))
            {
                await blobStorage.DeleteAsync(attachment.Path, cancellationToken);
            }

            (bool NeedsResize, uint Width, uint Height) imageResizeContext = kind switch
            {
                AttachmentKind.UserProfileImageSmall => (true, 256, 256),
                //#if (module == "Sales" || module == "Admin")
                AttachmentKind.ProductPrimaryImageMedium => (true, 512, 512),
                //#endif
                _ => (false, 0, 0)
            };

            byte[]? imageBytes = null;

            if (imageResizeContext.NeedsResize)
            {
                Stopwatch stopwatch = Stopwatch.StartNew();
                using MagickImage sourceImage = new(file.OpenReadStream());

                if (sourceImage.Width < imageResizeContext.Width || sourceImage.Height < imageResizeContext.Height)
                    return BadRequest(Localizer[nameof(AppStrings.ImageTooSmall), imageResizeContext.Width, imageResizeContext.Height, sourceImage.Width, sourceImage.Height].ToString());

                sourceImage.Resize(new MagickGeometry(imageResizeContext.Width, imageResizeContext.Height));

                await blobStorage.WriteAsync(attachment.Path, imageBytes = sourceImage.ToByteArray(MagickFormat.WebP), cancellationToken: cancellationToken);

                updateResizeDurationHistogram.Record(stopwatch.Elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("kind", kind.ToString()));
            }
            else
            {
                await blobStorage.WriteAsync(attachment.Path, file.OpenReadStream(), cancellationToken: cancellationToken);
            }

            await DbContext.Attachments.AddAsync(attachment, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);

            //#if (module == "Sales" || module == "Admin")
            if (attachment.Kind is AttachmentKind.ProductPrimaryImageMedium)
            {
                //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
                if (serviceProvider.GetService<IChatClient>() is IChatClient chatClient)
                {
                    var imageAnalysisAgent = chatClient.AsAIAgent(
                        instructions: """
                        You are a Product Image Specialist Agent. Your role is to analyze product images for an e-commerce catalog.

                        ANALYSIS PROCESS:
                        1. First, examine the image contents carefully
                        2. Determine if the primary subject is a car (vehicle)
                        3. If it is a car, provide a detailed, SEO-friendly description
                        4. If it is NOT a car, explain why it doesn't meet catalog requirements

                        RESPONSE FORMAT:
                        Return ONLY a JSON object with:
                        - "isCar": boolean (true if image shows a car, false otherwise)
                        - "confidence": number between 0-1 indicating certainty of classification
                        - "alt": string with detailed description for accessibility and SEO
                        - "reasoning": string briefly explaining your analysis decision

                        VALIDATION RULES:
                        - Image quality must be acceptable for catalog use
                        - Car must be clearly visible as the main subject
                        """,
                        name: "ProductImageAnalystAgent",
                        description: "Analyzes product images to ensure they meet catalog standards for car products");

                    ChatOptions chatOptions = new()
                    {
                        ResponseFormat = ChatResponseFormat.Json,
                        AdditionalProperties = new()
                        {
                            ["response_format"] = new { type = "json_object" }
                        }
                    };

                    configuration.GetRequiredSection("AI:ChatOptions").Bind(chatOptions);

                    var response = await imageAnalysisAgent.RunAsync<AIImageReviewResponse>(
                        messages: [
                            new ChatMessage(ChatRole.User, 
                                "Analyze this product image for our car catalog. Is this a valid car product image that meets our quality and content standards?")
                            {
                                Contents = [new DataContent(imageBytes, "image/webp")]
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

            if (kind is AttachmentKind.UserProfileImageSmall)
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
        }

        return Ok(altText);
    }

    private string GetFilePath(Guid attachmentId, AttachmentKind kind, string? fileName = null)
    {
        var filePath = kind switch
        {
            //#if (module == "Sales" || module == "Admin")
            AttachmentKind.ProductPrimaryImageMedium => $"{AppSettings.ProductImagesDir}{attachmentId}_{kind}.webp",
            AttachmentKind.ProductPrimaryImageOriginal => $"{AppSettings.ProductImagesDir}{attachmentId}_{kind}{Path.GetExtension(fileName)}",
            //#endif
            AttachmentKind.UserProfileImageSmall => $"{AppSettings.UserProfileImagesDir}{attachmentId}_{kind}.webp",
            AttachmentKind.UserProfileImageOriginal => $"{AppSettings.UserProfileImagesDir}{attachmentId}_{kind}{Path.GetExtension(fileName)}",
            _ => throw new NotImplementedException()
        };

        filePath = Environment.ExpandEnvironmentVariables(filePath);

        return filePath;
    }

    //#if (signalR == true || database == "PostgreSQL" || database == "SqlServer")
    public record AIImageReviewResponse(bool IsCar, double Confidence, string? Alt, string? Reasoning);
    //#endif
}

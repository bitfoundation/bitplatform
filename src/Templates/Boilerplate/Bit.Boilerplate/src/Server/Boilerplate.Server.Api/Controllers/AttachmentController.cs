//+:cnd:noEmit
using ImageMagick;
using FluentStorage.Blobs;
//#if (signalR == true)
using Microsoft.AspNetCore.SignalR;
using Boilerplate.Server.Api.SignalR;
//#endif
using Boilerplate.Shared.Controllers;
using Boilerplate.Server.Api.Services;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Models.Attachments;

namespace Boilerplate.Server.Api.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]
public partial class AttachmentController : AppControllerBase, IAttachmentController
{
    [AutoInject] private IBlobStorage blobStorage = default!;
    [AutoInject] private UserManager<User> userManager = default!;

    //#if (signalR == true)
    [AutoInject] private IHubContext<AppHub> appHubContext = default!;
    //#endif

    //#if (module == "Sales" || module == "Admin")
    [AutoInject] private ResponseCacheService responseCacheService = default!;
    //#endif

    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024 /*11MB*/)]
    public async Task UploadUserProfilePicture(IFormFile? file, CancellationToken cancellationToken)
    {
        await UploadAttachment(
            User.GetUserId(),
            [AttachmentKind.UserProfileImageSmall, AttachmentKind.UserProfileImageOriginal],
            file,
            cancellationToken);
    }

    //#if (module == "Sales" || module == "Admin")
    [HttpPost("{productId}")]
    [RequestSizeLimit(11 * 1024 * 1024 /*11MB*/)]
    public async Task UploadProductPrimaryImage(Guid productId, IFormFile? file, CancellationToken cancellationToken)
    {
        await UploadAttachment(
            productId,
            [AttachmentKind.ProductPrimaryImageMedium, AttachmentKind.ProductPrimaryImageOriginal],
            file,
            cancellationToken);
    }
    //#endif

    [AllowAnonymous]
    [HttpGet("{attachmentId}/{kind}")]
    [AppResponseCache(MaxAge = 3600 * 24 * 7, UserAgnostic = true)]
    public async Task<IActionResult> GetAttachment(Guid attachmentId, AttachmentKind kind, CancellationToken cancellationToken)
    {
        var filePath = kind switch
        {
            //#if (module == "Sales" || module == "Admin")
            AttachmentKind.ProductPrimaryImageMedium => $"{AppSettings.ProductImagesDir}{attachmentId}_{kind}.webp",
            //#endif
            AttachmentKind.UserProfileImageSmall => $"{AppSettings.UserProfileImagesDir}{attachmentId}_{kind}.webp",
            _ => throw new NotImplementedException()
        };

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
    [HttpDelete("{productId}")]
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
        await appHubContext.Clients.Clients(userSessionIdsExceptCurrentUserSessionId).SendAsync(SignalREvents.PUBLISH_MESSAGE, SharedPubSubMessages.PROFILE_UPDATED, user, cancellationToken);
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

    private async Task UploadAttachment(Guid attachmentId, AttachmentKind[] kinds, IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null)
            throw new BadRequestException();

        await DbContext.Attachments.Where(att => att.Id == attachmentId).ExecuteDeleteAsync(cancellationToken);

        foreach (var kind in kinds)
        {
            var attachment = new Attachment
            {
                Id = attachmentId,
                Kind = kind,
                Path = kind switch
                {
                    AttachmentKind.UserProfileImageOriginal => $"{AppSettings.UserProfileImagesDir}{attachmentId}_{kind}{Path.GetExtension(file.FileName)}",
                    AttachmentKind.UserProfileImageSmall => $"{AppSettings.UserProfileImagesDir}{attachmentId}_{kind}.webp",
                    //#if (module == "Sales" || module == "Admin")
                    AttachmentKind.ProductPrimaryImageOriginal => $"{AppSettings.ProductImagesDir}{attachmentId}_{kind}{Path.GetExtension(file.FileName)}",
                    AttachmentKind.ProductPrimaryImageMedium => $"{AppSettings.ProductImagesDir}{attachmentId}_{kind}.webp",
                    //#endif
                    _ => throw new NotImplementedException()
                }
            };

            if (await blobStorage.ExistsAsync(attachment.Path, cancellationToken))
            {
                await blobStorage.DeleteAsync(attachment.Path, cancellationToken);
            }

            var needsResize = kind switch
            {
                AttachmentKind.UserProfileImageSmall => true,
                //#if (module == "Sales" || module == "Admin")
                AttachmentKind.ProductPrimaryImageMedium => true,
                //#endif
                _ => false
            };

            if (needsResize)
            {
                var resizedImageSize = kind switch
                {
                    AttachmentKind.UserProfileImageSmall => new MagickGeometry(256, 256),
                    //#if (module == "Sales" || module == "Admin")
                    AttachmentKind.ProductPrimaryImageMedium => new MagickGeometry(512, 512),
                    //#endif
                    _ => throw new NotImplementedException()
                };

                using MagickImage sourceImage = new(file.OpenReadStream());

                sourceImage.Resize(resizedImageSize);

                await blobStorage.WriteAsync(attachment.Path, sourceImage.ToByteArray(MagickFormat.WebP), cancellationToken: cancellationToken);
            }
            else
            {
                await blobStorage.WriteAsync(attachment.Path, file.OpenReadStream(), cancellationToken: cancellationToken);
            }

            await DbContext.Attachments.AddAsync(attachment, cancellationToken);
            await DbContext.SaveChangesAsync(cancellationToken);

            //#if (module == "Sales" || module == "Admin")
            if (attachment.Kind is AttachmentKind.ProductPrimaryImageOriginal)
            {
                var product = await DbContext.Products.FindAsync([attachment.Id], cancellationToken);
                if (product is not null) // else means product is being added to the database.
                {
                    product.HasPrimaryImage = true;
                    await DbContext.SaveChangesAsync(cancellationToken);
                    await responseCacheService.PurgeProductCache(product.ShortId);
                }
            }
            //#endif

            if (kind is AttachmentKind.UserProfileImageOriginal)
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
    }
}

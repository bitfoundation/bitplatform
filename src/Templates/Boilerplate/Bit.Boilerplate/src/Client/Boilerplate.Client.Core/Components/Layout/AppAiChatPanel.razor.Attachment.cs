using Boilerplate.Shared.Features.Attachments;

namespace Boilerplate.Client.Core.Components.Layout;

// The image half of a message. Picking one only shows it - the thumbnail is the browser's own object url
// (BitFileUpload.ShowPreview) - and it is uploaded only when the message it belongs to is sent.
public partial class AppAiChatPanel
{
    private bool isUploadingAttachment;

    /// <summary>The image waiting to be sent: chosen and previewed, not necessarily uploaded yet.</summary>
    private BitFileInfo? pendingAttachment;

    /// <summary>
    /// The id <see cref="pendingAttachment"/> was stored under, once the upload has answered with one. This is what
    /// the message carries: the backend looks the blob up by id among its own chat images
    /// (See <c>AppChatbot.ReadAttachedImage</c>).
    /// </summary>
    private Guid? pendingAttachmentId;

    private TaskCompletionSource<bool>? attachmentUploadTcs;

    private BitFileUpload attachmentUploadRef = default!;


    /// <summary>Where the backend serves a stored chat image from - the route and the kind are fixed, the id is not.</summary>
    private string GetAttachmentUrl(Guid attachmentId)
        => new Uri(AbsoluteServerAddress, $"api/v1/Attachment/GetAttachment/{attachmentId}/{AttachmentKind.AiChatImage}").ToString();

    private async Task<string> GetAttachmentUploadUrl()
    {
        var uploadUrl = new Uri(AbsoluteServerAddress, "/api/v1/Attachment/UploadAiChatImage").ToString();

        if (CultureInfoManager.InvariantGlobalization is false)
        {
            uploadUrl += $"?culture={CultureInfo.CurrentUICulture.Name}"; // To have localized error messages from the server (if any).
        }

        return uploadUrl;
    }

    private async Task<Dictionary<string, string>> GetAttachmentUploadRequestHeaders()
    {
        var accessToken = await AuthManager.GetFreshAccessToken(requestedBy: nameof(BitFileUpload));

        return new() { { "Authorization", $"Bearer {accessToken}" } };
    }

    private async Task BrowseAttachment()
    {
        // AutoReset empties the list before the dialog opens, which revokes the preview url of whatever was picked
        // before - so the thumbnail goes with it, even if the dialog is then cancelled.
        pendingAttachment = null;
        pendingAttachmentId = null;

        await attachmentUploadRef.Browse();
    }

    /// <summary>
    /// Picks up what the user chose, through the picker, a drop or a paste. This also runs for every status change of
    /// a file that is uploading, and those arrive with the one file that moved - a file that has gone past being
    /// merely selected, which is what tells the two apart.
    /// </summary>
    private void HandleOnAttachmentChange(BitFileInfo[] files)
    {
        if (files.All(file => file.Status is BitFileUploadStatus.Pending or BitFileUploadStatus.NotAllowed) is false) return;

        pendingAttachment = files.FirstOrDefault(file => file.Status is BitFileUploadStatus.Pending);
        pendingAttachmentId = null;
    }

    private void HandleOnAttachmentInvalid(BitFileInfo[] files)
    {
        SnackBarService.Error(files[0].Message ?? Localizer[nameof(AppStrings.FileUploadFailed)]);
    }

    /// <summary>
    /// Uploads the image the message is about to carry, and answers whether the message can go. Called from
    /// <c>SendMessage</c> rather than from the picker, so an image the user drops again never reaches the server.
    /// </summary>
    private async Task<bool> UploadPendingAttachment()
    {
        if (pendingAttachment is not BitFileInfo attachment) return true;

        if (pendingAttachmentId is not null) return true; // Already up there, and this is a resend.

        if (await EnsureSignedIn(Localizer[nameof(AppStrings.AiChatPanelImageSignInTitle)],
                                 Localizer[nameof(AppStrings.AiChatPanelImageSignInMessage)]) is false) return false;

        var uploadTcs = new TaskCompletionSource<bool>();

        isUploadingAttachment = true;
        attachmentUploadTcs = uploadTcs;
        StateHasChanged();

        try
        {
            await attachmentUploadRef.Upload();

            // Upload leaves a file it has nothing left to do for exactly as it found it - one that was removed, or
            // that the validations rejected - and no callback would ever arrive to complete the wait below.
            if (attachment.Status is not BitFileUploadStatus.InProgress)
            {
                uploadTcs.TrySetResult(pendingAttachmentId is not null);
            }

            return await uploadTcs.Task.WaitAsync(CurrentCancellationToken);
        }
        finally
        {
            attachmentUploadTcs = null;
            isUploadingAttachment = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// The upload answers with the id the image was stored under, which is the whole of what the message carries.
    /// </summary>
    private void HandleOnAttachmentUploaded(BitFileInfo fileInfo)
    {
        if (Guid.TryParse(fileInfo.Message, out var attachmentId) is false)
        {
            SnackBarService.Error(Localizer[nameof(AppStrings.AiChatPanelAttachmentFailed)]);
            attachmentUploadTcs?.TrySetResult(false);
            return;
        }

        pendingAttachmentId = attachmentId;

        attachmentUploadTcs?.TrySetResult(true);
    }

    private void HandleOnAttachmentUploadFailed(BitFileInfo fileInfo)
    {
        // The server says why it rejected the image, which is the point of showing its text rather than a generic one.
        SnackBarService.Error(string.IsNullOrEmpty(fileInfo.Message) ? Localizer[nameof(AppStrings.FileUploadFailed)] : fileInfo.Message);

        attachmentUploadTcs?.TrySetResult(false);
    }

    /// <summary>
    /// Drops the image the user changed their mind about. A blob left behind by a message that failed after its image
    /// went up stays where it is: nothing references it, and deleting it would need an endpoint that deletes by id.
    /// </summary>
    private async Task RemovePendingAttachment()
    {
        pendingAttachment = null;
        pendingAttachmentId = null;

        await attachmentUploadRef.Reset();
    }
}

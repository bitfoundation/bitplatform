using Android.Webkit;
using Message = Android.OS.Message;
using AndroidWebView = Android.Webkit.WebView;

namespace Boilerplate.Client.Maui.Platforms.Android;

/// <summary>
/// Answers the permission requests the page makes - the microphone the AI chat panel dictates through, above all.
/// <para>
/// Android does not prompt for these itself: it hands every <c>getUserMedia</c> call to the host app as an
/// <see cref="WebChromeClient.OnPermissionRequest"/>, and the default answer to one is a refusal. BlazorWebView
/// installs a client of its own that leaves it at that default, which is why dictation works in the browser, on iOS
/// and on Windows - each of which grants it their own way - and only here does the microphone never open.
/// </para>
/// <para>
/// That client is still wanted for the file chooser every <c>&lt;input type="file"&gt;</c> goes through and for its
/// target="_blank" handling, so it is wrapped rather than replaced: what it overrides is forwarded on to it.
/// </para>
/// </summary>
public partial class AppWebChromeClient(WebChromeClient? blazorWebChromeClient) : WebChromeClient
{
    public override void OnPermissionRequest(PermissionRequest? request)
    {
        if (request is null) return;

        var resources = request.GetResources() ?? [];

        // An allow list of one, to be widened a resource at a time - the same shape as the Permissions-Policy header
        // the web app sends (see WebApplicationExtensions.UseSecurityHeaders), where microphone=(self) is opened and
        // everything beside it is left closed. Whoever comes to need the camera here opens it deliberately, rather
        // than it arriving with a request nothing in this app makes.
        //
        // Worth the strictness because Android does not prompt for any of this: it asks the host app instead, and
        // what is answered here is granted without the user ever being shown it. For the microphone that answer is
        // only half of the decision - RECORD_AUDIO still has to be granted, and the panel asks for it through
        // IPermissionService before it ever reaches getUserMedia.
        //
        // But ResourceProtectedMediaId has no such second half:
        // no runtime permission stands behind it, so a grant here hands the page the device's DRM identifier - a
        // stable value it can be fingerprinted by - and that is the end of it.
        if (resources is [var resource] && resource == PermissionRequest.ResourceAudioCapture)
        {
            request.Grant(resources);
            return;
        }

        request.Deny();
    }


    public override bool OnShowFileChooser(AndroidWebView? view, IValueCallback? filePathCallback, FileChooserParams? fileChooserParams)
    {
        return blazorWebChromeClient?.OnShowFileChooser(view, filePathCallback, fileChooserParams)
            ?? base.OnShowFileChooser(view, filePathCallback, fileChooserParams);
    }

    public override bool OnCreateWindow(AndroidWebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
    {
        return blazorWebChromeClient?.OnCreateWindow(view, isDialog, isUserGesture, resultMsg)
            ?? base.OnCreateWindow(view, isDialog, isUserGesture, resultMsg);
    }
}

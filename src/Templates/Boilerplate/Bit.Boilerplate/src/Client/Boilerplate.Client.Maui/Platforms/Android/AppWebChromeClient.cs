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
        request.Grant(resources);
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

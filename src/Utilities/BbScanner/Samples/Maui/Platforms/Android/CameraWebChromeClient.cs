using Android.Webkit;
using AndroidWebView = Android.Webkit.WebView;
using Message = Android.OS.Message;

namespace Bit.BbScanner.Samples.Maui;

/// <summary>
/// Lets the page open the camera: Android hands every <c>getUserMedia</c> call to the app as an
/// <see cref="WebChromeClient.OnPermissionRequest"/>, and the default answer - BlazorWebView's own client leaves it at
/// the default - is a refusal, which BbScanner reports as <c>PermissionDenied</c>.
/// </summary>
/// <remarks>
/// <para>
/// Only the camera is granted, and only once the user has granted the app the CAMERA runtime permission, which is asked
/// for here, the first time the page wants the camera: Android shows no prompt of its own for a WebView, so what is
/// granted here would otherwise reach the page without the user ever being asked.
/// </para>
/// <para>
/// BlazorWebView's client is still wanted for the file chooser every <c>&lt;input type="file"&gt;</c> goes through and
/// for its target="_blank" handling, so it is wrapped rather than replaced.
/// </para>
/// </remarks>
public class CameraWebChromeClient(WebChromeClient? blazorWebChromeClient) : WebChromeClient
{
    public override void OnPermissionRequest(PermissionRequest? request)
    {
        if (request is null) return;

        var resources = request.GetResources() ?? [];
        if (resources is not [PermissionRequest.ResourceVideoCapture])
        {
            request.Deny();
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
            if (status != PermissionStatus.Granted)
            {
                status = await Permissions.RequestAsync<Permissions.Camera>();
            }

            if (status == PermissionStatus.Granted)
            {
                request.Grant(resources);
            }
            else
            {
                request.Deny();
            }
        });
    }

    public override bool OnShowFileChooser(AndroidWebView? view, IValueCallback? filePathCallback, FileChooserParams? fileChooserParams)
        => blazorWebChromeClient?.OnShowFileChooser(view, filePathCallback, fileChooserParams)
            ?? base.OnShowFileChooser(view, filePathCallback, fileChooserParams);

    public override bool OnCreateWindow(AndroidWebView? view, bool isDialog, bool isUserGesture, Message? resultMsg)
        => blazorWebChromeClient?.OnCreateWindow(view, isDialog, isUserGesture, resultMsg)
            ?? base.OnCreateWindow(view, isDialog, isUserGesture, resultMsg);
}

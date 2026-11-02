# Bit.BbScanner.Maui

The device's own camera for [Bit.BbScanner](https://www.nuget.org/packages/Bit.BbScanner) in a .NET MAUI Blazor
Hybrid app: CameraX on Android, AVFoundation on iOS and Mac Catalyst, MediaCapture on Windows.

A Blazor Hybrid app is not a web page - .NET runs natively on the device - so the scanner does not need the WebView's
camera. With this package the camera's frames go from the platform straight to the decoder, in the app's own process:
the luminance plane, upright, at the camera's own resolution, decoded on a thread of its own rather than the app's UI
thread. Nothing crosses into the page. The camera's native preview is drawn behind the BlazorWebView, where the scanner
is, and the scanner's reticle, mask, highlights and controls are drawn over it by the page as usual.

## Setup

```bash
dotnet add package Bit.BbScanner.Maui
```

```csharp
// MauiProgram.cs
var builder = MauiApp.CreateBuilder();
builder.UseMauiApp<App>()
       .UseBbScanner();
builder.Services.AddMauiBlazorWebView();
```

```xml
<!-- MainPage.xaml: xmlns:bb="clr-namespace:Bit.BbScanner;assembly=Bit.BbScanner.Maui" -->
<bb:BbScannerHost>
    <BlazorWebView HostPage="wwwroot/index.html">
        <BlazorWebView.RootComponents>
            <RootComponent Selector="#app" ComponentType="{x:Type local:Routes}" />
        </BlazorWebView.RootComponents>
    </BlazorWebView>
</bb:BbScannerHost>
```

The Razor does not change: `<BbScanner>` on a page finds the device's camera registered and uses it.

Declare the camera on each platform; the platform asks the user itself:

- **Android**: `<uses-permission android:name="android.permission.CAMERA" />` in `AndroidManifest.xml`.
- **iOS and Mac Catalyst**: `NSCameraUsageDescription` in `Info.plist`, and the `com.apple.security.device.camera`
  entitlement in a sandboxed Mac Catalyst app.
- **Windows**: `<DeviceCapability Name="webcam" />` in `Package.appxmanifest`.

## The preview

While a camera is open the WebView is made transparent, and the scanner and every element above it in the page paint
no background, so the preview behind the WebView shows through; the `BbScannerHost` paints the page's own background
colour behind the WebView, so the page around the scanner looks as it did. An element above the scanner that paints a
colour of its own - a card the scanner sits in - shows the page's colour while the camera is open. The preview follows
the scanner as the page scrolls, a frame or so behind, and is cut to whatever clips the scanner in the page: a scrolling
panel it is partly scrolled out of shows the preview inside the panel only. Switching cameras keeps the page as it is
in between; stopping the camera puts the WebView and the page back as they were.

Without a `BbScannerHost` loaded there is nowhere to draw the preview, and the scanner opens the WebView's camera
instead, as it does without this package. With more than one host, a camera draws in the one loaded last, so the
scanner belongs in that host's BlazorWebView.

Only the scanner's own ancestors are made transparent. Anything else the page paints under the scanner covers the
preview: a scanner in a dialog shows the page content and the backdrop beneath the dialog where the camera should be.
Put the scanner in the page itself - a page or a panel of its own - rather than over other content.

## Documentation

The full guide is in the [BbScanner README](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/BbScanner),
and a working app in its [MAUI sample](https://github.com/bitfoundation/bitplatform/tree/develop/src/Utilities/BbScanner/Samples/Maui).

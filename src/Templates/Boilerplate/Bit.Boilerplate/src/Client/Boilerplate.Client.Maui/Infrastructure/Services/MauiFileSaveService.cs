namespace Boilerplate.Client.Maui.Infrastructure.Services;

/// <summary>
/// Android, iOS and macOS hand downloads to the host app rather than saving them, BlazorWebView wires that up on
/// none of them, and Android cannot read a <c>blob:</c> URL anyway - so the file goes to the share sheet instead.
/// </summary>
public partial class MauiFileSaveService : FileSaveService
{
    // The AutoInject generator writes a constructor for the class that declares the fields, not one that inherits
    // them. A new base dependency breaks this line, which is the point.
    public MauiFileSaveService(Bit.Butil.ObjectUrls objectUrls) : base(objectUrls) { }

    public override async Task Save(string fileName, string contentType, byte[] content)
    {
        // WebView2 downloads the anchor itself, with a real save dialog - better than a share sheet.
        if (AppPlatform.IsWindows)
        {
            await base.Save(fileName, contentType, content);
            return;
        }

        // App-private and cleared by the OS. The copy outlives the call: the app shared into may still be reading it.
        var filePath = Path.Combine(FileSystem.Current.CacheDirectory, fileName);

        await File.WriteAllBytesAsync(filePath, content);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = fileName,
            File = new ShareFile(filePath, contentType)
        });
    }
}

using Bit.Butil;

namespace Bit.Websites.Platform.Client.Pages.Butil;

public partial class Butil27FilesPage
{
    private ElementReference fileInput;

    private string? fileInfos;
    private string? fileText;
    private string? fileBytes;
    private string? fileDataUrl;
    private string? clearResult;
    private string? imagePreviewUrl;

    private string? blobText = "Hello from Bit.Butil ObjectUrls!";
    private string? objectUrl;
    private string? revokeResult;


    private async Task GetFileInfos()
    {
        var infos = await fileReader.GetFileInfos(fileInput);
        if (infos.Length == 0)
        {
            fileInfos = "No file selected.";
            return;
        }

        fileInfos = string.Join(" | ", infos.Select(i => $"{i.Name} ({i.Size} bytes, {i.Type})"));
    }

    private async Task ReadAsText()
    {
        var text = await fileReader.ReadAsText(fileInput);
        if (string.IsNullOrEmpty(text))
        {
            fileText = "No file or empty content.";
            return;
        }

        fileText = text.Length > 300 ? text[..300] + "…" : text;
    }

    private async Task ReadAsBytes()
    {
        var bytes = await fileReader.ReadAsBytes(fileInput);
        if (bytes is null)
        {
            fileBytes = "No file selected.";
            return;
        }

        fileBytes = $"{bytes.Length} bytes";
    }

    private async Task ReadAsDataUrl()
    {
        var dataUrl = await fileReader.ReadAsDataUrl(fileInput);
        if (string.IsNullOrEmpty(dataUrl))
        {
            fileDataUrl = "No file selected.";
            imagePreviewUrl = null;
            return;
        }

        imagePreviewUrl = dataUrl.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase)
            ? dataUrl
            : null;

        fileDataUrl = dataUrl.Length > 120 ? dataUrl[..120] + "…" : dataUrl;
    }

    private async Task Clear()
    {
        await fileReader.Clear(fileInput);
        imagePreviewUrl = null;
        clearResult = "Input cleared.";
    }

    private async Task CreateObjectUrl()
    {
        await RevokeObjectUrlInternal();

        var bytes = System.Text.Encoding.UTF8.GetBytes(blobText ?? string.Empty);
        objectUrl = await objectUrls.Create(bytes, "text/plain");
    }

    private async Task RevokeObjectUrl()
    {
        await RevokeObjectUrlInternal();
        revokeResult = "Object URL revoked.";
    }

    private async Task RevokeObjectUrlInternal()
    {
        if (objectUrl is null) return;
        await objectUrls.Revoke(objectUrl);
        objectUrl = null;
    }


    protected override async ValueTask DisposeAsync(bool disposing)
    {
        // Ensure any blob URL created via CreateObjectUrl is revoked when the user leaves the page.
        await RevokeObjectUrlInternal();

        await base.DisposeAsync(disposing);
    }


    private readonly string getFileInfosExampleCode =
@"@inject Bit.Butil.FileReader fileReader

<input type=""file"" @ref=""fileInput"" multiple />

<BitButton OnClick=""GetFileInfos"">GetFileInfos</BitButton>

<div>@fileInfos</div>

@code {
    private ElementReference fileInput;
    private string? fileInfos;

    private async Task GetFileInfos()
    {
        var infos = await fileReader.GetFileInfos(fileInput);
        if (infos.Length == 0)
        {
            fileInfos = ""No file selected."";
            return;
        }

        fileInfos = string.Join("" | "", infos.Select(i => $""{i.Name} ({i.Size} bytes, {i.Type})""));
    }
}";

    private readonly string readAsTextExampleCode =
@"@inject Bit.Butil.FileReader fileReader

<input type=""file"" @ref=""fileInput"" multiple />

<BitButton OnClick=""ReadAsText"">ReadAsText</BitButton>

<div>@fileText</div>

@code {
    private ElementReference fileInput;
    private string? fileText;

    private async Task ReadAsText()
    {
        var text = await fileReader.ReadAsText(fileInput);
        fileText = string.IsNullOrEmpty(text) ? ""No file or empty content."" : text;
    }
}";

    private readonly string readAsBytesExampleCode =
@"@inject Bit.Butil.FileReader fileReader

<input type=""file"" @ref=""fileInput"" multiple />

<BitButton OnClick=""ReadAsBytes"">ReadAsBytes</BitButton>

<div>@fileBytes</div>

@code {
    private ElementReference fileInput;
    private string? fileBytes;

    private async Task ReadAsBytes()
    {
        var bytes = await fileReader.ReadAsBytes(fileInput);
        fileBytes = bytes is null ? ""No file selected."" : $""{bytes.Length} bytes"";
    }
}";

    private readonly string readAsDataUrlExampleCode =
@"@inject Bit.Butil.FileReader fileReader

<input type=""file"" @ref=""fileInput"" multiple />

<BitButton OnClick=""ReadAsDataUrl"">ReadAsDataUrl</BitButton>

<div>@fileDataUrl</div>

@if (imagePreviewUrl is not null)
{
    <img src=""@imagePreviewUrl"" alt=""Preview"" />
}

@code {
    private ElementReference fileInput;
    private string? fileDataUrl;
    private string? imagePreviewUrl;

    private async Task ReadAsDataUrl()
    {
        var dataUrl = await fileReader.ReadAsDataUrl(fileInput);
        if (string.IsNullOrEmpty(dataUrl))
        {
            fileDataUrl = ""No file selected."";
            imagePreviewUrl = null;
            return;
        }

        imagePreviewUrl = dataUrl.StartsWith(""data:image/"", StringComparison.OrdinalIgnoreCase)
            ? dataUrl
            : null;
        fileDataUrl = dataUrl;
    }
}";

    private readonly string clearExampleCode =
@"@inject Bit.Butil.FileReader fileReader

<input type=""file"" @ref=""fileInput"" multiple />

<BitButton OnClick=""Clear"">Clear</BitButton>

@code {
    private ElementReference fileInput;

    private async Task Clear()
    {
        await fileReader.Clear(fileInput);
    }
}";

    private readonly string createExampleCode =
@"@inject Bit.Butil.ObjectUrls objectUrls

<BitTextField @bind-Value=""blobText"" Label=""Text to blob"" />

<BitButton OnClick=""CreateObjectUrl"">Create</BitButton>

@if (objectUrl is not null)
{
    <a href=""@objectUrl"" target=""_blank"" rel=""noopener"">Open blob URL</a>
}

@code {
    private string? blobText;
    private string? objectUrl;

    private async Task CreateObjectUrl()
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(blobText ?? string.Empty);
        objectUrl = await objectUrls.Create(bytes, ""text/plain"");
    }
}";

    private readonly string revokeExampleCode =
@"@inject Bit.Butil.ObjectUrls objectUrls

<BitButton OnClick=""RevokeObjectUrl"">Revoke</BitButton>

@code {
    private string? objectUrl;

    private async Task RevokeObjectUrl()
    {
        if (objectUrl is null) return;
        await objectUrls.Revoke(objectUrl);
        objectUrl = null;
    }
}";
}

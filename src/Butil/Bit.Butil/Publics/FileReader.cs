using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Bit.Butil;

/// <summary>
/// Helpers for reading bytes/text out of a Blob or File reference. Use it together with an
/// <c>&lt;input type="file"&gt;</c> element captured via <see cref="ElementReference"/>.
/// </summary>
public class FileReader(IJSRuntime js)
{
    /// <summary>
    /// Returns metadata for the file at index <paramref name="index"/> in the given input element's <c>files</c> list.
    /// </summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BlobInfo))]
    public ValueTask<BlobInfo?> GetFileInfo(ElementReference inputElement, int index = 0)
        => js.Invoke<BlobInfo?>("BitButil.fileReader.getFileInfo", inputElement, index);

    /// <summary>Returns metadata for every file in the given input.</summary>
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(BlobInfo))]
    public ValueTask<BlobInfo[]> GetFileInfos(ElementReference inputElement)
        => js.Invoke<BlobInfo[]>("BitButil.fileReader.getFileInfos", inputElement);

    /// <summary>Reads a single file as raw bytes, or <see langword="null"/> when no file exists at <paramref name="index"/>.</summary>
    public ValueTask<byte[]?> ReadAsBytes(ElementReference inputElement, int index = 0)
        => js.Invoke<byte[]?>("BitButil.fileReader.readAsBytes", inputElement, index);

    /// <summary>Reads a single file as UTF-8 text. Pass a different <paramref name="encoding"/> when the source is non-UTF-8.</summary>
    public ValueTask<string> ReadAsText(ElementReference inputElement, int index = 0, string encoding = "utf-8")
        => js.Invoke<string>("BitButil.fileReader.readAsText", inputElement, index, encoding);

    /// <summary>
    /// Reads a single file as a base-64 data URL (e.g. <c>data:image/png;base64,...</c>).
    /// Convenient for image previews; prefer <see cref="ReadAsBytes"/> when you'll process the bytes.
    /// </summary>
    public ValueTask<string> ReadAsDataUrl(ElementReference inputElement, int index = 0)
        => js.Invoke<string>("BitButil.fileReader.readAsDataUrl", inputElement, index);

    /// <summary>Clears the input's selection (resets <c>files</c>).</summary>
    public ValueTask Clear(ElementReference inputElement)
        => js.InvokeVoid("BitButil.fileReader.clear", inputElement);
}

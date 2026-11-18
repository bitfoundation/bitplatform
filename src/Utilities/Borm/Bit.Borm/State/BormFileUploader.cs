namespace Bit.Borm;

/// <summary>
/// Sends one chosen file wherever the application keeps files and returns what the field stores in its place -
/// an id, a URL. See <see cref="BormFieldBuilder.UploadWith"/>.
/// </summary>
public delegate Task<string> BormFileUploader(BormUploadContext upload);

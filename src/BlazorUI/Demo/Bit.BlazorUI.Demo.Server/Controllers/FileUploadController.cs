namespace Bit.BlazorUI.Demo.Server.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public partial class FileUploadController : AppControllerBase
{
    [AutoInject] private AppSettings _settings;

    [HttpPost]
    [RequestSizeLimit(2000 * 1024 * 1024 /*~2GB*/)]
    public async Task<IActionResult> UploadNonChunkedFile(IFormFile file,
                                                          [FromHeader(Name = "BIT_FILE_ID")][Required] string bitFileId,
                                                          CancellationToken cancellationToken)
    {
        if (file is null)
        {
            ModelState.AddModelError("File", $"The request couldn't be processed (Error 1).");
            return BadRequest(ModelState);
        }

        using var requestStream = file.OpenReadStream();

        if (Directory.Exists(_settings.UploadPath) is false)
        {
            Directory.CreateDirectory(_settings.UploadPath);
        }

        // the file is stored under the same {id}-{name} key the chunked endpoint uses, since that is the
        // one the remove endpoint deletes by - storing it under the bare name would make every removal a 404.
        var path = Path.Combine(_settings.UploadPath, $"{bitFileId}-{file.FileName}");

        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        using var targetStream = System.IO.File.Create(path);
        if (cancellationToken.IsCancellationRequested is false)
        {
            await requestStream.CopyToAsync(targetStream, cancellationToken);
        }

        return Ok();
    }


    [HttpPost]
    [RequestSizeLimit(11 * 1024 * 1024 /*11MB*/)]
    public async Task<IActionResult> UploadChunkedFile(IFormFile file,
                                                       [FromHeader(Name = "BIT_FILE_ID")][Required] string bitFileId,
                                                       CancellationToken cancellationToken)
    {
        if (file is null)
        {
            ModelState.AddModelError("File", $"The request couldn't be processed (Error 1).");
            return BadRequest(ModelState);
        }

        using var requestStream = file.OpenReadStream();

        if (Directory.Exists(_settings.UploadPath) is false)
        {
            Directory.CreateDirectory(_settings.UploadPath);
        }

        var path = Path.Combine(_settings.UploadPath, $"{bitFileId}-{file.FileName}");

        using var targetStream = System.IO.File.Exists(path)
            ? System.IO.File.Open(path, FileMode.Append)
            : System.IO.File.Create(path);

        if (cancellationToken.IsCancellationRequested is false)
            await requestStream.CopyToAsync(targetStream);

        return Ok();
    }


    [HttpDelete]
    public IActionResult RemoveFile(string fileName,
                                    [FromHeader(Name = "BIT_FILE_ID")][Required] string bitFileId)
    {
        var path = Path.Combine(_settings.UploadPath, $"{bitFileId}-{fileName}");

        // a delete is idempotent: a file this server never stored - the PreloadedFiles of the demo, which
        // stand in for the attachments of a record, are exactly that - is already in the state asked for,
        // and answering 404 would only mark it in the UI as a removal that failed.
        if (System.IO.File.Exists(path))
        {
            System.IO.File.Delete(path);
        }

        return Ok();
    }
}

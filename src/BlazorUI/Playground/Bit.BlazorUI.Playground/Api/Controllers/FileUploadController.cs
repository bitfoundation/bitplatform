using Microsoft.AspNetCore.Mvc;

namespace Bit.BlazorUI.Playground.Api.Controllers;

[ApiController]
[Route("api/[controller]/[action]")]
public class FileUploadController : ControllerBase
{
    private readonly string UploadPath;

    public FileUploadController(IConfiguration Configuration)
    {
        UploadPath = Configuration["UploadPath"];
    }


    [HttpPost]
    [RequestSizeLimit(2000 * 1024 * 1024 /*~2GB*/)]
    public async Task<IActionResult> UploadNonChunkedFile(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            ModelState.AddModelError("File", $"The request couldn't be processed (Error 1).");
            return BadRequest(ModelState);
        }

        using var requestStream = file.OpenReadStream();

        if (Directory.Exists(UploadPath) is false)
        {
            Directory.CreateDirectory(UploadPath);
        }

        var path = Path.Combine(UploadPath, file.FileName);

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
    public async Task<IActionResult> UploadChunkedFile(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            ModelState.AddModelError("File", $"The request couldn't be processed (Error 1).");
            return BadRequest(ModelState);
        }

        var bitFileId = Request.Headers["BIT_FILE_ID"].ToString();

        using var requestStream = file.OpenReadStream();

        if (Directory.Exists(UploadPath) is false)
        {
            Directory.CreateDirectory(UploadPath);
        }

        var path = Path.Combine(UploadPath, $"{bitFileId}-{file.FileName}");

        if (System.IO.File.Exists(path) is false)
        {
            using var targetStream = System.IO.File.Create(path);
            if (cancellationToken.IsCancellationRequested is false)
                await requestStream.CopyToAsync(targetStream, cancellationToken);
        }
        else
        {
            using var targetStream = System.IO.File.Open(path, FileMode.Append);
            if (cancellationToken.IsCancellationRequested is false)
                await requestStream.CopyToAsync(targetStream, cancellationToken);
        }

        return Ok();
    }


    [HttpDelete]
    public IActionResult RemoveFile(string fileName)
    {
        var path = Path.Combine(UploadPath, fileName);
        if (!System.IO.File.Exists(path)) return NotFound();

        System.IO.File.Delete(path);
        return Ok();
    }
}

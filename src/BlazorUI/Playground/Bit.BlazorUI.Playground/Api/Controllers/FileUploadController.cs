using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Bit.BlazorUI.Playground.Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class FileUploadController : ControllerBase
    {
        private readonly string BasePath;

        public FileUploadController(IConfiguration Configuration)
        {
            BasePath = Configuration["UploadPath"];
        }

        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [HttpPost]
        public async Task<IActionResult> UploadStreamedFile(IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null)
            {
                ModelState.AddModelError("File", $"The request couldn't be processed (Error 1).");
                return BadRequest(ModelState);
            }

            using var fileStream = file.OpenReadStream();
            using var memoryStream = new MemoryStream();
            await fileStream.CopyToAsync(memoryStream, cancellationToken);
            memoryStream.Seek(0, SeekOrigin.Begin);

            if (Directory.Exists(BasePath) is false)
            {
                Directory.CreateDirectory(BasePath);
            }

            var path = Path.Combine(BasePath, file.FileName);
            if (System.IO.File.Exists(path) is false)
            {
                using var targetStream = System.IO.File.Create(path);
                if (cancellationToken.IsCancellationRequested is false)
                    await memoryStream.CopyToAsync(targetStream);
            }
            else
            {
                using var targetStream = System.IO.File.Open(path, FileMode.Append);
                if (cancellationToken.IsCancellationRequested is false)
                    await memoryStream.CopyToAsync(targetStream);
            }

            return Ok();
        }

        [HttpDelete]
        public IActionResult RemoveFile(string fileName)
        {
            var path = Path.Combine(BasePath, fileName);
            if (!System.IO.File.Exists(path)) return NotFound();

            System.IO.File.Delete(path);
            return Ok();
        }
    }
}

using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;

namespace Bit.Client.Web.BlazorUI.Playground.Api.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class FileUploadController : ControllerBase
    {
        private const int BoundaryLengthLimit = int.MaxValue;
        private readonly string BasePath;
        
        public FileUploadController(IConfiguration Configuration)
        {
            BasePath = Configuration["UploadPath"];
        }

        [RequestFormLimits(ValueLengthLimit = int.MaxValue, MultipartBodyLengthLimit = int.MaxValue)]
        [DisableRequestSizeLimit]
        [HttpPost]
        public async Task<IActionResult> UploadStreamedFile()
        {
            if (!IsMultipartContentType(Request.ContentType))
            {
                ModelState.AddModelError("File", $"The request couldn't be processed (Error 1).");
                return BadRequest(ModelState);
            }

            var boundary = GetBoundary(MediaTypeHeaderValue.Parse(Request.ContentType), lengthLimit: BoundaryLengthLimit);
            var reader = new MultipartReader(boundary, HttpContext.Request.Body);
            var section = await reader.ReadNextSectionAsync();

            while (section != null)
            {
                var hasContentDispositionHeader = ContentDispositionHeaderValue.TryParse(section.ContentDisposition, out var contentDisposition);
                if (hasContentDispositionHeader)
                {
                    if (Directory.Exists(BasePath) is false)
                    {
                        Directory.CreateDirectory(BasePath);
                    }
                    var path = Path.Combine(BasePath, contentDisposition.FileName.Value);
                    if (System.IO.File.Exists(path) is false)
                    {
                        using var targetStream = System.IO.File.Create(path);
                        await section.Body.CopyToAsync(targetStream);
                    }
                    else
                    {
                        using var targetStream = System.IO.File.Open(path, FileMode.Append);
                        await section.Body.CopyToAsync(targetStream);
                    }

                    return Ok();
                }

                section = await reader.ReadNextSectionAsync();
            }

            return BadRequest();
        }

        [HttpDelete]
        public IActionResult RemoveFile(string fileName)
        {
            var path = Path.Combine(BasePath, fileName);
            if (!System.IO.File.Exists(path)) return NotFound();

            System.IO.File.Delete(path);
            return Ok();
        }

        public static string GetBoundary(MediaTypeHeaderValue contentType, int lengthLimit)
        {
            var boundary = HeaderUtilities.RemoveQuotes(contentType.Boundary).Value;

            if (string.IsNullOrWhiteSpace(boundary))
            {
                throw new InvalidDataException("Missing content-type boundary.");
            }

            if (boundary.Length > lengthLimit)
            {
                throw new InvalidDataException($"Multipart boundary length limit {lengthLimit} exceeded.");
            }

            return boundary;
        }

        public static bool IsMultipartContentType(string contentType)
        {
            return !string.IsNullOrEmpty(contentType)
                   && contentType.Contains("multipart/", StringComparison.OrdinalIgnoreCase);
        }
    }
}

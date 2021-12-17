using Bit.Core.Exceptions;
using Bit.Owin.Middlewares;
using Bit.WebApi.ActionFilters;
using BitChangeSetManager.DataAccess.Contracts;
using BitChangeSetManager.Dto;
using BitChangeSetManager.Model;
using System;
using System.Data.Entity;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace BitChangeSetManager.Api
{
    public class ChangeSetImagesController : BitChangeSetManagerDtoSetController<ChangeSetImagetDto, ChangeSetImage, Guid>
    {
    }

    [RoutePrefix("change-set-images")]
    public class ChangeSetImagesFileController : ApiController
    {
        public IBitChangeSetManagerRepository<ChangeSetImage> Repository { get; set; }

        [Route("add-new-images/{changeSetId}")]
        [HttpPost]
        public virtual async Task<IHttpActionResult> AddNewImages(Guid changeSetId, CancellationToken cancellationToken)
        {
            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

            MultipartMemoryStreamProvider provider = new MultipartMemoryStreamProvider();

            foreach (HttpContent file in (await Request.Content.ReadAsMultipartAsync(provider, cancellationToken)).Contents)
            {
                string filename = Path.GetFileName(file.Headers.ContentDisposition.FileName);

                byte[] data = await file.ReadAsByteArrayAsync();

                await Repository.AddAsync(new ChangeSetImage { ChangeSetId = changeSetId, Name = filename, Data = data }, cancellationToken);
            }

            return Ok();
        }

        /* C# client sample: Required nuget packages: IdentityModel + Microsoft.Net.Http
         
public static async Task UploadImagess()
{
    TokenClient tokenClient = new TokenClient(@"http://localhost:9090/bit-change-set-manager/core/connect/token", "BitChangeSetManager-ResOwner", "secret"); // See BitChangeSetManagerClientProvider.cs
    TokenResponse token = await tokenClient.RequestResourceOwnerPasswordAsync(userName: "test1", password: "test", scope: "openid profile user_info");

    if (token.IsError)
        throw new Exception("Login failed");

    Guid changeSetId = Guid.Parse("bfebb827-f76f-44cc-ac37-3f57f254221b");

    HttpClient client = new HttpClient();

    client.SetBearerToken(token.AccessToken);

    string[] fileNames = new[] { @"C:/temp/1.png", @"C:/temp/2.png" };

    using (MultipartFormDataContent content = new MultipartFormDataContent())
    {
        foreach (string fileName in fileNames)
        {
            StreamContent fileContent = new StreamContent(File.OpenRead(fileName));
            // or >> ByteArrayContent fileContent = new ByteArrayContent(File.ReadAllBytes(fileName));

            fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = fileName
            };

            content.Add(fileContent);
        }

        HttpResponseMessage result = await client.PostAsync($"http://localhost:9090/bit-change-set-manager/api/change-set-images/add-new-images/{changeSetId}", content);
    }
}
         */

        [Route("get-image/{imageId}")]
        [HttpGet, OwinActionFilter(typeof(OwinCacheResponseMiddleware))]
        public virtual async Task<HttpResponseMessage> GetImage(Guid imageId, CancellationToken cancellationToken)
        {
            ChangeSetImage image = await (await Repository.GetAllAsync(cancellationToken))
                .SingleOrDefaultAsync(img => img.Id == imageId, cancellationToken);

            if (image == null)
                throw new ResourceNotFoundException();

            MemoryStream memStream = new MemoryStream(image.Data);
            HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(memStream)
            };
            response.Content.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            return response;
        }
    }
}
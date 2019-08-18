using Bit.Core;
using Bit.Core.Contracts;
using Bit.Owin;
using Bit.Owin.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Hosting;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace WebApiFileUpload
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string baseAddress = "http://localhost:9003/";

            using (WebApp.Start<AppStartup>(url: baseAddress))
            {
                using (WebClient webClient = new WebClient())
                {
                    Console.WriteLine("Write file path and press enter");

                    string filePath = Console.ReadLine();

                    if (!File.Exists(filePath))
                        Console.WriteLine("File does not exists");
                    else
                        webClient.UploadFile($"{baseAddress}/api/file-manager/upload-files-to-folder" /* Client's url has /api */, filePath);
                }

                Console.ReadLine();
            }
        }
    }
}

[RoutePrefix("file-manager" /* There is no /api */)]
public class FileController : ApiController
{
    public IPathProvider PathProvider { get; set; }

    [HttpPost, Route("upload-files-to-folder")]
    public virtual async Task<IHttpActionResult> UploadFilesToFolder(CancellationToken cancellationToken)
    {
        if (!Request.Content.IsMimeMultipartContent())
            throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

        string uploadsFolder = PathProvider.MapStaticFilePath("uploads");

        Directory.CreateDirectory(uploadsFolder);

        MultipartFormDataStreamProvider provider = new MultipartFormDataStreamProvider(uploadsFolder); // this stores uploaded files to that folder

        await Request.Content.ReadAsMultipartAsync(provider, cancellationToken);

        return Ok();
    }

    [HttpPost, Route("upload-file-to-database")]
    public virtual async Task<IHttpActionResult> UploadFilesToDatabase(CancellationToken cancellationToken)
    {
        if (!Request.Content.IsMimeMultipartContent())
            throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

        MultipartMemoryStreamProvider provider = new MultipartMemoryStreamProvider();

        await Request.Content.ReadAsMultipartAsync(provider, cancellationToken);

        foreach (HttpContent file in provider.Contents)
        {
            string filename = Path.GetFileName(file.Headers.ContentDisposition.FileName.Trim('\"'));

            byte[] data = await file.ReadAsByteArrayAsync(); // save this array to database by entity framework, dapper etc.
        }

        return Ok();
    }
}

public class AppStartup : OwinAppStartup, IAppModule, IAppModulesProvider
{
    public override void Configuration(IAppBuilder owinApp)
    {
        DefaultAppModulesProvider.Current = this;

        base.Configuration(owinApp);
    }

    public IEnumerable<IAppModule> GetAppModules()
    {
        yield return this;
    }

    public void ConfigureDependencies(IServiceCollection services, IDependencyManager dependencyManager)
    {
        AssemblyContainer.Current.Init();

        dependencyManager.RegisterMinimalDependencies();

        dependencyManager.RegisterDefaultLogger(typeof(DebugLogStore).GetTypeInfo(), typeof(ConsoleLogStore).GetTypeInfo());

        dependencyManager.RegisterDefaultOwinApp();

        dependencyManager.RegisterMinimalOwinMiddlewares();

        dependencyManager.RegisterDefaultWebApiConfiguration();

        dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
        {
            webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();
        });
    }
}

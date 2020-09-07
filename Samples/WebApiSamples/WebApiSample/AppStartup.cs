using AutoMapper;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Model.Implementations;
using Bit.Owin;
using Bit.Owin.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.Application;
using Swashbuckle.Swagger;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace WebApiSample
{
    public class AppStartup : AutofacAspNetCoreAppStartup, IAppModule, IAppModulesProvider
    {
        public AppStartup(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {

        }

        public override IServiceProvider ConfigureServices(IServiceCollection services)
        {
            DefaultAppModulesProvider.Current = this;

            return base.ConfigureServices(services);
        }

        public IEnumerable<IAppModule> GetAppModules()
        {
            yield return this;
        }

        public virtual void ConfigureDependencies(IServiceCollection services, IDependencyManager dependencyManager)
        {
            AssemblyContainer.Current.Init();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterDefaultLogger(typeof(DebugLogStore).GetTypeInfo(), typeof(ConsoleLogStore).GetTypeInfo());

            dependencyManager.RegisterDefaultAspNetCoreApp();

            dependencyManager.RegisterMinimalAspNetCoreMiddlewares();

            dependencyManager.RegisterDefaultWebApiConfiguration();

            dependencyManager.RegisterWebApiMiddleware(webApiDependencyManager =>
            {
                webApiDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration();

                webApiDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", "SwaggerDemoApi");
                        c.ApplyDefaultApiConfig(httpConfiguration);
                        c.OperationFilter<FileOperationFilter>();
                    }).EnableBitSwaggerUi();
                });
            });

            dependencyManager.RegisterAutoMapper();
            dependencyManager.RegisterMapperConfiguration<DefaultMapperConfiguration>();

            dependencyManager.Register<IEmailService, DefaultEmailService>();
        }
    }

    public class FileOperationFilter : IOperationFilter // to enable file upload
    {
        public void Apply(Operation operation, SchemaRegistry schemaRegistry, ApiDescription apiDescription)
        {
            if (operation.operationId.StartsWith("File_" /*See FileController*/, StringComparison.CurrentCultureIgnoreCase))
            {
                if (operation.parameters == null)
                    operation.parameters = new List<Parameter>(1);
                else
                    operation.parameters.Clear();
                operation.parameters.Add(new Parameter
                {
                    name = "File",
                    @in = "formData",
                    description = "Upload software package",
                    required = true,
                    type = "file"
                });
                operation.consumes.Add("application/form-data");
            }
        }
    }

    public class ValuesController : ApiController
    {
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        public virtual IEmailService EmailService { get; set; }
    }

    public interface IEmailService
    {
        void SendEmail();
    }

    public class DefaultEmailService : IEmailService
    {
        public virtual void SendEmail()
        {

        }
    }

    #region Copied from https://github.com/billpratt/SwaggerDemoApi & http://wmpratt.com/swagger-and-asp-net-web-api-part-1/

    /// <summary>
    /// Superhero model to use for POST
    /// </summary>
    public class PostSuperheroModel
    {
        /// <summary>
        /// Superhero name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Superhero real name
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// Which comic book universe does the superhero belong to
        /// </summary>
        public Universe Universe { get; set; }
    }

    /// <summary>
    /// Superhero model to use for PUT
    /// </summary>
    public class PutSuperheroModel
    {
        /// <summary>
        /// Id of the Superhero
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Superhero name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Superhero real name
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// Which comic book universe does the superhero belong to
        /// </summary>
        public Universe Universe { get; set; }
    }

    /// <summary>
    /// Comicbook Universe Enum
    /// </summary>
    public enum Universe
    {
        /// <summary>
        /// Marvel Comics
        /// </summary>
        Marvel,

        /// <summary>
        /// DC Comics
        /// </summary>
        Dc
    }

    /// <summary>
    /// Superhero
    /// </summary>
    public class Superhero
    {
        /// <summary>
        /// Guid id of the superhero
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Superhero name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Superhero real name
        /// </summary>
        public string RealName { get; set; }

        /// <summary>
        /// Which comic book universe does the superhero belong to
        /// </summary>
        public Universe Universe { get; set; }
    }

    /// <summary>
    /// Superhero api
    /// </summary>
    public class SuperHeroesController : ApiController
    {
        public IMapper Mapper { get; set; }

        private static readonly List<Superhero> Superheroes = new List<Superhero>
        {
            new Superhero
            {
                Id = Guid.NewGuid(),
                Name = "Batman",
                RealName = "Bruce Wayne",
                Universe = Universe.Dc
            },
            new Superhero
            {
                Id = Guid.NewGuid(),
                Name = "Wolverine",
                RealName = "Logan",
                Universe = Universe.Marvel
            }
        };

        /// <summary>
        /// Get all superheroes
        /// </summary>
        /// <remarks>
        /// Get a list of all superheroes
        /// </remarks>
        /// <returns></returns>
        /// <response code="200"></response>
        [ResponseType(typeof(IEnumerable<Superhero>))]
        public HttpResponseMessage Get()
        {
            return Request.CreateResponse(HttpStatusCode.OK, Superheroes);
        }

        /// <summary>
        /// Get superhero by id
        /// </summary>
        /// <remarks>
        /// Get a superhero by id
        /// </remarks>
        /// <param name="id">Id of superhero</param>
        /// <returns></returns>
        /// <response code="200">Superhero found</response>
        /// <response code="404">Superhero not foundd</response>
        [ResponseType(typeof(Superhero))]
        public HttpResponseMessage GetById(Guid id)
        {
            Superhero superhero = Superheroes.FirstOrDefault(c => c.Id == id);

            return superhero == null
                ? Request.CreateErrorResponse(HttpStatusCode.NotFound, "Superhero not found")
                : Request.CreateResponse(HttpStatusCode.OK, superhero);
        }

        /// <summary>
        /// Add new superhero
        /// </summary>
        /// <remarks>
        /// Add a new superhero
        /// </remarks>
        /// <param name="postSuperheroModel">Superhero to add</param>
        /// <returns></returns>
        /// <response code="201">Superhero created</response>
        [ResponseType(typeof(Superhero))]
        public HttpResponseMessage Post(PostSuperheroModel postSuperheroModel)
        {
            // Map a PostSuperheroModel object to Superhero object
            Superhero superhero = Mapper.Map<Superhero>(postSuperheroModel);

            superhero.Id = Guid.NewGuid();
            Superheroes.Add(superhero);

            return Request.CreateResponse(HttpStatusCode.Created, superhero);
        }

        /// <summary>
        /// Update an existing superhero
        /// </summary>
        /// <param name="putSuperheroModel">Superhero to update</param>
        /// <returns></returns>
        /// <response code="200">Superhero updated</response>
        /// <response code="404">Superhero not found</response>
        [ResponseType(typeof(Superhero))]
        public HttpResponseMessage Put(PutSuperheroModel putSuperheroModel)
        {
            Superhero existingSuperhero = Superheroes.FirstOrDefault(c => c.Id == putSuperheroModel.Id);
            if (existingSuperhero == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Superhero not found");

            Superhero superhero = Mapper.Map<Superhero>(putSuperheroModel);
            Superheroes.Remove(existingSuperhero);
            Superheroes.Add(superhero);

            return Request.CreateResponse(HttpStatusCode.OK, superhero);
        }

        /// <summary>
        /// Delete a superhero
        /// </summary>
        /// <remarks>
        /// Delete a superhero
        /// </remarks>
        /// <param name="id">Id of the superhero to delete</param>
        /// <returns></returns>
        public HttpResponseMessage Delete(Guid id)
        {
            Superhero existingSuperhero = Superheroes.FirstOrDefault(c => c.Id == id);

            if (existingSuperhero == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Superhero not found");

            Superheroes.Remove(existingSuperhero);

            return Request.CreateResponse(HttpStatusCode.OK);
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

    #endregion
}

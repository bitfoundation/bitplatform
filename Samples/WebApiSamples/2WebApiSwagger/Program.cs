using AutoMapper;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Model.Implementations;
using Bit.Owin;
using Bit.Owin.Implementations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Owin.Hosting;
using Owin;
using Swashbuckle.Application;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;

namespace WebApiSwagger
{
    public class Program
    {
        public static void Main(string[] args)
        {
            string baseAddress = "http://localhost:9002/";

            using (WebApp.Start<AppStartup>(url: baseAddress))
            {
                Process.Start($"{baseAddress}api/swagger/");
                Console.ReadLine();
            }
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

                webApiDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                {
                    httpConfiguration.EnableSwagger(c =>
                    {
                        c.SingleApiVersion("v1", "SwaggerDemoApi");
                        c.IncludeXmlComments(Path.Combine(DefaultPathProvider.Current.GetCurrentAppPath(), "WebApiSwagger.xml"));
                        c.ApplyDefaultApiConfig(httpConfiguration);
                    }).EnableBitSwaggerUi();
                });
            });

            dependencyManager.RegisterAutoMapper();
            dependencyManager.RegisterMapperConfiguration<DefaultMapperConfiguration>();
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

    #endregion
}

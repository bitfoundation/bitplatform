using AutoMapper;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Owin.Contracts;
using Bit.Owin.Implementations;
using Bit.OwinCore;
using Bit.OwinCore.Contracts;
using Bit.OwinCore.Middlewares;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Swashbuckle.Application;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using System.Web.Http.Description;

namespace WebApiAspNetCoreHost
{
    public class AppStartup : AutofacAspNetCoreAppStartup, IAspNetCoreDependenciesManager, IDependenciesManagerProvider
    {
        public AppStartup(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {

        }

        public override IServiceProvider ConfigureServices(IServiceCollection services)
        {
            DefaultDependenciesManagerProvider.Current = this;

            return base.ConfigureServices(services);
        }

        public IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }

        public virtual void ConfigureDependencies(IServiceProvider serviceProvider, IServiceCollection services, IDependencyManager dependencyManager)
        {
            AssemblyContainer.Current.Init();

            IHostingEnvironment env = serviceProvider.GetService<IHostingEnvironment>();

            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterInstance(DefaultAppEnvironmentProvider.Current);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current);

            dependencyManager.Register<IRequestInformationProvider, AspNetCoreRequestInformationProvider>();

            dependencyManager.Register<ILogger, DefaultLogger>();
            if (env.IsDevelopment())
                dependencyManager.RegisterLogStore<DebugLogStore>();
            dependencyManager.RegisterLogStore<ConsoleLogStore>();

            dependencyManager.RegisterDefaultOwinApp();

            dependencyManager.RegisterOwinMiddleware<AspNetCoreAutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterAspNetCoreMiddleware<AspNetCoreLogRequestInformationMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiConfiguration();

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration("WebApi");

                    childDependencyManager.RegisterGlobalWebApiCustomizerUsing(httpConfiguration =>
                    {
                        httpConfiguration.EnableSwagger(c =>
                        {
                            c.SingleApiVersion("v1", "SwaggerDemoApi");
                            c.DescribeAllEnumsAsStrings();
                            c.RootUrl(req => new Uri(req.RequestUri, req.GetOwinContext().Request.PathBase.Value /* /api */).ToString());
                        }).EnableSwaggerUi();
                    });

                }).Resolve<IOwinMiddlewareConfiguration>("WebApi");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);
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
            var superhero = Superheroes.FirstOrDefault(c => c.Id == id);

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
        [Authorize(Roles = "write")]
        [ResponseType(typeof(Superhero))]
        public HttpResponseMessage Post(PostSuperheroModel postSuperheroModel)
        {
            // Map a PostSuperheroModel object to Superhero object
            var superhero = Mapper.Map<Superhero>(postSuperheroModel);

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
        [Authorize(Roles = "write")]
        //[Authorize]
        [ResponseType(typeof(Superhero))]
        public HttpResponseMessage Put(PutSuperheroModel putSuperheroModel)
        {
            var existingSuperhero = Superheroes.FirstOrDefault(c => c.Id == putSuperheroModel.Id);
            if (existingSuperhero == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Superhero not found");

            var superhero = Mapper.Map<Superhero>(putSuperheroModel);
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
            var existingSuperhero = Superheroes.FirstOrDefault(c => c.Id == id);

            if (existingSuperhero == null)
                return Request.CreateErrorResponse(HttpStatusCode.NotFound, "Superhero not found");

            Superheroes.Remove(existingSuperhero);

            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }

    #endregion
}

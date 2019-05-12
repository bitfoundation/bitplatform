using Bit.Owin.Contracts;
using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using Microsoft.OData.UriParser;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;

namespace Bit.OData.ODataControllers
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class ActionAttribute : Attribute, IActionHttpMethodProvider
    {
        private static readonly Collection<HttpMethod> SupportedMethods = new Collection<HttpMethod>(new[]
        {
            HttpMethod.Post
        });

        public Collection<HttpMethod> HttpMethods => SupportedMethods;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class FunctionAttribute : Attribute, IActionHttpMethodProvider
    {
        private static readonly Collection<HttpMethod> SupportedMethods = new Collection<HttpMethod>(new[]
        {
            HttpMethod.Get
        });

        public Collection<HttpMethod> HttpMethods => SupportedMethods;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class GetAttribute : Attribute, IActionHttpMethodProvider
    {
        private static readonly Collection<HttpMethod> SupportedMethods = new Collection<HttpMethod>(new[]
        {
            HttpMethod.Get
        });

        public Collection<HttpMethod> HttpMethods => SupportedMethods;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class PartialUpdateAttribute : Attribute, IActionHttpMethodProvider
    {
        private static readonly Collection<HttpMethod> SupportedMethods = new Collection<HttpMethod>(new[]
        {
            new HttpMethod("PATCH")
        });

        public Collection<HttpMethod> HttpMethods => SupportedMethods;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class CreateAttribute : ActionFilterAttribute, IActionHttpMethodProvider
    {
        private static readonly Collection<HttpMethod> SupportedMethods = new Collection<HttpMethod>(new[]
        {
            HttpMethod.Post
        });

        public Collection<HttpMethod> HttpMethods => SupportedMethods;

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext?.Response?.Headers != null && !actionExecutedContext.Response.Headers.Contains(nameof(HttpResponseHeaders.Location))
                && actionExecutedContext.Response.Content is ObjectContent objContent
                && actionExecutedContext.Response.IsSuccessStatusCode == true)
            {
                actionExecutedContext.Response.Headers.Location = new Uri("https://tempuri.org/");
            }

            base.OnActionExecuted(actionExecutedContext);
        }
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class UpdateAttribute : Attribute, IActionHttpMethodProvider
    {
        private static readonly Collection<HttpMethod> SupportedMethods = new Collection<HttpMethod>(new[]
        {
            HttpMethod.Put
        });

        public Collection<HttpMethod> HttpMethods => SupportedMethods;
    }

    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    public sealed class DeleteAttribute : Attribute, IActionHttpMethodProvider
    {
        private static readonly Collection<HttpMethod> SupportedMethods = new Collection<HttpMethod>(new[]
        {
            HttpMethod.Delete
        });

        public Collection<HttpMethod> HttpMethods => SupportedMethods;
    }

    public class DtoController<TDto> : ODataController
        where TDto : class
    {
        protected virtual ODataQueryOptions<TDto> GetODataQueryOptions()
        {
            return Request.GetODataQueryOptions<TDto>();
        }

        protected virtual SingleResult<T> SingleResult<T>(IQueryable<T> queryable)
        {
            return System.Web.Http.SingleResult.Create(queryable);
        }

        protected virtual SingleResult<T> SingleResult<T>(IEnumerable<T> values)
        {
            return SingleResult(queryable: values.AsQueryable());
        }

        protected virtual SingleResult<T> SingleResult<T>(T obj)
        {
            return SingleResult(values: new[] { obj });
        }

        protected virtual string CreateODataLink(string odataModule = null, string controller = null, string action = null, object routeValues = null)
        {
            IDictionary<string, object> routeValuesDictionary = Request
                .GetOwinContext()
                .GetDependencyResolver()
                .Resolve<IRouteValuesProvider>()
                .PopulateRouteValuesDictionary(routeValues ?? new { });

            return CreateODataLink(odataModule, controller, action, routeValuesDictionary);
        }

        protected virtual string CreateODataLink(string odataModule = null, string controller = null, string action = null, IDictionary<string, object> routeValues = null)
        {
            HttpRequestMessageProperties odataProps = Request.ODataProperties();

            if (odataModule == null)
            {
                odataModule = odataProps.RouteName.Replace("-odata", string.Empty);
            }
            if (controller == null)
            {
                controller = odataProps.Path.Segments.OfType<EntitySetSegment>().ExtendedSingle($"Finding entity set name from {odataProps.Path}").EntitySet.Name;
            }
            if (action == null)
            {
                action = odataProps.Path.Segments.OfType<OperationSegment>().ExtendedSingle($"Finding operation segment from {odataProps.Path}").Operations.ExtendedSingle($"Finding operation name from {odataProps.Path}").Name;
            }

            string url = RequestContext.Url.Link($"{odataModule}-odata", new { odataPath = $"{controller}/{action}", controller, action });

            url += "(";

            if (routeValues != null)
            {
                url += string.Join(",", routeValues.Select(routeValueItem =>
                {
                    object value = routeValueItem.Value;
                    if (value is string strValue)
                        value = HttpUtility.UrlEncode($"'{strValue}'");
                    return $"{routeValueItem.Key}={value}";
                }));
            }

            url += ")";

            return url;
        }
    }
}

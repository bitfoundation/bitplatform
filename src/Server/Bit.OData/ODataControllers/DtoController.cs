using System;
using System.Collections.ObjectModel;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using System.Web.OData;
using System.Web.OData.Extensions;
using System.Web.OData.Query;

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

        private static readonly Lazy<MethodInfo> GenerateODataLinkMethod = new Lazy<MethodInfo>(() =>
        {
            return typeof(MetadataController).GetTypeInfo()
                    .Assembly.GetType("System.Web.OData.Results.ResultHelpers")
                    .GetMethod("GenerateODataLink");
        });

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            if (actionExecutedContext?.Response?.Headers != null && !actionExecutedContext.Response.Headers.Contains(nameof(HttpResponseHeaders.Location))
                && actionExecutedContext.Response.Content is ObjectContent
                && actionExecutedContext.Response.IsSuccessStatusCode == true)
            {
                ObjectContent objContent = ((ObjectContent)(actionExecutedContext.Response.Content));

                if (objContent.Value == null)
                    return;

                actionExecutedContext.Response.Headers.Location = (Uri)GenerateODataLinkMethod.Value.Invoke(null, new [] { actionExecutedContext.Request, objContent.Value, false });
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
            HttpRequestMessageProperties requestODataProps = Request.ODataProperties();
            ODataQueryContext currentOdataQueryContext = new ODataQueryContext(Request.GetModel(), typeof(TDto).GetTypeInfo(), requestODataProps.Path);
            ODataQueryOptions<TDto> currentOdataQueryOptions = new ODataQueryOptions<TDto>(currentOdataQueryContext, Request);
            return currentOdataQueryOptions;
        }
    }
}
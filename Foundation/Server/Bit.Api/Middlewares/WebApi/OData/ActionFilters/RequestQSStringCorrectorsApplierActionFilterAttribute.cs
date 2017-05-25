using System;
using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Bit.Core.Contracts;

namespace Bit.Api.Middlewares.WebApi.OData.ActionFilters
{
    public class RequestQSStringCorrectorsApplierActionFilterAttribute : ActionFilterAttribute
    {
        private readonly IEnumerable<IStringCorrector> _stringCorrectors;

        public RequestQSStringCorrectorsApplierActionFilterAttribute(IEnumerable<IStringCorrector> stringCorrectors)
        {
            if (stringCorrectors == null)
                throw new ArgumentNullException(nameof(stringCorrectors));

            _stringCorrectors = stringCorrectors;
        }

        protected RequestQSStringCorrectorsApplierActionFilterAttribute()
        {

        }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            Uri url = actionContext.Request.RequestUri;

            if (!string.IsNullOrEmpty(url.Query))
            {
                foreach (IStringCorrector stringCorrector in _stringCorrectors)
                {
                    string originalQueryString = url.Query;
                    string fixedQueryString = stringCorrector.CorrectString(originalQueryString);
                    url = new Uri(url.AbsoluteUri.Replace(originalQueryString, fixedQueryString), UriKind.Absolute);
                }

                actionContext.Request.RequestUri = new Uri(url.ToString(), UriKind.Absolute);
            }

            base.OnActionExecuting(actionContext);
        }
    }
}

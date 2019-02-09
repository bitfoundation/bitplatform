using System;
using System.Collections.Generic;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Bit.Core.Contracts;

namespace Bit.OData.ActionFilters
{
    public class RequestQSStringCorrectorsApplierActionFilterAttribute : ActionFilterAttribute
    {
        public virtual IEnumerable<IStringCorrector> StringCorrectors { get; set; }

        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            Uri url = new Uri(actionContext.Request.RequestUri.ToString().AsUnescaped());

            if (!string.IsNullOrEmpty(url.Query))
            {
                foreach (IStringCorrector stringCorrector in StringCorrectors)
                {
                    string originalQueryString = url.Query;
                    string fixedQueryString = stringCorrector.CorrectString(originalQueryString);
                    url = new Uri(url.AbsoluteUri.Replace(originalQueryString, fixedQueryString, StringComparison.InvariantCultureIgnoreCase), UriKind.Absolute);
                }

                actionContext.Request.RequestUri = new Uri(url.ToString(), UriKind.Absolute);
            }

            base.OnActionExecuting(actionContext);
        }
    }
}

using Microsoft.AspNet.OData;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.AspNet.OData.Query;
using System.Reflection;

namespace System.Net.Http
{
    public static class HttpRequestMessageExtensions
    {
        public static ODataQueryOptions GetODataQueryOptions(this HttpRequestMessage request, TypeInfo dtoType)
        {
            HttpRequestMessageProperties requestODataProps = request.ODataProperties();
            ODataQueryContext currentOdataQueryContext = new ODataQueryContext(request.GetModel(), dtoType, requestODataProps.Path);
            ODataQueryOptions currentOdataQueryOptions = new ODataQueryOptions(currentOdataQueryContext, request);
            return currentOdataQueryOptions;
        }

        public static ODataQueryOptions<TDto> GetODataQueryOptions<TDto>(this HttpRequestMessage request)
        {
            HttpRequestMessageProperties requestODataProps = request.ODataProperties();
            ODataQueryContext currentOdataQueryContext = new ODataQueryContext(request.GetModel(), typeof(TDto), requestODataProps.Path);
            ODataQueryOptions<TDto> currentOdataQueryOptions = new ODataQueryOptions<TDto>(currentOdataQueryContext, request);
            return currentOdataQueryOptions;
        }
    }
}

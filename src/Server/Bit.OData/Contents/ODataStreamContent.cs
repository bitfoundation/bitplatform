using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;

namespace Bit.OData.Contents
{
    public class ODataStreamContent : StreamContent
    {
        public ODataStreamContent(Stream content)
            : base(content)
        {
            Headers.ContentType.MediaType = "application/json";
            Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata.metadata", "minimal"));
        }

        public ODataStreamContent(Stream content, int bufferSize)
            : base(content, bufferSize)
        {
            Headers.ContentType.MediaType = "application/json";
            Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata.metadata", "minimal"));
        }
    }
}

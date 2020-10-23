using Microsoft.AspNet.OData.Batch;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;

namespace Bit.OData.Implementations
{
    public class BitODataBatchHandler : DefaultODataBatchHandler
    {
        public BitODataBatchHandler(HttpServer httpServer)
            : base(httpServer)
        {
        }

        public async override Task<IList<ODataBatchResponseItem>> ExecuteRequestMessagesAsync(IEnumerable<ODataBatchRequestItem> requests, CancellationToken cancellationToken)
        {
            foreach (var batchReqs in requests.OfType<ChangeSetRequestItem>())
            {
                foreach (var req in batchReqs.Requests)
                {
                    if (req.Method == HttpMethod.Delete && req.RequestUri.AbsoluteUri.EndsWith("$ref"))
                    {
                        req.RequestUri = new Uri(req.RequestUri.AbsoluteUri.Replace(req.RequestUri.PathAndQuery, "/odata/Bit/Ref/HandleRef"));
                        req.Method = HttpMethod.Post;
                    }
                }
            }

            var result = await base.ExecuteRequestMessagesAsync(requests, cancellationToken);

            return result;
        }
    }
}

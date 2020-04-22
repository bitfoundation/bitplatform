using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.OData.Contents
{
    public class ODataPushStreamContent : PushStreamContent
    {
        public ODataPushStreamContent(Func<Stream, Task> onStreamAvailable, CancellationToken cancellationToken)
            : base(async (stream, content, transport) =>
            {
                if (onStreamAvailable == null)
                    throw new ArgumentNullException(nameof(onStreamAvailable));

                if (stream == null)
                    throw new ArgumentNullException(nameof(stream));

                await using (stream)
                {
                    await onStreamAvailable(stream);
                    await stream.FlushAsync(cancellationToken);
                }

            }, "application/json")
        {
            Headers.ContentType.Parameters.Add(new NameValueHeaderValue("odata.metadata", "minimal"));
        }
    }
}

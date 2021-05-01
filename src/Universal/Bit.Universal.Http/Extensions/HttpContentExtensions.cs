using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.Http.Extensions
{
#if !DotNet
    public static class HttpContentExtensions
    {
        public static Task<Stream> ReadAsStreamAsync(this HttpContent httpContent, CancellationToken cancellationToken)
        {
            return httpContent.ReadAsStreamAsync();
        }
    }
#endif
}

using System.Collections.Generic;

namespace System.Net.Http
{
    public static class HttpClientExtensions
    {
        public static HttpClient AddHeader(this HttpClient client, string key, string value)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            client.DefaultRequestHeaders.Add(key, value);

            return client;
        }

        public static HttpClient AddHeader(this HttpClient client, string key, IEnumerable<string> values)
        {
            if (client == null)
                throw new ArgumentNullException(nameof(client));

            if (values == null)
                throw new ArgumentNullException(nameof(values));

            client.DefaultRequestHeaders.Add(key, values);
            return client;
        }
    }
}

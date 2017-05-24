using System.Collections.Generic;

namespace System.Net.Http
{
    public static class HttpClientExtensions
    {
        public static HttpClient AddHeader(this HttpClient client, string key, string value)
        {
            client.DefaultRequestHeaders.Add(key, value);
            return client;
        }

        public static HttpClient AddHeader(this HttpClient client, string key, IEnumerable<string> values)
        {
            client.DefaultRequestHeaders.Add(key, values);
            return client;
        }
    }
}

namespace System;

public static partial class UriExtensions
{
    extension(Uri uri)
    {
        public string GetUrlWithoutQueryParameter(string key)
        {
            var qsCollection = AppQueryStringCollection.Parse(uri.Query);
            qsCollection.Remove(key);

            string pagePathWithoutQueryString = uri.GetLeftPart(UriPartial.Path);

            return qsCollection is { Count: > 0 }
                ? $"{pagePathWithoutQueryString}?{qsCollection}"
                : pagePathWithoutQueryString;
        }

        /// <summary>
        /// Reads culture from either route segment or query string.
        /// https://adminpanel.bitpaltform.dev/en-US/categories
        /// https://adminpanel.bitpaltform.dev/categories?culture=en-US
        /// </summary>
        public string? GetCulture()
        {
            if (CultureInfoManager.InvariantGlobalization)
                return null;

            if (AppQueryStringCollection.Parse(uri.Query).TryGetValue("culture", out var culture))
                return culture?.ToString();

            foreach (var segment in uri.Segments.Take(2))
            {
                var segmentValue = segment.Trim('/');
                if (CultureInfoManager.SupportedCultures.Any(sc => string.Equals(sc.Culture.Name, segmentValue, StringComparison.InvariantCultureIgnoreCase)))
                {
                    return segmentValue;
                }
            }

            return null;
        }

        public string GetUrlWithoutCulture()
        {
            uri = new Uri(uri.GetUrlWithoutQueryParameter("culture"));

            var culture = uri.GetCulture();

            if (string.IsNullOrEmpty(culture) is false)
            {
                uri = new Uri(uri.ToString()
                    .Replace($"{culture}/", string.Empty)
                    .Replace(culture, string.Empty));
            }

            return uri.ToString();
        }

        public string GetPath()
        {
            var uriBuilder = new UriBuilder(uri.GetUrlWithoutCulture()) { Query = string.Empty, Fragment = string.Empty };
            return uriBuilder.Path;
        }
    }
}

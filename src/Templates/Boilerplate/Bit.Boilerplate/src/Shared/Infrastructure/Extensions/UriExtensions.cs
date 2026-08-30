using System.Diagnostics.CodeAnalysis;

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
            {
                if (CultureInfoManager.GetCultureInfo(culture?.ToString()) is { } cultureInfo)
                    return cultureInfo.Name;
            }

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

            if (string.IsNullOrEmpty(culture))
                return uri.ToString();

            // Only the leading path segment carries the culture. Replacing the culture everywhere in the url instead
            // would strip its letters out of the rest of the path too, turning /en-US/product/en-US-shirt into
            // /product/-shirt.
            var segments = uri.AbsolutePath.Split('/'); // AbsolutePath always starts with '/', so segments[0] is empty.

            if (segments.Length < 2 || string.Equals(segments[1], culture, StringComparison.OrdinalIgnoreCase) is false)
                return uri.ToString(); // The culture came from somewhere other than the first segment.

            return new UriBuilder(uri)
            {
                Path = string.Join('/', segments.Take(1).Concat(segments.Skip(2)))
            }.Uri.ToString();
        }

        /// <summary>
        /// The url re-addressed to <paramref name="cultureName"/>: an existing culture (leading path segment or
        /// <c>?culture=</c>) is replaced by the new one as a leading path segment - the canonical form Server.Web
        /// serves pages under (See its <c>UseCultureUrlRedirection</c>). A culture-less url is returned unchanged.
        /// </summary>
        public string GetUrlWithCulture(string cultureName)
        {
            if (uri.GetCulture() is null)
                return uri.ToString();

            var urlWithoutCulture = new Uri(uri.GetUrlWithoutCulture());

            return new UriBuilder(urlWithoutCulture)
            {
                Path = $"/{cultureName}{urlWithoutCulture.AbsolutePath}"
            }.Uri.ToString();
        }

        /// <summary>
        /// The url with every query value replaced by <c>***</c>, keeping the path and the parameter names.
        /// </summary>
        /// <remarks>
        /// Use this wherever a url is recorded rather than navigated to. Several of this app's own routes carry a
        /// single-use credential in the query string - <c>?token=</c>, <c>?emailToken=</c>, <c>?phoneToken=</c>,
        /// <c>?otp=</c> - and <c>ITelemetryContext.PageUrl</c> is copied into the scope of every error record, which
        /// `DiagnosticLogger` keeps, the chatbot's `CheckLastError` tool reads, and Sentry/App Insights/OTLP export.
        /// <br/>
        /// Masking every value rather than a list of known-sensitive names is deliberate: this is a template, and a
        /// consumer's own token-carrying route would not be on any list this file could ship. The parameter names are
        /// kept because they are what makes a log record diagnosable.
        /// </remarks>
        public string GetUrlWithMaskedQueryValues()
        {
            var qsCollection = AppQueryStringCollection.Parse(uri.Query);

            if (qsCollection is { Count: 0 })
                return uri.ToString();

            // Composed by hand rather than through AppQueryStringCollection.ToString(), which would escape the mask
            // into %2A%2A%2A. The result is a log string, never something to navigate to.
            // The keys are re-escaped: Parse hands them back DECODED, so emitting one raw would let a key such as
            // `%0AInjected` put a newline into the log record, or `%26fake` invent a parameter that was never sent.
            return $"{uri.GetLeftPart(UriPartial.Path)}?{string.Join("&", qsCollection.Keys.Select(key => $"{Uri.EscapeDataString(key)}=***"))}";
        }

        public string GetPath()
        {
            var uriBuilder = new UriBuilder(uri.GetUrlWithoutCulture()) { Query = string.Empty, Fragment = string.Empty };
            return uriBuilder.Path;
        }

        /// <summary>
        /// True only for a relative url that cannot leave this app's origin, such as <c>/sign-in?otp=123</c>.
        /// <br/>
        /// <see cref="Uri.IsWellFormedUriString"/> with <see cref="UriKind.Relative"/> is NOT enough on its own:
        /// a network-path reference like <c>//attacker.com/path</c> is a well-formed RELATIVE reference, and both
        /// browsers and <c>NavigationManager.NavigateTo</c> resolve it to <c>https://attacker.com/path</c>.
        /// Browsers treat <c>/\host</c> and a leading backslash the same way, so those are rejected too.
        /// <br/>
        /// Use this wherever a url arrives from outside the app and is then navigated to - the Blazor Hybrid local
        /// HTTP server's external-sign-in callback, and every <c>return-url</c> that reaches
        /// <c>NavigationManager.NavigateTo</c>.
        /// </summary>
        /// <param name="requireLeadingSlash">
        /// Keep the default when the url is expected to be app-rooted (<c>/products/1</c>). Pass <c>false</c> for a
        /// <c>return-url</c>, because the app itself produces base-relative values without the leading slash - see
        /// <c>NavigationManagerExtensions.GetRelativePath</c>, which <c>AppShell</c> and <c>SignInModalService</c>
        /// feed straight into a <c>return-url</c>. Rootless values are equally origin-bound: a well-formed relative
        /// reference carries no scheme and no authority, so it always resolves against the app's own base.
        /// </param>
        public static bool IsAppRelativeUrl([NotNullWhen(true)] string? url, bool requireLeadingSlash = true)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            if (url[0] is '\\')
                return false;

            if (url[0] is '/')
            {
                if (url.Length > 1 && url[1] is '/' or '\\')
                    return false;
            }
            else if (requireLeadingSlash)
                return false;

            return Uri.IsWellFormedUriString(url, UriKind.Relative);
        }
    }
}

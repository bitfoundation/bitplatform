using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Core.Models;
using Bit.Owin.Contracts;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Validation;
using IdentityServer3.Core.ViewModels;
using Microsoft.Owin;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultViewService : IViewService
    {
        public virtual IHtmlPageProvider HtmlPageProvider { get; set; } = default!;

        public virtual IUrlStateProvider UrlStateProvider { get; set; } = default!;

        public virtual IPathProvider PathProvider { get; set; } = default!;

        public virtual AppEnvironment AppEnvironment { get; set; } = default!;

        public virtual IOwinContext OwinContext { get; set; } = default!;

        public virtual IRequestInformationProvider RequestInformationProvider { get; set; } = default!;

        public virtual Task<Stream> ClientPermissions(ClientPermissionsViewModel model)
        {
            string content = @"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>ClientPermissions >> Not Implemented</title>
                                </head>
                                <body>ClientPermissions >> Not Implemented</body>
                            </html>";

            return ReturnHtmlAsync(content, OwinContext.Request.CallCancelled);
        }

        public virtual Task<Stream> Consent(ConsentViewModel model, ValidatedAuthorizeRequest authorizeRequest)
        {
            string content = @"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>Consent >> Not Implemented</title>
                                </head>
                                <body>Consent >> Not Implemented</body>
                            </html>";

            return ReturnHtmlAsync(content, OwinContext.Request.CallCancelled);
        }

        public virtual Task<Stream> Error(ErrorViewModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            string content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>{model.ErrorMessage}</title>
                                </head>
                                <body>{model.ErrorMessage} <br />RequestId: {model.RequestId} </br>X-Correlation-ID: {RequestInformationProvider.XCorrelationId}</body>
                            </html>";

            return ReturnHtmlAsync(content, OwinContext.Request.CallCancelled);
        }

        public virtual async Task<Stream> LoggedOut(LoggedOutViewModel model, SignOutMessage message)
        {
            string? content = null;

            string? url = model?.RedirectUrl ?? message?.ReturnUrl;

            if (!string.IsNullOrEmpty(url))
            {
                return await RedirectToAsync(url, OwinContext.Request.CallCancelled).ConfigureAwait(false);
            }
            else
            {
                content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>No redirect url on logout</title>
                                </head>
                                <body>
                                    No redirect url on logout. Perhaps your redirect url is not listed in {nameof(OAuthClientsProvider.GetClients)} of {nameof(OAuthClientsProvider)}
                                </body>
                            </html>";
            }

            return await ReturnHtmlAsync(content, OwinContext.Request.CallCancelled).ConfigureAwait(false);
        }

        public virtual async Task<Stream> Login(LoginViewModel model, SignInMessage message)
        {
            if (model is null)
                throw new ArgumentNullException(nameof(model));

            if (message is null)
                throw new ArgumentNullException(nameof(message));

            string? signInType = null;

            if (model.Custom == null && message.ReturnUrl != null)
            {
                try
                {
                    dynamic custom = model.Custom = UrlStateProvider.GetState(new Uri(message.ReturnUrl));

                    try
                    {
                        signInType = custom.SignInType ?? message.IdP;
                    }
                    catch { }
                }
                catch
                {
                    model.Custom = new { };
                }
            }

            if (model.ErrorMessage is null && signInType is not null && model.ExternalProviders.Any(extProvider => extProvider.Type == signInType))
            {
                string redirectUri = model.ExternalProviders.Single(extProvider => extProvider.Type == signInType).Href;

                return await RedirectToAsync(redirectUri, OwinContext.Request.CallCancelled).ConfigureAwait(false);
            }

            string loginPagePath = PathProvider.MapStaticFilePath(AppEnvironment.GetConfig(AppEnvironment.KeyValues.IdentityServer.LoginPagePath, AppEnvironment.KeyValues.IdentityServer.LoginPagePathDefaultValue)!);

            if (File.Exists(loginPagePath))
            {
                JsonSerializerSettings jsonSerSettings = DefaultJsonContentFormatter.SerializeSettings();
                jsonSerSettings.ContractResolver = new BitCamelCasePropertyNamesContractResolver();

                string json = JsonConvert.SerializeObject(new
                {
                    model.AdditionalLinks,
                    model.AllowRememberMe,
                    model.AntiForgery,
                    model.ClientLogoUrl,
                    model.ClientName,
                    model.ClientUrl,
                    model.CurrentUser,
                    model.Custom,
                    model.ErrorMessage,
                    model.ExternalProviders,
                    model.LoginUrl,
                    model.LogoutUrl,
                    model.RememberMe,
                    model.RequestId,
                    model.SiteName,
                    model.SiteUrl,
                    UserName = model.Username, // https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/capitalization-conventions#capitalizing-compound-words-and-common-terms
                    ReturnUrl = message.ReturnUrl == null ? "" : new Uri(message.ReturnUrl).ParseQueryString()["redirect_uri"]
                }, Formatting.None, jsonSerSettings);

                string loginPageHtmlInitialHtml = await File.ReadAllTextAsync(loginPagePath, OwinContext.Request.CallCancelled).ConfigureAwait(false);

                string loginPageHtmlFinalHtml = (await HtmlPageProvider.GetHtmlPageAsync(loginPageHtmlInitialHtml, OwinContext.Request.CallCancelled).ConfigureAwait(false))
                    .Replace("{{model.LoginModel.toJson()}}", Microsoft.Security.Application.Encoder.HtmlEncode(json), StringComparison.InvariantCultureIgnoreCase);

                return await ReturnHtmlAsync(loginPageHtmlFinalHtml, OwinContext.Request.CallCancelled).ConfigureAwait(false);
            }
            else
            {
                model.ErrorMessage ??= "LoginPageFileNotFound";

                string content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>{model.ErrorMessage}</title>
                                </head>
                                <body>{model.ErrorMessage} <br />RequestId: {model.RequestId} </br>X-Correlation-ID: {RequestInformationProvider.XCorrelationId}</body>
                            </html>";

                return await ReturnHtmlAsync(content, OwinContext.Request.CallCancelled).ConfigureAwait(false);
            }
        }

        async Task<Stream> RedirectToAsync(string url, CancellationToken cancellationToken)
        {
            string content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta http-equiv='refresh' content='0;{url}'>
                                </head>
                                <body></body>
                            </html>";

            return await ReturnHtmlAsync(content, cancellationToken).ConfigureAwait(false);
        }

        async Task<Stream> ReturnHtmlAsync(string html, CancellationToken cancellationToken)
        {
            MemoryStream viewStream = new MemoryStream();

            await using StreamWriter writer = new StreamWriter(viewStream, Encoding.UTF8, bufferSize: -1, leaveOpen: true);

            await writer.WriteAsync(html).ConfigureAwait(false);

            await writer.FlushAsync().ConfigureAwait(false);

            viewStream.Seek(0, SeekOrigin.Begin);

            return viewStream;
        }

        public virtual Task<Stream> Logout(LogoutViewModel model, SignOutMessage message)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            // Based on current InvokeLogOut Middleware, this method will not be called, because of context.Authentication.SignOut("custom", "Bearer"); code.

            string content = $@"<!DOCTYPE html>
                            <html>
                                <body>
                                    <form id='logoutForm' method='post' action='{model.LogoutUrl}'>
                                        <input type='hidden' name='{model.AntiForgery.Name}' value='{model.AntiForgery.Value}'>
                                    </form>
                                    <script>
                                        document.getElementById('logoutForm').submit();
                                    </script>
                                </body>
                            </html>";

            return ReturnHtmlAsync(content, OwinContext.Request.CallCancelled);
        }
    }
}

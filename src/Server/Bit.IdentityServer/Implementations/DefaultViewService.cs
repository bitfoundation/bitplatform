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
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultViewService : IViewService
    {
        public virtual IHtmlPageProvider HtmlPageProvider { get; set; }

        public virtual IUrlStateProvider UrlStateProvider { get; set; }

        public virtual IPathProvider PathProvider { get; set; }

        public virtual AppEnvironment AppEnvironment { get; set; }

        public virtual IOwinContext OwinContext { get; set; }

        public virtual Task<Stream> ClientPermissions(ClientPermissionsViewModel model)
        {
            string content = @"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>ClientPermissions >> Not Implemented</title>
                                </head>
                                <body>ClientPermissions >> Not Implemented</body>
                            </html>";

            return ReturnHtmlAsync(model, content, OwinContext.Request.CallCancelled);
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

            return ReturnHtmlAsync(model, content, OwinContext.Request.CallCancelled);
        }

        public virtual Task<Stream> Error(ErrorViewModel model)
        {
            string content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>{model.ErrorMessage}</title>
                                </head>
                                <body>{model.ErrorMessage} <br /> RequestId: {model.RequestId}</body>
                            </html>";

            return ReturnHtmlAsync(model, content, OwinContext.Request.CallCancelled);
        }

        public virtual Task<Stream> LoggedOut(LoggedOutViewModel model, SignOutMessage message)
        {
            string content = null;

            string url = model?.RedirectUrl ?? message?.ReturnUrl;

            if (!string.IsNullOrEmpty(url))
            {
                content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta http-equiv='refresh' content='0;{url}'>
                                </head>
                                <body></body>
                            </html>";
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

            return ReturnHtmlAsync(model, content, OwinContext.Request.CallCancelled);
        }

        public virtual async Task<Stream> Login(LoginViewModel model, SignInMessage message)
        {
            JsonSerializerSettings jsonSerSettings = DefaultJsonContentFormatter.SerializeSettings();
            jsonSerSettings.ContractResolver = new BitCamelCasePropertyNamesContractResolver();

            if (model.Custom == null && message.ReturnUrl != null)
            {
                try
                {
                    dynamic custom = model.Custom = UrlStateProvider.GetState(new Uri(message.ReturnUrl));

                    string signInType = null;

                    try
                    {
                        signInType = custom.SignInType ?? message.IdP;
                    }
                    catch { }

                    if (signInType != null && model.ExternalProviders.Any(extProvider => extProvider.Type == signInType))
                    {
                        string redirectUri = model.ExternalProviders.Single(extProvider => extProvider.Type == signInType).Href;

                        return await ReturnHtmlAsync(model, $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta http-equiv='refresh' content='0;{redirectUri}'>
                                </head>
                                <body></body>
                            </html>", OwinContext.Request.CallCancelled).ConfigureAwait(false);
                    }
                }
                catch
                {
                    model.Custom = new { };
                }
            }

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
                model.Username,
                ReturnUrl = message.ReturnUrl == null ? "" : new Uri(message.ReturnUrl).ParseQueryString()["redirect_uri"]
            }, Formatting.None, jsonSerSettings);

            string loginPageHtmlInitialHtml = File.ReadAllText(PathProvider.MapStaticFilePath(AppEnvironment.GetConfig("LoginPagePath", "loginPage.html")));

            string loginPageHtmlFinalHtml = (await HtmlPageProvider.GetHtmlPageAsync(loginPageHtmlInitialHtml, OwinContext.Request.CallCancelled).ConfigureAwait(false))
                .Replace("{{model.LoginModel.toJson()}}", Microsoft.Security.Application.Encoder.HtmlEncode(json), StringComparison.InvariantCultureIgnoreCase);

            return await ReturnHtmlAsync(model, loginPageHtmlFinalHtml, OwinContext.Request.CallCancelled).ConfigureAwait(false);
        }

        private async Task<Stream> ReturnHtmlAsync(CommonViewModel model, string html, CancellationToken cancellationToken)
        {
            MemoryStream viewStream = new MemoryStream();

            StreamWriter writer = new StreamWriter(viewStream);

            await writer.WriteAsync(html).ConfigureAwait(false);

            await writer.FlushAsync().ConfigureAwait(false);

            viewStream.Seek(0, SeekOrigin.Begin);

            return viewStream;
        }

        public virtual Task<Stream> Logout(LogoutViewModel model, SignOutMessage message)
        {
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

            return ReturnHtmlAsync(model, content, OwinContext.Request.CallCancelled);
        }
    }
}

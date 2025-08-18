using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Web;
using Autofac;
using Bit.Core.Contracts;
using Bit.Core.Exceptions.Contracts;
using Bit.Core.Models;
using Bit.IdentityServer.Contracts;
using Bit.IdentityServer.Implementations;
using Bit.Owin.Contracts;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Owin;
using Newtonsoft.Json;
using Owin;

namespace Bit.IdentityServer
{
    public class IdentityServerMiddlewareConfiguration : IOwinMiddlewareConfiguration
    {
        public virtual AppEnvironment AppEnvironment { get; set; } = default!;
        public virtual IAppCertificatesProvider AppCertificatesProvider { get; set; } = default!;

        public virtual void Configure(IAppBuilder owinApp)
        {
            if (owinApp == null)
                throw new ArgumentNullException(nameof(owinApp));

            string issuerName = AppEnvironment.GetSsoIssuerName();

            X509SecurityKey issuerSigningKey = new X509SecurityKey(AppCertificatesProvider.GetSingleSignOnServerCertificate());

            owinApp.Map("/core/connect/token", coreApp =>
            {
                coreApp.Run(async context =>
                {
                    try
                    {
                        using var streamReader = new System.IO.StreamReader(context.Request.Body);
                        var requestBody = await streamReader.ReadToEndAsync(context.Request.CallCancelled);
                        var parsedRequestBody = HttpUtility.ParseQueryString(requestBody);
                        var dict = parsedRequestBody.AllKeys.ToDictionary(k => k, k => parsedRequestBody[k] as object);
                        if (dict.TryGetValue("acr_values", out var acr_values) && string.IsNullOrEmpty(acr_values?.ToString()) is false)
                        {
                            dict["acr_values"] = acr_values.ToString()!.Split(' ')
                                .ToDictionary(acr_value => acr_value.Split(':')[0], acr_value => HttpUtility.UrlDecode(acr_value.Split(':').ElementAtOrDefault(1) ?? ""));
                        }
                        string json = JsonConvert.SerializeObject(dict);
                        var request = System.Text.Json.JsonSerializer.Deserialize<LocalAuthenticationContext>(json);

                        var userService = context.GetDependencyResolver().Resolve<UserService>();

                        var bitJwtToken = await userService.LocalLogin(request, context.Request.CallCancelled);

                        ClaimsIdentity claimsIdentity = new([new("primary_sid", bitJwtToken.UserId), .. bitJwtToken.Claims.Select(c => new Claim(c.Key, c.Value ?? "NULL"))]);

                        var jwt = GenerateJwtToken(claimsIdentity, issuerSigningKey, bitJwtToken.ExpiresIn);

                        context.Response.ContentType = "application/json";

                        context.Response.StatusCode = 200;

                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            access_token = jwt,
                            token_type = "Bearer",
                            expires_in = bitJwtToken.ExpiresIn.TotalSeconds
                        }, JsonSerializerOptions.Web));
                    }
                    catch (Exception exp)
                    {
                        var exceptionToHttpErrorMapper = context.GetDependencyResolver().Resolve<IExceptionToHttpErrorMapper>();
                        context.Response.StatusCode = (int)exceptionToHttpErrorMapper.GetStatusCode(exp);
                        context.Response.ContentType = "application/json";
                        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(new
                        {
                            error = exp is IKnownException ? "invalid_grant" : "server_error",
                            error_description = exceptionToHttpErrorMapper.GetMessage(exp),
                        }, JsonSerializerOptions.Web));
                    }
                });
            });
        }

        private string GenerateJwtToken(ClaimsIdentity identity, X509SecurityKey issuerSigningKey, TimeSpan lifetime)
        {
            var jwtSecurityTokenHandler = new JwtSecurityTokenHandler();

            var issuerName = AppEnvironment.GetSsoIssuerName();

            var securityToken = jwtSecurityTokenHandler
                .CreateJwtSecurityToken(new SecurityTokenDescriptor
                {
                    Issuer = issuerName,
                    Audience = $"{issuerName}/resources",
                    IssuedAt = DateTimeOffset.UtcNow.DateTime,
                    Expires = DateTimeOffset.UtcNow.Add(lifetime).UtcDateTime,
                    SigningCredentials = new(issuerSigningKey, SecurityAlgorithms.RsaSha256),
                    Subject = new ClaimsIdentity(identity.Claims),
                });

            var encodedJwt = jwtSecurityTokenHandler.WriteToken(securityToken);

            return encodedJwt;
        }
    }
}

﻿using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Boilerplate.Server.Api;
using Boilerplate.Server.Api.Models.Identity;
using Boilerplate.Server.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static IServiceCollection AddIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;
        var settings = appSettings.IdentitySettings;

        services.AddIdentity<User, Role>(options =>
        {
            options.User.RequireUniqueEmail = settings.RequireUniqueEmail;
            options.SignIn.RequireConfirmedEmail = true;
            options.Password.RequireDigit = settings.PasswordRequireDigit;
            options.Password.RequireLowercase = settings.PasswordRequireLowercase;
            options.Password.RequireUppercase = settings.PasswordRequireUppercase;
            options.Password.RequireNonAlphanumeric = settings.PasswordRequireNonAlphanumeric;
            options.Password.RequiredLength = settings.PasswordRequiredLength;
        }).AddEntityFrameworkStores<AppDbContext>().AddDefaultTokenProviders();

        return services;
    }

    public static IServiceCollection AddJwt(this IServiceCollection services, IConfiguration configuration)
    {
        // https://github.com/dotnet/aspnetcore/issues/4660
        JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
        JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
        JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
        var settings = appSettings.JwtSettings;

        services.AddScoped<IJwtService, JwtService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var certificatePath = Path.Combine(Directory.GetCurrentDirectory(), "IdentityCertificate.pfx");
            RSA? rsaPrivateKey;
            using (X509Certificate2 signingCert = new X509Certificate2(certificatePath, appSettings.JwtSettings.IdentityCertificatePassword, OperatingSystem.IsWindows() ? X509KeyStorageFlags.EphemeralKeySet : X509KeyStorageFlags.DefaultKeySet))
            {
                rsaPrivateKey = signingCert.GetRSAPrivateKey();
            }

            var validationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new RsaSecurityKey(rsaPrivateKey),

                RequireExpirationTime = true,
                ValidateLifetime = true,

                ValidateAudience = true,
                ValidAudience = settings.Audience,

                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = async context =>
                {
                    // The server accepts the access_token from either the authorization header, the cookie, or the request URL query string

                    var access_token = context.Request.Cookies["access_token"];

                    if (string.IsNullOrEmpty(access_token))
                    {
                        access_token = context.Request.Query["access_token"];
                    }

                    context.Token = access_token;
                }
            };

            options.SaveToken = true;
            options.TokenValidationParameters = validationParameters;
        });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Boilerplate.Server.Api.xml"));
            options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "Boilerplate.Shared.xml"));

            options.OperationFilter<ODataOperationFilter>();

            options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "JWT Authorization header using the Bearer scheme."
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "bearerAuth"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    public static IServiceCollection AddHealthChecks(this IServiceCollection services, IWebHostEnvironment env, IConfiguration configuration)
    {
        var appSettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>()!;

        var healthCheckSettings = appSettings.HealthCheckSettings;

        if (healthCheckSettings.EnableHealthChecks is false)
            return services;

        services.AddHealthChecksUI(setupSettings: setup =>
        {
            setup.AddHealthCheckEndpoint("BPHealthChecks", env.IsDevelopment() ? "https://localhost:5031/healthz" : "/healthz");
        }).AddInMemoryStorage();

        var healthChecksBuilder = services.AddHealthChecks()
            .AddProcessAllocatedMemoryHealthCheck(maximumMegabytesAllocated: 6 * 1024)
            .AddDiskStorageHealthCheck(opt =>
                opt.AddDrive(Path.GetPathRoot(Directory.GetCurrentDirectory())!, minimumFreeMegabytes: 5 * 1024))
            .AddDbContextCheck<AppDbContext>();

        var emailSettings = appSettings.EmailSettings;

        if (emailSettings.UseLocalFolderForEmails is false)
        {
            healthChecksBuilder
                .AddSmtpHealthCheck(options =>
                {
                    options.Host = emailSettings.Host;
                    options.Port = emailSettings.Port;

                    if (emailSettings.HasCredential)
                    {
                        options.LoginWith(emailSettings.UserName, emailSettings.Password);
                    }
                });
        }

        return services;
    }
}

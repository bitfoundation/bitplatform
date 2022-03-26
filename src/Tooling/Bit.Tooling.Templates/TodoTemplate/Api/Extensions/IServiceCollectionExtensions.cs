using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TodoTemplate.Api;
using TodoTemplate.Api.Models.Account;
using TodoTemplate.Api.Services.Implementations;

namespace Microsoft.Extensions.DependencyInjection;

public static class IServiceCollectionExtensions
{
    public static void AddTodoTemplateIdentity(this IServiceCollection services, IConfiguration configuration)
    {
        var appsettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
        var settings = appsettings.IdentitySettings;

        services.AddIdentity<User, Role>(options =>
        {
            options.User.RequireUniqueEmail = settings.RequireUniqueEmail;
            options.SignIn.RequireConfirmedEmail = true;
            options.Password.RequireDigit = settings.PasswordRequireDigit;
            options.Password.RequireLowercase = settings.PasswordRequireLowercase;
            options.Password.RequireUppercase = settings.PasswordRequireUppercase;
            options.Password.RequireNonAlphanumeric = settings.PasswordRequireNonAlphanumeric;
            options.Password.RequiredLength = settings.PasswordRequiredLength;
        }).AddEntityFrameworkStores<TodoTemplateDbContext>().AddDefaultTokenProviders();
    }

    public static void AddTodoTemplateJwt(this IServiceCollection services, IConfiguration configuration)
    {
        var appsettings = configuration.GetSection(nameof(AppSettings)).Get<AppSettings>();
        var settings = appsettings.JwtSettings;

        services.AddScoped<IJwtService, JwtService>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        }).AddJwtBearer(options =>
        {
            var secretKey = Encoding.UTF8.GetBytes(settings.SecretKey);

            var validationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),

                RequireExpirationTime = true,
                ValidateLifetime = true,

                ValidateAudience = true,
                ValidAudience = settings.Audience,

                ValidateIssuer = true,
                ValidIssuer = settings.Issuer,
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var access_token = context.Request.Cookies["access_token"];

                    if (string.IsNullOrEmpty(access_token))
                    {
                        access_token = context.Request.Query["access_token"];
                    }

                    context.Token = access_token;

                    return Task.CompletedTask;
                }
            };

            options.SaveToken = true;
            options.TokenValidationParameters = validationParameters;
        });

        services.AddAuthorization();
    }

    public static void AddTodoTemplateSwaggerGen(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
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
                    new string[] {}
                }
            });
        });
    }
}

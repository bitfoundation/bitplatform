using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using TodoTemplate.Api.Contracts;
using TodoTemplate.Api.Data.Models.Account;
using TodoTemplate.Api.Implementations;

namespace TodoTemplate.Api.Extensions
{
    public static class IServiceCollectionExtensions
    {
        public static void AddCustomIdentity(this IServiceCollection services, IdentitySettings settings)
        {
            services.AddIdentity<User, Role>(options =>
            {
                options.User.RequireUniqueEmail = settings.RequireUniqueEmail;
                options.Password.RequireDigit = settings.PasswordRequireDigit;
                options.Password.RequireLowercase = settings.PasswordRequireLowercase;
                options.Password.RequireUppercase = settings.PasswordRequireUppercase;
                options.Password.RequireNonAlphanumeric = settings.PasswordRequireNonAlphanumeric;
                options.Password.RequiredLength = settings.PasswordRequiredLength;

            }).AddEntityFrameworkStores<TodoTemplateDbContext>().AddDefaultTokenProviders();
        }

        public static void AddCustomJwt(this IServiceCollection services, JwtSettings settings)
        {
            services.AddScoped<IJwtService, JwtService>();

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(options =>
            {
                var secretKey = Encoding.UTF8.GetBytes(settings.SecretKey);
                var encryptionKey = Encoding.UTF8.GetBytes(settings.EncryptKey);

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

                    TokenDecryptionKey = new SymmetricSecurityKey(encryptionKey)
                };

                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = validationParameters;
            });
        }

        public static void AddCustomSwaggerGen(this IServiceCollection services)
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
}

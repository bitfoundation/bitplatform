using System.IO.Compression;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using TodoTemplate.Api.Data.Models.Account;

#if BlazorWebAssembly
using System.Net.Http;
using Microsoft.AspNetCore.Components;
#endif

namespace TodoTemplate.Api;

public class Startup
{
    public IConfiguration Configuration { get; }

    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
#if BlazorWebAssembly
            services.AddTodoTemplateServices();
            services.AddScoped(c =>
            {
                // this is for pre rendering of blazor client/wasm
                // Using this registration + registrations provided in Program.cs/Startup.cs of TodoTemplate.App project,
                // you can inject HttpClient and call TodoTemplate.Api api controllers in blazor pages.
                // for other usages of http client, for example calling 3rd party apis, please use services.AddHttpClient("NamedHttpClient"), then inject IHttpClientFactory and use its CreateClient("NamedHttpClient") method.
                return new HttpClient(new TodoTemplateHttpClientHandler()) { BaseAddress = new Uri($"{c.GetRequiredService<NavigationManager>().BaseUri}api/") };
            });
            services.AddRazorPages();
#endif

        services.AddCors();

        services.AddMvcCore();

        services.AddControllers();

        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();

        services.AddTodoTemplateSharedServices();

        services.AddResponseCompression(opts =>
        {
            opts.EnableForHttps = true;
            opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Where(m => m != "text/html").Concat(new[] { "application/octet-stream" }).ToArray();
            opts.Providers.Add<BrotliCompressionProvider>();
            opts.Providers.Add<GzipCompressionProvider>();
        })
            .Configure<BrotliCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest)
            .Configure<GzipCompressionProviderOptions>(opt => opt.Level = CompressionLevel.Fastest);

        services.AddDbContext<TodoTemplateDbContext>(options =>
        {
            options.UseSqlServer(Configuration.GetConnectionString("SqlServerConnection"));
        });

        services.AddEndpointsApiExplorer();

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

        services.AddAutoMapper(typeof(Startup).Assembly);

        services.AddIdentity<User, Role>(options =>
        {
            options.User.RequireUniqueEmail = true;
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;

        }).AddEntityFrameworkStores<TodoTemplateDbContext>().AddDefaultTokenProviders();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;

        }).AddJwtBearer(options =>
        {
            var secretKey = Encoding.UTF8.GetBytes("LongerThan-16Char-SecretKey");
            var encryptionKey = Encoding.UTF8.GetBytes("16CharEncryptKey");

            var validationParameters = new TokenValidationParameters
            {
                ClockSkew = TimeSpan.Zero,
                RequireSignedTokens = true,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),

                RequireExpirationTime = true,
                ValidateLifetime = true,

                ValidateAudience = true,
                ValidAudience = "MyWebsite",

                ValidateIssuer = true,
                ValidIssuer = "MyWebsite",

                TokenDecryptionKey = new SymmetricSecurityKey(encryptionKey)
            };

            options.RequireHttpsMetadata = false;
            options.SaveToken = true;
            options.TokenValidationParameters = validationParameters;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseSwagger();

            app.UseSwaggerUI();

            app.UseDeveloperExceptionPage();

#if BlazorWebAssembly
                app.UseWebAssemblyDebugging();
#endif
        }

#if BlazorWebAssembly
            app.UseBlazorFrameworkFiles();
#endif

        app.UseResponseCompression();

        app.UseStaticFiles(new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue()
                {
                    MaxAge = TimeSpan.FromDays(365),
                    Public = true
                };
            }
        });

        app.UseRouting();

        app.UseCors(options => options.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());

        app.UseAuthentication();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapDefaultControllerRoute();

#if BlazorWebAssembly
                endpoints.MapFallbackToPage("/_Host");
#endif
        });
    }
}

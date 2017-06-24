using System.Threading.Tasks;
using Bit.Core.Contracts;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Bit.OwinCore.Middlewares
{
    public class AspNetCoreLogRequestInformationMiddleware
    {
        private readonly RequestDelegate Next;

        public AspNetCoreLogRequestInformationMiddleware(RequestDelegate next)
        {
            Next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            ILogger logger = context.RequestServices.GetService<ILogger>();

            IRequestInformationProvider requestInformationProvider = context.RequestServices.GetService<IRequestInformationProvider>();

            logger.AddLogData(nameof(IRequestInformationProvider.HttpMethod), requestInformationProvider.HttpMethod);
            logger.AddLogData(nameof(IRequestInformationProvider.RequestUri), requestInformationProvider.RequestUri);

            if (requestInformationProvider.ClientAppVersion != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientAppVersion), requestInformationProvider.ClientAppVersion);

            if (requestInformationProvider.SystemLanguage != null)
                logger.AddLogData(nameof(IRequestInformationProvider.SystemLanguage), requestInformationProvider.SystemLanguage);

            if (requestInformationProvider.ClientCulture != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientCulture), requestInformationProvider.ClientCulture);

            if (requestInformationProvider.ClientType != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientType), requestInformationProvider.ClientType);

            if (requestInformationProvider.ClientDateTime != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientDateTime), requestInformationProvider.ClientDateTime);

            if (requestInformationProvider.ClientDebugMode != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientDebugMode), requestInformationProvider.ClientDebugMode);

            if (requestInformationProvider.ClientScreenSize != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientScreenSize), requestInformationProvider.ClientScreenSize);

            if (requestInformationProvider.ContentType != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ContentType), requestInformationProvider.ContentType);

            if (requestInformationProvider.Origin != null)
                logger.AddLogData(nameof(IRequestInformationProvider.Origin), requestInformationProvider.Origin);

            if (requestInformationProvider.Referer != null)
                logger.AddLogData(nameof(IRequestInformationProvider.Referer), requestInformationProvider.Referer);

            logger.AddLogData(nameof(IRequestInformationProvider.ClientIp), requestInformationProvider.ClientIp);

            if (requestInformationProvider.ClientPlatform != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientPlatform), requestInformationProvider.ClientPlatform);

            if (requestInformationProvider.ClientRoute != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientRoute), requestInformationProvider.ClientRoute);

            if (requestInformationProvider.ClientSysLanguage != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientSysLanguage), requestInformationProvider.ClientSysLanguage);

            if (requestInformationProvider.ClientTheme != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientTheme), requestInformationProvider.ClientTheme);

            if (requestInformationProvider.UserAgent != null)
                logger.AddLogData(nameof(IRequestInformationProvider.UserAgent), requestInformationProvider.UserAgent);

            if (requestInformationProvider.CorrelationId != null)
                logger.AddLogData("X-CorrelationId", requestInformationProvider.CorrelationId);

            await Next.Invoke(context);
        }
    }
}
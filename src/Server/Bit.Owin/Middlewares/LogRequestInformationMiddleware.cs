using System.Threading.Tasks;
using Bit.Core.Contracts;
using Microsoft.Owin;

namespace Bit.Owin.Middlewares
{
    public class LogRequestInformationMiddleware : OwinMiddleware
    {
        public LogRequestInformationMiddleware(OwinMiddleware next)
            : base(next)
        {
        }

        public override Task Invoke(IOwinContext context)
        {
            IDependencyResolver dependencyResolver = context.GetDependencyResolver();

            ILogger logger = dependencyResolver.Resolve<ILogger>();

            IRequestInformationProvider requestInformationProvider =
                dependencyResolver.Resolve<IRequestInformationProvider>();

            LogRequest(logger, requestInformationProvider);

            return Next.Invoke(context);
        }

        public static void LogRequest(ILogger logger, IRequestInformationProvider requestInformationProvider)
        {
            logger.AddLogData(nameof(IRequestInformationProvider.HttpMethod), requestInformationProvider.HttpMethod);
            logger.AddLogData(nameof(IRequestInformationProvider.DisplayUrl), requestInformationProvider.DisplayUrl);

            if (requestInformationProvider.ClientAppVersion != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientAppVersion), requestInformationProvider.ClientAppVersion);

            if (requestInformationProvider.SystemLanguage != null)
                logger.AddLogData(nameof(IRequestInformationProvider.SystemLanguage), requestInformationProvider.SystemLanguage);

            if (requestInformationProvider.ClientCulture != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientCulture), requestInformationProvider.ClientCulture);

            if (requestInformationProvider.ClientType != null)
                logger.AddLogData(nameof(IRequestInformationProvider.ClientType), requestInformationProvider.ClientType);

            if (requestInformationProvider.BitClientType != null)
                logger.AddLogData(nameof(IRequestInformationProvider.BitClientType), requestInformationProvider.BitClientType);

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
        }
    }
}
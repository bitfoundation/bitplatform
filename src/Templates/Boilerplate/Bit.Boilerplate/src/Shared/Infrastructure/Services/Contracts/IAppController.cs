namespace Boilerplate.Shared.Infrastructure.Services.Contracts
{
    public interface IAppController
    {
        void AddQueryString(string key, object? value) { }
        void AddQueryStrings(Dictionary<string, object?> queryString) { }
    }
}

namespace Boilerplate.Shared.Infrastructure.Services.Contracts
{
    public static class IAppControllerExtensions
    {
        extension<TAppController>(TAppController controller)
            where TAppController : IAppController
        {
            public TAppController WithQuery(string? existingQueryString)
            {
                return controller.WithQuery(queryString: AppQueryStringCollection.Parse(existingQueryString));
            }

            public TAppController WithQuery(string key, object? value)
            {
                controller.AddQueryString(key, value);
                return controller;
            }

            public TAppController WithQuery(Dictionary<string, object?> queryString)
            {
                controller.AddQueryStrings(queryString);
                return controller;
            }

            public TAppController WithQueryIf(bool condition, string key, object? value)
            {
                if (condition)
                {
                    controller.WithQuery(key, value);
                }
                return controller;
            }
        }
    }
}

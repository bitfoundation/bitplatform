using Boilerplate.Shared.Controllers;

namespace Boilerplate.Shared.Controllers
{
    public interface IAppController
    {
        void AddQueryString(string existingQueryString) { }
        void AddQueryString(string key, object? value) { }
        void AddQueryStrings(Dictionary<string, object?> queryString) { }
    }
}

namespace Boilerplate.Shared
{
    public static class IAppControllerExtensions
    {
        public static TAppController WithQueryString<TAppController>(this TAppController controller, string existingQueryString)
            where TAppController : IAppController
        {
            controller.AddQueryString(existingQueryString);
            return controller;
        }

        public static TAppController WithQueryString<TAppController>(this TAppController controller, string key, object? value)
            where TAppController : IAppController
        {
            controller.AddQueryString(key, value);
            return controller;
        }
        public static TAppController WithQueryStrings<TAppController>(this TAppController controller, Dictionary<string, object?> queryString)
            where TAppController : IAppController
        {
            controller.AddQueryStrings(queryString);
            return controller;
        }
    }
}

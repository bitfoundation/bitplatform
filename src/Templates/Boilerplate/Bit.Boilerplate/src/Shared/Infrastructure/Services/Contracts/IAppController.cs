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
                var copy = Copy(controller);
                copy.AddQueryString(key, value);
                return copy;
            }

            public TAppController WithQuery(Dictionary<string, object?> queryString)
            {
                var copy = Copy(controller);
                copy.AddQueryStrings(queryString);
                return copy;
            }

            public TAppController WithQueryIf(bool condition, string key, object? value)
            {
                return condition ? controller.WithQuery(key, value) : controller;
            }
        }

        private static TAppController Copy<TAppController>(TAppController controller)
            where TAppController : IAppController
        {
            return controller is ICloneable cloneable ? (TAppController)cloneable.Clone() : controller;
        }
    }
}

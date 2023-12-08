using System.Text.Json;
using System.Web;
using Boilerplate.Client.Core.Controllers.Categories;
using Boilerplate.Client.Core.Controllers.Identity;
using Boilerplate.Client.Core.Controllers.Todo;
using Boilerplate.Shared.Dtos.Categories;
using Boilerplate.Shared.Dtos.Dashboard;
using Boilerplate.Shared.Dtos.Identity;
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Dtos.Todo;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IHttpClientServiceCollectionExtensions
    {
        public static void AddTypedHttpClients(this IServiceCollection services)
        {
            services.TryAddTransient<ICategoryController, CategoryController>();
            services.TryAddTransient<IProductController, ProductController>();
            services.TryAddTransient<ITodoItemController, TodoItemController>();
            services.TryAddTransient<IDashboardController, DashboardController>();
            services.TryAddTransient<IUserController, UserController>();
            services.TryAddTransient<IIdentityController, IdentityController>();
        }
    }
}

namespace Boilerplate.Client.Core.Services
{
    internal class AppControllerBase
    {
        public Dictionary<string, object?> QueryString { get; set; } = [];

        internal static string BuildQueryString(Dictionary<string, object?>? query)
        {
            if (query is not { Count: > 0 })
                return string.Empty;

            var collection = HttpUtility.ParseQueryString(string.Empty);

            foreach (var key in query.Keys)
            {
                collection.Add(key, query[key]?.ToString());
            }

            query.Clear();

            return $"?{collection}";
        }
    }

    internal class CategoryController(HttpClient httpClient, JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : AppControllerBase, ICategoryController
    {
        public virtual async Task<IAsyncEnumerable<CategoryDto>> Get(CancellationToken cancellationToken = default)
        {
            var url = $"Category/Get{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<IAsyncEnumerable<CategoryDto>>();

                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }

        public virtual async Task<CategoryDto> Get(int id, CancellationToken cancellationToken = default)
        {
            var url = $"Category/Get/{id}{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<CategoryDto>();

                return (await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task<PagedResult<CategoryDto>> GetCategories(CancellationToken cancellationToken = default)
        {
            var url = $"Category/GetCategories{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<PagedResult<CategoryDto>>();

                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }

        public virtual async Task<CategoryDto> Create(CategoryDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Category/Create{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var requestJsonTypeInfo = options.GetTypeInfo<CategoryDto>();

                var response = await httpClient.PostAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);

                var responseJsonTypeInfo = options.GetTypeInfo<CategoryDto>();

                return (await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task<CategoryDto> Update(CategoryDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Category/Update{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var requestJsonTypeInfo = options.GetTypeInfo<CategoryDto>();

                var response = await httpClient.PutAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);

                var responseJsonTypeInfo = options.GetTypeInfo<CategoryDto>();

                return (await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task Delete(int id, CancellationToken cancellationToken = default)
        {
            var url = $"Category/Delete/{id}{BuildQueryString(QueryString)}";

            await httpClient.DeleteAsync(url, cancellationToken);
        }
    }

    internal class ProductController(HttpClient httpClient, JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : AppControllerBase, IProductController
    {
        public virtual async Task<ProductDto> Get(int id, CancellationToken cancellationToken = default)
        {
            var url = $"Product/Get/{id}{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<ProductDto>();

                return (await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task<PagedResult<ProductDto>> GetProducts(CancellationToken cancellationToken = default)
        {
            var url = $"Product/GetProducts{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<PagedResult<ProductDto>>();

                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }

        public virtual async Task<ProductDto> Create(ProductDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Product/Create{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var requestJsonTypeInfo = options.GetTypeInfo<ProductDto>();

                var response = await httpClient.PostAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);

                var responseJsonTypeInfo = options.GetTypeInfo<ProductDto>();

                return (await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task<ProductDto> Update(ProductDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Product/Update{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var requestJsonTypeInfo = options.GetTypeInfo<ProductDto>();

                var response = await httpClient.PutAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);

                var responseJsonTypeInfo = options.GetTypeInfo<ProductDto>();

                return (await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task Delete(int id, CancellationToken cancellationToken = default)
        {
            var url = $"Product/Delete/{id}{BuildQueryString(QueryString)}";

            await httpClient.DeleteAsync(url, cancellationToken);
        }
    }

    internal class TodoItemController(HttpClient httpClient, JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : AppControllerBase, ITodoItemController
    {
        public virtual async Task<IAsyncEnumerable<TodoItemDto>> Get(CancellationToken cancellationToken = default)
        {
            var url = $"TodoItem/Get{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<IAsyncEnumerable<TodoItemDto>>();

                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }

        public virtual async Task<TodoItemDto> Get(int id, CancellationToken cancellationToken = default)
        {
            var url = $"TodoItem/Get/{id}{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<TodoItemDto>();

                return (await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task<PagedResult<TodoItemDto>> GetTodoItems(CancellationToken cancellationToken = default)
        {
            var url = $"TodoItem/GetTodoItems{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<PagedResult<TodoItemDto>>();

                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }

        public virtual async Task<TodoItemDto> Create(TodoItemDto body, CancellationToken cancellationToken = default)
        {
            var url = $"TodoItem/Create{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var requestJsonTypeInfo = options.GetTypeInfo<TodoItemDto>();

                var response = await httpClient.PostAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);

                var responseJsonTypeInfo = options.GetTypeInfo<TodoItemDto>();

                return (await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task<TodoItemDto> Update(TodoItemDto body, CancellationToken cancellationToken = default)
        {
            var url = $"TodoItem/Update{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var requestJsonTypeInfo = options.GetTypeInfo<TodoItemDto>();

                var response = await httpClient.PutAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);

                var responseJsonTypeInfo = options.GetTypeInfo<TodoItemDto>();

                return (await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task Delete(int id, CancellationToken cancellationToken = default)
        {
            var url = $"TodoItem/Delete/{id}{BuildQueryString(QueryString)}";

            await httpClient.DeleteAsync(url, cancellationToken);
        }
    }

    internal class DashboardController(HttpClient httpClient, JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : AppControllerBase, IDashboardController
    {
        public virtual async Task<OverallAnalyticsStatsDataResponseDto> GetOverallAnalyticsStatsData(CancellationToken cancellationToken = default)
        {
            var url = $"Dashboard/GetOverallAnalyticsStatsData{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<OverallAnalyticsStatsDataResponseDto>();

                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }

        public virtual async Task<IAsyncEnumerable<ProductsCountPerCategoryResponseDto>> GetProductsCountPerCategoryStats(CancellationToken cancellationToken = default)
        {
            var url = $"Dashboard/GetProductsCountPerCategoryStats{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<IAsyncEnumerable<ProductsCountPerCategoryResponseDto>>();

                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }

        public virtual async Task<IAsyncEnumerable<ProductSaleStatResponseDto>> GetProductsSalesStats(CancellationToken cancellationToken = default)
        {
            var url = $"Dashboard/GetProductsSalesStats{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<IAsyncEnumerable<ProductSaleStatResponseDto>>();

                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }

        public virtual async Task<ProductPercentagePerCategoryResponseDto[]> GetProductsPercentagePerCategoryStats(CancellationToken cancellationToken = default)
        {
            var url = $"Dashboard/GetProductsPercentagePerCategoryStats{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<ProductPercentagePerCategoryResponseDto[]>();

                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }
    }

    internal class IdentityController(HttpClient httpClient, JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : AppControllerBase, IIdentityController
    {
        public virtual async Task SignUp(SignUpRequestDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Identity/SignUp{BuildQueryString(QueryString)}";

            var requestJsonTypeInfo = options.GetTypeInfo<SignUpRequestDto>();

            await httpClient.PostAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);
        }

        public virtual async Task SendConfirmationEmail(SendConfirmationEmailRequestDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Identity/SendConfirmationEmail{BuildQueryString(QueryString)}";

            var requestJsonTypeInfo = options.GetTypeInfo<SendConfirmationEmailRequestDto>();

            await httpClient.PostAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);
        }

        public virtual async Task<TokenResponseDto> SignIn(SignInRequestDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Identity/SignIn{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var requestJsonTypeInfo = options.GetTypeInfo<SignInRequestDto>();

                var response = await httpClient.PostAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);

                var responseJsonTypeInfo = options.GetTypeInfo<TokenResponseDto>();

                return (await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task<TokenResponseDto> Refresh(RefreshRequestDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Identity/Refresh{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var requestJsonTypeInfo = options.GetTypeInfo<RefreshRequestDto>();

                var response = await httpClient.PostAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);

                var responseJsonTypeInfo = options.GetTypeInfo<TokenResponseDto>();

                return (await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task SendResetPasswordEmail(SendResetPasswordEmailRequestDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Identity/SendResetPasswordEmail{BuildQueryString(QueryString)}";

            var requestJsonTypeInfo = options.GetTypeInfo<SendResetPasswordEmailRequestDto>();

            await httpClient.PostAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);
        }

        public virtual async Task ResetPassword(ResetPasswordRequestDto body, CancellationToken cancellationToken = default)
        {
            var url = $"Identity/ResetPassword{BuildQueryString(QueryString)}";

            var requestJsonTypeInfo = options.GetTypeInfo<ResetPasswordRequestDto>();

            await httpClient.PostAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);
        }
    }

    internal class UserController(HttpClient httpClient, JsonSerializerOptions options, IPrerenderStateService prerenderStateService) : AppControllerBase, IUserController
    {
        public virtual async Task<UserDto> GetCurrentUser(CancellationToken cancellationToken = default)
        {
            var url = $"User/GetCurrentUser{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var responseJsonTypeInfo = options.GetTypeInfo<UserDto>();
                return await httpClient.GetFromJsonAsync(url, responseJsonTypeInfo, cancellationToken);
            }))!;
        }

        public virtual async Task<UserDto> Update(EditUserDto body, CancellationToken cancellationToken = default)
        {
            var url = $"User/Update{BuildQueryString(QueryString)}";

            return (await prerenderStateService.GetValue(url, async () =>
            {
                var requestJsonTypeInfo = options.GetTypeInfo<EditUserDto>();

                var response = await httpClient.PutAsJsonAsync(url, body, requestJsonTypeInfo, cancellationToken);

                var responseJsonTypeInfo = options.GetTypeInfo<UserDto>();

                return (await response.Content.ReadFromJsonAsync(responseJsonTypeInfo, cancellationToken))!;
            }))!;
        }

        public virtual async Task Delete(CancellationToken cancellationToken = default)
        {
            var url = $"User/Delete{BuildQueryString(QueryString)}";

            await httpClient.DeleteAsync(url, cancellationToken);
        }
    }
}

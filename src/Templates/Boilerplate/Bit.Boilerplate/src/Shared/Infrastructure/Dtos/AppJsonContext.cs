//+:cnd:noEmit
//#if (sample == true || offlineDb == true)
using Boilerplate.Shared.Features.Todo;
//#endif
//#if (module == "Admin")
//#endif
//#if (module == "Admin" || module == "Sales")
using Boilerplate.Shared.Features.Categories;
using Boilerplate.Shared.Features.Products;
using Boilerplate.Shared.Features.Dashboard;
//#endif
//#if (notification == true)
//#endif
//#if (signalR == true)
using Boilerplate.Shared.Features.Chatbot;
using Boilerplate.Shared.Infrastructure.Dtos.SignalR;
//#endif
using Boilerplate.Shared.Features.Statistics;
using Boilerplate.Shared.Features.Diagnostic;
//#if (offlineDb == true)
using CommunityToolkit.Datasync.Server.Abstractions.Json;
//#endif

namespace Boilerplate.Shared.Infrastructure.Dtos;

/// <summary>
/// https://devblogs.microsoft.com/dotnet/try-the-new-system-text-json-source-generator/
/// </summary>
[JsonSourceGenerationOptions(

  //#if (offlineDb == true)
  Converters = [
    typeof(DateTimeConverter),
    typeof(DateTimeOffsetConverter),
    typeof(TimeOnlyConverter)
  ],
  //#endif


  PropertyNameCaseInsensitive = true,
  PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
  DictionaryKeyPolicy = JsonKnownNamingPolicy.CamelCase,
  UseStringEnumConverter = true,
  WriteIndented = false,
  GenerationMode = JsonSourceGenerationMode.Metadata,
  AllowTrailingCommas = true,
  DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault
)]


[JsonSerializable(typeof(Dictionary<string, JsonElement>))]
[JsonSerializable(typeof(Dictionary<string, string?>))]
[JsonSerializable(typeof(TimeSpan))]
[JsonSerializable(typeof(string[]))]
[JsonSerializable(typeof(Guid[]))]
[JsonSerializable(typeof(GitHubStats))]
[JsonSerializable(typeof(NugetStatsDto))]
[JsonSerializable(typeof(AppProblemDetails))]
//#if (notification == true)
[JsonSerializable(typeof(PushNotificationSubscriptionDto))]
//#endif
//#if (sample == true || offlineDb == true)
[JsonSerializable(typeof(TodoItemDto))]
[JsonSerializable(typeof(PagedResponse<TodoItemDto>))]
[JsonSerializable(typeof(List<TodoItemDto>))]
//#endif
//#if (module == "Admin" || module == "Sales")
[JsonSerializable(typeof(CategoryDto))]
[JsonSerializable(typeof(List<CategoryDto>))]
[JsonSerializable(typeof(PagedResponse<CategoryDto>))]
[JsonSerializable(typeof(ProductDto))]
[JsonSerializable(typeof(List<ProductDto>))]
[JsonSerializable(typeof(PagedResponse<ProductDto>))]
//#endif
//#if (module == "Admin")
[JsonSerializable(typeof(List<ProductsCountPerCategoryResponseDto>))]
[JsonSerializable(typeof(OverallAnalyticsStatsDataResponseDto))]
[JsonSerializable(typeof(List<ProductPercentagePerCategoryResponseDto>))]
//#endif

//#if (signalR == true)
[JsonSerializable(typeof(DiagnosticLogDto[]))]
[JsonSerializable(typeof(StartChatRequest))]
[JsonSerializable(typeof(List<SystemPromptDto>))]
[JsonSerializable(typeof(BackgroundJobProgressDto))]
//#endif
public partial class AppJsonContext : JsonSerializerContext
{
}

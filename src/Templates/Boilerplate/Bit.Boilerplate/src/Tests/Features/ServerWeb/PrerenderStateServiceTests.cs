using Microsoft.AspNetCore.Components.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Boilerplate.Client.Web.Infrastructure.Services;
using Boilerplate.Shared.Features.Identity.Dtos;

namespace Boilerplate.Tests.Features.ServerWeb;

[TestClass, TestCategory("IntegrationTest"), TestCategory("PreRendering")]
public partial class PrerenderStateServiceTests
{
    public TestContext TestContext { get; set; } = default!;

    [TestMethod]
    public async Task PrerenderedValues_Should_ReachTheClient_WithoutCallingTheApiAgain()
    {
        await using var server = new AppTestServer();
        await server.Build(configureTestConfiguration: configuration =>
        {
            configuration["WebAppRender:BlazorMode"] = nameof(BlazorWebAppMode.BlazorWebAssembly);
            configuration["WebAppRender:PrerenderEnabled"] = "true";
        }).Start(TestContext.CancellationToken);

        var user = new UserDto
        {
            Id = Guid.NewGuid(),
            UserName = "prerender",
            Email = "prerender@bitplatform.dev",
            FullName = "Prerender Test",
            Gender = Gender.Other,
            BirthDate = new DateTimeOffset(1990, 5, 17, 0, 0, 0, TimeSpan.FromHours(3.5)),
            HasProfilePicture = true,
            Version = "42"
        };

        var store = new InMemoryStateStore();

        await using (var scope = server.WebApp.Services.CreateAsyncScope())
        {
            var serverPrerenderState = scope.ServiceProvider.GetRequiredService<IPrerenderStateService>();

            await serverPrerenderState.GetValue("user", () => Task.FromResult<UserDto?>(user));
            await serverPrerenderState.GetValue("nothing", () => Task.FromResult<UserDto?>(null));

            await using var bunit = new Bunit.BunitContext();
            await scope.ServiceProvider.GetRequiredService<ComponentStatePersistenceManager>().PersistStateAsync(store, bunit.Renderer);
        }

        var clientStateManager = new ComponentStatePersistenceManager(NullLogger<ComponentStatePersistenceManager>.Instance);
        await clientStateManager.RestoreStateAsync(store);

        var jsonSerializerOptions = server.WebApp.Services.GetRequiredService<JsonSerializerOptions>();
        var clientPrerenderState = new WebClientPrerenderStateService(jsonSerializerOptions, clientStateManager.State);

        var apiCalls = 0;
        Task<UserDto?> CallTheApi()
        {
            apiCalls++;
            return Task.FromResult<UserDto?>(null);
        }

        var restoredUser = await clientPrerenderState.GetValue("user", CallTheApi);
        var restoredNothing = await clientPrerenderState.GetValue("nothing", CallTheApi);

        Assert.AreEqual(0, apiCalls, "The client called the api for a value the pre-render had already persisted.");

        var typeInfo = jsonSerializerOptions.GetTypeInfo<UserDto>();
        var userFromTheApi = JsonSerializer.Deserialize(JsonSerializer.SerializeToUtf8Bytes(user, typeInfo), typeInfo)!;

        Assert.IsNotNull(restoredUser);
        Assert.AreEqual(userFromTheApi.Id, restoredUser.Id);
        Assert.AreEqual(userFromTheApi.Email, restoredUser.Email);
        Assert.AreEqual(userFromTheApi.FullName, restoredUser.FullName);
        Assert.AreEqual(userFromTheApi.Gender, restoredUser.Gender);
        Assert.AreEqual(userFromTheApi.Version, restoredUser.Version);
        Assert.IsTrue(userFromTheApi.BirthDate!.Value.EqualsExact(restoredUser.BirthDate!.Value),
            $"The pre-rendered value has to be the one the api would have sent: {restoredUser.BirthDate:o} instead of {userFromTheApi.BirthDate:o}.");

        Assert.IsNull(restoredNothing);
    }

    private sealed class InMemoryStateStore : IPersistentComponentStateStore
    {
        private IDictionary<string, byte[]> state = new Dictionary<string, byte[]>();

        public Task<IDictionary<string, byte[]>> GetPersistedStateAsync() => Task.FromResult(state);

        public Task PersistStateAsync(IReadOnlyDictionary<string, byte[]> state)
        {
            this.state = state.ToDictionary();
            return Task.CompletedTask;
        }
    }
}

using Boilerplate.Shared.Dtos.Identity;

namespace Boilerplate.Tests;

[TestClass]
public partial class SampleApiTest
{
    [TestMethod]
    public async Task SignInTest()
    {
        await using var server = new AppTestServer();

        await server.Build(services =>
        {
            // Services registered in this test project will be used instead of the application's services, allowing you to fake certain behaviors during testing.
        }).Start();

        var client = server.CreateClient();

        using var response = await client.PostAsJsonAsync("/api/Identity/SignIn", new SignInRequestDto
        {
            Email = "test@bitplatform.dev",
            Password = "123456"
        });

        var signInResponse = await response.EnsureSuccessStatusCode().Content.ReadFromJsonAsync<SignInResponseDto>();

        client.DefaultRequestHeaders.Authorization = new("Bearer", signInResponse!.AccessToken);

        var user = await client.GetFromJsonAsync<UserDto>("api/User/GetCurrentUser");

        Assert.AreEqual(Guid.Parse("8ff71671-a1d6-4f97-abb9-d87d7b47d6e7"), user!.Id);
    }
}

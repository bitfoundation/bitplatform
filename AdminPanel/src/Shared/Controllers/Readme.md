**Bit.SourceGenerator interface based HttpClient proxy generator**

**Introduction:**
When defining server-side APIs, there are no restrictions. You can leverage features such as Api versioning, OData, ASP.NET Core Minimal API,
Middleware, and more. On the client side, use HttpClient and optionally employ Bit.SourceGenerator.

Getting started:

1- Creating a Custom Interface:
Define an interface, for instance, `ICategoryController`, extending `IAppController`.

```csharp
public interface ICategoryController : IAppController
```

2- Simply inject these interfaces into your classes, and you're all set!

3- (Optional) implement that interface in server project.
```csharp
public class CategoryController : AppControllerBase, ICategoryController
```
Interface implementation on the server-side is not mandatory nor possible in some situations (For example ASP.NET Core Minimal API)

**Note:** If you implement the interface on the server-side, C# compiler ensure
that methods seen by the client in `ICategoryController` are present in `CategoryController` during build.

**Note:** Server-side methods may have conditions that make direct definition in the client-side interface challenging.
For example, an `Upload` method in `AttachmentController` has `IFormFile`,
and `Refresh` method of `IdentityController` returns `ActionResult` and these types are not present in client side.
In this case you can still use `Bit.SourceGenerator`, but in order to prevent C# compiler's build error, write the followings:
```csharp
[HttpPost]
Task<TokenResponseDto> Refresh(RefreshRequestDto body) => default!;
```
instead of
```csharp
[HttpPost]
Task<TokenResponseDto> Refresh(RefreshRequestDto body);
```

**Convention Over Configuration:**

While following the Convention over Configuration principle, methods like `Create` in `ICategoryController` send requests to `api/Category/Create`,
you are not bound by this convention. Use any `RoutePrefix` you prefer, as long as your API is accepting/returning json you're all set!

**Advanced sample**:
Explore `IMinimalApiController` for example of ASP.NET Core Minimal API that has the following characteristics:

1- No server-side web api controllers because of ASP.NET Core Minimal API.

2- Receiving output as `JsonElement`.

3- Example of Query String.

**Note:** We supprt [RFC6570](https://datatracker.ietf.org/doc/html/rfc6570) for request url templates thanks to [DoLess.UriTemplates](https://github.com/letsar/DoLess.UriTemplates?tab=readme-ov-file#examples)!


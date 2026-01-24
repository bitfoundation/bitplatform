# Stage 8: Blazor Pages, Components, Styling & Navigation

Welcome to Stage 8 of the getting started guide! In this stage, we'll explore how the Blazor UI architecture works in this project, including component structure, styling with SCSS, theme variables, and navigation.

---

## 1. Component Structure: The Three-File Pattern

In this project, Blazor pages and components follow a **three-file structure** that separates concerns for better maintainability:

### Example: ProductsPage

Let's examine the `ProductsPage` as a real example from the project:

#### File 1: `ProductsPage.razor` (Markup)
**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor`](/src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor)

This file contains the **UI markup** using Razor syntax and Bit.BlazorUI components:

```xml
@attribute [Route(PageUrls.Products)]
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@inherits AppPageBase

<AppPageData Title="@Localizer[nameof(AppStrings.Products)]" />

<section>
    <BitStack>
        <BitStack FitHeight>
            <BitButton IconName="@BitIconName.Add" 
                       OnClick="WrapHandled(CreateProduct)">
                @Localizer[nameof(AppStrings.AddProduct)]
            </BitButton>
            @if (isLoading)
            {
                <BitSlickBarsLoading />
            }
        </BitStack>
        <BitSpacer />
        <BitSearchBox OnSearch="HandleOnSearch"
                      Placeholder="@Localizer[nameof(AppStrings.SearchProductsPlaceholder)]" />
    </BitStack>
    <!-- BitDataGrid for displaying products -->
</section>
```

**Key Points:**
- Uses `@attribute` for routing and authorization
- Inherits from `AppPageBase` (more on this later)
- Uses `Bit.BlazorUI` components like `BitButton`, `BitStack`, `BitDataGrid`
- References code-behind variables like `isSmallScreen`, `isLoading`
- Uses `WrapHandled()` for event handlers (exception handling)

#### File 2: `ProductsPage.razor.cs` (Code-Behind)
**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor.cs)

This file contains the **component logic** - all C# code for the component:

```csharp
using Boilerplate.Shared.Dtos.Products;
using Boilerplate.Shared.Controllers.Products;

namespace Boilerplate.Client.Core.Components.Pages.Products;

public partial class ProductsPage
{
    private bool isLoading;
    private string? searchQuery;
    private ProductDto? deletingProduct;

    private BitDataGrid<ProductDto>? dataGrid;
    private BitDataGridItemsProvider<ProductDto> productsProvider = default!;

    [AutoInject] IProductController productController = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
        PrepareGridDataProvider();
    }

    private void PrepareGridDataProvider()
    {
        productsProvider = async req =>
        {
            isLoading = true;
            StateHasChanged();

            try
            {
                var query = new ODataQuery { /* OData options */ };

                var data = await productController.WithQuery(query.ToString())
                                                  .GetProducts(req.CancellationToken);

                return BitDataGridItemsProviderResult.From(data!.Items!, (int)data!.TotalCount);
            }
            finally
            {
                isLoading = false;
                StateHasChanged();
            }
        };
    }

    private async Task CreateProduct()
    {
        NavigationManager.NavigateTo(PageUrls.AddOrEditProduct);
    }

    private async Task DeleteProduct()
    {
        if (deletingProduct is null) return;

        await productController.Delete(deletingProduct.Id, 
            deletingProduct.Version, 
            CurrentCancellationToken);

        await RefreshData();
    }
}
```

**Key Points:**
- Declared as `partial class` to connect with the `.razor` file
- Uses `[AutoInject]` for dependency injection (simplified DI pattern)
- Overrides `OnInitAsync()` instead of `OnInitializedAsync()` (safer lifecycle method from base class)
- Has access to inherited services like `ExceptionHandler`, `NavigationManager`, `CurrentCancellationToken` from `AppPageBase`

#### File 3: `ProductsPage.razor.scss` (Scoped Styles)
**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor.scss`](/src/Client/Boilerplate.Client.Core/Components/Pages/Products/ProductsPage.razor.scss)

This file contains **component-specific styles** that are automatically scoped to this component:

```scss
@import '../../../Styles/abstracts/_media-queries.scss';
@import '../../../Styles/abstracts/_bit-css-variables.scss';

.grid-container {
    overflow: auto;
    height: calc(#{$bit-env-height-available} - 12.1rem);
}

::deep {
    .products-grid {
        width: 100%;
        background-color: $bit-color-background-secondary;

        .name-col {
            padding-inline-start: 16px;
        }

        thead {
            background-color: $bit-color-background-tertiary;
        }

        td {
            border-bottom: 1px solid $bit-color-border-tertiary;
        }
    }
}
```

**Key Points:**
- Imports shared SCSS files for media queries and theme variables
- Styles are automatically scoped to this component
- Uses `::deep` selector to style child components (explained in detail below)
- Uses theme color variables like `$bit-color-background-secondary` for dark/light mode support

### Navigation Integration

For a page to be accessible in the application, it needs to be added to the navigation system:

#### Adding to NavBar
**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Layout/NavBar.razor`](/src/Client/Boilerplate.Client.Core/Components/Layout/NavBar.razor)

```xml
<BitNavBar TItem="BitNavBarOption">
    <BitNavBarOption Text="@Localizer[nameof(AppStrings.Home)]" 
                     IconName="@BitIconName.Home" 
                     Url="@PageUrls.Home" />
    
    <BitNavBarOption Text="@Localizer[nameof(AppStrings.Terms)]" 
                     IconName="@BitIconName.EntityExtraction" 
                     Url="@PageUrls.Terms" />
</BitNavBar>
```

#### Adding to Navigation Panel
**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.items.cs`](/src/Client/Boilerplate.Client.Core/Components/Layout/MainLayout.razor.items.cs)

```csharp
navPanelItems.Add(new()
{
    Text = localizer[nameof(AppStrings.About)],
    IconName = BitIconName.Info,
    Url = PageUrls.About,
});
```

This ensures the page appears in both the navigation panel and nav bar (mobile).

### Cross-Platform Pages: Platform-Specific Components

Some pages need access to native platform features (e.g., device info, native APIs). Instead of placing them in `Boilerplate.Client.Core` and using dependency injection or pub-sub patterns, you can create **platform-specific pages** directly in platform projects.

#### Example: AboutPage

The `AboutPage` demonstrates this pattern by existing in multiple platform projects:
- [`/src/Client/Boilerplate.Client.Maui/Components/Pages/AboutPage.razor`](/src/Client/Boilerplate.Client.Maui/Components/Pages/AboutPage.razor)
- [`/src/Client/Boilerplate.Client.Windows/Components/Pages/AboutPage.razor`](/src/Client/Boilerplate.Client.Windows/Components/Pages/AboutPage.razor)
- [`/src/Client/Boilerplate.Client.Web/Components/Pages/AboutPage.razor`](/src/Client/Boilerplate.Client.Web/Components/Pages/AboutPage.razor)

**Benefits of this approach:**
- ✅ Direct access to native platform features without DI or interfaces
- ✅ Platform-specific implementations without conditional compilation
- ✅ Cleaner code - no abstraction layers needed

**Example from `AboutPage.razor` in Maui project:**

```xml
@attribute [Route(PageUrls.About)]
@inherits AppPageBase

<section>
    <BitStack AutoWidth>
        <BitText>App Name: <b>@appName</b></BitText>
        <BitText>App Version: <b>@appVersion</b></BitText>
        <BitText>OS: <b>@platform</b></BitText>
        <BitText>OEM: <b>@oem</b></BitText>
    </BitStack>
</section>
```

The code-behind can directly access MAUI APIs without needing an interface:
```csharp
// Direct access to MAUI features
var appName = AppInfo.Current.Name;
var platform = DeviceInfo.Current.Platform;
```

#### SCSS Support in Platform Projects

All platform projects (Maui, Windows, Web) are configured to support SCSS compilation. Check the `.csproj` files:

**In `Boilerplate.Client.Maui.csproj`, `Boilerplate.Client.Windows.csproj`, and `Boilerplate.Client.Web.csproj`:**

```xml
<Target Name="BeforeBuildTasks" AfterTargets="CoreCompile">
    <CallTarget Targets="BuildCssFiles" />
</Target>

<Target Name="BuildCssFiles">
    <Exec Command="../Boilerplate.Client.Core/node_modules/.bin/sass Components:Components" />
</Target>
```

This means you can create `AboutPage.razor.scss` in any of these projects, and it will be automatically compiled and scoped to that component.

---

## 2. SCSS Styling Architecture

### 2.1 Isolated Component Styles

Each component's `.razor.scss` file creates **isolated styles** that only apply to that specific component. This prevents style conflicts and makes components more maintainable.

**How it works:**
- During build, SCSS files are compiled to CSS
- Blazor applies unique identifiers to ensure styles are scoped
- Styles won't accidentally affect other components

### 2.2 Global Styles: `app.scss`

**Location**: [`/src/Client/Boilerplate.Client.Core/Styles/app.scss`](/src/Client/Boilerplate.Client.Core/Styles/app.scss)

This is the **main global stylesheet** that applies to the entire application:

```scss
@import '../Styles/abstracts/_media-queries.scss';
@import '../Styles/abstracts/_bit-css-variables.scss';

* {
    box-sizing: border-box;
    font-family: "Segoe UI";
}

html, body, #app-container {
    margin: 0;
    padding: 0;
    width: 100%;
    height: 100%;
    overflow: auto;
}

h1, h2, h3, h4, h5 {
    margin: 0;
}
```

**Use cases for global styles:**
- CSS resets and normalization
- Global typography settings
- Utility classes used across the app
- Base HTML element styling

### 2.3 Theme Color Variables: `_bit-css-variables.scss`

**Location**: [`/src/Client/Boilerplate.Client.Core/Styles/abstracts/_bit-css-variables.scss`](/src/Client/Boilerplate.Client.Core/Styles/abstracts/_bit-css-variables.scss)

This file provides **SCSS variables** that map to CSS custom properties from Bit.BlazorUI's theme system. These variables automatically support both dark and light modes.

**Example variables:**

```scss
/*-------- Colors --------*/
// Primary
$bit-color-primary: var(--bit-clr-pri);
$bit-color-primary-hover: var(--bit-clr-pri-hover);
$bit-color-primary-active: var(--bit-clr-pri-active);

// Background
$bit-color-background-primary: var(--bit-clr-bg-pri);
$bit-color-background-secondary: var(--bit-clr-bg-sec);
$bit-color-background-tertiary: var(--bit-clr-bg-ter);

// Foreground (text colors)
$bit-color-foreground-primary: var(--bit-clr-fg-pri);
$bit-color-foreground-secondary: var(--bit-clr-fg-sec);
$bit-color-foreground-tertiary: var(--bit-clr-fg-ter);

// Borders
$bit-color-border-primary: var(--bit-clr-brd-pri);
$bit-color-border-secondary: var(--bit-clr-brd-sec);
$bit-color-border-tertiary: var(--bit-clr-brd-ter);

// Semantic Colors
$bit-color-success: var(--bit-clr-suc);
$bit-color-warning: var(--bit-clr-wrn);
$bit-color-error: var(--bit-clr-err);
$bit-color-info: var(--bit-clr-inf);

// Environment Variables
$bit-env-height-available: var(--bit-env-height-avl);
$bit-env-width-available: var(--bit-env-width-avl);
```

**⚠️ CRITICAL: Always Use Theme Variables**

You **MUST** use these theme variables in your C#, Razor, and SCSS files instead of hardcoded colors. This ensures:
- ✅ Automatic dark/light mode support
- ✅ Consistent design throughout the app
- ✅ Easy theme customization

**Example from `CategoriesPage.razor.scss`:**

```scss
::deep {
    .categories-grid {
        background-color: $bit-color-background-secondary;  // ✅ Correct - adapts to theme

        thead {
            background-color: $bit-color-background-tertiary;  // ✅ Correct
        }

        td {
            border-bottom: 1px solid $bit-color-border-tertiary;  // ✅ Correct
        }
    }
}
```

**❌ What NOT to do:**

```scss
.my-component {
    background-color: #ffffff;  // ❌ Wrong - hardcoded, breaks dark mode
    color: black;               // ❌ Wrong - won't adapt to theme
}
```

### 2.4 The `::deep` Selector

The `::deep` selector (also known as deep selector or `>>>`) allows you to **style child components** from a parent component's scoped stylesheet.

**Why is it needed?**

By default, component styles are scoped and don't affect child components. When you use Bit.BlazorUI components (like `BitDataGrid`, `BitButton`, etc.), you need `::deep` to style their internal elements.

**Example from `ProductsPage.razor.scss`:**

```scss
// Without ::deep, this would only style elements directly in ProductsPage
::deep {
    .products-grid {
        // These styles now reach into BitDataGrid's internal structure
        width: 100%;
        height: 100%;
        background-color: $bit-color-background-secondary;

        .name-col {
            padding-inline-start: 16px;
        }

        thead {
            height: 44px;
            background-color: $bit-color-background-tertiary;
        }

        td {
            height: 44px;
            white-space: nowrap;
            border-bottom: 1px solid $bit-color-border-tertiary;
        }
    }

    .bitdatagrid-paginator {
        padding: 8px;
        font-size: 14px;
        background-color: $bit-color-background-secondary;

        button {
            cursor: pointer;
            font-size: 12px;
        }
    }
}
```

**Use case:**
- Styling Bit.BlazorUI and other 3rd-party UI library components (which are child components)

---

## 3. Bit.BlazorUI Components & Documentation

### Using Bit.BlazorUI Components

This project uses **`Bit.BlazorUI`** as the primary UI component library. You MUST use these components instead of generic HTML elements to ensure UI consistency and leverage built-in features.

**Examples from the project:**

```xml
<!-- ✅ Use Bit.BlazorUI components -->
<BitButton OnClick="WrapHandled(CreateProduct)">
    @Localizer[nameof(AppStrings.AddProduct)]
</BitButton>

<BitSearchBox OnSearch="HandleOnSearch" />

<BitDataGrid TGridItem="ProductDto" ItemsProvider="productsProvider" />

<!-- ❌ Avoid generic HTML when Bit component exists -->
<button>Add Product</button>
<input type="text" />
```

### Component-Specific Styling Properties

**Important Note:** Each Bit.BlazorUI component has its own **CSS variables** and **styling parameters** (`Styles` and `Classes` properties) that allow you to style nested child elements.

**Example using `Styles` parameter from `SignOutConfirmDialog.razor`:**
```xml
<BitDialog @bind-IsOpen="isSignOutDialogOpen"
           Styles="@(new() { OkButton = "width:100%", CancelButton = "width:100%" })" />
```

**Example using `Classes` parameter from `AppMenu.razor`:**
```xml
<BitCallout @bind-IsOpen="isMenuOpen"
            Classes="@(new() { Callout = "app-menu-callout" })">
    <!-- Content -->
</BitCallout>
```

**Why this approach?**
- ✅ More explicit and type-safe
- ✅ IntelliSense support for nested element names
- ✅ No need for `::deep` selector using `Styles` parameter
- ✅ Style values can be bound to C# variables

### Comprehensive Documentation

**`Bit.BlazorUI` has extensive documentation at:**
📚 **https://blazorui.bitplatform.dev**

The documentation includes:
- Complete API reference for every component
- Interactive examples and demos
- Property descriptions
- Usage patterns
- Styling guides

### Automatic DeepWiki Integration

**You don't need to manually search the documentation!** 

When you ask questions in **GitHub Copilot Chat** or give commands related to UI components, the system **automatically queries the DeepWiki knowledge base** for `bitfoundation/bitplatform` to find relevant information.

**Example interactions:**

- **You ask:** "How do I add a filter to BitDataGrid?"
  - **Copilot:** Automatically searches DeepWiki and provides the answer with code examples

- **You ask:** "How to customize BitButton colors?"
  - **Copilot:** Retrieves information about `BitColor` enum and styling options

- **You command:** "Add a BitDatePicker with validation"
  - **Copilot:** Finds the correct implementation pattern and creates the code

**You can ask naturally:**
- "How do I make a BitModal full screen?"
- "Show me BitDataGrid pagination examples"
- "How to add icons to BitNavMenu items?"
- "What properties does BitChart have?"
- "How can I implement a Grid System and layout using BitGrid and BitStack components, especially if I'm familiar with the Bootstrap grid system?"

The DeepWiki system handles the documentation lookup automatically!

---

## 4. Navigation with PageUrls

### PageUrls Class

**Location**: [`/src/Shared/PageUrls.cs`](/src/Shared/PageUrls.cs)

The project uses a **centralized `PageUrls` class** to define all route paths as constants. This prevents typos and makes route changes easier to manage.

```csharp
namespace Boilerplate.Shared;

public static partial class PageUrls
{
    public const string Home = "/";
    public const string NotFound = "/not-found";
    public const string Terms = "/terms";
    public const string Settings = "/settings";
    public const string About = "/about";
}
```

**Additional partial files:**
- `PageUrls.Identity.cs` - Identity-related routes (sign in, sign up, etc.)
- `PageUrls.SettingsSections.cs` - Settings section routes

### Using PageUrls

**In Razor files (routing):**

```xml
@attribute [Route(PageUrls.Products)]
@attribute [Route("{culture?}" + PageUrls.Products)]
```

**In C# code (navigation):**

```csharp
private async Task CreateProduct()
{
    NavigationManager.NavigateTo(PageUrls.AddOrEditProduct);
}

// With parameters
NavigationManager.NavigateTo($"{PageUrls.AddOrEditProduct}/{product.Id}");
```

**In Razor markup (links):**

```xml
<BitButton Href="@($"{PageUrls.AddOrEditProduct}/{product.Id}")" />

<BitNavLink Href="@PageUrls.Dashboard">
    Dashboard
</BitNavLink>
```

**Benefits:**
- ✅ No magic strings - compile-time safety
- ✅ IntelliSense support
- ✅ Easy to refactor routes
- ✅ Centralized routes

---

## 5. Component Base Classes

The project provides enhanced base classes that add powerful features to your components and pages.

### 5.1 AppComponentBase

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`](/src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs)

This is the **base class for all components**. Most `.razor.cs` files inherit from this class.

**Key features provided:**

```csharp
public partial class AppComponentBase
{
    [AutoInject] protected IJSRuntime JSRuntime = default!;
    [AutoInject] protected NavigationManager NavigationManager = default!;
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
    [AutoInject] protected IExceptionHandler ExceptionHandler = default!;

    protected CancellationToken CurrentCancellationToken { get; }

    protected bool InPrerenderSession { get; }

    protected virtual Task OnInitAsync() { }
    protected virtual Task OnParamsSetAsync() { }
    protected virtual Task OnAfterFirstRenderAsync() { }
}
```

**Key benefits:**

1. **Automatic Exception Handling**: Enhanced lifecycle methods catch and handle exceptions:
   - `OnInitAsync()` instead of `OnInitializedAsync()`
   - `OnParamsSetAsync()` instead of `OnParametersSetAsync()`
   - `OnAfterFirstRenderAsync()` - only fires once, not on every render

2. **Pre-Injected Services**: All components automatically have access to commonly used services without needing to inject them manually:
   ```csharp
   // ✅ Available in any component inheriting from AppComponentBase
   protected override async Task OnInitAsync()
   {
       var userName = await StorageService.GetItem("username");
       var message = Localizer[nameof(AppStrings.Welcome)];
       NavigationManager.NavigateTo(PageUrls.Dashboard);
   }
   ```

3. **Automatic Cancellation Token**: Use `CurrentCancellationToken` for all async operations. It automatically cancels when the user navigates away.

### 5.2 AppPageBase

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/Pages/AppPageBase.cs`](/src/Client/Boilerplate.Client.Core/Components/Pages/AppPageBase.cs)

This is the **base class for pages** (extends `AppComponentBase` with page-specific features).

```csharp
public abstract partial class AppPageBase : AppComponentBase
{
    [Parameter] public string? culture { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);

        if (firstRender)
        {
            if (string.IsNullOrEmpty(culture) is false)
            {
                // Validates culture parameter
            }
        }
    }
}
```

**Additional features:**
- Culture/localization support with automatic validation
- Page-level metadata configuration via `AppPageData` component
- Everything from `AppComponentBase`

**Example usage in a page:**

```xml
@attribute [Route(PageUrls.Products)]
@inherits AppPageBase

<AppPageData Title="@Localizer[nameof(AppStrings.Products)]"
             PageTitle="@Localizer[nameof(AppStrings.ProductsPageTitle)]" />

<!-- Page content -->
```

```csharp
public partial class ProductsPage
{
    [AutoInject] IProductController productController = default!;

    protected override async Task OnInitAsync()
    {
        await base.OnInitAsync();
        PrepareGridDataProvider();
    }

    private async Task DeleteProduct()
    {
        await productController.Delete(deletingProduct.Id, 
            deletingProduct.Version, 
            CurrentCancellationToken);
    }
}
```

---

### AI Wiki: Answered Questions
* [How can I implement a `Grid System` and layout using `BitGrid` and `BitStack` components, especially if I'm familiar with the Bootstrap grid system?](https://deepwiki.com/search/how-can-i-implement-a-grid-sys_25d76f3c-d0a6-4c75-8b9c-7f86ae317fb6)
* [What is the optimal way to load page data using `StateHasChanged` in conjunction with a `Skeleton UI` or `Shimmer` ?](https://deepwiki.com/search/what-is-the-optimal-way-to-loa_e9b729ca-d36b-4c61-a855-7d21ceb783ae)
* [How is SCSS compiled to CSS in real-time within Visual Studio and Visual Studio Code?](https://deepwiki.com/search/how-is-scss-compiled-to-css-in_d4ea9c05-f002-4300-99df-076c167993d5)

Ask your own question [here](https://wiki.bitplatform.dev)

---
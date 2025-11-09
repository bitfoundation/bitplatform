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
@attribute [Route("{culture?}" + PageUrls.Products)]
@attribute [Authorize(Policy = AuthPolicies.PRIVILEGED_ACCESS)]
@attribute [Authorize(Policy = AppFeatures.AdminPanel.ManageProductCatalog)]
@inherits AppPageBase

<AppPageData Title="@Localizer[nameof(AppStrings.Products)]"
             PageTitle="@Localizer[nameof(AppStrings.ProductsPageTitle)]" />

<section>
    <BitStack>
        <BitStack FitHeight 
                  Gap="0.5rem"
                  Horizontal="isSmallScreen is false">
            <BitStack Horizontal FitHeight>
                <BitButton ReversedIcon
                           IconName="@BitIconName.Add" 
                           OnClick="WrapHandled(CreateProduct)">
                    @Localizer[nameof(AppStrings.AddProduct)]
                </BitButton>
                @if (isLoading)
                {
                    <BitSlickBarsLoading CustomSize="32" />
                }
            </BitStack>
            <BitSpacer />
            <BitSearchBox Underlined
                          ShowSearchButton
                          OnSearch="HandleOnSearch"
                          Color="BitColor.Secondary"
                          Style="@($"width:{(isSmallScreen ? 100 : 50)}%")"
                          Placeholder="@Localizer[nameof(AppStrings.SearchProductsPlaceholder)]" />
        </BitStack>
        <!-- BitDataGrid for displaying products with pagination -->
    </BitStack>
</section>
```

**Key Points:**
- Uses `@attribute` for routing and authorization
- Inherits from `AppPageBase` (more on this later)
- Uses `Bit.BlazorUI` components like `BitButton`, `BitStack`, `BitDataGrid`
- References code-behind variables like `isSmallScreen`, `isLoading`
- Uses `WrapHandled()` for event handlers (automatic exception handling)

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
    private bool isSmallScreen;
    private string? searchQuery;
    private bool isDeleteDialogOpen;
    private ProductDto? deletingProduct;
    private string productNameFilter = string.Empty;
    private string categoryNameFilter = string.Empty;

    private BitDataGrid<ProductDto>? dataGrid;
    private BitDataGridItemsProvider<ProductDto> productsProvider = default!;
    private BitDataGridPaginationState pagination = new() { ItemsPerPage = 10 };

    [AutoInject] IProductController productController = default!;

    private string ProductNameFilter
    {
        get => productNameFilter;
        set
        {
            productNameFilter = value;
            _ = RefreshData();
        }
    }

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
                var query = new ODataQuery
                {
                    Top = req.Count ?? 10,
                    Skip = req.StartIndex,
                    OrderBy = string.Join(", ", req.GetSortByProperties()
                        .Select(p => $"{p.PropertyName} {(p.Direction == BitDataGridSortDirection.Ascending ? "asc" : "desc")}"))
                };

                if (string.IsNullOrEmpty(ProductNameFilter) is false)
                {
                    query.Filter = $"contains(tolower({nameof(ProductDto.Name)}),'{ProductNameFilter.ToLower()}')";
                }

                var queriedRequest = productController.WithQuery(query.ToString());
                var data = await (string.IsNullOrWhiteSpace(searchQuery)
                            ? queriedRequest.GetProducts(req.CancellationToken)
                            : queriedRequest.SearchProducts(searchQuery, req.CancellationToken));

                return BitDataGridItemsProviderResult.From(data!.Items!, (int)data!.TotalCount);
            }
            catch (Exception exp)
            {
                ExceptionHandler.Handle(exp);
                return BitDataGridItemsProviderResult.From(new List<ProductDto> { }, 0);
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

        try
        {
            await productController.Delete(deletingProduct.Id, 
                deletingProduct.ConcurrencyStamp.ToStampString(), 
                CurrentCancellationToken);

            await RefreshData();
        }
        finally
        {
            deletingProduct = null;
        }
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

section {
    width: 100%;
}

.grid-container {
    overflow: auto;
    height: calc(#{$bit-env-height-available} - 12.1rem);

    @include lt-md {
        height: calc(#{$bit-env-height-available} - 17rem);
    }

    @include lt-sm {
        height: calc(#{$bit-env-height-available} - 16rem);
    }
}

::deep {
    .products-grid {
        width: 100%;
        height: 100%;
        border-spacing: 0;
        background-color: $bit-color-background-secondary;

        .name-col {
            padding-inline-start: 16px;
        }

        .category-col {
            width: 135px;
        }

        .price-col {
            width: 135px;
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
}
```

**Key Points:**
- Imports shared SCSS files for media queries and theme variables
- Styles are automatically scoped to this component
- Uses `::deep` selector to style child components (explained in detail below)
- Uses theme color variables like `$bit-color-background-secondary` for dark/light mode support

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

:root[bit-theme="dark"] {
    //--bit-clr-bg-pri: #010409;
    // In case you need to change the background color, make sure to also update 
    // ThemeColors.cs's PrimaryDarkBgColor accordingly.
}

* {
    box-sizing: border-box;
    font-family: "Segoe UI";
    -webkit-text-size-adjust: none;
    -webkit-font-smoothing: antialiased;
    -webkit-tap-highlight-color: transparent;
}

html, body, #app-container {
    margin: 0;
    padding: 0;
    width: 100%;
    height: 100%;
    overflow: auto;
    scroll-behavior: smooth;

    @include lt-md {
        overflow: hidden;
    }
}

h1, h2, h3, h4, h5 {
    margin: 0;
}

.max-width {
    width: min(25rem, 100%);
}

.modal {
    top: $bit-env-inset-top;
    left: $bit-env-inset-left;
    right: $bit-env-inset-right;
    bottom: $bit-env-inset-bottom;
    width: $bit-env-width-available;
    height: $bit-env-height-available;
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

**Use cases:**
- Styling Bit.BlazorUI components (which are child components)
- Customizing component library defaults
- Applying styles that need to penetrate component boundaries

### Alternative: Component-Specific Styling Properties

**Important Note:** Each Bit.BlazorUI component has its own **CSS variables** and **styling parameters** (`Styles` and `Classes` properties) that allow you to style nested child elements **without needing `::deep`** in most cases.

**Example using `Styles` parameter:**
```xml
<BitDropdown Styles="@(new() { Container="height:32px;background-color:var(--bit-clr-bg-sec)" })" />
```

This approach is preferred when available, as it's more explicit and type-safe.

---

## 3. Bit.BlazorUI Components & Documentation

### Using Bit.BlazorUI Components

This project uses **`Bit.BlazorUI`** as the primary UI component library. You **MUST** use these components instead of generic HTML elements to ensure UI consistency and leverage built-in features.

**Examples from the project:**

```xml
<!-- ✅ Use Bit.BlazorUI components -->
<BitButton IconName="@BitIconName.Add" 
           OnClick="WrapHandled(CreateProduct)">
    @Localizer[nameof(AppStrings.AddProduct)]
</BitButton>

<BitSearchBox Underlined
              ShowSearchButton
              OnSearch="HandleOnSearch"
              Color="BitColor.Secondary"
              Placeholder="@Localizer[nameof(AppStrings.SearchProductsPlaceholder)]" />

<BitDataGrid TGridItem="ProductDto"
             Pagination="pagination"
             ItemsProvider="productsProvider">
    <!-- ... -->
</BitDataGrid>

<BitStack Horizontal Gap="0.5rem">
    <BitButton />
    <BitButton />
</BitStack>

<!-- ❌ Avoid generic HTML when Bit component exists -->
<button onclick="...">Add Product</button>  <!-- Don't do this -->
<input type="text" />  <!-- Use BitTextField instead -->
<div style="display: flex;">  <!-- Use BitStack instead -->
```

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
    
    public const string Categories = "/categories";
    public const string Dashboard = "/dashboard";
    public const string Products = "/products";
    public const string AddOrEditProduct = "/add-edit-product";
    public const string Todo = "/todo";
    public const string SystemPrompts = "/system-prompts";
    public const string Authorize = "/authorize";
    public const string Roles = "/user-groups";
    public const string Users = "/users";
    public const string OfflineDatabaseDemo = "/offline-database-demo";
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
- ✅ Centralized route management

---

## 5. Component Base Classes

The project provides enhanced base classes that add powerful features to your components and pages.

### 5.1 AppComponentBase

**Location**: [`/src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs`](/src/Client/Boilerplate.Client.Core/Components/AppComponentBase.cs)

This is the **base class for all components**. Most `.razor.cs` files inherit from this class.

**Key features provided:**

```csharp
public partial class AppComponentBase : ComponentBase, IAsyncDisposable
{
    // Automatic dependency injection via [AutoInject]
    [AutoInject] protected IJSRuntime JSRuntime = default!;
    [AutoInject] protected IStorageService StorageService = default!;
    [AutoInject] protected JsonSerializerOptions JsonSerializerOptions = default!;
    [AutoInject] protected IPrerenderStateService PrerenderStateService = default!;
    [AutoInject] protected PubSubService PubSubService = default!;
    [AutoInject] protected IConfiguration Configuration = default!;
    [AutoInject] protected NavigationManager NavigationManager = default!;
    [AutoInject] protected IAuthTokenProvider AuthTokenProvider = default!;
    [AutoInject] protected IStringLocalizer<AppStrings> Localizer = default!;
    [AutoInject] protected IExceptionHandler ExceptionHandler = default!;
    [AutoInject] protected AuthManager AuthManager = default!;
    [AutoInject] protected SnackBarService SnackBarService = default!;
    [AutoInject] protected ITelemetryContext TelemetryContext = default!;
    [AutoInject] protected IAuthorizationService AuthorizationService = default!;
    [AutoInject] protected AbsoluteServerAddressProvider AbsoluteServerAddress = default!;

    // Automatic cancellation token management
    protected CancellationToken CurrentCancellationToken { get; }

    // Check if in pre-render mode
    protected bool InPrerenderSession { get; }

    // Enhanced lifecycle methods with automatic exception handling
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
                // Validates culture parameter and redirects to 404 if invalid
                if (CultureInfoManager.InvariantGlobalization || 
                    CultureInfoManager.SupportedCultures.Any(sc => 
                        string.Equals(sc.Culture.Name, culture, StringComparison.InvariantCultureIgnoreCase)) is false)
                {
                    NavigationManager.NavigateTo($"{PageUrls.NotFound}?url={Uri.EscapeDataString(NavigationManager.GetRelativePath())}", replace: true);
                }
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
    // All services from AppComponentBase are available
    [AutoInject] IProductController productController = default!;

    protected override async Task OnInitAsync()
    {
        // Enhanced lifecycle with automatic exception handling
        await base.OnInitAsync();
        PrepareGridDataProvider();
    }

    private async Task DeleteProduct()
    {
        // CurrentCancellationToken automatically cancels if user navigates away
        await productController.Delete(deletingProduct.Id, 
            deletingProduct.ConcurrencyStamp.ToStampString(), 
            CurrentCancellationToken);
    }
}
```

---

## Summary

In this stage, you learned about:

✅ **Three-File Component Structure**: `.razor` (markup), `.razor.cs` (logic), `.razor.scss` (styles)

✅ **SCSS Styling Architecture**:
   - Isolated component styles (`.razor.scss`)
   - Global styles (`app.scss`)
   - Theme color variables (`_bit-css-variables.scss`)
   - Using `::deep` selector to style child components

✅ **Always Use Theme Variables**: `$bit-color-background-primary`, `$bit-color-foreground-primary`, etc. for dark/light mode support

✅ **Bit.BlazorUI Components**: Use these instead of generic HTML elements
   - Documentation: https://blazorui.bitplatform.dev
   - Automatic DeepWiki integration - just ask questions naturally!

✅ **Navigation with PageUrls**: Centralized route constants for compile-time safety

✅ **Component Base Classes**:
   - `AppComponentBase`: Enhanced lifecycle methods with automatic exception handling and pre-injected services
   - `AppPageBase`: Page-specific features + everything from `AppComponentBase`
   - Use `OnInitAsync()`, `OnParamsSetAsync()`, `OnAfterFirstRenderAsync()` instead of standard Blazor lifecycle methods

---

# Stage 18: Other Available Prompt Templates

This project includes several specialized prompt templates designed to help you with specific development tasks. These prompts are located in the `.github/prompts/` directory and can be activated by referencing them in your GitHub Copilot Chat.

## Available Prompt Templates

### 1. Scaffold Prompt (`scaffold.prompt.md`)

**Purpose**: Automates the creation of a complete CRUD (Create, Read, Update, Delete) implementation for a new entity.

**What it does**:
- Creates an Entity Type Configuration for Entity Framework Core
- Generates a DTO (Data Transfer Object) with proper validation attributes
- Creates a Mapperly-based Mapper for high-performance object mapping
- Implements an API Controller with full CRUD operations
- Generates an IAppController interface for strongly-typed HTTP client calls
- Adds resource strings to `AppStrings.resx` for localization
- Creates a Blazor Data Grid page for viewing and managing records
- Creates an Add/Edit page for creating and modifying records
- Updates `PageUrls.cs`, `NavBar`, and `NavPanel` for navigation
- Registers the DTO in `AppJsonContext`
- Creates an EF Core migration

**When to use it**:
- When you need to add a new entity to your application (e.g., Product, Order, Invoice, etc.)
- When you want to quickly scaffold a complete feature with all layers (database, API, UI)
- When you want to ensure consistency across your codebase by following established patterns

**How to use it**:
Simply reference the prompt in GitHub Copilot Chat and describe your entity:
```
Run .github/prompts/scaffold.prompt.md for a "Product" entity with properties: Name (string), Description (string), Price (decimal), CategoryId (Guid), IsActive (bool)
```

**Key benefits**:
- Saves hours of repetitive coding
- Ensures all architectural layers are properly connected
- Follows best practices and project conventions automatically
- Reduces human error by generating consistent code

---

### 2. Resx Prompt (`resx.prompt.md`)

**Purpose**: Helps you move hardcoded strings to resource files for proper localization support.

**What it does**:
- Identifies hardcoded user-facing strings in your code
- Adds new resource entries to `src/Shared/Resources/AppStrings.resx`
- Generates strongly-typed resource classes by running `dotnet build -t:PrepareResources`
- Updates your code to use `IStringLocalizer<AppStrings>` with the `nameof(AppStrings.ResourceKey)` pattern

**When to use it**:
- When you have hardcoded strings in your Razor components or C# code
- When preparing your application for multi-language support
- When you want to ensure all user-facing text can be translated
- During code review when you notice strings that should be localized

**How to use it**:
Select the code with hardcoded strings and reference the prompt:
```
Run .github/prompts/resx.prompt.md on the selected code
```

**What it WON'T move**:
- CSS class names or IDs
- Configuration keys
- API endpoints or URLs
- Technical constants (file extensions, mime types, etc.)
- Log messages (developer-facing content)

**Key benefits**:
- Makes your application ready for internationalization
- Centralizes all user-facing text in one place
- Enables easy translation to multiple languages
- Follows best practices for .NET localization

---

### 3. Bitify Prompt (`bitify.prompt.md`)

**Purpose**: Modernizes your Blazor pages by replacing standard HTML elements and custom CSS with Bit.BlazorUI components and theme-aware styling.

**What it does**:
- Analyzes your existing `.razor`, `.razor.cs`, and `.razor.scss` files
- Creates a comprehensive modernization inventory of HTML elements that can be replaced
- Uses DeepWiki to research appropriate Bit.BlazorUI components
- Replaces generic HTML elements with feature-rich Bit.BlazorUI components:
  - `<input>` → `BitTextField`, `BitCheckbox`, etc.
  - `<button>` → `BitButton`, `BitActionButton`
  - `<select>` → `BitDropdown`
  - `<table>` → `BitDataGrid`
  - Layout divs → `BitStack`, `BitGrid`
- Converts hardcoded colors to `$bit-color-*` theme variables
- Updates custom CSS to use `::deep` selectors for component styling
- Modernizes the code-behind to work with component properties and events

**When to use it**:
- When you have pages using plain HTML that should use Bit.BlazorUI components
- When upgrading legacy code to modern component-based architecture
- When you want to ensure theme compatibility (light/dark mode support)
- When reducing custom CSS in favor of built-in component features
- When you need consistent UI/UX across your application

**How to use it**:
Open the page you want to modernize and reference the prompt:
```
Run .github/prompts/bitify.prompt.md on the current page
```

**Replacement Examples**:
- Form inputs → `BitTextField`, `BitDropdown`, `BitCheckbox`, `BitChoiceGroup`
- Buttons → `BitButton`, `BitActionButton`, `BitIconButton`
- Layout structures → `BitStack`, `BitGrid`
- Navigation → `BitNavBar`, `BitBreadcrumb`, `BitNav`
- Data tables → `BitDataGrid`, `BitBasicList`
- Cards/Containers → `BitCard`, `BitPanel`

**Key benefits**:
- Provides a consistent, professional UI out of the box
- Ensures dark/light theme compatibility automatically
- Reduces custom CSS maintenance burden
- Leverages built-in accessibility features
- Improves responsive design with minimal effort
- Gets automatic updates when Bit.BlazorUI improves

**Important**: This prompt requires the `DeepWiki_ask_question` tool to research Bit.BlazorUI components effectively.

---

## How to Use These Prompts

All of these specialized prompts can be activated in GitHub Copilot Chat by referencing their file path:

```
Run .github/prompts/<prompt-name>.prompt.md [with additional context]
```

### Tips for Best Results:

1. **Be Specific**: Provide clear details about what you want to accomplish
2. **Provide Context**: If working with existing code, make sure the relevant files are open or selected
3. **Review the Output**: Always review the generated code to ensure it meets your specific requirements
4. **Iterate if Needed**: You can refine the results by asking follow-up questions
5. **Build and Test**: Always build the project and test the functionality after using these prompts

### Example Usage Scenarios:

**Scenario 1 - New Feature**:
```
Run .github/prompts/scaffold.prompt.md for an "Invoice" entity with properties: InvoiceNumber (string), CustomerName (string), TotalAmount (decimal), InvoiceDate (DateTime), IsPaid (bool)
```

**Scenario 2 - Localization**:
```
Select your component with hardcoded strings, then:
Run .github/prompts/resx.prompt.md on the selected code
```

**Scenario 3 - UI Modernization**:
```
Open the page you want to modernize, then:
Run .github/prompts/bitify.prompt.md on HomePage.razor and its related files
```

---

## Creating Your Own Prompts

You can also create custom prompt templates for your team's specific needs:

1. Create a new `.prompt.md` file in `.github/prompts/`
2. Follow the structure of existing prompts
3. Document the purpose, usage, and expected outcomes
4. Share with your team and reference it in GitHub Copilot Chat

---

**Next Steps**: Try using one of these prompts on your current work! They're designed to save you time and ensure consistency across your codebase.

**Do you have any questions about these specialized prompts, or would you like to see examples of using any of them? Or shall we proceed to Stage 19 (Project miscellaneous files)?**

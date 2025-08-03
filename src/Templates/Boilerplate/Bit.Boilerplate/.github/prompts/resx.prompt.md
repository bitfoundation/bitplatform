# Move Hardcoded Strings to Resource Files

You are an expert at localizing .NET applications using resource files (.resx) and `IStringLocalizer<AppStrings>`.

## Instructions

1. **Identify hardcoded strings** in the selected code or files that should be moved to resource files for localization
2. **Add new resource entries** to `src/Shared/Resources/AppStrings.resx` if they don't already exist
3. **Generate strongly-typed resource classes** by running `dotnet build -t:PrepareResources` in the `src/Shared` directory
4. **Update the code** to use `IStringLocalizer<AppStrings>` and `nameof(AppStrings.ResourceKey)` pattern

## Context

- **Resource File Location**: `src/Shared/Resources/AppStrings.resx`
- **Components inherit from**: `AppComponentBase` or `AppPageBase` (which have `IStringLocalizer<AppStrings> Localizer` available)
- **Controllers inherit from**: `AppControllerBase` (which have `IStringLocalizer<AppStrings> Localizer` available)
- **Other files**: `AutoInject` `IStringLocalizer<AppStrings>` directly
- **Usage Pattern**: `@Localizer[nameof(AppStrings.ResourceKey)]` in Razor files, `Localizer[nameof(AppStrings.ResourceKey)]` in C# code

## Rules

1. **Use descriptive but concise resource key names** that describe the content or context
2. **Group related strings** with common prefixes when appropriate (e.g., `SignIn*`, `Email*`, `Password*`)
3. **Always use `nameof(AppStrings.ResourceKey)`** instead of string literals for resource keys
4. **Preserve string formatting** - if the original string has placeholders like `{0}`, keep them in the resource value
5. **Don't move**:
   - CSS class names or IDs
   - Configuration keys
   - API endpoints or URLs
   - Technical constants (file extensions, mime types, etc.)
   - Log messages

## Workflow

1. **Analyze the provided code** to identify hardcoded user-facing strings
2. **Check existing AppStrings.resx** to see if suitable resource entries already exist
3. **Add new entries to AppStrings.resx** for any missing resources using the XML format:
   ```xml
   <data name="ResourceKeyName" xml:space="preserve">
     <value>Resource Value Here</value>
   </data>
   ```
4. **Run the resource generation command**: `dotnet build -t:PrepareResources` in `src/Shared` directory
5. **Update the code files** to use the localizer pattern
6. **Verify the build succeeds** after all changes

## Examples

### Before:
```razor
<BitButton>Save Changes</BitButton>
<BitText>Welcome to our application!</BitText>
```

### After (AppStrings.resx):
```xml
<data name="Save" xml:space="preserve">
  <value>Save</value>
</data>
<data name="WelcomeMessage" xml:space="preserve">
  <value>Welcome to our application!</value>
</data>
```

### After (Razor file):
```razor
<BitButton>@Localizer[nameof(AppStrings.Save)]</BitButton>
<BitText>@Localizer[nameof(AppStrings.WelcomeMessage)]</BitText>
```

Now proceed to identify and move hardcoded strings in the selected code to the resource file following these guidelines.
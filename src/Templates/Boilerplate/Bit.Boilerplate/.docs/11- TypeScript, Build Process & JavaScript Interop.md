# Stage 11: TypeScript, Build Process & JavaScript Interop

Welcome to Stage 11! In this stage, you'll learn how TypeScript is integrated into the Blazor application, how the build process works, and how C# code interacts with JavaScript through JavaScript Interop.

---

## 1. TypeScript Configuration

### tsconfig.json

The TypeScript configuration is located at:
**[/src/Client/Boilerplate.Client.Core/tsconfig.json](/src/Client/Boilerplate.Client.Core/tsconfig.json)**

```jsonc
{
    "compileOnSave": true,
    "compilerOptions": {
        "strict": true,
        "target": "ES2019",
        "module": "es2015",
        "noImplicitAny": true,
        "lib": [ "DOM", "ESNext" ],
        "moduleResolution": "node"
    }
}
```

**Key Configuration Points:**
- **strict**: Enables all strict type-checking options
- **target**: Compiles to ES2019 for broad browser compatibility
- **module**: Uses ES2015 module system
- **noImplicitAny**: Requires explicit type annotations
- **lib**: Includes DOM and ESNext library types
- **moduleResolution**: Uses Node.js-style module resolution

---

## 2. Node.js Dependencies

### package.json

The Node.js dependencies are defined in:
**[/src/Client/Boilerplate.Client.Core/package.json](/src/Client/Boilerplate.Client.Core/package.json)**

```json
{
    "devDependencies": {
        "esbuild": "0.25.12",
        "sass": "1.93.3",
        "typescript": "5.9.3"
    }
}
```

**Dependencies Explained:**
- **TypeScript**: Compiles TypeScript to JavaScript
- **esbuild**: Super-fast JavaScript bundler that combines all JS modules into a single `app.js` file
- **sass**: Compiles SCSS to CSS

---

## 3. MSBuild Integration & Build Process

### How the Build Works

The build process is integrated into MSBuild through custom targets in:
**[/src/Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj](/src/Client/Boilerplate.Client.Core/Boilerplate.Client.Core.csproj)**

Here's the build pipeline:

```xml
<Target Name="BeforeBuildTasks" AfterTargets="CoreCompile">
    <CallTarget Targets="InstallNodejsDependencies" />
    <CallTarget Targets="BuildJavaScript" />
    <CallTarget Targets="BuildCssFiles" />
</Target>
```

### Step 1: Install Node.js Dependencies

```xml
<Target Name="InstallNodejsDependencies" Inputs="package.json" Outputs="node_modules\.package-lock.json">
    <Exec Command="npm install" StandardOutputImportance="high" StandardErrorImportance="high" />
</Target>
```

- Runs `npm install` to install packages from `package.json`
- Only runs when `package.json` changes (incremental build optimization)

### Step 2: Build JavaScript

```xml
<Target Name="BuildJavaScript" Inputs="@(TypeScriptFiles);tsconfig.json;package.json" Outputs="wwwroot\scripts\app.js">
    <Exec Command="node_modules/.bin/tsc" StandardOutputImportance="high" StandardErrorImportance="high" />
    <Exec Condition=" '$(Environment)' == 'Development' " Command="node_modules/.bin/esbuild Scripts/index.js --bundle --outfile=wwwroot/scripts/app.js" StandardOutputImportance="high" StandardErrorImportance="high" />
    <Exec Condition=" '$(Environment)' != 'Development' " Command="node_modules/.bin/esbuild Scripts/index.js --bundle --outfile=wwwroot/scripts/app.js --minify" StandardOutputImportance="high" StandardErrorImportance="high" />
</Target>
```

**Build Steps:**
1. **TypeScript Compilation**: `tsc` compiles all `.ts` files to `.js` files
2. **Bundling (Development)**: `esbuild` bundles all JS modules into `wwwroot/scripts/app.js`
3. **Bundling (Production)**: Same as above, but with `--minify` flag for smaller file size

### Step 3: Build CSS Files

```xml
<Target Name="BuildCssFiles">
    <Exec Command="node_modules/.bin/sass Components:Components Styles/app.scss:wwwroot/styles/app.css --style compressed --silence-deprecation=import --update --color" StandardOutputImportance="high" StandardErrorImportance="high" LogStandardErrorAsError="true" />
</Target>
```

- Compiles all SCSS files to CSS
- Generates component-scoped styles and global `app.css`

---

## 4. TypeScript Entry Points

### index.ts - The Main Entry Point

**[/src/Client/Boilerplate.Client.Core/Scripts/index.ts](/src/Client/Boilerplate.Client.Core/Scripts/index.ts)**

```typescript
import './bswup';
import './theme';
import './events';
import { App } from './App';
import { WebInteropApp } from './WebInteropApp';

// Expose classes on window global
(window as any).App = App;
(window as any).WebInteropApp = WebInteropApp;
```

**Purpose:**
- Imports all necessary modules
- Exposes TypeScript classes to the global `window` object
- This makes them callable from C# via `jsRuntime.InvokeAsync<T>("App.methodName")`

---

## 5. JavaScript Interop: C# to JavaScript

### App.ts - The Main Interop Class

**[/src/Client/Boilerplate.Client.Core/Scripts/App.ts](/src/Client/Boilerplate.Client.Core/Scripts/App.ts)**

Here's a simplified version focusing on the `getTimeZone` method:

```typescript
export class App {
    public static getTimeZone(): string {
        return Intl.DateTimeFormat().resolvedOptions().timeZone;
    }
    
    // Other methods...
}
```

### C# Extension Method

**[/src/Client/Boilerplate.Client.Core/Extensions/IJSRuntimeExtensions.cs](/src/Client/Boilerplate.Client.Core/Extensions/IJSRuntimeExtensions.cs)**

```csharp
public static partial class IJSRuntimeExtensions
{
    public static ValueTask<string> GetTimeZone(this IJSRuntime jsRuntime)
    {
        return jsRuntime.InvokeAsync<string>("App.getTimeZone");
    }
}
```

**How it Works:**
1. **TypeScript Side**: `App.getTimeZone()` returns the browser's timezone using the `Intl` API
2. **C# Extension**: Provides a strongly-typed wrapper around the JS interop call
3. **Invocation**: `jsRuntime.InvokeAsync<string>("App.getTimeZone")` calls the JavaScript function and returns the result

### Usage in Components

**[/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs](/src/Client/Boilerplate.Client.Core/Components/AppClientCoordinator.cs)** (Line 75)

```csharp
TelemetryContext.TimeZone = await jsRuntime.GetTimeZone();
```

**Benefits of This Pattern:**
- ✅ **Type Safety**: IntelliSense and compile-time checking
- ✅ **Reusability**: One extension method used throughout the application
- ✅ **Maintainability**: Easy to update if the JavaScript API changes
- ✅ **Discoverability**: Developers can find available JS functions via extension methods

---

## 6. Practical Demo: Adding a New npm Package

Let's walk through adding the `uuid` package to generate unique identifiers.

### Step 1: Install the Package

Run these commands in the `Boilerplate.Client.Core` directory:

```powershell
npm install uuid
npm install @types/uuid --save-dev
```

This updates your `package.json`:

```json
{
    "dependencies": {
        "uuid": "^11.0.5"
    },
    "devDependencies": {
        "esbuild": "0.25.12",
        "sass": "1.93.3",
        "typescript": "5.9.3",
        "@types/uuid": "^10.0.0"
    }
}
```

### Step 2: Add TypeScript Method to App.ts

Add this to **[/src/Client/Boilerplate.Client.Core/Scripts/App.ts](/src/Client/Boilerplate.Client.Core/Scripts/App.ts)**:

```typescript
import { v4 as uuidv4 } from 'uuid';

export class App {
    // ... existing methods ...
    
    public static generateUuid(): string {
        return uuidv4();
    }
}
```

### Step 3: Create C# Extension Method

Add this to **[/src/Client/Boilerplate.Client.Core/Extensions/IJSRuntimeExtensions.cs](/src/Client/Boilerplate.Client.Core/Extensions/IJSRuntimeExtensions.cs)**:

```csharp
public static ValueTask<string> GenerateUuid(this IJSRuntime jsRuntime)
{
    return jsRuntime.InvokeAsync<string>("App.generateUuid");
}
```

### Step 4: Use in a Component

Now you can use it anywhere in your Blazor components:

```csharp
public partial class MyComponent : AppComponentBase
{
    protected override async Task OnInitAsync()
    {
        var uniqueId = await JSRuntime.GenerateUuid();
        Logger.LogInformation("Generated UUID: {UniqueId}", uniqueId);
    }
}
```

---

## 7. Build Process Summary

Here's the complete flow when you build the project:

```
1. MSBuild starts
   ↓
2. CoreCompile completes
   ↓
3. BeforeBuildTasks runs:
   ├─ InstallNodejsDependencies (npm install)
   ├─ BuildJavaScript
   │  ├─ tsc (TypeScript → JavaScript)
   │  └─ esbuild (Bundle → wwwroot/scripts/app.js)
   └─ BuildCssFiles
      └─ sass (SCSS → CSS)
   ↓
4. Static web assets are resolved
   ↓
5. Build completes
```

---

## 8. Key Takeaways

✅ **TypeScript Integration**: TypeScript is fully integrated into the MSBuild process

✅ **Automatic Builds**: TypeScript and SCSS compile automatically during project build

✅ **Incremental Builds**: Only rebuilds when source files change

✅ **JavaScript Interop Pattern**: 
- TypeScript methods → Exposed on `window.App`
- C# extension methods → Strongly-typed wrappers
- Usage in components → Clean, type-safe API

✅ **npm Package Support**: Easy to add any npm package (e.g., uuid, moment, lodash)

✅ **Production Optimization**: Minification enabled for non-development builds

---

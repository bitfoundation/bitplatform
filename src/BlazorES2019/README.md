# bit BlazorES2019

While .NET can run on the followings:

**Browsers**:
- Safari 15+
- Firefox 100+
- Chrome/Edge 95+

**OS**:

- iOS 15+
- Windows 7 SP1+
- macOS (Monterey) 12+
- Android 8+ & [WebView](https://play.google.com/store/apps/details?id=com.google.android.webview) 84+

The TypeScript project responsible for generating `blazor.web.js`, `blazor.webassembly.js`, `blazor.webview.js`, and `blazor.server.js` targets `ES2022+`, leading to the following updates in the compatibility table.

**Browsers**:
- Safari 16.4+

**OS**:

- iOS 16.4+
- macOS (Monterey) 13.3+
- Android 8+ & [WebView](https://play.google.com/store/apps/details?id=com.google.android.webview) 94+

Therefore, we decided to generate an `ES2019`-based output from the `aspnetcore` repository using the following script:

.NET 9
```shell
git clone https://github.com/dotnet/aspnetcore.git
cd aspnetcore
git switch release/9.0
git submodule update --init --recursive
sed -i 's/"target": "ES2022"/"target": "ES2019"/' src/Components/Shared.JS/tsconfig.json
npm install
npm run-script build
cd src/Components/Web.JS
npm install
npm run-script build:production
```

.NET 10
```shell
git clone https://github.com/dotnet/aspnetcore.git
cd aspnetcore
git switch release/10.0
git submodule update --init --recursive
sed -i 's/"target": "ES2024"/"target": "ES2019"/' src/Components/Shared.JS/tsconfig.json
npm install
npm run-script build
cd src/Components/Web.JS
npm install
npm run-script build:production
```

We're running the above script in [pre-release](https://github.com/bitfoundation/bitplatform/blob/develop/.github/workflows/prerelease.nuget.org.yml) and [release ](https://github.com/bitfoundation/bitplatform/blob/develop/.github/workflows/nuget.org.yml)nuget packages pipelines so you can use these assets published in [nuget.org](https://www.nuget.org/packages/Bit.BlazorES2019/)

To ensure compatibility with older browsers in Blazor WebAssembly, add the following configuration to your client web project:

```csproj
<PropertyGroup>
     <WasmEnableSIMD>false</WasmEnableSIMD>
</PropertyGroup>
```
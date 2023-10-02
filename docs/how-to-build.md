# How to build bit platform

- [Projects](#projects)
- [Basic build requirements](#basic-build-requirements)
- [Projects build requirements](#projects-build-requirements)

<br/>

## Projects

bit platform consists of multiple different projects/prodcuts with followings being the most important ones:

- [bit platform website](../src/Websites/Platform/)
- [bit BlazorUI (Blazor components)](../src/BlazorUI/)
- [bit Project Templates](../src/Templates/)

building each one of them requires some specific steps that explained below.

<br/>

## Basic build requirements

Building each of bit platform projects need the following basic requirements other than the specific requirements that explained later:

- [.NET 8 RC1 SDK (8.0.100-rc.1.23463.5)](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- [Node.js](https://nodejs.org)

<br/>

## Projects build requriements

Building each bit platform project requires specific steps that explained per project below:

<br/>

### bit Platform Website
This website only requires the basic requirements and can be simply build for the different Blazor hosting models as follows:

- **Blazor Server**: The default mode of the project is Blazor server and building this mode just needs to run the following command in the `Bit.Websites.Platform.Web` project folder:

```bash
dotnet build
```
and to run the project you just need to execute the following command in the same folder:

```bash
dotnet watch
```

- **Blazor WASM**: First you need to switch to Blazor WebAssembly by changing the value of the BlazorMode property in the `Directory.build.props` file located in the `src` folder:

```xml
<BlazorMode>BlazorWebAssembly</BlazorMode>
```

then change the `Sdk` of the `Bit.Websites.Platform.Web` project:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
```

To build the project then go to the `Bit.Websites.Platform.Api` project folder and run the following command there:

```bash
dotnet build
```

and to run the project execute the followng command:

```bash
dotnet watch
```

<br/>

### bit BlazorUI (Blazor components)
The bit BlazorUI product has many sub-projects with the main one being the BlazorUI project itself.

To build the BlazorUI project you only need the basic requirements and simply running the following command in its folder (`Bit.BlazorUI` in `src` folder):

```bash
dotnet build
```

Another project is the bit BlazorUI `Demo` project which has two different projects (Web & App) with different requirements to build.

The `Web` project just like the bit platform website only needs the basic requirements and can be simply built for the different Blazor hosting models as explained below:

- **Blazor Server**: The default mode of the project is Blazor server and building this mode just needs to run the following command in the `Bit.BlazorUI.Demo.Client.Web` project folder (`Demo\Client\Web`):

```bash
dotnet build
```

and to run the project you just need to execute the following command in the same folder:


```bash
dotnet watch
```

- **Blazor WASM**: First you need to switch to Blazor WebAssembly by changing the value of the BlazorMode property in the `Directory.build.props` file located in the root of the `Demo` folder:

```xml
<BlazorMode>BlazorWebAssembly</BlazorMode>
```

then change the `Sdk` of the `Bit.BlazorUI.Demo.Client.Web` project:

```xml
<Project Sdk="Microsoft.NET.Sdk.BlazorWebAssembly">
```

To build the project then go to the `Bit.BlazorUI.Demo.Server.Api` project folder (`Demo\Server\Api`) and run the following command there:

```bash
dotnet build
```

and to run the project execute the followng command in the same folder:

```bash
dotnet watch
```

The `App` project unlike the Web needs the `MAUI` workloads to build. This requirement cab installed with the following commands:

Linux:
```bash
dotnet workload install maui-android --sdk-version=8.0.100-rc.1.23455.8
dotnet workload install wasm-tools wasm-experimental --sdk-version=8.0.100-rc.1.23455.8
```
**Note**: because of MAUI shortcomings, for now only android version can be built on Linux.

Windows & macOS:
```bash
dotnet workload install maui --sdk-version=8.0.100-rc.1.23455.8
dotnet workload install wasm-tools wasm-experimental --sdk-version=8.0.100-rc.1.23455.8
```

To build the App project you first need to change the value of the `BlazorMode` in `Directory.build.props` file in the `Demo` folder:

```xml
<BlazorMode>BlazorHybrid</BlazorMode>
```

Then run the following command in the `Bit.BlazorUI.Demo.Client.App` project folder (`Demo\Client\App`):

```bash
dotnet build
```

and to run the project execute the followng command in the same folder:

```bash
dotnet watch
```


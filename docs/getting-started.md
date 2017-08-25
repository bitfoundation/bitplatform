# Getting started

**Prerequisites:**

1- ***Visual Studio***:

    To develop traditional ASP.NET projects, you can use any versions of Visual Studio from Visual Studio 2015 Update 3 to Visual Studio 2017 Update 3 with ASP.NET workload installed.

    To develop ASP.NET Core 1.0 projects, you can use any versions of Visual Studio from Visual Studio 2017 to Visual Studio 2017 Update 3 with ASP.NET Core workload installed.

    To develop ASP.NET Core 2.0 projects, you've to install Visual Studio 2017 Update 3 with ASP.NET Core workload installed.

    Download Visual Studio from [here](https://www.visualstudio.com/downloads/).


2- ***[.NET Core SDK](https://www.microsoft.com/net/download/core)*** >> For ASP .NET Core development only

3- ***[.NET 4.6.1](https://www.microsoft.com/en-us/download/details.aspx?id=49978) Developer Pack***


**Which of them shoud I choose?**

Note that based on bit framework's abstractions, **there's no difference between codes of developers who are running their app on top of traditional ASP .NET or the new ASP .NET Core one**.

Bit is based on [OWIN](http://owin.org) abstractions. Owin stands for **Open Web Interface for .NET**.

Both ASP .NET and ASP .NET Core have support for OWIN. See [here](https://www.nuget.org/packages/Microsoft.Owin.Host.SystemWeb/) and [here](https://www.nuget.org/packages/Microsoft.AspNetCore.Owin/) You can Also self host Owin apps inside windows services, console application etc using [Owin Self Host](https://www.nuget.org/packages/Microsoft.Owin.SelfHost/).

ASP .NET apps run on Full .NET Framework, which means you've to run them on Windows only. ASP .NET Core apps have two options, run on Full .NET Framework or run on .NET Core.

Bit has supported both traditional ASP .NET and ASP .NET Core with Full .NET Framework, and we're working on [.NET Core support](https://github.com/bit-foundation/bit-framework/issues/59).

There are several good news here: 
    
    1- **There will be no code change required** for you to switch from ASP .NET Core with Full .NET Framework to ASP .NET Core with .NET Core.

    2- We'll achieve this goal soon. (: (After they released .NET Core 2.1)

We recommend you to start with ASP.NET Core with Full .NET Framework, and start an easy switch whenever we announced our .NET Core support.
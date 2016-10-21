# bit-framework
Full Stack Framework to build Web Sites, Web Apps, Hybrid Apps and Native Mobile Apps

Build Instructions:

Requirements: 

  1- Visual Studio 2015 Update 3
  
  2- TypeScript 2.0.3
  
  3- .NET Core 1.0.1 - VS 2015 Tooling Preview 2 (Optional Requirement)
  
  4- Node JS - NPM - Gulp - Bower - Typings installed globally
  
Steps:

  1- Clone the repository using git clone https://github.com/bit-foundation/bit-framework.git
  
  2- Open Utilities.sln and right click on Foundation\Utilities\Foundation.CodeGenerators\Foundation.CodeGenerators\Implementations\HtmlClientProxyGenerator\Templates and run custom tool to generate T4 template results, then rebuild solution in release mode.
  
  3- Close the Visual Studio and install Foundation\Utilities\Foundation.VSPackage\bin\Release\Foundation.VSPackage.vsix
  
  4- Run bower install for Foundation\HtmlClient\Foundation.Test.HtmlClient\bower.json
  
  5- Run typings install for Foundation\HtmlClient\Foundation.Core.HtmlClient
  
  6- Build the solution. Note that due the lack of TypeScript Project System, you might have to build the solution more than once.
 
To run the samples:

  1- Run Visual Studio as Administrator
  
  2- Make sure that 80 port is free
  
  3- Set Foundation.Test as startup project and run the samples.

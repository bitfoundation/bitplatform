# bit-framework
Full Stack Framework to build Web Sites, Web Apps, Hybrid Apps and Native Mobile Apps

Build Instructions:

Requirements: 

  1- Visual Studio 2015 Update 3
  
  2- TypeScript 2.1.4
  
  3- .NET Core VS 2015 Update 3 Tooling Preview 2 (Optional Requirement)
  
  4- Node JS - NPM - Gulp - Bower - Typings installed globally
  
Steps:

  1- Clone the repository using git clone https://github.com/bit-foundation/bit-framework.git
  
  2- Open Tools > Options > Environment > Extensions and updates and add https://myget.org/F/bit-foundation/vsix as additional extension gallery.
  
  3- Install Bit-Foundation-VSPackage and restart the visual studio.
  
  4- Run bower install for Foundation\HtmlClient\Foundation.Test.HtmlClient\bower.json
  
  5- Run typings install for Foundation\HtmlClient\Foundation.Core.HtmlClient
  
  6- Build the solution. Note that due the lack of TypeScript Project System, you might have to build the solution more than once.
 
To run the samples:

  1- Run Visual Studio as Administrator
    
  2- Set Foundation.Test as startup project and run the samples.

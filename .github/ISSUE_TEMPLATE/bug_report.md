name: 🐞 Bug Report
description: Create a report about something that is not working
body:
- type: checkboxes
  attributes:
    label: Is there an existing issue for this?
    description: Please search to see if an issue already exists for the bug you encountered ([bitplatform/issues](https://github.com/bitfoundation/bitplatform/issues)).
    options:
    - label: I have searched the existing issues
      required: true
- type: textarea
  attributes:
    label: Describe the bug
    description: A clear and concise description of what the bug is.
  validations:
    required: true
- type: textarea
  attributes:
    label: Expected Behavior
    description: A clear and concise description of what you expected to happen.
  validations:
    required: false
- type: textarea
  attributes:
    label: Steps To Reproduce
    description: |
      Steps to reproduce the behavior.
      
      We ❤ code! Point us to a minimalistic repro project hosted in a public GitHub repo.
      For a repro project, create a new project using the template of your choice, apply the minimum required code to result in the issue you're observing.

      We will close this issue if:
      - The repro project you share with us is complex. We can't investigate custom projects, so don't point us to such, please.
      - If we will not be able to repro the behavior you're reporting.
      - If the repro project is attached as a `.zip` file.
      - If the GitHub repro project is set to `Private`.
  validations:
    required: false
- type: textarea
  attributes:
    label: Exceptions (if any)
    description: Include the exception you get when facing this issue.
    placeholder: 
  validations:
    required: false
- type: input
  attributes:
    label: .NET Version 
    description: |
      Run `dotnet --version`
  validations:
    required: false
- type: textarea
  attributes:
    label: Anything else?
    description: |
      - Developer's machine Operating System? For example `Windows 10 19041`
      - .NET Sdk version? For example `6.0.300`
      - IDE / Version? For example `Visual Studio 17.3 Preview 1`
      - Bit.Client.Web.BlazorUI nuget package's version? (If applicable) For example `12.0.0`
      - Bit.Tooling.Templates.Todo nuget package's version? (If applicable) For example `1.0.0`
      - Blazor mode? (If applicable) `Web Assembly (Client)`, `Server`, `Hybrid (MAUI)`
      - Customer / Consumers Browser / OS? (If applicable) For example `Chrome 101` For blazor web assembly and server or `Android 12` for hybrid

      Links? References? Anything that will give us more context about the issue you are encountering!

      Tip: You can attach images or log files by clicking this area to highlight it and then dragging files in.
  validations:
    required: false

# Getting started

**Prerequisites:**

* Visual Studio 2015 Update 3 or [Visual Studio 2017 Update 2](https://www.visualstudio.com/downloads/). We've supported Community, Professional and Enterprise editions[^1]

* [.NET 4.7 Developer Pack](https://www.microsoft.com/en-us/download/details.aspx?id=55168)

**Steps:**

* In Visual Studio open "Tools &gt; Options &gt; Environment &gt; Extensions and updates" and add [https://myget.org/F/bit-foundation/vsix](https://myget.org/F/bit-foundation/vsix) as additional extension gallery. Then install **Bit VS Extension V1** from Tools &gt; Extensions and updates

{% youtube %}
https://youtu.be/zT8mxN-zf4I
{% endyoutube %}

* Add [https://www.myget.org/F/bit-foundation/api/v3/index.json](https://www.myget.org/F/bit-foundation/api/v3/index.json) as nuget package source from "Tools &gt; Nuget Package Manager &gt; Package Manager Settings"

[^1]: We're going to support [Jet brains Rider IDE](https://www.jetbrains.com/rider/), [Visual Studio Code](https://code.visualstudio.com/) and [Visual Studio for mac](https://www.visualstudio.com/vs/visual-studio-mac/). In a meaning time you'll be able to develop on mac and linux too. Follow these issues links to get notified whenever these items get ready: [Rider](https://github.com/bit-foundation/bit-framework/issues/58), [Visual Studio Code](https://github.com/bit-foundation/bit-framework/issues/57) and [Visual Studio for mac](https://github.com/bit-foundation/bit-framework/issues/56).


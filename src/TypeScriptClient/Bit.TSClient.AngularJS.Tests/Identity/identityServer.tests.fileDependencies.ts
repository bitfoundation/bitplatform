/// <reference path="../typings.d.ts" />

module Bit.Identity.Tests {

    let dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerFileDependency({
        name: "normalize",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/normalize-css/normalize",
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/angular-material/angular-material",
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/controls",
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "fa-IR-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/fa-IR",
        predicate: (appInfo) => {
            return appInfo.culture == "FaIr";
        },
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/en-US",
        predicate: (appInfo) => {
            return appInfo.culture == "EnUs";
        },
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "Core-js",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/Core-js/client/Core"
    });

    dependencyManager.registerFileDependency({
        name: "fetch",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/whatwg-fetch/fetch",
        predicate: (appInfo) => {
            return typeof (fetch) == "undefined";
        }
    });

    dependencyManager.registerFileDependency({
        name: "runtime",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/regenerator-runtime/runtime"
    });

    dependencyManager.registerFileDependency({
        name: "angular",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/angular/angular"
    });

    dependencyManager.registerFileDependency({
        name: "angular-animate",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/angular-animate/angular-animate"
    });

    dependencyManager.registerFileDependency({
        name: "angular-area",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/angular-aria/angular-aria"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/angular-material/angular-material"
    });

    dependencyManager.registerFileDependency({
        name: "angular-messages",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/angular-messages/angular-messages"
    });

    dependencyManager.registerFileDependency({
        name: "angular-translate",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/angular-translate/dist/angular-translate"
    });
}

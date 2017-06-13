/// <reference path="../typings.d.ts" />

module Bit.Identity.Tests {

    let dependencyManager = Bit.DependencyManager.getCurrent();

    dependencyManager.registerFileDependency({
        name: "normalize",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/normalize-css/normalize",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/angular-material/angular-material",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/controls",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "fa-IR-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/fa-IR",
        predicate: (appInfo) => {
            return appInfo.culture == "FaIr";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/en-US",
        predicate: (appInfo) => {
            return appInfo.culture == "EnUs";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "Core-js",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/Core.js/client/Core"
    });

    dependencyManager.registerFileDependency({
        name: "fetch",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/fetch/fetch",
        predicate: (appInfo) => {
            return typeof (fetch) == "undefined";
        }
    });

    dependencyManager.registerFileDependency({
        name: "runtime",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/regenerator/packages/regenerator-runtime/runtime"
    });

    dependencyManager.registerFileDependency({
        name: "jQuery",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/jquery/dist/jquery",
        onLoad: () => {
            // For electron compatibility
            if (typeof window["require"] != 'undefined' && window["module"] != null && window["module"].exports != null) {
                window["$"] = window["jQuery"] = window["module"].exports;
            }
        }
    });

    dependencyManager.registerFileDependency({
        name: "angular",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/angular/angular"
    });

    dependencyManager.registerFileDependency({
        name: "angular-animate",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/angular-animate/angular-animate"
    });

    dependencyManager.registerFileDependency({
        name: "angular-area",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/angular-aria/angular-aria"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/angular-material/angular-material"
    });

    dependencyManager.registerFileDependency({
        name: "angular-messages",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/angular-messages/angular-messages"
    });

    dependencyManager.registerFileDependency({
        name: "angular-translate",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/angular-translate/angular-translate"
    });

    dependencyManager.registerFileDependency({
        name: "encoderjs",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/bower_components/encoderjs/encoder"
    });
}
/// <reference path="../typings.d.ts" />

module Bit.Identity.Tests {

    let dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerFileDependency({
        name: "normalize",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/normalize-css/normalize",
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/angular-material/angular-material",
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/contents/styles/controls",
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "fa-IR-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/contents/styles/fa-IR",
        predicate: (appInfo) => {
            return appInfo.culture == "FaIr";
        },
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/contents/styles/en-US",
        predicate: (appInfo) => {
            return appInfo.culture == "EnUs";
        },
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "Core-js",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/Core-js/client/Core"
    });

    dependencyManager.registerFileDependency({
        name: "angular",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/angular/angular"
    });

    dependencyManager.registerFileDependency({
        name: "angular-animate",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/angular-animate/angular-animate"
    });

    dependencyManager.registerFileDependency({
        name: "angular-area",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/angular-aria/angular-aria"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/angular-material/angular-material"
    });

    dependencyManager.registerFileDependency({
        name: "angular-messages",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/angular-messages/angular-messages"
    });

    dependencyManager.registerFileDependency({
        name: "angular-translate",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/angular-translate/dist/angular-translate"
    });
}

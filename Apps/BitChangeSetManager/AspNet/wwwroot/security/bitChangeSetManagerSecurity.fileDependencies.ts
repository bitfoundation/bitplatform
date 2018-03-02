/// <reference path="imports.ts" />

module BitChangeSetManagerSecurity {

    let dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerFileDependency({
        name: "normalize",
        path: "node_modules/normalize-css/normalize",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material-styles",
        path: "node_modules/angular-material/angular-material",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "node_modules/@bit/bit-framework/contents/styles/controls",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "node_modules/@bit/bit-framework/contents/styles/en-US",
        fileDependecyType: "Style",
        predicate: appEnvProvider => appEnvProvider.culture == "EnUs"
    });

    dependencyManager.registerFileDependency({
        name: "bit-change-set-manager-styles",
        path: "view/styles/bitChangeSetManagerStyles",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "Core-js",
        path: "node_modules/core-js/client/core"
    });

    dependencyManager.registerFileDependency({
        name: "fetch",
        path: "node_modules/whatwg-fetch/fetch",
        predicate: (appInfo) => {
            return typeof (fetch) == "undefined";
        }
    });

    dependencyManager.registerFileDependency({
        name: "runtime",
        path: "node_modules/regenerator-runtime/runtime"
    });

    dependencyManager.registerFileDependency({
        name: "jQuery",
        path: "node_modules/jquery/dist/jquery",
        onLoad: () => {
            // For electron compatibility
            if (typeof window["require"] != 'undefined' && window["module"] != null && window["module"].exports != null) {
                window["$"] = window["jQuery"] = window["module"].exports;
            }
        }
    });

    dependencyManager.registerFileDependency({
        name: "angular",
        path: "node_modules/angular/angular"
    });

    dependencyManager.registerFileDependency({
        name: "jayData",
        path: "node_modules/@bit/jaydata/jaydata"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-odata-provider",
        path: "node_modules/@bit/jaydata/jaydataproviders/oDataProvider"
    });

    dependencyManager.registerFileDependency({
        name: "angular-animate",
        path: "node_modules/angular-animate/angular-animate"
    });

    dependencyManager.registerFileDependency({
        name: "angular-area",
        path: "node_modules/angular-aria/angular-aria"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material",
        path: "node_modules/angular-material/angular-material"
    });

    dependencyManager.registerFileDependency({
        name: "angular-messages",
        path: "node_modules/angular-messages/angular-messages"
    });

    dependencyManager.registerFileDependency({
        name: "angular-translate",
        path: "node_modules/angular-translate/dist/angular-translate"
    });

    dependencyManager.registerFileDependency({
        name: "encoderjs",
        path: "node_modules/@bit/encoder/encoder"
    });

    dependencyManager.registerFileDependency({
        name: "bit-model-context",
        path: "node_modules/@bit/bit-framework/Bit.Model.Context"
    });
}
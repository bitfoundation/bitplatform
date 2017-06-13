/// <reference path="imports.ts" />

module BitChangeSetManagerSecurity {

    let dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerFileDependency({
        name: "normalize",
        path: "bower_components/normalize-css/normalize",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material-styles",
        path: "bower_components/angular-material/angular-material",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "bower_components/bit-releases/typescript-client/contents/styles/controls",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "bower_components/bit-releases/typescript-client/contents/styles/en-US",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "Core-js",
        path: "bower_components/Core.js/client/Core"
    });

    dependencyManager.registerFileDependency({
        name: "fetch",
        path: "bower_components/fetch/fetch",
        predicate: (appInfo) => {
            return typeof (fetch) == "undefined";
        }
    });

    dependencyManager.registerFileDependency({
        name: "runtime",
        path: "bower_components/regenerator/packages/regenerator-runtime/runtime"
    });

    dependencyManager.registerFileDependency({
        name: "jQuery",
        path: "bower_components/jquery/dist/jquery",
        onLoad: () => {
            // For electron compatibility
            if (typeof window["require"] != 'undefined' && window["module"] != null && window["module"].exports != null) {
                window["$"] = window["jQuery"] = window["module"].exports;
            }
        }
    });

    dependencyManager.registerFileDependency({
        name: "angular",
        path: "bower_components/angular/angular"
    });

    dependencyManager.registerFileDependency({
        name: "odataJS",
        path: "bower_components/olingo-odatajs/odatajs"
    });

    dependencyManager.registerFileDependency({
        name: "jayData",
        path: "bower_components/jaydata/dist/jaydata"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-odata-provider",
        path: "bower_components/jaydata/dist/jaydataproviders/oDataProvider"
    });

    dependencyManager.registerFileDependency({
        name: "angular-animate",
        path: "bower_components/angular-animate/angular-animate"
    });

    dependencyManager.registerFileDependency({
        name: "angular-area",
        path: "bower_components/angular-aria/angular-aria"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material",
        path: "bower_components/angular-material/angular-material"
    });

    dependencyManager.registerFileDependency({
        name: "angular-messages",
        path: "bower_components/angular-messages/angular-messages"
    });

    dependencyManager.registerFileDependency({
        name: "angular-translate",
        path: "bower_components/angular-translate/angular-translate"
    });

    dependencyManager.registerFileDependency({
        name: "encoderjs",
        path: "bower_components/encoderjs/encoder"
    });

    dependencyManager.registerFileDependency({
        name: "bit-model-context",
        path: "bower_components/bit-releases/typescript-client/Bit.Model.Context"
    });
}
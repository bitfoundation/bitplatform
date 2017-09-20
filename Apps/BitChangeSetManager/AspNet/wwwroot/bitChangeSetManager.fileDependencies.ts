/// <reference path="imports.ts" />

module BitChangeSetManager {

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
        name: "kendo-common-styles",
        path: "node_modules/kendo-ui/styles/kendo.common.min",
        fileDependecyType: "Style",
        onError: () => {
            console.warn("Download professional version of kendo and copy that to node_modules/kendo-ui/")
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-light-blue-theme-styles",
        path: "node_modules/kendo-ui/styles/kendo.material.min",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-rtl-styles",
        path: "node_modules/kendo-ui/styles/kendo.rtl.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker-styles",
        path: "node_modules/persian-datepicker/dist/css/persian-datepicker.min",
        fileDependecyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker-blue-styles",
        path: "node_modules/persian-datepicker/dist/css/theme/persian-datepicker-blue.min",
        fileDependecyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "node_modules/@bit/bit-framework/contents/styles/controls",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "light-blue-theme-custom-styles",
        path: "node_modules/@bit/bit-framework/contents/styles/theme.light.blue",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "node_modules/@bit/bit-framework/contents/styles/en-US",
        fileDependecyType: "Style",
        predicate: appEnvProvider => appEnvProvider.culture == "EnUs"
    });

    dependencyManager.registerFileDependency({
        name: "fa-IR-styles",
        path: "node_modules/@bit/bit-framework/contents/styles/fa-IR",
        fileDependecyType: "Style",
        predicate: appEnvProvider => appEnvProvider.culture == "FaIr"
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
        name: "event-source-polyfill",
        path: "node_modules/event-source-polyfill/eventsource",
        predicate: (appInfo) => {
            return typeof (window["EventSource"]) == "undefined";
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
        name: "kendo-ui-web",
        path: "node_modules/kendo-ui/js/kendo.web.min"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-culture-fa-IR",
        path: "node_modules/kendo-ui/js/cultures/kendo.culture.fa-IR.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-messages-fa-IR",
        path: "node_modules/kendo-ui/js/messages/kendo.messages.fa-IR.min",
        continueOnError: true,
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });

    dependencyManager.registerFileDependency({
        name: "odataJS",
        path: "node_modules/@bit/jaydata-odatajs/jaydata-odatajs-4.0.1"
    });

    dependencyManager.registerFileDependency({
        name: "jayData",
        path: "node_modules/@bit/jaydata/jaydata"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-inMemory-provider",
        path: "node_modules/@bit/jaydata/jaydataproviders/InMemoryProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-odata-provider",
        path: "node_modules/@bit/jaydata/jaydataproviders/oDataProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-indexedDb-provider",
        path: "node_modules/@bit/jaydata/jaydataproviders/IndexedDbProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-kendo-module",
        path: "node_modules/@bit/jaydata/jaydatamodules/kendo"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-inmemroy-module",
        path: "node_modules/@bit/jaydata/jaydatamodules/inMemory"
    });

    dependencyManager.registerFileDependency({
        name: "ng-component-router",
        path: "node_modules/ngcomponentrouter/angular_1_router"
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
        name: "persian-date",
        path: "node_modules/persian-datepicker/assets/persian-date.min",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker",
        path: "node_modules/persian-datepicker/dist/js/persian-datepicker",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "signalR",
        path: "node_modules/signalr/jquery.signalR"
    });

    dependencyManager.registerFileDependency({
        name: "pubsub-js",
        path: "node_modules/pubsub-js/src/pubsub"
    });

    dependencyManager.registerFileDependency({
        name: "decimaljs",
        path: "node_modules/decimal.js/decimal"
    });

    dependencyManager.registerFileDependency({
        name: "bit-model-context",
        path: "node_modules/@bit/bit-framework/Bit.Model.Context"
    });

    dependencyManager.registerFileDependency({
        name: "bit-change-set-manager-context",
        path: "BitChangeSetManager.Model.Context"
    });
}
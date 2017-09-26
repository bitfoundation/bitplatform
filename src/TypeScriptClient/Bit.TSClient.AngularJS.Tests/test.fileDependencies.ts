module Bit.Tests {

    let dependencyManager = DependencyManager.getCurrent();

    dependencyManager.registerFileDependency({
        name: "normalize",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/normalize-css/normalize",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/angular-material/angular-material",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-common-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.common.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet";
        },
        fileDependecyType: "Style",
        onError: () => {
            console.warn("Download professional version of kendo and copy that to bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/kendo-ui/");
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-light-green-theme-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.metro.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.theme == "LightGreen";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-light-blue-theme-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.material.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.theme == "LightBlue";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-dark-amber-theme-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.materialblack.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.theme == "DarkAmber";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-rtl-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.rtl.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/persian-datepicker/dist/css/persian-datepicker.min",
        fileDependecyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker-blue-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/persian-datepicker/dist/css/theme/persian-datepicker-blue.min",
        fileDependecyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/controls",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "dark-amber-theme-custom-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/theme.dark.amber",
        fileDependecyType: "Style",
        predicate: (appInfo) => {
            return appInfo.theme == "DarkAmber";
        }
    });

    dependencyManager.registerFileDependency({
        name: "light-blue-theme-custom-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/theme.light.blue",
        predicate: (appInfo) => {
            return appInfo.theme == "LightBlue";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "light-green-theme-custom-styles",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/contents/styles/theme.light.green",
        predicate: (appInfo) => {
            return appInfo.theme == "LightGreen";
        },
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
        name: "event-source-polyfill",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/event-source-polyfill/eventsourc",
        predicate: (appInfo) => {
            return typeof (window["EventSource"]) == "undefined";
        }
    });

    dependencyManager.registerFileDependency({
        name: "runtime",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/regenerator-runtime/runtime"
    });

    dependencyManager.registerFileDependency({
        name: "jQuery",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/jquery/dist/jquery",
        onLoad: () => {
            // For electron compatibility
            if (typeof window["require"] != 'undefined' && window["module"] != null && window["module"].exports != null) {
                window["$"] = window["jQuery"] = window["module"].exports;
            }
        }
    });

    dependencyManager.registerFileDependency({
        name: "angular",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/angular/angular"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-ui-web",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/kendo-ui/js/kendo.web.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-culture-fa-IR",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/kendo-ui/js/cultures/kendo.culture.fa-IR.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-messages-fa-IR",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/kendo-ui/js/messages/kendo.messages.fa-IR.min",
        continueOnError: true,
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });

    dependencyManager.registerFileDependency({
        name: "odataJS",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/@bit/jaydata-odatajs/jaydata-odatajs-4.0.1"
    });

    dependencyManager.registerFileDependency({
        name: "jayData",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydata"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-inMemory-provider",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydataproviders/InMemoryProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-odata-provider",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydataproviders/oDataProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-indexedDb-provider",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydataproviders/indexedDbProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-webSql-provider",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydataproviders/SqLiteProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-kendo-module",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydatamodules/kendo"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-inmemroy-module",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydatamodules/inmemory"
    });

    dependencyManager.registerFileDependency({
        name: "ui-router",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/node_modules/@uirouter/angularjs/release/angular-ui-router"
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

    dependencyManager.registerFileDependency({
        name: "persian-date",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/persian-datepicker/assets/persian-date.min",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS/node_modules/persian-datepicker/dist/js/persian-datepicker",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "jasmine",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Test/node_modules/jasmine-core/lib/jasmine-core/jasmine"
    });

    dependencyManager.registerFileDependency({
        name: "jasmine-jquery",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Test/node_modules/jasmine-jquery/lib/jasmine-jquery",
        loadTime: "Defered"
    });

    dependencyManager.registerFileDependency({
        name: "signalR",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/signalr/jquery.signalR"
    });

    dependencyManager.registerFileDependency({
        name: "pubsub-js",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/pubsub-js/src/pubsub"
    });

    dependencyManager.registerFileDependency({
        name: "decimaljs",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/node_modules/decimal.js/decimal"
    });

    dependencyManager.registerFileDependency({
        name: "bit-model-context",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.Core/Bit.Model.Context",
        fileDependecyType: "Script"
    });

    dependencyManager.registerFileDependency({
        name: "bit-test-context",
        path: "bit-framework/src/TypeScriptClient/Bit.TSClient.AngularJS.Tests/Test.Model.Context",
        fileDependecyType: "Script"
    });
}
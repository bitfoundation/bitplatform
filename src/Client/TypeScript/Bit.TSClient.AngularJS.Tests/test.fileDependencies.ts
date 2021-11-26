module Bit.Tests {

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
        name: "kendo-common-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.common.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet";
        },
        fileDependencyType: "Style",
        onError: () => {
            console.warn("Download professional version of kendo and copy that to src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/kendo-ui/");
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-light-green-theme-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.metro.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.theme == "LightGreen";
        },
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-light-blue-theme-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.material.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.theme == "LightBlue";
        },
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-dark-amber-theme-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.materialblack.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.theme == "DarkAmber";
        },
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-rtl-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/kendo-ui/styles/kendo.rtl.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        },
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/persian-datepicker/dist/css/persian-datepicker.min",
        fileDependencyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker-blue-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/persian-datepicker/dist/css/theme/persian-datepicker-blue.min",
        fileDependencyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/contents/styles/controls",
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "dark-amber-theme-custom-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/contents/styles/theme.dark.amber",
        fileDependencyType: "Style",
        predicate: (appInfo) => {
            return appInfo.theme == "DarkAmber";
        }
    });

    dependencyManager.registerFileDependency({
        name: "light-blue-theme-custom-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/contents/styles/theme.light.blue",
        predicate: (appInfo) => {
            return appInfo.theme == "LightBlue";
        },
        fileDependencyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "light-green-theme-custom-styles",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/contents/styles/theme.light.green",
        predicate: (appInfo) => {
            return appInfo.theme == "LightGreen";
        },
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
        name: "event-source-polyfill",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/event-source-polyfill/eventsourc",
        predicate: (appInfo) => {
            return typeof (window["EventSource"]) == "undefined";
        }
    });

    dependencyManager.registerFileDependency({
        name: "jQuery",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/jquery/dist/jquery",
        onLoad: () => {
            // For electron compatibility
            if (typeof window["require"] != 'undefined' && window["module"] != null && window["module"].exports != null) {
                window["$"] = window["jQuery"] = window["module"].exports;
            }
        }
    });

    dependencyManager.registerFileDependency({
        name: "angular",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/angular/angular"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-ui-web",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/kendo-ui/js/kendo.web.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-culture-fa-IR",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/kendo-ui/js/cultures/kendo.culture.fa-IR.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-messages-fa-IR",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/kendo-ui/js/messages/kendo.messages.fa-IR.min",
        continueOnError: true,
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });

    dependencyManager.registerFileDependency({
        name: "jayData",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydata"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-inMemory-provider",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydataproviders/InMemoryProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-odata-provider",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydataproviders/oDataProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-indexedDb-provider",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydataproviders/indexedDbProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-webSql-provider",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydataproviders/SqLiteProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-kendo-module",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydatamodules/kendo"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-inmemroy-module",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/@bit/jaydata/jaydatamodules/inmemory"
    });

    dependencyManager.registerFileDependency({
        name: "ui-router",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS.Tests/node_modules/@uirouter/angularjs/release/angular-ui-router"
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

    dependencyManager.registerFileDependency({
        name: "persian-date",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/persian-datepicker/assets/persian-date.min",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS/node_modules/persian-datepicker/dist/js/persian-datepicker",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "jasmine",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS.Test/node_modules/jasmine-core/lib/jasmine-core/jasmine"
    });

    dependencyManager.registerFileDependency({
        name: "jasmine-jquery",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS.Test/node_modules/jasmine-jquery/lib/jasmine-jquery",
        loadTime: "Deferred"
    });

    dependencyManager.registerFileDependency({
        name: "signalR",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/signalr/jquery.signalR"
    });

    dependencyManager.registerFileDependency({
        name: "pubsub-js",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/pubsub-js/src/pubsub"
    });

    dependencyManager.registerFileDependency({
        name: "decimaljs",
        path: "src/Client/TypeScript/Bit.TSClient.Core/node_modules/decimal.js/decimal"
    });

    dependencyManager.registerFileDependency({
        name: "bit-model-context",
        path: "src/Client/TypeScript/Bit.TSClient.Core/Bit.Model.Context",
        fileDependencyType: "Script"
    });

    dependencyManager.registerFileDependency({
        name: "bit-test-context",
        path: "src/Client/TypeScript/Bit.TSClient.AngularJS.Tests/Test.Model.Context",
        fileDependencyType: "Script"
    });
}

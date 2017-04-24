/// <reference path="imports.ts" />

module BitChangeSetManager {

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
        name: "kendo-common-styles",
        path: "bower_components/kendo-ui/styles/kendo.common.min",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-light-blue-theme-styles",
        path: "bower_components/kendo-ui/styles/kendo.material.min",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-rtl-styles",
        path: "bower_components/kendo-ui/styles/kendo.rtl.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "persian-datePicker-styles",
        path: "bower_components/persian-datepicker/dist/css/persian-datepicker-0.4.9",
        fileDependecyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-datePicker-blue-styles",
        path: "bower_components/persian-datepicker/dist/css/theme/persian-datepicker-blue",
        fileDependecyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "bower_components/bit-releases/foundation.view/contents/styles/controls",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "light-blue-theme-custom-styles",
        path: "bower_components/bit-releases/foundation.view/contents/styles/theme.light.blue",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "bower_components/bit-releases/foundation.view/contents/styles/en-US",
        fileDependecyType: "Style",
        predicate: appEnvProvider => appEnvProvider.culture == "EnUs"
    });

    dependencyManager.registerFileDependency({
        name: "fa-IR-styles",
        path: "bower_components/bit-releases/foundation.view/contents/styles/fa-IR",
        fileDependecyType: "Style",
        predicate: appEnvProvider => appEnvProvider.culture == "FaIr"
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
        path: "bower_components/jquery/dist/jquery"
    });

    dependencyManager.registerFileDependency({
        name: "angular",
        path: "bower_components/angular/angular"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-ui-web",
        path: "bower_components/kendo-ui/js/kendo.web.min"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-culture-fa-IR",
        path: "bower_components/kendo-ui/js/cultures/kendo.culture.fa-IR.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });

    dependencyManager.registerFileDependency({
        // https://raw.githubusercontent.com/bit-foundation/kendo-ui-core/master/src/messages/kendo.messages.fa-IR.js
        name: "kendo-messages-fa-IR",
        path: "bower_components/kendo-ui/js/messages/kendo.messages.fa-IR",
        continueOnError: true,
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
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
        name: "jayData-inMemory-provider",
        path: "bower_components/jaydata/dist/jaydataproviders/InMemoryProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-odata-provider",
        path: "bower_components/jaydata/dist/jaydataproviders/oDataProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-indexedDb-provider",
        path: "bower_components/jaydata/dist/jaydataproviders/indexedDbProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-kendo-module",
        path: "bower_components/jaydata/dist/jaydatamodules/kendo"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-inmemroy-module",
        path: "bower_components/jaydata/dist/jaydatamodules/inmemory"
    });

    dependencyManager.registerFileDependency({
        name: "ng-component-router",
        path: "bower_components/ngComponentRouter/angular_1_router"
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
        name: "persian-date",
        path: "bower_components/persian-date/dist/0.1.8/persian-date-0.1.8",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker",
        path: "bower_components/persian-datepicker/dist/js/persian-datepicker-0.4.9",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "signalR",
        path: "bower_components/signalr/jquery.signalR"
    });

    dependencyManager.registerFileDependency({
        name: "decimaljs",
        path: "bower_components/decimal.js/decimal"
    });

    dependencyManager.registerFileDependency({
        name: "foundation-model-context",
        path: "bower_components/bit-releases/foundation.viewmodel/Foundation.Model.Context"
    });

    dependencyManager.registerFileDependency({
        name: "bit-change-set-manager-context",
        path: "bitChangeSetManager.Model.Context"
    });

    dependencyManager.registerFileDependency({
        name: "bit-change-set-manager-styles",
        path: "view/bitChangeSetManagerStyles",
        fileDependecyType: "Style"
    });
}
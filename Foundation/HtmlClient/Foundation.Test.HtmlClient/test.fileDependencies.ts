module Foundation.Test {

    let dependencyManager = Core.DependencyManager.getCurrent();

    dependencyManager.registerFileDependency({
        name: "normalize",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/normalize-css/normalize",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/angular-material/angular-material",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-common-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/styles/kendo.common.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-light-green-theme-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/styles/kendo.metro.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.theme == "LightGreen";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-light-blue-theme-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/styles/kendo.material.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.theme == "LightBlue";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-dark-amber-theme-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/styles/kendo.materialblack.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.theme == "DarkAmber";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-rtl-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/styles/kendo.rtl.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "persian-datePicker-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/persian-datepicker/dist/css/persian-datepicker-0.4.9",
        fileDependecyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-datePicker-blue-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/persian-datepicker/dist/css/theme/persian-datepicker-blue",
        fileDependecyType: "Style",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "controls-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.View.HtmlClient/contents/styles/controls",
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "dark-amber-theme-custom-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.View.HtmlClient/contents/styles/theme.dark.amber",
        fileDependecyType: "Style",
        predicate: (appInfo) => {
            return appInfo.theme == "DarkAmber";
        }
    });

    dependencyManager.registerFileDependency({
        name: "light-blue-theme-custom-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.View.HtmlClient/contents/styles/theme.light.blue",
        predicate: (appInfo) => {
            return appInfo.theme == "LightBlue";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "light-green-theme-custom-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.View.HtmlClient/contents/styles/theme.light.green",
        predicate: (appInfo) => {
            return appInfo.theme == "LightGreen";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "fa-IR-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.View.HtmlClient/contents/styles/fa-IR",
        predicate: (appInfo) => {
            return appInfo.culture == "FaIr";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "en-US-styles",
        path: "bit-framework/Foundation/HtmlClient/Foundation.View.HtmlClient/contents/styles/en-US",
        predicate: (appInfo) => {
            return appInfo.culture == "EnUs";
        },
        fileDependecyType: "Style"
    });

    dependencyManager.registerFileDependency({
        name: "Core-js",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/Core.js/client/Core"
    });

    dependencyManager.registerFileDependency({
        name: "fetch",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/fetch/fetch",
        predicate: (appInfo) => {
            return typeof (fetch) == "undefined";
        }
    });

    dependencyManager.registerFileDependency({
        name: "runtime",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/regenerator/packages/regenerator-runtime/runtime"
    });

    dependencyManager.registerFileDependency({
        name: "jQuery",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/jquery/dist/jquery"
    });

    dependencyManager.registerFileDependency({
        name: "angular",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/angular/angular"
    });

    dependencyManager.registerFileDependency({
        name: "kendo-ui-web",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/js/kendo.web.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.getConfig<string>("KendoUILoadMode") == "Web";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-ui-core",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/src/js/kendo.ui.core",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.getConfig<string>("KendoUILoadMode") == "Core";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-core",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/src/js/kendo.Core",
        predicate: (appInfo) => {
            return appInfo.screenSize == "MobileAndPhablet" && appInfo.getConfig<string>("KendoUILoadMode") == "Core";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-data",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/src/js/kendo.data",
        predicate: (appInfo) => {
            return appInfo.screenSize == "MobileAndPhablet" && appInfo.getConfig<string>("KendoUILoadMode") == "Core";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-binder",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/src/js/kendo.binder",
        predicate: (appInfo) => {
            return appInfo.screenSize == "MobileAndPhablet" && appInfo.getConfig<string>("KendoUILoadMode") == "Core";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-culture-fa-IR",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/js/cultures/kendo.culture.fa-IR.min",
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });

    dependencyManager.registerFileDependency({
        name: "kendo-messages-fa-IR",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/kendo-ui/js/messages/kendo.messages.fa-IR",
        continueOnError: true,
        predicate: (appInfo) => {
            return appInfo.screenSize == "DesktopAndTablet" && appInfo.culture == "FaIr";
        }
    });

    dependencyManager.registerFileDependency({
        name: "odataJS",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/olingo-odatajs/odatajs"
    });

    dependencyManager.registerFileDependency({
        name: "jayData",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/jaydata/dist/jaydata"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-inMemory-provider",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/jaydata/dist/jaydataproviders/InMemoryProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-odata-provider",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/jaydata/dist/jaydataproviders/oDataProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-indexedDb-provider",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/jaydata/dist/jaydataproviders/indexedDbProvider"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-kendo-module",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/jaydata/dist/jaydatamodules/kendo"
    });

    dependencyManager.registerFileDependency({
        name: "jayData-inmemroy-module",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/jaydata/dist/jaydatamodules/inmemory"
    });

    dependencyManager.registerFileDependency({
        name: "ng-component-router",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/ngComponentRouter/angular_1_router"
    });

    dependencyManager.registerFileDependency({
        name: "angular-animate",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/angular-animate/angular-animate"
    });

    dependencyManager.registerFileDependency({
        name: "angular-area",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/angular-aria/angular-aria"
    });

    dependencyManager.registerFileDependency({
        name: "angular-material",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/angular-material/angular-material"
    });

    dependencyManager.registerFileDependency({
        name: "angular-messages",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/angular-messages/angular-messages"
    });

    dependencyManager.registerFileDependency({
        name: "angular-translate",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/angular-translate/angular-translate"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/persian-date/dist/0.1.8/persian-date-0.1.8",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "persian-date-picker",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/persian-datepicker/dist/js/persian-datepicker-0.4.9",
        predicate: (appInfo) => appInfo.culture == "FaIr"
    });

    dependencyManager.registerFileDependency({
        name: "jasmine",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/jasmine-core/lib/jasmine-core/jasmine"
    });

    dependencyManager.registerFileDependency({
        name: "jasmine-jquery",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/jasmine-jquery/lib/jasmine-jquery",
        loadTime: "Defered"
    });

    dependencyManager.registerFileDependency({
        name: "signalR",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/signalr/jquery.signalR",
        loadTime: "Defered"
    });

    dependencyManager.registerFileDependency({
        name: "decimaljs",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/bower_components/decimal.js/decimal"
    });

    dependencyManager.registerFileDependency({
        name: "foundation-model-context",
        path: "bit-framework/Foundation/HtmlClient/Foundation.ViewModel.HtmlClient/Foundation.Model.Context",
        fileDependecyType: "Script"
    });

    dependencyManager.registerFileDependency({
        name: "foundation-test-context",
        path: "bit-framework/Foundation/HtmlClient/Foundation.Test.HtmlClient/Test.Model.Context",
        fileDependecyType: "Script"
    });
}
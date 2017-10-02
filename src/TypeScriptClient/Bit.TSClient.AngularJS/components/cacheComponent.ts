module Bit.Components {

    export class CacheComponent {

        public constructor(public $rootScope: ng.IScope, public $element: JQuery, public $compile: ng.ICompileService) {

        }

        public childElement: JQuery;
        public controller: { onActivated?: (initialActivation: boolean) => void, onDeactivated?: () => void };

        public async $postLink(): Promise<void> {

            const childElementName = this.$element.prop("tagName").toLowerCase().replace("-cache", "");
            const findCachedVersionInBody = angular.element(document.body).children(childElementName);
            const isFirstTime = findCachedVersionInBody.length == 0;
            this.childElement = isFirstTime ? angular.element(`<${childElementName}></${childElementName}>`) : findCachedVersionInBody;
            const componentName = this.$element.children("cache").attr("component-name");

            if (isFirstTime == true) {
                angular.element(document.body).append(this.childElement);
                const childScope = this.$rootScope.$new(true, this.$rootScope);
                this.$compile(this.childElement)(childScope);
            }

            while (this.controller == null) {
                this.controller = this.childElement.data(`$${componentName}Controller`);
                await new Promise(res => setTimeout(res));
            }

            this.$element.append(this.childElement);
            this.childElement.show();
            if (this.controller.onActivated != null)
                this.controller.onActivated(isFirstTime);
        }

        public $onDestroy() {
            this.childElement.hide();
            angular.element(document.body).append(this.childElement);
            if (this.controller.onDeactivated != null)
                this.controller.onDeactivated();
        }
    }

    CacheComponent.$inject = ["$rootScope", "$element", "$compile"];
}
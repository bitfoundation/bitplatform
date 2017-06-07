module BitChangeSetManager.View.Directives {

    @DirectiveDependency({
        name: "BitMenuContainer",
        scope: false,
        replace: true,
        terminal: true,
        template: `<md-sidenav ng-transclude></md-sidenav>`,
        transclude: true
    })
    export class BitMenuContainerDirective {

        public constructor( @Inject("$element") public $element: JQuery,
            @Inject("$attrs") public $attrs: ng.IAttributes & { mdComponentId: string },
            @Inject("$mdSidenav") public $mdSidenav: ng.material.ISidenavService,
            @Inject("ClientAppProfileManager") public clientAppProfileManager: Bit.ClientAppProfileManager,
            @Inject("$window") public $window: ng.IWindowService) {

        }

        private onMouseMove(e: MouseEvent) {

            if ((this.direction == "Ltr" && e.clientX == 0) || (this.direction == "Rtl" && e.clientX == document.body.clientWidth - 1)) {
                if (!this.$mdSidenavObject.isOpen()) {
                    this.$mdSidenavObject.open();
                }
            }

        }

        public direction: "Ltr" | "Rtl";
        public $mdSidenavObject: ng.material.ISidenavObject;

        @Command()
        public $postLink() {

            this.$mdSidenavObject = this.$mdSidenav(this.$attrs.mdComponentId);

            this.$element.mouseleave(() => {
                this.$mdSidenavObject.close();
            });

            this.direction = this.clientAppProfileManager.getClientAppProfile().direction;

            if (!this.$element.hasClass("md-sidenav-right") && !this.$element.hasClass("md-sidenav-left")) {
                if (this.direction == "Rtl")
                    this.$element.addClass("md-sidenav-right");
                else
                    this.$element.addClass("md-sidenav-left");
            }

            this.$window.addEventListener("mousemove", this.onMouseMove.bind(this));
        }

        public $onDestroy() {
            this.$window.addEventListener("mousemove", this.onMouseMove);
        }
    }
}
module BitChangeSetManager.View.Directives {

    @DirectiveDependency({
        name: "BackToTop",
        scope: false,
        terminal: true,
        restrict: "A",
    })
    export class BackToTopDirective {

        public constructor( @Inject("$element") public $element: JQuery, @Inject("$window") public $window: ng.IWindowService) {

        }

        private hideShow$elementBasedOnCurrentPosition() {
            if (this.$window.scrollY > 50) {
                this.$element.fadeIn();
            } else {
                this.$element.fadeOut();
            }
        }

        @Command()
        public async $postLink(): Promise<void> {

            this.$element.addClass("back-to-top");

            this.$element.click(() => {
                $("html, body").animate({ scrollTop: 0 }, 100);
            });

            this.hideShow$elementBasedOnCurrentPosition();

            this.$window.addEventListener("scroll", this.hideShow$elementBasedOnCurrentPosition.bind(this));
        }

        public $onDestroy() {
            this.$window.removeEventListener("scroll", this.hideShow$elementBasedOnCurrentPosition);
        }
    }
}
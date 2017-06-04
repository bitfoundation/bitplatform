module BitChangeSetManager.View.Directives {

    @DirectiveDependency({ name: "backToTop" })
    export class BackToTopDirective implements FoundationVM.Contracts.IDirective {
        public getDirectiveFactory(): ng.IDirectiveFactory {
            return () => ({
                scope: false,
                terminal: true,
                restrict: "A",
                link($scope: ng.IScope, $element: JQuery, attributes: ng.IAttributes) {

                    let dependencyManager = FoundationCore.DependencyManager.getCurrent();

                    $element.addClass("back-to-top");

                    $element.click(() => {
                        $("html, body").animate({ scrollTop: 0 }, 100);
                    });

                    let $window = $(window);

                    let hideShow$elementBasedOnCurrentPosition = () => {
                        if ($window.scrollTop() > 100) {
                            $element.fadeIn();
                        } else {
                            $element.fadeOut();
                        }
                    }

                    hideShow$elementBasedOnCurrentPosition();

                    $window.scroll(() => {
                        hideShow$elementBasedOnCurrentPosition();
                    });
                }
            });
        }
    }
}
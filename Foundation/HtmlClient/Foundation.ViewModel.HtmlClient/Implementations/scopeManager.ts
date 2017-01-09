module Foundation.ViewModel {
    export class ScopeManager {

        public static Use$ApplyAsync = true;

        public static update$scope($scope: ng.IScope, use$ApplyAsync: boolean = ScopeManager.Use$ApplyAsync): void {

            if (use$ApplyAsync == true) {
                setTimeout(() => {
                    $scope.$applyAsync();
                }, 0);
            }
            else if ($scope.$$phase != "$apply" && $scope.$$phase != "$digest" && $scope.$root.$$phase != "$apply" && $scope.$root.$$phase != "$digest")
                $scope.$apply();

        }

    }
}
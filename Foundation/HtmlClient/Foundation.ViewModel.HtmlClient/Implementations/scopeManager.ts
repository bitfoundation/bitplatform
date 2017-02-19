module Foundation.ViewModel {
    export class ScopeManager {

        public static update$scope($scope: ng.IScope, $applyMode: "$applyAsync" | "$apply" = Default$scopeConfiguration.currentConfig.$appyMode): void {

            if ($applyMode == "$applyAsync") {
                setTimeout(() => {
                    $scope.$applyAsync();
                }, 0);
            }
            else if ($scope.$$phase != "$apply" && $scope.$$phase != "$digest" && $scope.$root.$$phase != "$apply" && $scope.$root.$$phase != "$digest")
                $scope.$apply();

        }

    }
}
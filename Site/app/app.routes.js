(function() {
	angular.module('app').config(configure);

	configure.$inject = ['$routeProvider'];
	function configure($routeProvider) {
		$routeProvider.when('/', {
			templateUrl: 'app/videos.html',
			controller: 'VideosController',
			controllerAs: 'vm'
		})
		.otherwise({ redirectTo: '/' });
	}
})();

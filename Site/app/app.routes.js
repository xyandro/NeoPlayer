(function() {
	angular.module('app').config(configure);

	configure.$inject = ['$routeProvider'];
	function configure($routeProvider) {
		$routeProvider.when('/', {
			templateUrl: 'app/movies.html',
			controller: 'MoviesController',
			controllerAs: 'vm'
		}).when('/customers', {
			templateUrl: 'app/customers.html',
			controller: 'CustomersController',
			controllerAs: 'vm'
		}).when('/orders/:id?', {
			templateUrl: 'app/orders.html',
			controller: 'OrdersController',
			controllerAs: 'vm'
		})
		.otherwise({ redirectTo: '/' });
	}
})();

(function () {
	angular.module('app').service('DataService', DataService);

	DataService.$inject = ['$http'];
	function DataService($http) {
		this.getCustomers = function () {
			//return promise
			return $http.get('customers.json').then(function (response) {
				var customers = response.data;
				customers[2].name = 'John Papa';
				return customers;
			});
		};

		this.getMovies = function () {
			//return promise
			//return $http.get('service/movies.json').then(function (response) { return response.data; });
			return $http.get('customers.json').then(function (response) { return response.data; });
		};
	}
})();

//(function () {
//	angular.module('app').factory('dataService', dataService);

//	function dataService() {
//		var factory = {};

//		factory.getCustomers = function () {
//			return [
//				{ id: 1, name: 'Buttercup', city: 'Orlando' },
//				{ id: 2, name: 'Max', city: 'New York' },
//				{ id: 3, name: 'John', city: 'Las Vegas' },
//				{ id: 4, name: 'Dan', city: 'Phoenix' },
//				{ id: 5, name: 'Ward', city: 'San Francisco' },
//			]
//		};

//		return factory;
//	}
//})();

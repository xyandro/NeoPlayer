(function () {
	angular.module('app').controller('OrdersController', OrdersController);
	OrdersController.$inject = ['$routeParams'];
	function OrdersController($routeParams) {
		var vm = this;
		vm.customerId = $routeParams.id;
	}
})();

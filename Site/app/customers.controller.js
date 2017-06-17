(function () {
    angular.module('app').controller('CustomersController', CustomersController);
    //Also acceptable: function CustomersController(DataService) {
    CustomersController.$inject = ['DataService', '$window'];
    function CustomersController(dataService, $window) {
        var vm = this;
        vm.searchText = '';
        function activate() {
            dataService.getCustomers().then(function (customers) {
                vm.people = customers;
            }, function (error) {
                $window.alert('Failed: ' + error);
            });
        }
        vm.addCustomer = function () {
            vm.people.push({ id: 6, name: 'Tina', city: 'Santa Fe' });
        };
        activate();
    }
})();

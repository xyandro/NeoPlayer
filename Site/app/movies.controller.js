(function () {
	angular.module('app').controller('MoviesController', MoviesController);

	MoviesController.$inject = ['$http', '$filter'];
	function MoviesController($http, $filter) {
		var vm = this;
		vm.searchText = '';

		function refresh() {
			$http.get('service/movies').then(function (response) {
				vm.movies = response.data;
				setTimeout(refresh, 5000);
			});
		}

		vm.resetSearch = function (movie) {
			vm.searchText = '';
		};

		vm.queueMovie = function (movie) {
			var url = 'service/' + (movie.queued ? "de" : "en") + 'queue?movie=' + encodeURIComponent(movie.name);
			$http.get(url).then(function (response) {
				movie.queued = !movie.queued;
			});
		};

		vm.queueMovies = function () {
			var enqueue = false;
			var result = $filter('filter')(vm.movies, vm.searchText);
			var str = "";
			for (var x = 0; x < result.length; ++x) {
				str += x == 0 ? "?" : "&";
				str += 'movie=' + encodeURIComponent(result[x].name);
				if (!result[x].queued)
					enqueue = true;
			}
			var url = 'service/' + (enqueue ? "en" : "de") + 'queue' + str;
			$http.get(url).then(function (response) {
				for (var x = 0; x < result.length; ++x) {
					result[x].queued = enqueue;
				}
			});
		};

		refresh();
	}
})();

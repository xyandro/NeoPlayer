(function () {
	angular.module('app').controller('VideosController', VideosController);

	VideosController.$inject = ['$http', '$filter'];
	function VideosController($http, $filter) {
		var vm = this;
		vm.searchText = '';
		vm.curPos = 0;
		vm.maxPos = 0;
		vm.playing = false;
		vm.currentSong = '';

		vm.refresh = function () {
			$http.get('service/videos').then(function (response) {
				vm.videos = response.data;
				setTimeout(vm.refresh, 5000);
			});
		}

		vm.resetSearch = function (video) {
			vm.searchText = '';
		}

		vm.queueVideo = function (video) {
			var url = 'service/' + (video.queued ? "de" : "en") + 'queue?video=' + encodeURIComponent(video.name);
			$http.get(url).then(function (response) {
				video.queued = !video.queued;
			});
		}

		vm.queueVideos = function () {
			var enqueue = false;
			var result = $filter('filter')(vm.videos, vm.searchText);
			var str = "";
			for (var x = 0; x < result.length; ++x) {
				str += x == 0 ? "?" : "&";
				str += 'video=' + encodeURIComponent(result[x].name);
				if (!result[x].queued)
					enqueue = true;
			}
			var url = 'service/' + (enqueue ? "en" : "de") + 'queue' + str;
			$http.get(url).then(function (response) {
				for (var x = 0; x < result.length; ++x) {
					result[x].queued = enqueue;
				}
			});
		}

		vm.pause = function () {
			$http.get('service/pause');
			vm.playing = !vm.playing;
		}

		vm.next = function () {
			$http.get('service/next');
		}

		vm.setPos = function (value) {
			$http.get('service/setpos?pos=' + value);
		}

		vm.jumpPos = function (value) {
			$http.get('service/jumppos?offset=' + value);
		}

		vm.updatePlayInfo = function () {
			$http.get('service/getplayinfo').then(function (response) {
				vm.maxPos = response.data.Max;
				vm.curPos = response.data.Position;
				vm.playing = response.data.Playing;
				vm.currentSong = response.data.CurrentSong;
				vm.test = true;
			});
		}

		vm.refresh();
		setInterval(vm.updatePlayInfo, 1000);
	}
})();

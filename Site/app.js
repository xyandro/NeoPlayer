var app = angular.module("app", []);

angular.module("app").controller("NeoMediaController", NeoMediaController);

NeoMediaController.$inject = ["$http", "$filter"];
function NeoMediaController($http, $filter) {
	var vm = this;
	vm.searchText = "";
	vm.curPos = 0;
	vm.maxPos = 0;
	vm.playing = false;
	vm.currentSong = "";
	vm.first = true;

	vm.resetSearch = function (video) {
		vm.searchText = "";
	}

	vm.queueVideo = function (video) {
		var url = "service/" + (video.queued ? "de" : "en") + "queue?video=" + encodeURIComponent(video.name);
		$http.get(url).then(function (response) {
			video.queued = !video.queued;
		});
	}

	vm.queueVideos = function () {
		var enqueue = false;
		var result = $filter("filter")(vm.videos, vm.searchText);
		var str = "";
		for (var x = 0; x < result.length; ++x) {
			str += x == 0 ? "?" : "&";
			str += "video=" + encodeURIComponent(result[x].name);
			if (!result[x].queued)
				enqueue = true;
		}
		var url = "service/" + (enqueue ? "en" : "de") + "queue" + str;
		$http.get(url).then(function (response) {
			for (var x = 0; x < result.length; ++x) {
				result[x].queued = enqueue;
			}
		});
	}

	vm.pause = function () {
		$http.get("service/pause");
		vm.playing = !vm.playing;
	}

	vm.next = function () {
		$http.get("service/next");
	}

	vm.setPosition = function (position, relative) {
		// Ignore first call, which sets the video position to 0 when a new client connects
		if (vm.first) {
			vm.first = false;
			return;
		}
		$http.get("service/setPosition?position=" + position + "&relative=" + relative);
	}

	vm.getStatus = function () {
		$http.get("service/getStatus").then(function (response) {
			vm.maxPos = response.data.Max;
			vm.curPos = response.data.Position;
			vm.playing = response.data.Playing;
			vm.currentSong = response.data.CurrentSong;
			vm.videos = response.data.Videos;
			setTimeout(vm.getStatus, 1000);
		}, function (response) {
			setTimeout(vm.getStatus, 5000);
		});
	}

	vm.getStatus();
}

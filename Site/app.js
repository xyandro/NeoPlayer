var app = angular.module("app", []);

angular.module("app").controller("NeoMediaController", NeoMediaController);

NeoMediaController.$inject = ["$http", "$filter"];
function NeoMediaController($http, $filter) {
	var vm = this;

	vm.Modes = {
		Video: "Video",
		Slideshow: "Slideshow",
	}

	vm.SearchText = "";
	vm.PlayerPosition = 0;
	vm.PlayerMax = 0;
	vm.PlayerIsPlaying = false;
	vm.PlayerCurrentSong = "";
	vm.Mode = vm.Modes.Slideshow;
	vm.SlideshowQuery = "";

	vm.resetSearch = function (video) {
		vm.SearchText = "";
	}

	vm.queueVideo = function (video) {
		var url = "Service/" + (video.Queued ? "De" : "En") + "queue?Video=" + encodeURIComponent(video.Name);
		$http.get(url).then(function (response) {
			video.Queued = !video.Queued;
		});
	}

	vm.queueVideos = function () {
		var enqueue = false;
		var result = $filter("filter")(vm.Videos, vm.SearchText);
		var str = "";
		for (var x = 0; x < result.length; ++x) {
			str += x == 0 ? "?" : "&";
			str += "Video=" + encodeURIComponent(result[x].Name);
			if (!result[x].Queued)
				enqueue = true;
		}
		var url = "Service/" + (enqueue ? "En" : "De") + "queue" + str;
		$http.get(url).then(function (response) {
			for (var x = 0; x < result.length; ++x) {
				result[x].Queued = enqueue;
			}
		});
	}

	vm.pause = function () {
		$http.get("Service/Pause");
		vm.PlayerIsPlaying = !vm.PlayerIsPlaying;
	}

	vm.next = function () {
		$http.get("Service/Next");
	}

	vm.firstSetPosition = true;
	vm.setPosition = function (position, relative) {
		// Ignore first call, which sets the video position to 0 when a new client connects
		if (vm.firstSetPosition) {
			vm.firstSetPosition = false;
			return;
		}
		$http.get("Service/SetPosition?Position=" + position + "&Relative=" + relative);
	}

	vm.getStatus = function () {
		$http.get("Service/GetStatus").then(function (response) {
			vm.PlayerMax = response.data.PlayerMax;
			vm.PlayerPosition = response.data.PlayerPosition;
			vm.PlayerIsPlaying = response.data.PlayerIsPlaying;
			vm.PlayerCurrentSong = response.data.PlayerCurrentSong;
			vm.Videos = response.data.Videos;
			vm.SlideshowQuery = response.data.SlideshowQuery;
			vm.SlideshowImageDisplayTime = response.data.SlideshowImageDisplayTime;
			vm.SlideshowImagesPaused = response.data.SlideshowImagesPaused;

			setTimeout(vm.getStatus, 1000);
		}, function (response) {
			setTimeout(vm.getStatus, 5000);
		});
	}

	vm.toggleMode = function () {
		if (vm.Mode == vm.Modes.Video)
			vm.Mode = vm.Modes.Slideshow;
		else
			vm.Mode = vm.Modes.Video;
	}

	vm.setQuery = function (query) {
		$http.get("Service/SetQuery?Query=" + encodeURIComponent(query));
	}

	vm.changeImage = function (offset) {
		$http.get("Service/ChangeImage?Offset=" + encodeURIComponent(offset));
	}

	vm.firstSetSlideshowImageDisplayTime = true;
	vm.setSlideshowImageDisplayTime = function (displayTime) {
		if (vm.firstSetSlideshowImageDisplayTime) {
			vm.firstSetSlideshowImageDisplayTime = false;
			return;
		}
		$http.get("Service/SetSlideshowImageDisplayTime?DisplayTime=" + encodeURIComponent(displayTime));
	}

	vm.queryFocus = function (target) {
		if (!target.value)
			target.value = vm.SlideshowQuery;
		target.select();
	}

	vm.toggleSlideshowImagesPaused = function () {
		$http.get("Service/ToggleSlideshowImagesPaused");
		vm.SlideshowImagesPaused = !vm.SlideshowImagesPaused;
	}

	vm.getStatus();
}

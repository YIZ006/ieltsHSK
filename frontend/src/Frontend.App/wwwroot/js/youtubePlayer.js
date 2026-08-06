let ytPlayer;
let ytDotNetHelper;
let ytProgressInterval;

window.initYouTubePlayer = function (videoId, dotNetHelper) {
    ytDotNetHelper = dotNetHelper;
    
    // Load YouTube API if not loaded
    if (!window.YT) {
        var tag = document.createElement('script');
        tag.src = "https://www.youtube.com/iframe_api";
        var firstScriptTag = document.getElementsByTagName('script')[0];
        firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);
        
        window.onYouTubeIframeAPIReady = function() {
            createPlayer(videoId);
        };
    } else {
        createPlayer(videoId);
    }
}

function createPlayer(videoId) {
    if (ytPlayer) {
        ytPlayer.destroy();
    }
    ytPlayer = new YT.Player('yt-hidden-player', {
        height: '0',
        width: '0',
        videoId: videoId,
        playerVars: {
            'playsinline': 1,
            'controls': 0,
            'disablekb': 1
        },
        events: {
            'onReady': onPlayerReady,
            'onStateChange': onPlayerStateChange
        }
    });
}

function onPlayerReady(event) {
    // Player is ready
    if (ytDotNetHelper) {
        ytDotNetHelper.invokeMethodAsync('OnPlayerReady', ytPlayer.getDuration());
    }
}

function onPlayerStateChange(event) {
    if (event.data == YT.PlayerState.PLAYING) {
        ytProgressInterval = setInterval(updateProgress, 1000);
        if (ytDotNetHelper) ytDotNetHelper.invokeMethodAsync('OnPlayerStateChanged', true);
    } else {
        clearInterval(ytProgressInterval);
        if (ytDotNetHelper) ytDotNetHelper.invokeMethodAsync('OnPlayerStateChanged', false);
    }
}

function updateProgress() {
    if (ytPlayer && ytPlayer.getCurrentTime && ytDotNetHelper) {
        let currentTime = ytPlayer.getCurrentTime();
        ytDotNetHelper.invokeMethodAsync('OnPlayerProgress', currentTime);
    }
}

window.playYouTube = function() {
    if (ytPlayer && ytPlayer.playVideo) {
        ytPlayer.playVideo();
    }
}

window.pauseYouTube = function() {
    if (ytPlayer && ytPlayer.pauseVideo) {
        ytPlayer.pauseVideo();
    }
}

window.seekYouTube = function(seconds) {
    if (ytPlayer && ytPlayer.seekTo) {
        ytPlayer.seekTo(seconds, true);
    }
}

window.setYouTubeVolume = function(volume) {
    if (ytPlayer && ytPlayer.setVolume) {
        ytPlayer.setVolume(volume);
    }
}

window.handleSeekClick = function(event) {
    if (!ytPlayer || !ytPlayer.getDuration) return;
    
    // Calculate progress percentage
    let rect = event.currentTarget.getBoundingClientRect();
    let x = event.clientX - rect.left;
    let width = rect.width;
    let percentage = x / width;
    
    // Seek
    let duration = ytPlayer.getDuration();
    ytPlayer.seekTo(duration * percentage, true);
}

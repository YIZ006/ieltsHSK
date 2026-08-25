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
        try { ytPlayer.destroy(); } catch (e) { }
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

// ═══════════════════════════════════════════════════════════════════════════
// YOUTUBE SHADOWING VIDEO INTEROP
// ═══════════════════════════════════════════════════════════════════════════
let shadowPlayer = null;
let shadowInterval = null;

window.initYouTubeShadowPlayer = function (elementId, videoId, dotNetHelper) {
    if (shadowInterval) clearInterval(shadowInterval);
    if (shadowPlayer) {
        try { shadowPlayer.destroy(); } catch (e) { }
    }

    const create = () => {
        shadowPlayer = new YT.Player(elementId, {
            height: '100%',
            width: '100%',
            videoId: videoId,
            playerVars: {
                'playsinline': 1,
                'controls': 1,
                'rel': 0,
                'modestbranding': 1
            },
            events: {
                'onReady': function () {
                    if (dotNetHelper) {
                        try { dotNetHelper.invokeMethodAsync('OnShadowPlayerReady'); } catch (e) { }
                    }
                }
            }
        });
    };

    if (!window.YT || !window.YT.Player) {
        var tag = document.createElement('script');
        tag.src = "https://www.youtube.com/iframe_api";
        var firstScriptTag = document.getElementsByTagName('script')[0];
        firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);
        window.onYouTubeIframeAPIReady = function() {
            create();
        };
    } else {
        create();
    }
};

window.playYouTubeSegment = function (startTime, endTime, rate, isLoop, dotNetHelper) {
    if (!shadowPlayer || !shadowPlayer.seekTo) return;
    if (shadowInterval) clearInterval(shadowInterval);

    if (rate && shadowPlayer.setPlaybackRate) {
        shadowPlayer.setPlaybackRate(rate);
    }

    // Lead in slightly (60ms) before the first word
    const safeStart = Math.max(0, startTime - 0.06);
    shadowPlayer.seekTo(safeStart, true);
    shadowPlayer.playVideo();

    // Natural speech tail padding (0.18s) to allow final consonants to sound completely
    const targetEnd = endTime + 0.18;

    shadowInterval = setInterval(() => {
        if (!shadowPlayer || !shadowPlayer.getCurrentTime) return;
        const cur = shadowPlayer.getCurrentTime();
        if (cur >= targetEnd) {
            if (isLoop) {
                shadowPlayer.seekTo(safeStart, true);
                shadowPlayer.playVideo();
            } else {
                clearInterval(shadowInterval);
                shadowPlayer.pauseVideo();
                if (dotNetHelper) {
                    try { dotNetHelper.invokeMethodAsync('OnSegmentPlaybackEnded'); } catch (e) { }
                }
            }
        }
    }, 40);
};

window.pauseYouTubeShadowPlayer = function () {
    if (shadowInterval) clearInterval(shadowInterval);
    if (shadowPlayer && shadowPlayer.pauseVideo) {
        shadowPlayer.pauseVideo();
    }
};

window.setYouTubeShadowRate = function (rate) {
    if (shadowPlayer && shadowPlayer.setPlaybackRate) {
        shadowPlayer.setPlaybackRate(rate);
    }
};

// ═══════════════════════════════════════════════════════════════════════════
// DIRECT MP3 AUDIO SHADOWING (ENGNOVATE / CAMBRIDGE IELTS NATIVE AUDIO)
// ═══════════════════════════════════════════════════════════════════════════
let nativeAudio = null;
let audioSegmentInterval = null;

window.initAudioShadowPlayer = function (audioElementId, audioUrl) {
    if (audioSegmentInterval) clearInterval(audioSegmentInterval);
    nativeAudio = document.getElementById(audioElementId);
    if (!nativeAudio) {
        nativeAudio = new Audio(audioUrl);
    } else if (audioUrl && nativeAudio.src !== audioUrl) {
        nativeAudio.src = audioUrl;
    }
};

window.playAudioSegment = function (startTime, endTime, rate, isLoop, dotNetHelper) {
    if (!nativeAudio) {
        nativeAudio = document.getElementById('engnovate-main-audio');
    }
    if (!nativeAudio) return;
    if (audioSegmentInterval) clearInterval(audioSegmentInterval);

    if (rate) {
        nativeAudio.playbackRate = rate;
    }

    const safeStart = Math.max(0, startTime);
    nativeAudio.currentTime = safeStart;
    nativeAudio.play().catch(e => console.log('Audio play error:', e));

    audioSegmentInterval = setInterval(() => {
        if (!nativeAudio) return;
        if (nativeAudio.currentTime >= endTime) {
            if (isLoop) {
                nativeAudio.currentTime = safeStart;
                nativeAudio.play().catch(e => console.log('Audio play error:', e));
            } else {
                clearInterval(audioSegmentInterval);
                nativeAudio.pause();
                if (dotNetHelper) {
                    try { dotNetHelper.invokeMethodAsync('OnSegmentPlaybackEnded'); } catch (e) { }
                }
            }
        }
    }, 25);
};

window.pauseAudioSegment = function () {
    if (audioSegmentInterval) clearInterval(audioSegmentInterval);
    if (nativeAudio) {
        nativeAudio.pause();
    }
};

window.setAudioSegmentRate = function (rate) {
    if (nativeAudio && rate) {
        nativeAudio.playbackRate = rate;
    }
};


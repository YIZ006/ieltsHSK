let ytPlayer;
let ytDotNetHelper;
let ytProgressInterval;
let isDirectAudio = false;
let directAudioPlayer = null;

window.initYouTubePlayer = function (urlOrId, dotNetHelper) {
    ytDotNetHelper = dotNetHelper;
    if (ytProgressInterval) {
        clearInterval(ytProgressInterval);
        ytProgressInterval = null;
    }

    // Stop and clean up any existing HTML5 audio player
    if (directAudioPlayer) {
        try {
            directAudioPlayer.pause();
            directAudioPlayer.src = "";
            directAudioPlayer.load();
        } catch (e) { }
        directAudioPlayer = null;
    }

    const isMp3OrAudio = urlOrId && (
        urlOrId.endsWith('.mp3') ||
        urlOrId.endsWith('.wav') ||
        urlOrId.endsWith('.ogg') ||
        urlOrId.endsWith('.m4a') ||
        urlOrId.includes('/audio/') ||
        urlOrId.startsWith('sample-data/') ||
        urlOrId.startsWith('blob:') ||
        (urlOrId.startsWith('http') && !urlOrId.includes('youtube.com') && !urlOrId.includes('youtu.be'))
    );

    if (isMp3OrAudio) {
        isDirectAudio = true;
        let audioSrc = urlOrId;
        if (!audioSrc.startsWith('http') && !audioSrc.startsWith('/') && !audioSrc.startsWith('blob:')) {
            audioSrc = '/' + audioSrc;
        }

        directAudioPlayer = new Audio(audioSrc);
        directAudioPlayer.preload = "auto";

        const notifyReady = () => {
            if (ytDotNetHelper && directAudioPlayer && directAudioPlayer.duration > 0) {
                ytDotNetHelper.invokeMethodAsync('OnPlayerReady', directAudioPlayer.duration);
            }
        };

        directAudioPlayer.addEventListener('loadedmetadata', notifyReady);
        directAudioPlayer.addEventListener('canplaythrough', notifyReady);

        directAudioPlayer.addEventListener('play', function () {
            if (ytProgressInterval) clearInterval(ytProgressInterval);
            ytProgressInterval = setInterval(updateProgress, 250);
            if (ytDotNetHelper) ytDotNetHelper.invokeMethodAsync('OnPlayerStateChanged', true);
        });

        directAudioPlayer.addEventListener('pause', function () {
            if (ytProgressInterval) clearInterval(ytProgressInterval);
            if (ytDotNetHelper) ytDotNetHelper.invokeMethodAsync('OnPlayerStateChanged', false);
        });

        directAudioPlayer.addEventListener('ended', function () {
            if (ytProgressInterval) clearInterval(ytProgressInterval);
            if (ytDotNetHelper) ytDotNetHelper.invokeMethodAsync('OnPlayerStateChanged', false);
        });

        if (directAudioPlayer.readyState >= 1) {
            notifyReady();
        }

        window.initProgressBarClick();
        return;
    }

    isDirectAudio = false;
    let videoId = urlOrId;
    if (urlOrId && (urlOrId.includes('http://') || urlOrId.includes('https://'))) {
        videoId = urlOrId.split('/').pop().split('?')[0];
    }

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

    window.initProgressBarClick();
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
    if (ytDotNetHelper && ytPlayer && ytPlayer.getDuration) {
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
    if (isDirectAudio && directAudioPlayer && ytDotNetHelper) {
        ytDotNetHelper.invokeMethodAsync('OnPlayerProgress', directAudioPlayer.currentTime);
    } else if (ytPlayer && ytPlayer.getCurrentTime && ytDotNetHelper) {
        let currentTime = ytPlayer.getCurrentTime();
        ytDotNetHelper.invokeMethodAsync('OnPlayerProgress', currentTime);
    }
}

window.playYouTube = function() {
    if (isDirectAudio && directAudioPlayer) {
        directAudioPlayer.play().catch(e => console.warn('Direct audio play error:', e));
    } else if (ytPlayer && ytPlayer.playVideo) {
        ytPlayer.playVideo();
    }
}

window.pauseYouTube = function() {
    if (isDirectAudio && directAudioPlayer) {
        directAudioPlayer.pause();
    } else if (ytPlayer && ytPlayer.pauseVideo) {
        ytPlayer.pauseVideo();
    }
}

window.seekYouTube = function(seconds) {
    if (isDirectAudio && directAudioPlayer) {
        directAudioPlayer.currentTime = seconds;
        if (ytDotNetHelper) ytDotNetHelper.invokeMethodAsync('OnPlayerProgress', directAudioPlayer.currentTime);
    } else if (ytPlayer && ytPlayer.seekTo) {
        ytPlayer.seekTo(seconds, true);
    }
}

window.setYouTubeVolume = function(volume) {
    if (isDirectAudio && directAudioPlayer) {
        directAudioPlayer.volume = Math.max(0, Math.min(1, volume / 100));
    } else if (ytPlayer && ytPlayer.setVolume) {
        ytPlayer.setVolume(volume);
    }
}

window.initProgressBarClick = function() {
    setTimeout(function() {
        const container = document.querySelector('.progress-container');
        if (container && !container.dataset.boundClick) {
            container.dataset.boundClick = "true";
            container.addEventListener('click', function(e) {
                const rect = container.getBoundingClientRect();
                const clickX = e.clientX - rect.left;
                const ratio = Math.max(0, Math.min(1, clickX / rect.width));
                if (isDirectAudio && directAudioPlayer && directAudioPlayer.duration) {
                    const targetTime = ratio * directAudioPlayer.duration;
                    directAudioPlayer.currentTime = targetTime;
                    if (ytDotNetHelper) ytDotNetHelper.invokeMethodAsync('OnPlayerProgress', targetTime);
                } else if (ytPlayer && ytPlayer.getDuration) {
                    const targetTime = ratio * ytPlayer.getDuration();
                    ytPlayer.seekTo(targetTime, true);
                    if (ytDotNetHelper) ytDotNetHelper.invokeMethodAsync('OnPlayerProgress', targetTime);
                }
            });
        }
    }, 100);
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


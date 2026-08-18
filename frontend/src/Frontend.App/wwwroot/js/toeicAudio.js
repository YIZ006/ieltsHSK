window.toeicAudio = (() => {
    let player;

    const stop = () => {
        if (!player) return;
        player.onended = null;
        player.pause();
        player = null;
    };

    const play = async (url, dotNetHelper) => {
        stop();
        player = new Audio(url);
        player.preload = 'auto';
        player.onended = () => dotNetHelper.invokeMethodAsync('OnToeicAudioEnded');
        try {
            await player.play();
        } catch (error) {
            console.warn('Unable to start TOEIC audio.', error);
        }
    };

    return { play, stop };
})();

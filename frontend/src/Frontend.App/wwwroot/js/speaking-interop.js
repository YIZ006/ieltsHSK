// speaking-interop.js — IELTS Speaking Interop Module
window.SpeakingInterop = (() => {
    let mediaStream = null;
    let mediaRecorder = null;
    let audioChunks = [];
    let recognition = null;
    let transcript = '';
    let dotNetRef = null;
    let audioContext = null;
    let analyser = null;
    let animFrameId = null;
    let recordingStartTime = null;
    let waveformCanvas = null;

    // ══════════════════════════════════════════
    // DEVICE SETUP
    // ══════════════════════════════════════════

    async function requestMicPermission() {
        try {
            mediaStream = await navigator.mediaDevices.getUserMedia({ audio: true, video: false });
            return { success: true };
        } catch (err) {
            return { success: false, error: err.name };
        }
    }

    function startMicVisualizer(canvasId) {
        if (!mediaStream) return;
        waveformCanvas = document.getElementById(canvasId);
        if (!waveformCanvas) return;

        audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const source = audioContext.createMediaStreamSource(mediaStream);
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 256;
        source.connect(analyser);

        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);
        const ctx = waveformCanvas.getContext('2d');

        function draw() {
            animFrameId = requestAnimationFrame(draw);
            analyser.getByteFrequencyData(dataArray);

            const w = waveformCanvas.width;
            const h = waveformCanvas.height;
            ctx.clearRect(0, 0, w, h);

            const barWidth = (w / bufferLength) * 2.5;
            let x = 0;
            for (let i = 0; i < bufferLength; i++) {
                const barH = (dataArray[i] / 255) * h;
                const r = 232, g = 62, b = 140;
                ctx.fillStyle = `rgba(${r},${g},${b},0.85)`;
                ctx.fillRect(x, h - barH, barWidth, barH);
                x += barWidth + 1;
            }
        }
        draw();
    }

    function stopVisualizer() {
        if (animFrameId) cancelAnimationFrame(animFrameId);
        if (audioContext) { audioContext.close(); audioContext = null; }
        if (waveformCanvas) {
            const ctx = waveformCanvas.getContext('2d');
            ctx.clearRect(0, 0, waveformCanvas.width, waveformCanvas.height);
        }
    }

    // Tạo âm thanh test loa bằng Web Audio API Oscillator (không cần file)
    async function playTestAudio() {
        return new Promise(async (resolve) => {
            try {
                const ctx = new (window.AudioContext || window.webkitAudioContext)();

                // Chuỗi nốt: Do Mi Sol Mi Do (dễ nhận biết)
                const notes = [
                    { freq: 523.25, dur: 0.18 }, // C5
                    { freq: 659.25, dur: 0.18 }, // E5
                    { freq: 783.99, dur: 0.22 }, // G5
                    { freq: 659.25, dur: 0.18 }, // E5
                    { freq: 523.25, dur: 0.30 }, // C5 (dài hơn)
                ];

                let time = ctx.currentTime + 0.05; // nhỏ delay để tránh click

                for (const note of notes) {
                    const osc = ctx.createOscillator();
                    const gain = ctx.createGain();

                    osc.type = 'sine';
                    osc.frequency.setValueAtTime(note.freq, time);

                    // Envelope: attack 10ms, sustain, release 50ms
                    gain.gain.setValueAtTime(0, time);
                    gain.gain.linearRampToValueAtTime(0.6, time + 0.01);
                    gain.gain.setValueAtTime(0.6, time + note.dur - 0.05);
                    gain.gain.linearRampToValueAtTime(0, time + note.dur);

                    osc.connect(gain);
                    gain.connect(ctx.destination);

                    osc.start(time);
                    osc.stop(time + note.dur);

                    time += note.dur + 0.04; // gap giữa các nốt
                }

                // Đợi chuỗi nốt phát xong
                const totalDuration = notes.reduce((s, n) => s + n.dur + 0.04, 0.1) * 1000;
                setTimeout(() => {
                    ctx.close();
                    resolve(true);
                }, totalDuration);

            } catch (err) {
                console.error('Speaker test error:', err);
                resolve(false);
            }
        });
    }

    async function recordMicTest(seconds) {
        if (!mediaStream) return null;
        return new Promise(resolve => {
            const chunks = [];
            const rec = new MediaRecorder(mediaStream);
            rec.ondataavailable = e => chunks.push(e.data);
            rec.onstop = () => {
                const blob = new Blob(chunks, { type: 'audio/webm' });
                const url = URL.createObjectURL(blob);
                resolve(url);
            };
            rec.start();
            setTimeout(() => rec.stop(), seconds * 1000);
        });
    }

    // ══════════════════════════════════════════
    // RECORDING + SPEECH RECOGNITION
    // ══════════════════════════════════════════

    function startRecording(canvasId, netRef) {
        if (!mediaStream) return false;
        dotNetRef = netRef;
        transcript = '';
        audioChunks = [];
        recordingStartTime = Date.now();

        // MediaRecorder
        mediaRecorder = new MediaRecorder(mediaStream);
        mediaRecorder.ondataavailable = e => audioChunks.push(e.data);
        mediaRecorder.start(100);

        // Web Speech API
        const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
        if (SpeechRecognition) {
            recognition = new SpeechRecognition();
            recognition.lang = 'en-US';
            recognition.continuous = true;
            recognition.interimResults = true;

            recognition.onresult = (event) => {
                let interim = '';
                let final = '';
                for (let i = event.resultIndex; i < event.results.length; i++) {
                    if (event.results[i].isFinal) final += event.results[i][0].transcript + ' ';
                    else interim += event.results[i][0].transcript;
                }
                if (final) transcript += final;
                const display = transcript + interim;
                dotNetRef.invokeMethodAsync('OnTranscriptUpdate', display.trim());
            };

            recognition.onerror = (e) => {
                if (e.error !== 'no-speech') console.warn('Speech recognition error:', e.error);
            };

            recognition.start();
        }

        // Waveform during recording
        startRecordingWaveform(canvasId);
        return true;
    }

    function startRecordingWaveform(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || !mediaStream) return;

        if (audioContext && audioContext.state !== 'closed') {
            audioContext.close();
        }
        audioContext = new (window.AudioContext || window.webkitAudioContext)();
        const source = audioContext.createMediaStreamSource(mediaStream);
        analyser = audioContext.createAnalyser();
        analyser.fftSize = 512;
        source.connect(analyser);

        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);
        const ctx = canvas.getContext('2d');
        const w = canvas.width, h = canvas.height;

        function draw() {
            animFrameId = requestAnimationFrame(draw);
            analyser.getByteTimeDomainData(dataArray);
            ctx.fillStyle = 'rgba(17, 24, 39, 0.85)';
            ctx.fillRect(0, 0, w, h);
            ctx.lineWidth = 2;
            ctx.strokeStyle = '#f472b6';
            ctx.beginPath();
            const sliceWidth = w / bufferLength;
            let x = 0;
            for (let i = 0; i < bufferLength; i++) {
                const v = dataArray[i] / 128.0;
                const y = (v * h) / 2;
                i === 0 ? ctx.moveTo(x, y) : ctx.lineTo(x, y);
                x += sliceWidth;
            }
            ctx.lineTo(w, h / 2);
            ctx.stroke();
        }
        draw();
    }

    function stopRecording() {
        return new Promise(resolve => {
            stopVisualizer();
            const durationMs = Date.now() - (recordingStartTime || Date.now());

            if (recognition) { try { recognition.stop(); } catch (e) {} recognition = null; }

            if (mediaRecorder && mediaRecorder.state !== 'inactive') {
                mediaRecorder.onstop = () => {
                    resolve({ transcript: transcript.trim(), durationMs });
                };
                mediaRecorder.stop();
            } else {
                resolve({ transcript: transcript.trim(), durationMs });
            }
        });
    }

    // ══════════════════════════════════════════
    // RULE-BASED EVALUATION
    // ══════════════════════════════════════════

    function evaluate(transcript, durationMs) {
        const durationSec = Math.max(durationMs / 1000, 1);
        const raw = transcript.trim().toLowerCase();
        const words = raw.split(/\s+/).filter(w => w.length > 1);
        const wordCount = words.length;

        if (wordCount < 3) {
            return { fluency: 0, lexical: 0, grammar: 0, coherence: 0, overall: 0, wpm: 0, details: {} };
        }

        const wpm = Math.round((wordCount / durationSec) * 60);

        // 1. FLUENCY — wpm + filler words
        const fillers = new Set(['um', 'uh', 'uhh', 'hmm', 'er', 'err', 'like', 'basically', 'literally', 'actually']);
        const fillerCount = words.filter(w => fillers.has(w)).length;
        const fillerRatio = fillerCount / wordCount;
        let fluency = 0;
        if (wpm >= 140) fluency = 9;
        else if (wpm >= 120) fluency = 8;
        else if (wpm >= 100) fluency = 7;
        else if (wpm >= 80) fluency = 6;
        else if (wpm >= 60) fluency = 5;
        else if (wpm >= 40) fluency = 4;
        else fluency = 3;
        fluency = Math.max(1, fluency - Math.round(fillerRatio * 10));

        // 2. LEXICAL RESOURCE — unique word ratio + advanced vocab
        const uniqueWords = new Set(words);
        const ttr = uniqueWords.size / wordCount;
        const advancedVocab = [
            'significant', 'substantial', 'fundamental', 'consequently', 'approximately',
            'demonstrate', 'indicate', 'contribute', 'establish', 'maintain', 'achieve',
            'develop', 'involve', 'require', 'consider', 'various', 'particularly',
            'generally', 'certainly', 'extremely', 'absolutely', 'definitely',
            'beneficial', 'essential', 'crucial', 'effective', 'efficient', 'relevant',
            'moreover', 'furthermore', 'nevertheless', 'despite', 'whereas', 'whilst'
        ];
        const advancedCount = words.filter(w => advancedVocab.includes(w)).length;
        let lexical = 3;
        if (ttr > 0.8) lexical = 9;
        else if (ttr > 0.7) lexical = 8;
        else if (ttr > 0.6) lexical = 7;
        else if (ttr > 0.5) lexical = 6;
        else if (ttr > 0.4) lexical = 5;
        else if (ttr > 0.3) lexical = 4;
        lexical = Math.min(9, lexical + Math.floor(advancedCount / 2));

        // 3. GRAMMAR — sentence complexity + variety
        const sentences = transcript.split(/[.!?]+/).filter(s => s.trim().length > 0);
        const avgSentLen = wordCount / Math.max(sentences.length, 1);
        const complexConnectors = ['although', 'because', 'since', 'while', 'whereas',
            'which', 'who', 'that', 'if', 'unless', 'until', 'when', 'after', 'before',
            'as soon as', 'in order to', 'so that', 'provided that'];
        const complexCount = complexConnectors.filter(c => raw.includes(c)).length;
        let grammar = 4;
        if (avgSentLen > 20) grammar = 8;
        else if (avgSentLen > 15) grammar = 7;
        else if (avgSentLen > 10) grammar = 6;
        else if (avgSentLen > 7) grammar = 5;
        grammar = Math.min(9, grammar + Math.floor(complexCount / 2));

        // 4. COHERENCE — discourse markers
        const markers = [
            'however', 'furthermore', 'moreover', 'therefore', 'consequently',
            'in addition', 'on the other hand', 'in contrast', 'for example',
            'for instance', 'as a result', 'in conclusion', 'to begin with',
            'firstly', 'secondly', 'finally', 'in my opinion', 'personally',
            'i believe', 'i think', 'i feel that', 'it seems', 'to be honest',
            'what i mean is', 'that is to say', 'in other words'
        ];
        const markerCount = markers.filter(m => raw.includes(m)).length;
        let coherence = 3;
        if (markerCount >= 5) coherence = 9;
        else if (markerCount >= 4) coherence = 8;
        else if (markerCount >= 3) coherence = 7;
        else if (markerCount >= 2) coherence = 6;
        else if (markerCount >= 1) coherence = 5;

        const overall = Math.round((fluency + lexical + grammar + coherence) / 4 * 2) / 2;

        return {
            fluency, lexical, grammar, coherence, overall,
            wpm, wordCount,
            details: {
                fillerCount, ttr: Math.round(ttr * 100),
                advancedCount, complexCount, markerCount,
                sentenceCount: sentences.length,
                markers: markers.filter(m => raw.includes(m)).slice(0, 5),
                fillerWords: words.filter(w => fillers.has(w)).slice(0, 5)
            }
        };
    }

    // ══════════════════════════════════════════
    // VIDEO HELPERS
    // ══════════════════════════════════════════

    function bindVideoEnded(videoId, netRef) {
        const vid = document.getElementById(videoId);
        if (!vid) return;
        vid.onended = () => netRef.invokeMethodAsync('OnVideoEnded');
    }

    function stopMicStream() {
        if (mediaStream) {
            mediaStream.getTracks().forEach(t => t.stop());
            mediaStream = null;
        }
        stopVisualizer();
    }

    return {
        requestMicPermission,
        startMicVisualizer,
        stopVisualizer,
        playTestAudio,
        recordMicTest,
        startRecording,
        stopRecording,
        evaluate,
        bindVideoEnded,
        stopMicStream
    };
})();

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

    function stopMediaStream() {
        if (mediaStream) {
            mediaStream.getTracks().forEach(track => track.stop());
            mediaStream = null;
        }
    }

    function microphoneConstraints(deviceId) {
        const constraints = {
            echoCancellation: true,
            noiseSuppression: true,
            autoGainControl: true
        };
        if (deviceId) constraints.deviceId = { exact: deviceId };
        return constraints;
    }

    async function openMicrophone(deviceId) {
        try {
            stopVisualizer();
            stopMediaStream();
            mediaStream = await navigator.mediaDevices.getUserMedia({
                audio: microphoneConstraints(deviceId),
                video: false
            });
            const track = mediaStream.getAudioTracks()[0];
            return { success: true, deviceId: track?.getSettings().deviceId || deviceId || '' };
        } catch (err) {
            return { success: false, error: err.name || 'NotReadableError' };
        }
    }

    async function requestMicPermission() {
        return openMicrophone();
    }

    async function selectMicrophone(deviceId) {
        return openMicrophone(deviceId);
    }

    async function getMicrophones() {
        const devices = await navigator.mediaDevices.enumerateDevices();
        let index = 0;
        return devices
            .filter(device => device.kind === 'audioinput')
            .map(device => ({
                deviceId: device.deviceId,
                label: device.label || `Microphone ${++index}`
            }));
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

    let currentRecordedBlob = null;
    let selectedDeviceId = '';

    // ══════════════════════════════════════════
    // RECORDING + SPEECH RECOGNITION
    // ══════════════════════════════════════════

    async function startRecording(canvasId, netRef, deviceId) {
        dotNetRef = netRef;
        transcript = '';
        audioChunks = [];
        currentRecordedBlob = null;
        recordingStartTime = Date.now();

        // Ensure active microphone mediaStream
        if (!mediaStream || mediaStream.getAudioTracks().length === 0 || mediaStream.getAudioTracks().every(t => t.readyState === 'ended')) {
            const micRes = await openMicrophone(deviceId || selectedDeviceId);
            if (!micRes.success) {
                console.error('Failed to open microphone for recording:', micRes.error);
                return false;
            }
        }

        try {
            // MediaRecorder
            const mimeType = MediaRecorder.isTypeSupported('audio/webm;codecs=opus')
                ? 'audio/webm;codecs=opus'
                : (MediaRecorder.isTypeSupported('audio/webm') ? 'audio/webm' : (MediaRecorder.isTypeSupported('audio/mp4') ? 'audio/mp4' : ''));
            
            mediaRecorder = mimeType ? new MediaRecorder(mediaStream, { mimeType }) : new MediaRecorder(mediaStream);
            mediaRecorder.ondataavailable = e => {
                if (e.data && e.data.size > 0) {
                    audioChunks.push(e.data);
                }
            };
            mediaRecorder.start(100);
        } catch (recErr) {
            console.warn('MediaRecorder error:', recErr);
        }

        // Web Speech API
        try {
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
                    if (dotNetRef) {
                        dotNetRef.invokeMethodAsync('OnTranscriptUpdate', display.trim());
                    }
                };

                recognition.onerror = (e) => {
                    console.warn('Web Speech API note:', e.error);
                };

                recognition.start();
            }
        } catch (speechErr) {
            console.warn('SpeechRecognition initialization note:', speechErr);
        }

        // Waveform during recording
        startRecordingWaveform(canvasId);
        return true;
    }

    function startRecordingWaveform(canvasId) {
        const canvas = document.getElementById(canvasId);
        if (!canvas || !mediaStream) return;

        if (audioContext && audioContext.state !== 'closed') {
            try { audioContext.close(); } catch (e) {}
        }
        try {
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
        } catch (e) {
            console.warn('Waveform audio context error:', e);
        }
    }

    function stopRecording() {
        return new Promise(resolve => {
            stopVisualizer();
            const durationMs = Date.now() - (recordingStartTime || Date.now());

            if (recognition) {
                try { recognition.stop(); } catch (e) {}
                recognition = null;
            }

            if (mediaRecorder && mediaRecorder.state !== 'inactive') {
                mediaRecorder.onstop = () => {
                    currentRecordedBlob = new Blob(audioChunks, {
                        type: mediaRecorder.mimeType || 'audio/webm'
                    });
                    resolve({
                        transcript: transcript.trim(),
                        durationMs,
                        hasAudioBlob: currentRecordedBlob.size > 0,
                        blobSize: currentRecordedBlob.size
                    });
                };
                mediaRecorder.stop();
            } else {
                currentRecordedBlob = new Blob(audioChunks, { type: 'audio/webm' });
                resolve({
                    transcript: transcript.trim(),
                    durationMs,
                    hasAudioBlob: currentRecordedBlob.size > 0,
                    blobSize: currentRecordedBlob.size
                });
            }
        });
    }

    async function uploadCurrentRecording(uploadUrl, authToken, questionId, partNumber, durationMs, sessionId, examUrl) {
        if (!currentRecordedBlob || currentRecordedBlob.size === 0) {
            return { success: false, error: 'No audio recorded' };
        }

        try {
            const formData = new FormData();
            formData.append('audioFile', currentRecordedBlob, `recording_${questionId || Date.now()}.webm`);
            formData.append('questionId', questionId || 0);
            formData.append('partNumber', partNumber || 1);
            formData.append('durationMs', Math.round(durationMs || 0));
            formData.append('sessionId', sessionId || '');
            formData.append('examUrl', examUrl || '');
            formData.append('transcript', transcript.trim());

            const headers = {};
            if (authToken) {
                headers['Authorization'] = `Bearer ${authToken}`;
            }

            const res = await fetch(uploadUrl, {
                method: 'POST',
                headers: headers,
                body: formData
            });

            if (res.ok) {
                const data = await res.json();
                return { success: true, data };
            } else {
                const errText = await res.text();
                return { success: false, error: errText };
            }
        } catch (err) {
            console.error('Audio upload error:', err);
            return { success: false, error: err.message || 'Network error' };
        }
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
            return {
                fluency: 0, lexical: 0, grammar: 0, coherence: 0, overall: 0,
                wpm: 0, wordCount: 0,
                details: {
                    fillerCount: 0, ttr: 0, advancedCount: 0,
                    complexCount: 0, markerCount: 0, sentenceCount: 0,
                    markers: [], fillerWords: []
                }
            };
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

    function playVideo(videoId) {
        const vid = document.getElementById(videoId);
        if (vid) {
            vid.currentTime = 0;
            vid.play().catch(e => console.warn('Video play error:', e));
        }
    }

    function stopMicStream() {
        stopVisualizer();
        stopMediaStream();
    }

    return {
        requestMicPermission,
        selectMicrophone,
        getMicrophones,
        startMicVisualizer,
        stopVisualizer,
        playTestAudio,
        recordMicTest,
        startRecording,
        stopRecording,
        evaluate,
        bindVideoEnded,
        playVideo,
        stopMicStream,
        uploadCurrentRecording
    };
})();

// SpeakAlongInterop - Real Web Speech API, MediaRecorder, Karaoke Sync & Visualizer
window.SpeakAlongInterop = {
    _recognition: null,
    _isRecording: false,
    _dotNetRef: null,
    _mediaRecorder: null,
    _audioChunks: [],
    _finalTranscript: '',
    _startTime: 0,
    _activeUtterance: null,
    _audioCtx: null,
    _analyser: null,
    _animFrame: null,
    _waveformCanvas: null,

    // Text-to-Speech with Karaoke Word Sync & Rate control
    speakTextWithKaraoke: function (text, lang, rate, dotNetRef) {
        return new Promise((resolve) => {
            if (!window.speechSynthesis) {
                resolve(false);
                return;
            }

            // Cancel ongoing TTS
            window.speechSynthesis.cancel();

            if (!text || text.trim() === '') {
                resolve(true);
                return;
            }

            const utterance = new SpeechSynthesisUtterance(text);
            utterance.lang = lang || 'en-US';
            utterance.rate = rate || 1.0;
            utterance.pitch = 1.0;

            const voices = window.speechSynthesis.getVoices();
            const englishVoice = voices.find(v => v.lang.startsWith('en') && 
                (v.name.includes('Google') || v.name.includes('Natural') || v.name.includes('Samantha') || v.name.includes('US') || v.name.includes('Jenny') || v.name.includes('Guy')));
            if (englishVoice) {
                utterance.voice = englishVoice;
            }

            // Word Boundary for Karaoke sync
            if (dotNetRef) {
                utterance.onboundary = function (event) {
                    if (event.name === 'word') {
                        try {
                            dotNetRef.invokeMethodAsync('OnKaraokeWordBoundary', event.charIndex, event.charLength || 0);
                        } catch (err) { }
                    }
                };
            }

            utterance.onend = function () {
                window.SpeakAlongInterop._activeUtterance = null;
                if (dotNetRef) {
                    try { dotNetRef.invokeMethodAsync('OnKaraokeEnded'); } catch (err) { }
                }
                resolve(true);
            };

            utterance.onerror = function (e) {
                console.warn('TTS error:', e);
                window.SpeakAlongInterop._activeUtterance = null;
                if (dotNetRef) {
                    try { dotNetRef.invokeMethodAsync('OnKaraokeEnded'); } catch (err) { }
                }
                resolve(false);
            };

            window.SpeakAlongInterop._activeUtterance = utterance;
            window.speechSynthesis.speak(utterance);
        });
    },

    speakText: function (text, lang, rate) {
        return this.speakTextWithKaraoke(text, lang, rate, null);
    },

    stopSpeaking: function () {
        if (window.speechSynthesis) {
            window.speechSynthesis.cancel();
        }
        this._activeUtterance = null;
    },

    // Start recording audio + real-time speech recognition + live wave visualizer
    startRecording: function (dotNetRef, deviceId, canvasId) {
        this._dotNetRef = dotNetRef;
        this._isRecording = true;
        this._audioChunks = [];
        this._finalTranscript = '';
        this._startTime = Date.now();

        // Stop any ongoing TTS
        this.stopSpeaking();

        return new Promise((resolve) => {
            // 1. Initialize SpeechRecognition
            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (SpeechRecognition) {
                try {
                    this._recognition = new SpeechRecognition();
                    this._recognition.lang = 'en-US';
                    this._recognition.continuous = true;
                    this._recognition.interimResults = true;
                    this._recognition.maxAlternatives = 1;

                    this._recognition.onresult = (event) => {
                        let interim = '';
                        let currentFinal = '';
                        let totalConfidence = 0;
                        let confidenceCount = 0;

                        for (let i = event.resultIndex; i < event.results.length; ++i) {
                            if (event.results[i][0].confidence > 0) {
                                totalConfidence += event.results[i][0].confidence;
                                confidenceCount++;
                            }
                            if (event.results[i].isFinal) {
                                currentFinal += event.results[i][0].transcript + ' ';
                            } else {
                                interim += event.results[i][0].transcript;
                            }
                        }
                        if (currentFinal) {
                            this._finalTranscript += currentFinal;
                        }
                        const liveText = (this._finalTranscript + ' ' + interim).trim();
                        this._lastFullTranscript = liveText;
                        if (confidenceCount > 0) {
                            this._lastConfidence = totalConfidence / confidenceCount;
                        }

                        if (this._dotNetRef) {
                            this._dotNetRef.invokeMethodAsync('OnSpeechRecognized', liveText);
                        }
                    };

                    this._recognition.onerror = (e) => {
                        console.warn('SpeechRecognition error:', e.error);
                    };

                    this._recognition.start();
                } catch (e) {
                    console.warn('SpeechRecognition failed to start:', e);
                }
            }

            // 2. Initialize MediaRecorder & Live Sound Waveform Visualizer
            if (navigator.mediaDevices && navigator.mediaDevices.getUserMedia) {
                const constraints = {
                    echoCancellation: true,
                    noiseSuppression: true,
                    autoGainControl: true
                };
                if (deviceId) {
                    constraints.deviceId = { exact: deviceId };
                }

                navigator.mediaDevices.getUserMedia({ audio: constraints })
                    .then((stream) => {
                        this._mediaRecorder = new MediaRecorder(stream);
                        this._mediaRecorder.ondataavailable = (event) => {
                            if (event.data && event.data.size > 0) {
                                this._audioChunks.push(event.data);
                            }
                        };
                        this._mediaRecorder.start(100);

                        // Start visualizer on canvas
                        this.startLiveWaveform(stream, canvasId || 'active-recording-waveform');

                        resolve(true);
                    })
                    .catch((err) => {
                        console.error('Mic access error:', err);
                        resolve(false);
                    });
            } else {
                resolve(true);
            }
        });
    },

    startLiveWaveform: function (stream, canvasId) {
        this.stopLiveWaveform();
        this._waveformCanvas = document.getElementById(canvasId);
        if (!this._waveformCanvas) return;

        try {
            this._audioCtx = new (window.AudioContext || window.webkitAudioContext)();
            const source = this._audioCtx.createMediaStreamSource(stream);
            this._analyser = this._audioCtx.createAnalyser();
            this._analyser.fftSize = 256;
            source.connect(this._analyser);

            const bufferLength = this._analyser.frequencyBinCount;
            const dataArray = new Uint8Array(bufferLength);
            const ctx = this._waveformCanvas.getContext('2d');

            const draw = () => {
                this._animFrame = requestAnimationFrame(draw);
                this._analyser.getByteFrequencyData(dataArray);

                const w = this._waveformCanvas.width;
                const h = this._waveformCanvas.height;
                ctx.clearRect(0, 0, w, h);

                const barWidth = (w / bufferLength) * 2.2;
                let x = 0;
                for (let i = 0; i < bufferLength; i++) {
                    const barH = (dataArray[i] / 255) * h;
                    // Gradient: Blue to Cyan/Emerald
                    const gradient = ctx.createLinearGradient(0, h, 0, 0);
                    gradient.addColorStop(0, '#0284c7');
                    gradient.addColorStop(1, '#06b6d4');

                    ctx.fillStyle = gradient;
                    ctx.beginPath();
                    ctx.roundRect(x, h - barH, barWidth, barH, [4, 4, 0, 0]);
                    ctx.fill();

                    x += barWidth + 2;
                }
            };
            draw();
        } catch (e) {
            console.warn('Waveform visualizer failed:', e);
        }
    },

    stopLiveWaveform: function () {
        if (this._animFrame) cancelAnimationFrame(this._animFrame);
        if (this._audioCtx) {
            try { this._audioCtx.close(); } catch (e) { }
            this._audioCtx = null;
        }
        if (this._waveformCanvas) {
            const ctx = this._waveformCanvas.getContext('2d');
            ctx.clearRect(0, 0, this._waveformCanvas.width, this._waveformCanvas.height);
        }
    },

    // Stop recording and return payload: transcript + audio playback url + duration
    stopRecording: function () {
        return new Promise((resolve) => {
            const durationSec = (Date.now() - this._startTime) / 1000.0;
            this.stopLiveWaveform();

            // Stop SpeechRecognition
            if (this._recognition) {
                try {
                    this._recognition.stop();
                } catch (e) { }
            }

            // Stop MediaRecorder and build Audio URL
            const resultText = (this._lastFullTranscript || this._finalTranscript).trim();
            const confidence = this._lastConfidence || 0.85;

            if (this._mediaRecorder && this._mediaRecorder.state !== 'inactive') {
                this._mediaRecorder.onstop = () => {
                    let audioUrl = '';
                    if (this._audioChunks.length > 0) {
                        const audioBlob = new Blob(this._audioChunks, { type: 'audio/webm' });
                        audioUrl = URL.createObjectURL(audioBlob);
                    }
                    this.stopMicStream();

                    resolve(JSON.stringify({
                        transcript: resultText,
                        audioUrl: audioUrl,
                        durationSeconds: durationSec,
                        confidence: confidence
                    }));
                };
                this._mediaRecorder.stop();
            } else {
                this.stopMicStream();
                resolve(JSON.stringify({
                    transcript: resultText,
                    audioUrl: '',
                    durationSeconds: durationSec,
                    confidence: confidence
                }));
            }
        });
    },

    stopMicStream: function () {
        if (this._mediaRecorder && this._mediaRecorder.stream) {
            this._mediaRecorder.stream.getTracks().forEach(track => track.stop());
        }
        this._isRecording = false;
    }
};
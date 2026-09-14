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
    _karaokeTimers: [],

    // Text-to-Speech with Karaoke Word Sync & Rate control
    speakTextWithKaraoke: function (text, lang, rate, dotNetRef) {
        return new Promise((resolve) => {
            if (!window.speechSynthesis) {
                resolve(false);
                return;
            }

            // Cancel ongoing TTS & clear any pending karaoke timers
            window.speechSynthesis.cancel();
            if (window.SpeakAlongInterop._karaokeTimers) {
                window.SpeakAlongInterop._karaokeTimers.forEach(t => clearTimeout(t));
            }
            window.SpeakAlongInterop._karaokeTimers = [];

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

            // --- Fallback timer-based karaoke ---
            // Chrome/Cốc Cốc thường không fire 'word' boundary.
            // Tính tốc độ đọc: ~150 wpm ở rate=1.0, mỗi từ ~160ms/ký tự trung bình 5 ký tự.
            let boundaryFired = false;
            const setupFallbackKaraoke = () => {
                if (boundaryFired || !dotNetRef) return;
                const words = text.trim().split(/\s+/);
                const AVG_CHARS_PER_SEC = 14 * (rate || 1.0); // ~14 chars/sec at 1x
                let charOffset = 0;
                let timeMs = 100; // small initial delay for TTS to start
                words.forEach((word, idx) => {
                    const wordDuration = (word.length / AVG_CHARS_PER_SEC) * 1000;
                    const t = setTimeout(() => {
                        try { dotNetRef.invokeMethodAsync('OnKaraokeWordBoundary', charOffset, word.length); } catch (e) { }
                    }, timeMs);
                    window.SpeakAlongInterop._karaokeTimers.push(t);
                    charOffset += word.length + 1; // +1 for space
                    timeMs += wordDuration;
                });
            };

            if (dotNetRef) {
                utterance.onboundary = function (event) {
                    if (event.name === 'word') {
                        boundaryFired = true;
                        try {
                            dotNetRef.invokeMethodAsync('OnKaraokeWordBoundary', event.charIndex, event.charLength || 0);
                        } catch (err) { }
                    }
                };

                // If no boundary event fires within 600ms → use fallback
                const fallbackCheckTimer = setTimeout(() => {
                    if (!boundaryFired) {
                        console.log('[TTS] onboundary not supported, using timer-based karaoke fallback');
                        setupFallbackKaraoke();
                    }
                }, 600);
                window.SpeakAlongInterop._karaokeTimers.push(fallbackCheckTimer);
            }

            utterance.onend = function () {
                window.SpeakAlongInterop._activeUtterance = null;
                if (window.SpeakAlongInterop._karaokeTimers) {
                    window.SpeakAlongInterop._karaokeTimers.forEach(t => clearTimeout(t));
                    window.SpeakAlongInterop._karaokeTimers = [];
                }
                if (dotNetRef) {
                    try { dotNetRef.invokeMethodAsync('OnKaraokeEnded'); } catch (err) { }
                }
                resolve(true);
            };

            utterance.onerror = function (e) {
                console.warn('TTS error:', e);
                window.SpeakAlongInterop._activeUtterance = null;
                if (window.SpeakAlongInterop._karaokeTimers) {
                    window.SpeakAlongInterop._karaokeTimers.forEach(t => clearTimeout(t));
                    window.SpeakAlongInterop._karaokeTimers = [];
                }
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
        if (this._karaokeTimers) {
            this._karaokeTimers.forEach(t => clearTimeout(t));
            this._karaokeTimers = [];
        }
    },

    // Start recording audio + real-time speech recognition + live wave visualizer
    startRecording: function (dotNetRef, deviceId, canvasId) {
        this._dotNetRef = dotNetRef;
        this._isRecording = true;
        this._audioChunks = [];
        this._finalTranscript = '';
        this._lastFullTranscript = '';
        this._voiceDetected = false;
        this._voiceEnergySum = 0;
        this._voiceFrameCount = 0;
        this._speechError = null;
        this._startTime = Date.now();

        // Stop any ongoing TTS
        this.stopSpeaking();

        return new Promise((resolve) => {
            // 1. Initialize SpeechRecognition (Web Speech API)
            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            if (SpeechRecognition) {
                try {
                    if (this._recognition) {
                        try { this._recognition.abort(); } catch (e) { }
                    }
                    this._recognition = new SpeechRecognition();
                    this._recognition.lang = 'en-US';
                    this._recognition.continuous = false; // Fast, responsive for single shadowing sentence
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
                        if (liveText) {
                            this._lastFullTranscript = liveText;
                        }
                        if (confidenceCount > 0) {
                            this._lastConfidence = totalConfidence / confidenceCount;
                        }

                        if (this._dotNetRef && liveText) {
                            this._dotNetRef.invokeMethodAsync('OnSpeechRecognized', liveText);
                        }
                    };

                    this._recognition.onerror = (e) => {
                        console.warn('SpeechRecognition note:', e.error);
                        this._speechError = e.error;
                    };

                    this._recognition.start();
                } catch (e) {
                    console.warn('SpeechRecognition failed to start:', e);
                    this._speechError = 'not-supported';
                }
            } else {
                this._speechError = 'not-supported';
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
                        this._activeStream = stream;

                        let mimeType = 'audio/webm;codecs=opus';
                        if (!window.MediaRecorder || !MediaRecorder.isTypeSupported(mimeType)) {
                            mimeType = MediaRecorder.isTypeSupported('audio/webm') ? 'audio/webm' : (MediaRecorder.isTypeSupported('audio/mp4') ? 'audio/mp4' : '');
                        }
                        this._mediaRecorderMime = mimeType;
                        this._mediaRecorder = mimeType ? new MediaRecorder(stream, { mimeType }) : new MediaRecorder(stream);
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
            this._analyser.smoothingTimeConstant = 0.75;
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

                let sum = 0;
                for (let i = 0; i < bufferLength; i++) {
                    sum += dataArray[i];
                }
                const avg = sum / bufferLength;

                // Voice Activity Detection (VAD)
                if (avg > 7) {
                    this._voiceDetected = true;
                    this._voiceEnergySum += avg;
                    this._voiceFrameCount++;
                }

                const barWidth = (w / bufferLength) * 2.2;
                let x = 0;
                for (let i = 0; i < bufferLength; i++) {
                    const barH = (dataArray[i] / 255) * h;
                    const gradient = ctx.createLinearGradient(0, h, 0, 0);
                    gradient.addColorStop(0, '#0284c7');
                    gradient.addColorStop(1, '#06b6d4');

                    ctx.fillStyle = gradient;
                    ctx.beginPath();
                    if (ctx.roundRect) {
                        ctx.roundRect(x, h - barH, barWidth, barH, [3, 3, 0, 0]);
                    } else {
                        ctx.rect(x, h - barH, barWidth, barH);
                    }
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
        if (this._audioCtx && this._audioCtx.state !== 'closed') {
            try { this._audioCtx.close(); } catch (e) { }
            this._audioCtx = null;
        }
        if (this._waveformCanvas) {
            const ctx = this._waveformCanvas.getContext('2d');
            ctx.clearRect(0, 0, this._waveformCanvas.width, this._waveformCanvas.height);
        }
    },

    // Stop recording and return payload: transcript + audio playback url + duration + voice activity
    stopRecording: function () {
        return new Promise((resolve) => {
            const durationSec = Math.max(0.5, (Date.now() - this._startTime) / 1000.0);

            // Stop SpeechRecognition safely
            if (this._recognition) {
                try {
                    this._recognition.stop();
                } catch (e) { }
            }

            const resultText = (this._lastFullTranscript || this._finalTranscript || '').trim();
            const confidence = this._lastConfidence || 0.85;
            const hasVoice = (this._voiceFrameCount >= 5) || (this._voiceEnergySum > 60);

            const finalizePayload = (audioUrl) => {
                this.stopLiveWaveform();
                this.stopMicStream();

                resolve(JSON.stringify({
                    transcript: resultText,
                    audioUrl: audioUrl || '',
                    durationSeconds: durationSec,
                    confidence: confidence,
                    voiceDetected: hasVoice,
                    voiceDurationSec: Math.round((this._voiceFrameCount * 0.02) * 10) / 10,
                    speechApiError: this._speechError
                }));
            };

            if (this._mediaRecorder && this._mediaRecorder.state === 'recording') {
                try {
                    this._mediaRecorder.requestData();
                } catch (e) { }

                this._mediaRecorder.onstop = () => {
                    let audioUrl = '';
                    if (this._audioChunks.length > 0) {
                        try {
                            const audioBlob = new Blob(this._audioChunks, { type: this._mediaRecorderMime || 'audio/webm' });
                            if (audioBlob.size > 0) {
                                audioUrl = URL.createObjectURL(audioBlob);
                            }
                        } catch (e) {
                            console.warn('Audio blob creation error:', e);
                        }
                    }
                    finalizePayload(audioUrl);
                };
                try {
                    this._mediaRecorder.stop();
                } catch (e) {
                    finalizePayload('');
                }
            } else {
                finalizePayload('');
            }
        });
    },

    stopMicStream: function () {
        if (this._activeStream) {
            try {
                this._activeStream.getTracks().forEach(track => track.stop());
            } catch (e) { }
            this._activeStream = null;
        }
        if (this._mediaRecorder && this._mediaRecorder.stream) {
            try {
                this._mediaRecorder.stream.getTracks().forEach(track => track.stop());
            } catch (e) { }
        }
        this._isRecording = false;
    }
};
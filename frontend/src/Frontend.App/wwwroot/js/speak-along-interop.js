// SpeakAlongInterop - JavaScript interop for IELTS Speak Along feature

window.SpeakAlongInterop = {
    _recognition: null,
    _isRecording: false,
    _dotNetRef: null,
    _mediaRecorder: null,
    _audioChunks: [],

    // Text-to-Speech
    speakText: function (text, lang) {
        return new Promise((resolve) => {
            if (!window.speechSynthesis) {
                resolve(false);
                return;
            }
            const utterance = new SpeechSynthesisUtterance(text);
            utterance.lang = lang || 'en-US';
            utterance.rate = 0.9;
            utterance.onend = function () { resolve(true); };
            utterance.onerror = function () { resolve(false); };
            window.speechSynthesis.speak(utterance);
        });
    },

    // Start recording using Web Speech API
    startRecording: function (dotNetRef) {
        this._dotNetRef = dotNetRef;
        this._isRecording = true;
        this._audioChunks = [];

        // Use MediaRecorder for audio
        return navigator.mediaDevices.getUserMedia({ audio: true })
            .then(function (stream) {
                this._mediaRecorder = new MediaRecorder(stream);
                this._mediaRecorder.ondataavailable = function (event) {
                    if (event.data.size > 0) {
                        this._audioChunks.push(event.data);
                    }
                }.bind(this);
                this._mediaRecorder.start(100);
                return true;
            }.bind(this))
            .catch(function (err) {
                console.error('Error accessing microphone:', err);
                return false;
            });
    },

    // Stop recording and get transcript
    stopRecording: function () {
        return new Promise(function (resolve) {
            if (this._mediaRecorder && this._mediaRecorder.state !== 'inactive') {
                this._mediaRecorder.onstop = function () {
                    // For demo, return mock transcript
                    // In production, send audio to backend for transcription
                    resolve("This is a simulated transcript of your speech.");
                }.bind(this);
                this._mediaRecorder.stop();
                this._isRecording = false;
            } else {
                resolve("No recording found.");
            }
        }.bind(this));
    },

    // Stop microphone stream
    stopMicStream: function () {
        if (this._mediaRecorder && this._mediaRecorder.stream) {
            this._mediaRecorder.stream.getTracks().forEach(function (track) {
                track.stop();
            });
        }
        this._isRecording = false;
    }
};
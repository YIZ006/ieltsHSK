// ChengyuAudio - Modern Web Speech API Dual Youth Voices Engine for HSK Idioms
(function () {
  'use strict';

  var activeUtterance = null;
  var isSpeaking = false;

  window.chengyuAudio = {
    play: function (text, lang, callback) {
      this.stop();

      var clean = (text || '').replace(/[\u{1F300}-\u{1FAFF}\u{2600}-\u{27BF}]/gu, '').trim();
      if (!clean) {
        if (typeof callback === 'function') callback();
        return;
      }

      if (!('speechSynthesis' in window)) {
        if (typeof callback === 'function') callback();
        return;
      }

      var isTaiwan = (lang === 'zh-TW');
      var u = new SpeechSynthesisUtterance(clean);
      u.lang = isTaiwan ? 'zh-TW' : 'zh-CN';
      // Dual youth voices: Bạn Trái tươi sáng nhẹ (1.05), Bạn Phải ngọt trầm (0.95)
      u.pitch = isTaiwan ? 0.95 : 1.05;
      u.rate = 1.0;

      try {
        var voices = window.speechSynthesis.getVoices() || [];
        var matched = voices.find(function (v) {
          if (isTaiwan) {
            return v.lang === 'zh-TW' || v.lang === 'cmn-Hant-TW' || (v.lang && v.lang.indexOf('TW') !== -1);
          }
          return v.lang === 'zh-CN' || v.lang === 'cmn-Hans-CN' || (v.lang && v.lang.indexOf('CN') !== -1);
        });
        if (matched) u.voice = matched;
      } catch (e) {}

      var finished = false;
      function onComplete() {
        if (!finished) {
          finished = true;
          activeUtterance = null;
          isSpeaking = false;
          if (typeof callback === 'function') callback();
        }
      }

      u.onend = onComplete;
      u.onerror = onComplete;

      activeUtterance = u;
      isSpeaking = true;

      // 50ms buffer để Windows Speech driver dọn sạch buffer cũ trước khi đọc câu mới
      setTimeout(function () {
        if (activeUtterance === u) {
          window.speechSynthesis.speak(u);
        }
      }, 50);
    },

    playPromise: function (text, lang) {
      var self = this;
      return new Promise(function (resolve) {
        self.play(text, lang, resolve);
      });
    },

    stop: function () {
      activeUtterance = null;
      isSpeaking = false;
      if ('speechSynthesis' in window) {
        try {
          window.speechSynthesis.cancel();
        } catch (e) {}
      }
    }
  };
})();

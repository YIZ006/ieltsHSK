// Offline Storage & Network Manager using IndexedDB and Cache API
const OfflineDB = {
  dbName: 'ielts_offline_db',
  dbVersion: 2,
  mediaCacheName: 'offline-media-v1',

  _db: null,
  _downloadedKeysCache: new Set(),
  _cacheInitialized: false,

  async initCacheIndex() {
    if (this._cacheInitialized) return this._downloadedKeysCache;
    try {
      const db = await this.getDB();
      const keys = await new Promise((resolve) => {
        const tx = db.transaction('exams', 'readonly');
        const store = tx.objectStore('exams');
        const req = store.getAllKeys();
        req.onsuccess = () => resolve(req.result || []);
        req.onerror = () => resolve([]);
      });
      keys.forEach(k => this._downloadedKeysCache.add(String(k)));
      this._cacheInitialized = true;
    } catch (e) {
      console.warn('[OfflineDB] initCacheIndex error:', e);
    }
    return this._downloadedKeysCache;
  },

  async getDB() {
    if (this._db) return this._db;
    return new Promise((resolve, reject) => {
      const request = indexedDB.open(this.dbName, this.dbVersion);
      request.onupgradeneeded = (e) => {
        const db = e.target.result;
        if (!db.objectStoreNames.contains('exams')) {
          db.createObjectStore('exams', { keyPath: 'id' });
        }
        if (!db.objectStoreNames.contains('offline_submissions')) {
          db.createObjectStore('offline_submissions', { keyPath: 'id' });
        }
        if (!db.objectStoreNames.contains('vocab_cache')) {
          db.createObjectStore('vocab_cache', { keyPath: 'key' });
        }
      };
      request.onsuccess = (e) => {
        this._db = e.target.result;
        resolve(this._db);
      };
      request.onerror = (e) => reject(e.target.error);
    });
  },

  isOnline() {
    return navigator.onLine;
  },

  registerNetworkStatus(dotNetRef) {
    const notify = () => {
      try {
        dotNetRef.invokeMethodAsync('OnNetworkStatusChanged', navigator.onLine);
      } catch (err) {
        console.warn('[OfflineStorage] Notify network status error:', err);
      }
    };
    window.addEventListener('online', notify);
    window.addEventListener('offline', notify);
    return true;
  },

  async saveExamData(testId, data) {
    this._downloadedKeysCache.add(String(testId));
    const db = await this.getDB();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('exams', 'readwrite');
      const store = tx.objectStore('exams');
      const record = {
        id: testId,
        data: data,
        savedAt: Date.now()
      };
      const req = store.put(record);
      req.onsuccess = () => resolve(true);
      req.onerror = (e) => reject(e.target.error);
    });
  },

  async getExamData(testId) {
    const db = await this.getDB();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('exams', 'readonly');
      const store = tx.objectStore('exams');
      const req = store.get(testId);
      req.onsuccess = () => resolve(req.result ? req.result.data : null);
      req.onerror = (e) => reject(e.target.error);
    });
  },

  async removeExamData(testId) {
    this._downloadedKeysCache.delete(String(testId));
    const db = await this.getDB();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('exams', 'readwrite');
      const store = tx.objectStore('exams');
      const req = store.delete(testId);
      req.onsuccess = () => resolve(true);
      req.onerror = (e) => reject(e.target.error);
    });
  },

  async getDownloadedTestIds() {
    const db = await this.getDB();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('exams', 'readonly');
      const store = tx.objectStore('exams');
      const req = store.getAllKeys();
      req.onsuccess = () => resolve(req.result || []);
      req.onerror = (e) => reject(e.target.error);
    });
  },

  async getAllExams() {
    const db = await this.getDB();
    return new Promise((resolve, reject) => {
      const tx = db.transaction('exams', 'readonly');
      const store = tx.objectStore('exams');
      const req = store.getAll();
      req.onsuccess = () => {
        const list = (req.result || []).map(r => r.data);
        resolve(list);
      };
      req.onerror = (e) => reject(e.target.error);
    });
  },

  async cacheMedia(url) {
    if (!url) return false;
    // Bỏ qua link YouTube / nhúng video vì không thể fetch CORS vào CacheStorage
    if (url.includes('youtube.com') || url.includes('youtu.be') || url.includes('vimeo.com')) {
      return false;
    }
    try {
      const cache = await caches.open(this.mediaCacheName);
      // Fetch và lưu vào Cache
      const res = await fetch(url, { mode: 'cors' });
      if (res && res.ok) {
        await cache.put(url, res);
        return true;
      }
      return false;
    } catch (err) {
      console.warn('[OfflineStorage] cacheMedia skipped/failed for url:', url, err);
      return false;
    }
  },

  async isMediaCached(url) {
    if (!url) return false;
    try {
      const cache = await caches.open(this.mediaCacheName);
      const match = await cache.match(url);
      return !!match;
    } catch {
      return false;
    }
  },

  async removeMedia(url) {
    if (!url) return false;
    try {
      const cache = await caches.open(this.mediaCacheName);
      return await cache.delete(url);
    } catch {
      return false;
    }
  },

  async getMediaBlobUrl(url) {
    if (!url) return null;
    try {
      const cache = await caches.open(this.mediaCacheName);
      const match = await cache.match(url);
      if (match) {
        const blob = await match.blob();
        return URL.createObjectURL(blob);
      }
      return null;
    } catch (err) {
      console.warn('[OfflineStorage] getMediaBlobUrl error:', err);
      return null;
    }
  },

  async saveVocabCache(key, jsonString) {
    if (!key || !jsonString) return false;
    try {
      const db = await this.getDB();
      return new Promise((resolve, reject) => {
        const tx = db.transaction('vocab_cache', 'readwrite');
        const store = tx.objectStore('vocab_cache');
        const record = {
          key: key,
          json: jsonString,
          savedAt: Date.now()
        };
        const req = store.put(record);
        req.onsuccess = () => resolve(true);
        req.onerror = (e) => reject(e.target.error);
      });
    } catch (err) {
      console.warn('[OfflineDB] saveVocabCache error:', err);
      return false;
    }
  },

  async getVocabCache(key) {
    if (!key) return null;
    try {
      const db = await this.getDB();
      return new Promise((resolve, reject) => {
        const tx = db.transaction('vocab_cache', 'readonly');
        const store = tx.objectStore('vocab_cache');
        const req = store.get(key);
        req.onsuccess = () => {
          if (!req.result || !req.result.json) return resolve(null);
          resolve(req.result.json);
        };
        req.onerror = (e) => reject(e.target.error);
      });
    } catch (err) {
      console.warn('[OfflineDB] getVocabCache error:', err);
      return null;
    }
  },

  async isResourceDownloaded(key, mediaUrl) {
    try {
      if (!this._cacheInitialized) {
        await this.initCacheIndex();
      }
      if (key && this._downloadedKeysCache.has(String(key))) {
        return true;
      }
      const db = await this.getDB();
      const examExists = await new Promise((resolve) => {
        const tx = db.transaction('exams', 'readonly');
        const store = tx.objectStore('exams');
        const req = store.get(key);
        req.onsuccess = () => resolve(!!req.result);
        req.onerror = () => resolve(false);
      });
      if (examExists) {
        if (key) this._downloadedKeysCache.add(String(key));
        return true;
      }
      if (mediaUrl) {
        return await this.isMediaCached(mediaUrl);
      }
      return false;
    } catch {
      return false;
    }
  },

  async getStorageEstimate() {
    try {
      if (navigator.storage && navigator.storage.estimate) {
        const est = await navigator.storage.estimate();
        const mb = ((est.usage || 0) / (1024 * 1024)).toFixed(1);
        return `${mb} MB`;
      }
      return 'N/A';
    } catch {
      return 'N/A';
    }
  },

  speakChinese(text) {
    if (!text) return;
    try {
      if ('speechSynthesis' in window) {
        window.speechSynthesis.cancel();
        const u = new SpeechSynthesisUtterance(text);
        u.lang = 'zh-CN';
        u.rate = 0.9;
        window.speechSynthesis.speak(u);
      }
    } catch (e) {
      console.warn('[OfflineDB] Speech synthesis error:', e);
    }
  },

  showToast(msg, type) {
    try {
      if (window.MascotManager && typeof window.MascotManager.showToast === 'function') {
        window.MascotManager.showToast(msg, type);
      } else {
        console.log(`[Toast ${type}]: ${msg}`);
      }
    } catch (e) {
      console.warn('[OfflineDB] showToast error:', e);
    }
  }
};

window.OfflineDB = OfflineDB;

// Tự động khởi tạo & giải nén danh mục offline ngầm khi trình duyệt rảnh
if (typeof requestIdleCallback !== 'undefined') {
  requestIdleCallback(() => OfflineDB.initCacheIndex());
} else {
  setTimeout(() => OfflineDB.initCacheIndex(), 100);
}


// Offline Storage & Network Manager using IndexedDB and Cache API
const OfflineDB = {
  dbName: 'ielts_offline_db',
  dbVersion: 1,
  mediaCacheName: 'offline-media-v1',

  _db: null,

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
  }
};

window.OfflineDB = OfflineDB;

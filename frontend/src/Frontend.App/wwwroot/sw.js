// PWA Service Worker — cache-first cho file tĩnh, network-first cho navigation Blazor WASM
// và network-first (fallback cache) cho dữ liệu JSON trong /sample-data/ để không bị kẹt dữ liệu cũ.
const CACHE = 'app-v13-hsk-grammar-studio';

const ASSETS = [
  './',
  './index.html',
  './manifest.json',
  './favicon.png',
  './icons/icon-192.png',
  './icons/icon-512.png',
  './icons/icon-maskable-512.png'
];

// Dữ liệu học tập HSK: tải sẵn ngay khi cài Service Worker để dùng được offline
const PRECACHE_DATA = [
  './sample-data/hsk-chengyu.json',
  './sample-data/hsk-culture-stories.json',
  './sample-data/hsk-genz-slang.json',
  './sample-data/hsk-grammar.json'
];

async function precacheFresh(cache, url) {
  // cache: 'reload' bỏ qua HTTP cache để luôn lấy bản mới nhất từ server
  const res = await fetch(new Request(url, { cache: 'reload' }));
  if (res && res.ok) await cache.put(url, res);
}

// Network-first cho JSON dữ liệu: online luôn lấy bản mới (revalidate), mất mạng/chậm thì dùng bản đã lưu
async function networkFirstData(req) {
  const cache = await caches.open(CACHE);
  const cached = await cache.match(req);
  const controller = new AbortController();
  // Chỉ áp timeout khi đã có bản lưu để fallback; nếu chưa có thì chờ mạng bình thường
  const timer = cached ? setTimeout(() => controller.abort(), 3500) : null;
  try {
    const res = await fetch(new Request(req, { cache: 'no-cache' }), { signal: controller.signal });
    if (timer) clearTimeout(timer);
    if (res && res.ok) {
      cache.put(req, res.clone()).catch(() => {});
      return res;
    }
    return cached || res;
  } catch {
    if (timer) clearTimeout(timer);
    return cached || new Response('', { status: 503, statusText: 'Data unavailable offline' });
  }
}

self.addEventListener('install', e => {
  e.waitUntil(
    caches.open(CACHE)
      .then(c => Promise.allSettled([
        ...ASSETS.map(a => c.add(a)),
        ...PRECACHE_DATA.map(a => precacheFresh(c, a))
      ]))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener('fetch', e => {
  const req = e.request;
  if (req.method !== 'GET') return;

  const url = new URL(req.url);
  // Bỏ qua request cross-origin (Google Fonts, CDN, GSI, Cloudflare R2...)
  if (url.origin !== location.origin) return;

  // Tuyệt đối không can thiệp API request, appsettings, file biên dịch Blazor WASM, mascot script, và các file CSS cập nhật
  if (url.pathname.startsWith('/api/') || 
      url.pathname.startsWith('/_framework/') || 
      url.pathname.includes('appsettings') ||
      url.pathname.includes('mascot-interop') ||
      url.pathname.includes('Frontend.App.styles.css') ||
      url.pathname.includes('app.css')) return;

  // Dữ liệu JSON học tập (thành ngữ, văn hóa, tiếng lóng, đề thi...): Network-first + fallback cache
  if (url.pathname.includes('/sample-data/') && url.pathname.endsWith('.json')) {
    e.respondWith(networkFirstData(req));
    return;
  }

  // Với request navigation (đổi trang Blazor WASM), dùng Network-first
  if (req.mode === 'navigate') {
    e.respondWith(
      fetch(req).then(async res => {
        if (!res.ok && res.status === 404) {
          const cached = await caches.match('./index.html') 
                      || await caches.match('/index.html') 
                      || await caches.match('index.html');
          if (cached) return cached;
        }
        return res;
      }).catch(async () => {
        try {
          const cached = await caches.match('./index.html') 
                      || await caches.match('/index.html') 
                      || await caches.match('index.html');
          if (cached) return cached;
        } catch {}
        return new Response('<!DOCTYPE html><html><body>Offline</body></html>', { 
          status: 200, 
          headers: { 'Content-Type': 'text/html' } 
        });
      })
    );
    return;
  }

  // Với static assets (wasm, dll, css, js, images), dùng Cache-first
  e.respondWith(
    caches.match(req).then(hit => {
      if (hit) return hit;
      return fetch(req).then(res => {
        if (res && res.ok) {
          const copy = res.clone();
          caches.open(CACHE).then(c => c.put(req, copy)).catch(() => {});
        }
        return res;
      }).catch(() => {
        return new Response('', { status: 404, statusText: 'Resource unavailable offline' });
      });
    }).catch(() => {
      return new Response('', { status: 404, statusText: 'Cache lookup failed' });
    })
  );
});

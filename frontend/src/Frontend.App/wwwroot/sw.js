// PWA Service Worker — cache-first cho file tĩnh, network-first cho navigation Blazor WASM
const CACHE = 'app-v3';

const ASSETS = [
  './',
  './index.html',
  './manifest.json',
  './favicon.png',
  './icons/icon-192.png',
  './icons/icon-512.png',
  './icons/icon-maskable-512.png'
];

self.addEventListener('install', e => {
  e.waitUntil(
    caches.open(CACHE)
      .then(c => Promise.allSettled(ASSETS.map(a => c.add(a))))
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

  // Tuyệt đối không can thiệp API request và file biên dịch Blazor WASM
  if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/_framework/')) return;

  // Với request navigation (đổi trang Blazor WASM), dùng Network-first
  if (req.mode === 'navigate') {
    e.respondWith(
      fetch(req).catch(async () => {
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
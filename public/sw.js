// Service worker for the Valheim Character File Viewer PWA.
//
// Only kicks in when this app is served over http(s) (including
// localhost) — browsers refuse to register service workers on a plain
// file:// origin, which is how this tool is often opened directly. See
// README.md for how to serve it locally if you want installability/
// offline support to actually work.

const CACHE_NAME = "valheim-viewer-v3";
const APP_SHELL = ["./", "./index.html", "./manifest.json", "./icon.svg", "./vendor/cytoscape.min.js"];

self.addEventListener("install", (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => cache.addAll(APP_SHELL))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", (event) => {
  event.waitUntil(
    caches.keys()
      .then((keys) => Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k))))
      .then(() => self.clients.claim())
  );
});

// Network-first, falling back to cache only when offline. This is a
// single-file app under active development — a cache-first strategy meant
// every reload showed whatever was cached one edit ago instead of what was
// actually just saved, which is worse than just not caching at all. Network
// requests still repopulate the cache as they succeed, so offline use (the
// actual point of caching here) keeps working once you've loaded it at
// least once.
self.addEventListener("fetch", (event) => {
  const req = event.request;
  if (req.method !== "GET") return;

  event.respondWith(
    fetch(req)
      .then((res) => {
        if (res && res.ok) {
          const copy = res.clone();
          caches.open(CACHE_NAME).then((cache) => cache.put(req, copy));
        }
        return res;
      })
      .catch(() => caches.match(req))
  );
});

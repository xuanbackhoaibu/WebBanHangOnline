const CACHE_NAME = "xuanbac-shop-v23";
const CORE_ASSETS = [
  "/",
  "/Home/About",
  "/Product",
  "/css/site.css",
  "/css/muva.css",
  "/css/muva-sync.css",
  "/js/site.js",
  "/js/muva.js",
  "/favicon.ico",
  "/images/men-banner.jpg",
  "/images/women-banner.jpg"
];

self.addEventListener("install", event => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then(cache => cache.addAll(CORE_ASSETS))
      .then(() => self.skipWaiting())
  );
});

self.addEventListener("activate", event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(keys.filter(key => key !== CACHE_NAME).map(key => caches.delete(key))))
      .then(() => self.clients.claim())
  );
});

self.addEventListener("fetch", event => {
  const request = event.request;
  const url = new URL(request.url);

  if (url.origin !== self.location.origin) {
    return;
  }

  const sensitivePrefixes = [
    "/Admin",
    "/Cart",
    "/Order",
    "/Payment",
    "/Identity",
    "/Wishlist",
    "/Notification",
    "/Support",
    "/hangfire",
    "/swagger"
  ];

  const path = url.pathname.toLowerCase();
  if (sensitivePrefixes.some(prefix => path.startsWith(prefix.toLowerCase()))) {
    event.respondWith(fetch(request));
    return;
  }

  if (request.method !== "GET") {
    return;
  }

  if (request.mode === "navigate") {
    event.respondWith(
      fetch(request)
        .then(response => {
          const copy = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(request, copy));
          return response;
        })
        .catch(() => caches.match(request).then(response => response || caches.match("/Home/About")))
    );
    return;
  }

  if (request.destination === "style" || request.destination === "script") {
    event.respondWith(
      fetch(request)
        .then(response => {
          if (!response || response.status !== 200) return response;
          const copy = response.clone();
          caches.open(CACHE_NAME).then(cache => cache.put(request, copy));
          return response;
        })
        .catch(() => caches.match(request))
    );
    return;
  }

  event.respondWith(
    caches.match(request).then(cached => {
      if (cached) return cached;

      return fetch(request).then(response => {
        if (!response || response.status !== 200) return response;
        const copy = response.clone();
        caches.open(CACHE_NAME).then(cache => cache.put(request, copy));
        return response;
      });
    })
  );
});

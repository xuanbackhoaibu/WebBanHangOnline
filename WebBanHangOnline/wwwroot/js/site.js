document.addEventListener("DOMContentLoaded", function () {
    document.body.classList.add("page-enter");

    initThemeToggle();
    initPageTransitions();
    initRevealMotion();
    initPwa();

    document.querySelectorAll(".btn-plus").forEach(btn => {
        btn.addEventListener("click", function () {
            const row = btn.closest("tr");
            const input = row.querySelector(".quantity-input");
            input.value = parseInt(input.value) + 1;
            submitUpdate(input);
        });
    });

    document.querySelectorAll(".btn-minus").forEach(btn => {
        btn.addEventListener("click", function () {
            const row = btn.closest("tr");
            const input = row.querySelector(".quantity-input");
            if (parseInt(input.value) > 1) {
                input.value = parseInt(input.value) - 1;
                submitUpdate(input);
            }
        });
    });

    function submitUpdate(input) {
        const id = input.dataset.id;
        const quantity = input.value;

        fetch("/Cart/Update", {
            method: "POST",
            headers: {
                "Content-Type": "application/x-www-form-urlencoded"
            },
            body: `id=${id}&quantity=${quantity}`
        })
            .then(() => location.reload());
    }
});

function initRevealMotion() {
    const items = document.querySelectorAll(".home-page .product-card, .catalog-product-card, .trend-card, .checkout-section");
    if (!items.length) return;

    items.forEach((item, index) => {
        item.classList.add("xb-reveal");
        item.style.transitionDelay = `${Math.min(index % 8, 5) * 45}ms`;
    });

    if (!("IntersectionObserver" in window)) {
        items.forEach(item => item.classList.add("is-visible"));
        return;
    }

    const observer = new IntersectionObserver(entries => {
        entries.forEach(entry => {
            if (!entry.isIntersecting) return;
            entry.target.classList.add("is-visible");
            observer.unobserve(entry.target);
        });
    }, { threshold: 0.14 });

    items.forEach(item => observer.observe(item));
}

function initThemeToggle() {
    const toggle = document.getElementById("themeToggle");
    if (!toggle) return;

    const icon = toggle.querySelector("i");
    const syncIcon = () => {
        const isDark = document.documentElement.dataset.theme === "dark";
        toggle.classList.toggle("is-dark", isDark);
        if (icon) {
            icon.className = isDark ? "bi bi-sun" : "bi bi-moon-stars";
        }
    };

    syncIcon();

    toggle.addEventListener("click", () => {
        const nextTheme = document.documentElement.dataset.theme === "dark" ? "light" : "dark";
        document.documentElement.dataset.theme = nextTheme;
        localStorage.setItem("xb-theme", nextTheme);
        syncIcon();
        window.xbToast?.({
            title: nextTheme === "dark" ? "Đã bật Dark Mode" : "Đã bật Light Mode",
            message: "Giao diện sẽ được ghi nhớ cho lần truy cập sau.",
            icon: nextTheme === "dark" ? "bi-moon-stars" : "bi-sun"
        });
    });
}

function initPageTransitions() {
    document.querySelectorAll("a[href]").forEach(link => {
        link.addEventListener("click", event => {
            const href = link.getAttribute("href") || "";
            const target = link.getAttribute("target");
            const isDownload = link.hasAttribute("download");
            const isToggle = link.dataset.bsToggle || link.getAttribute("data-bs-toggle");

            if (
                event.defaultPrevented ||
                event.metaKey ||
                event.ctrlKey ||
                event.shiftKey ||
                event.altKey ||
                target === "_blank" ||
                isDownload ||
                isToggle ||
                href.startsWith("#") ||
                href.startsWith("javascript:") ||
                href.startsWith("mailto:") ||
                href.startsWith("tel:")
            ) {
                return;
            }

            const nextUrl = new URL(href, window.location.href);
            if (nextUrl.origin !== window.location.origin || nextUrl.href === window.location.href) return;

            document.body.classList.add("page-leaving");
        });
    });

    document.querySelectorAll("form").forEach(form => {
        form.addEventListener("submit", event => {
            if (form.dataset.noPageLoader === "true") return;
            setTimeout(() => {
                if (!event.defaultPrevented) {
                    document.body.classList.add("page-leaving");
                }
            }, 0);
        });
    });
}

function initPwa() {
    if (!("serviceWorker" in navigator)) return;

    window.addEventListener("load", () => {
        navigator.serviceWorker.register("/service-worker.js").catch(() => {
            // PWA is progressive; browsing should stay normal when registration is blocked.
        });
    });
}

window.xbToast = function xbToast(options) {
    const root = document.getElementById("toastRoot");
    if (!root) return;

    const title = options?.title || "Thông báo";
    const message = options?.message || "";
    const icon = options?.icon || "bi-check2";
    const timeout = options?.timeout ?? 3200;

    const toast = document.createElement("div");
    toast.className = "xb-toast";
    toast.innerHTML = `
        <span class="xb-toast-icon"><i class="bi ${icon}"></i></span>
        <span class="xb-toast-copy">
            <strong class="xb-toast-title"></strong>
            <span class="xb-toast-message"></span>
        </span>
        <button type="button" class="xb-toast-close" aria-label="Đóng"><i class="bi bi-x"></i></button>
    `;

    toast.querySelector(".xb-toast-title").innerText = title;
    toast.querySelector(".xb-toast-message").innerText = message;

    const hide = () => {
        toast.classList.add("is-hiding");
        setTimeout(() => toast.remove(), 220);
    };

    toast.querySelector(".xb-toast-close").addEventListener("click", hide);
    root.appendChild(toast);
    setTimeout(hide, timeout);
};

window.xbAnimateToCart = function xbAnimateToCart(imageElement) {
    const target = document.querySelector(".xb-cart-target");
    if (!imageElement || !target || window.matchMedia("(prefers-reduced-motion: reduce)").matches) {
        return Promise.resolve();
    }

    const sourceRect = imageElement.getBoundingClientRect();
    const targetRect = target.getBoundingClientRect();
    const flyer = imageElement.cloneNode(true);

    flyer.className = "cart-flyer";
    flyer.style.left = `${sourceRect.left}px`;
    flyer.style.top = `${sourceRect.top}px`;
    document.body.appendChild(flyer);

    requestAnimationFrame(() => {
        const moveX = targetRect.left + targetRect.width / 2 - sourceRect.left - 27;
        const moveY = targetRect.top + targetRect.height / 2 - sourceRect.top - 32;
        flyer.style.transform = `translate(${moveX}px, ${moveY}px) scale(0.22) rotate(8deg)`;
        flyer.style.opacity = "0.2";
    });

    return new Promise(resolve => {
        setTimeout(() => {
            flyer.remove();
            target.classList.add("cart-pulse");
            setTimeout(() => target.classList.remove("cart-pulse"), 460);
            resolve();
        }, 640);
    });
};

// ===== FLASH SALE COUNTDOWN =====
(function () {
    const countdownEl = document.getElementById("countdown");
    if (!countdownEl) return;

    const endTime = new Date().getTime() + 3 * 60 * 60 * 1000;

    setInterval(() => {
        const d = endTime - new Date().getTime();
        if (d <= 0) {
            countdownEl.innerText = "Đã kết thúc";
            return;
        }

        const h = String(Math.floor(d / 36e5)).padStart(2, "0");
        const m = String(Math.floor(d % 36e5 / 6e4)).padStart(2, "0");
        const s = String(Math.floor(d % 6e4 / 1000)).padStart(2, "0");

        countdownEl.innerText = `${h}:${m}:${s}`;
    }, 1000);
})();


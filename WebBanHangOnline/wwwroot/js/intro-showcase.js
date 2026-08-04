(function () {
  const page = document.querySelector("[data-intro-page]");
  const canvas = document.querySelector("[data-intro-particles]");
  const carousel = document.querySelector("[data-intro-carousel]");
  const cards = Array.from(document.querySelectorAll("[data-intro-card]"));
  const captionName = document.querySelector("[data-intro-caption-name]");
  const captionPrice = document.querySelector("[data-intro-caption-price]");
  const previousButton = document.querySelector("[data-intro-prev]");
  const nextButton = document.querySelector("[data-intro-next]");
  const detail = document.querySelector("[data-intro-detail]");
  const detailPanel = detail?.querySelector(".intro-detail-panel");
  const detailImage = document.querySelector("[data-intro-detail-image]");
  const detailName = document.querySelector("[data-intro-detail-name]");
  const detailPrice = document.querySelector("[data-intro-detail-price]");
  const detailLink = document.querySelector("[data-intro-detail-link]");
  const searchInput = document.querySelector("[data-intro-search]");
  const filterButtons = Array.from(document.querySelectorAll("[data-intro-filter]"));
  const emptyState = document.querySelector("[data-intro-empty]");
  const progress = document.querySelector("[data-intro-progress]");
  const sectionButtons = Array.from(document.querySelectorAll("[data-intro-section-link]"));
  const sections = Array.from(document.querySelectorAll("[data-intro-section]"));
  const revealItems = Array.from(document.querySelectorAll("[data-intro-reveal]"));
  const cursor = document.querySelector("[data-intro-cursor]");
  const cursorDot = document.querySelector("[data-intro-cursor-dot]");
  const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
  const coarsePointer = window.matchMedia("(pointer: coarse)").matches;
  const cleanup = [];

  if (!page) return;

  const state = {
    currentIndex: 0,
    visualIndex: 0,
    dragging: false,
    dragMoved: false,
    dragStartX: 0,
    dragLastX: 0,
    dragOffset: 0,
    velocity: 0,
    scrollProgress: 0,
    pointer: { x: window.innerWidth / 2, y: window.innerHeight / 2, active: false },
    cursor: { x: window.innerWidth / 2, y: window.innerHeight / 2, scale: 1 },
    filter: "all",
    search: "",
    activeBeforeDetail: null,
    detailOpen: false
  };

  const clamp = (value, min, max) => Math.min(max, Math.max(min, value));
  const wrapIndex = index => (index + cards.length) % cards.length;
  const focusableSelector = "a[href], button:not([disabled]), input:not([disabled]), [tabindex]:not([tabindex='-1'])";

  const on = (target, event, handler, options) => {
    if (!target) return;
    target.addEventListener(event, handler, options);
    cleanup.push(() => target.removeEventListener(event, handler, options));
  };

  const setInteractive = element => {
    element?.classList.add("is-interactive");
  };

  const unsetInteractive = element => {
    element?.classList.remove("is-interactive");
  };

  const updateCaption = () => {
    const activeCard = cards[state.currentIndex];
    if (!activeCard) return;
    if (captionName) captionName.textContent = activeCard.dataset.name || "";
    if (captionPrice) captionPrice.textContent = activeCard.dataset.price || "";
  };

  const offsetFor = (index, activeIndex) => {
    let offset = index - activeIndex;
    const half = cards.length / 2;
    if (offset > half) offset -= cards.length;
    if (offset < -half) offset += cards.length;
    return offset;
  };

  const renderCards = (visualIndex = state.currentIndex) => {
    if (!cards.length) return;
    const scrollDepth = state.scrollProgress * 110;

    cards.forEach((card, index) => {
      const filtered = card.hidden || card.classList.contains("is-filtered");
      const offset = offsetFor(index, visualIndex);
      const abs = Math.abs(offset);
      const direction = offset === 0 ? 0 : Math.sign(offset);
      const curve = abs * abs;
      const x = offset * 158 + direction * curve * 24;
      const y = Math.sin((index + visualIndex) * 1.16) * 44 + abs * 10 + state.scrollProgress * 28;
      const z = 255 - abs * 92 - scrollDepth;
      const rotateZ = offset * -7.5 + Math.sin(index * 0.7) * 3;
      const rotateY = offset * -14;
      const scale = 1 - Math.min(abs * 0.082, 0.42) + (abs < 0.5 ? state.scrollProgress * 0.035 : 0);

      card.style.setProperty("--x", `${x.toFixed(2)}px`);
      card.style.setProperty("--y", `${y.toFixed(2)}px`);
      card.style.setProperty("--z", `${z.toFixed(2)}px`);
      card.style.setProperty("--rotate", `${rotateZ.toFixed(2)}deg`);
      card.style.setProperty("--rotate-y", `${rotateY.toFixed(2)}deg`);
      card.style.setProperty("--scale", scale.toFixed(3));
      card.style.setProperty("--opacity", filtered ? "0" : Math.max(0, 1 - abs * 0.18).toFixed(2));
      card.style.zIndex = String(100 - Math.round(abs * 10));
      card.classList.toggle("is-active", abs < 0.5);
      card.classList.toggle("is-far", abs > 3.8 || filtered);
      card.setAttribute("aria-current", abs < 0.5 ? "true" : "false");
    });

    updateCaption();
  };

  const goTo = (index, animate = true) => {
    if (!cards.length) return;
    state.currentIndex = wrapIndex(index);
    state.visualIndex = state.currentIndex;
    state.dragOffset = 0;
    page.style.setProperty("--scene-hue", `${state.currentIndex * 37}deg`);
    page.classList.toggle("is-snapping", animate);
    renderCards();
    window.setTimeout(() => page.classList.remove("is-snapping"), 420);
  };

  const activeCard = () => cards[state.currentIndex];

  const openDetail = card => {
    if (!detail || !card || state.detailOpen) return;
    state.detailOpen = true;
    state.activeBeforeDetail = document.activeElement;
    const image = card.dataset.image || card.querySelector("img")?.getAttribute("src") || "";
    const name = card.dataset.name || "";
    const price = card.dataset.price || "";

    detail.hidden = false;
    detail.classList.add("is-open");
    document.body.classList.add("intro-detail-lock");
    if (detailImage) {
      detailImage.src = image;
      detailImage.alt = name;
    }
    if (detailName) detailName.textContent = name;
    if (detailPrice) detailPrice.textContent = price;
    if (detailLink) detailLink.href = card.getAttribute("href") || "#";

    goTo(Number(card.dataset.cardIndex || state.currentIndex));
    window.setTimeout(() => detailPanel?.focus(), 40);
  };

  const closeDetail = () => {
    if (!detail || !state.detailOpen) return;
    state.detailOpen = false;
    detail.classList.remove("is-open");
    document.body.classList.remove("intro-detail-lock");
    window.setTimeout(() => {
      detail.hidden = true;
      if (state.activeBeforeDetail instanceof HTMLElement) state.activeBeforeDetail.focus();
    }, reducedMotion ? 0 : 320);
  };

  const updateFilter = () => {
    const query = state.search.trim().toLowerCase();
    let visible = 0;

    cards.forEach(card => {
      const categoryMatch = state.filter === "all" || card.dataset.category === state.filter;
      const text = `${card.dataset.keywords || ""} ${card.dataset.name || ""}`.toLowerCase();
      const searchMatch = !query || text.includes(query);
      const show = categoryMatch && searchMatch;

      card.hidden = false;
      card.classList.toggle("is-filtered", !show);
      card.setAttribute("aria-hidden", show ? "false" : "true");
      if (show) visible += 1;
    });

    if (emptyState) emptyState.hidden = visible > 0;
    if (!visible) page.classList.add("has-empty-cards");
    else page.classList.remove("has-empty-cards");
    renderCards();
  };

  const initCards = () => {
    cards.forEach((card, index) => {
      card.style.setProperty("--hover-x", "50%");
      card.style.setProperty("--hover-y", "50%");
      card.setAttribute("tabindex", "0");
      card.setAttribute("role", "button");
      card.dataset.cardIndex = String(index);

      on(card, "pointerenter", () => {
        setInteractive(page);
        if (!coarsePointer) card.classList.add("is-hovered");
      });

      on(card, "pointerleave", () => {
        unsetInteractive(page);
        card.classList.remove("is-hovered");
        card.style.setProperty("--tilt-x", "0deg");
        card.style.setProperty("--tilt-y", "0deg");
      });

      on(card, "pointermove", event => {
        if (coarsePointer || reducedMotion) return;
        const rect = card.getBoundingClientRect();
        const px = clamp((event.clientX - rect.left) / rect.width, 0, 1);
        const py = clamp((event.clientY - rect.top) / rect.height, 0, 1);
        card.style.setProperty("--hover-x", `${(px * 100).toFixed(1)}%`);
        card.style.setProperty("--hover-y", `${(py * 100).toFixed(1)}%`);
        card.style.setProperty("--tilt-x", `${((0.5 - py) * 10).toFixed(2)}deg`);
        card.style.setProperty("--tilt-y", `${((px - 0.5) * 12).toFixed(2)}deg`);
      });

      on(card, "click", event => {
        if (state.dragMoved) {
          event.preventDefault();
          return;
        }
        event.preventDefault();
        openDetail(card);
      });

      on(card, "keydown", event => {
        if (event.key === "Enter" || event.key === " ") {
          event.preventDefault();
          openDetail(card);
        }
      });
    });
  };

  const initDrag = () => {
    if (!carousel || !cards.length) return;

    on(carousel, "pointerdown", event => {
      if (event.target.closest("[data-intro-detail], input, button")) return;
      state.dragging = true;
      state.dragMoved = false;
      state.dragStartX = event.clientX;
      state.dragLastX = event.clientX;
      state.velocity = 0;
      carousel.setPointerCapture?.(event.pointerId);
      carousel.classList.add("is-dragging");
    });

    on(carousel, "pointermove", event => {
      if (!state.dragging) return;
      const delta = event.clientX - state.dragStartX;
      state.velocity = event.clientX - state.dragLastX;
      state.dragLastX = event.clientX;
      state.dragMoved = state.dragMoved || Math.abs(delta) > 6;
      state.dragOffset = clamp(delta / 210, -2.4, 2.4);
      state.visualIndex = state.currentIndex - state.dragOffset;
      renderCards(state.visualIndex);
    });

    const release = () => {
      if (!state.dragging) return;
      state.dragging = false;
      carousel.classList.remove("is-dragging");
      const inertia = clamp(state.velocity / 42, -0.7, 0.7);
      goTo(state.currentIndex - Math.round(state.dragOffset + inertia));
    };

    on(carousel, "pointerup", release);
    on(carousel, "pointercancel", release);
    on(carousel, "wheel", event => {
      if (state.detailOpen) return;
      event.preventDefault();
      goTo(state.currentIndex + Math.sign(event.deltaY || event.deltaX));
    }, { passive: false });
  };

  const initButtons = () => {
    const buttons = Array.from(document.querySelectorAll(".intro-primary, .intro-secondary, .intro-control, .intro-filter-chips button, .intro-section-nav button, .intro-detail-close"));
    buttons.forEach(button => {
      on(button, "pointerenter", () => setInteractive(page));
      on(button, "pointerleave", () => {
        unsetInteractive(page);
        button.style.setProperty("--magnet-x", "0px");
        button.style.setProperty("--magnet-y", "0px");
      });
      on(button, "pointermove", event => {
        if (coarsePointer || reducedMotion) return;
        const rect = button.getBoundingClientRect();
        button.style.setProperty("--magnet-x", `${((event.clientX - rect.left) / rect.width - 0.5) * 10}px`);
        button.style.setProperty("--magnet-y", `${((event.clientY - rect.top) / rect.height - 0.5) * 8}px`);
      });
      on(button, "pointerdown", () => page.classList.add("is-pressing"));
      on(button, "pointerup", () => page.classList.remove("is-pressing"));
    });

    Array.from(document.querySelectorAll(".intro-actions a, .intro-detail-link")).forEach(link => {
      on(link, "click", () => {
        link.classList.add("is-loading");
      });
    });

    on(previousButton, "click", () => goTo(state.currentIndex - 1));
    on(nextButton, "click", () => goTo(state.currentIndex + 1));
  };

  const initDetail = () => {
    if (!detail) return;
    Array.from(document.querySelectorAll("[data-intro-detail-close]")).forEach(button => on(button, "click", closeDetail));

    Array.from(document.querySelectorAll("[data-intro-tab]")).forEach(tab => {
      on(tab, "click", () => {
        const name = tab.dataset.introTab;
        document.querySelectorAll("[data-intro-tab]").forEach(item => {
          const active = item === tab;
          item.classList.toggle("is-active", active);
          item.setAttribute("aria-selected", active ? "true" : "false");
        });
        document.querySelectorAll("[data-intro-tab-panel]").forEach(panel => {
          panel.classList.toggle("is-active", panel.dataset.introTabPanel === name);
        });
      });
    });

    on(document, "keydown", event => {
      if (event.key === "Escape") closeDetail();
      if (!state.detailOpen || event.key !== "Tab") return;
      const focusables = Array.from(detail.querySelectorAll(focusableSelector)).filter(item => item instanceof HTMLElement && item.offsetParent !== null);
      if (!focusables.length) return;
      const first = focusables[0];
      const last = focusables[focusables.length - 1];
      if (event.shiftKey && document.activeElement === first) {
        event.preventDefault();
        last.focus();
      } else if (!event.shiftKey && document.activeElement === last) {
        event.preventDefault();
        first.focus();
      }
    });
  };

  const initSearch = () => {
    on(searchInput, "input", event => {
      state.search = event.target.value || "";
      page.classList.add("is-filtering");
      window.setTimeout(() => page.classList.remove("is-filtering"), 280);
      updateFilter();
    });

    filterButtons.forEach(button => {
      on(button, "click", () => {
        state.filter = button.dataset.introFilter || "all";
        filterButtons.forEach(item => item.classList.toggle("is-active", item === button));
        updateFilter();
      });
    });
  };

  const initParallaxAndCursor = () => {
    let cursorAnimationId = 0;
    if (!coarsePointer && !reducedMotion) {
      on(window, "pointermove", event => {
        state.pointer.x = event.clientX;
        state.pointer.y = event.clientY;
        state.pointer.active = true;
      });
    }

    const tick = () => {
      const nx = clamp(state.pointer.x / window.innerWidth - 0.5, -0.5, 0.5);
      const ny = clamp(state.pointer.y / window.innerHeight - 0.5, -0.5, 0.5);
      if (!coarsePointer && !reducedMotion) {
        page.style.setProperty("--mx", `${(nx * 18).toFixed(2)}px`);
        page.style.setProperty("--my", `${(ny * 18).toFixed(2)}px`);
        page.style.setProperty("--bgx", `${(-nx * 16).toFixed(2)}px`);
        page.style.setProperty("--bgy", `${(-ny * 14).toFixed(2)}px`);
        page.style.setProperty("--rx", `${(-ny * 5).toFixed(2)}deg`);
        page.style.setProperty("--ry", `${(nx * 7).toFixed(2)}deg`);
        page.style.setProperty("--glow-x", `${(state.pointer.x + nx * 34).toFixed(1)}px`);
        page.style.setProperty("--glow-y", `${(state.pointer.y + ny * 34).toFixed(1)}px`);
      }

      if (cursor && cursorDot && !coarsePointer) {
        state.cursor.x += (state.pointer.x - state.cursor.x) * 0.18;
        state.cursor.y += (state.pointer.y - state.cursor.y) * 0.18;
        cursor.style.transform = `translate3d(${state.cursor.x}px, ${state.cursor.y}px, 0) translate(-50%, -50%) scale(${page.classList.contains("is-interactive") ? 1.85 : 1})`;
        cursorDot.style.transform = `translate3d(${state.pointer.x}px, ${state.pointer.y}px, 0) translate(-50%, -50%) scale(${page.classList.contains("is-pressing") ? 0.45 : 1})`;
      }

      cursorAnimationId = requestAnimationFrame(tick);
    };

    if (!reducedMotion) {
      cursorAnimationId = requestAnimationFrame(tick);
      cleanup.push(() => cancelAnimationFrame(cursorAnimationId));
    }
  };

  const initScroll = () => {
    let ticking = false;
    const update = () => {
      ticking = false;
      const max = Math.max(1, document.documentElement.scrollHeight - window.innerHeight);
      state.scrollProgress = clamp(window.scrollY / max, 0, 1);
      if (progress) progress.style.transform = `scaleY(${state.scrollProgress})`;
      page.style.setProperty("--scroll-depth", state.scrollProgress.toFixed(3));
      renderCards();
    };

    on(window, "scroll", () => {
      if (!ticking) {
        ticking = true;
        requestAnimationFrame(update);
      }
    }, { passive: true });

    const revealObserver = new IntersectionObserver(entries => {
      entries.forEach(entry => entry.target.classList.toggle("is-visible", entry.isIntersecting));
    }, { threshold: 0.28 });
    revealItems.forEach(item => revealObserver.observe(item));
    cleanup.push(() => revealObserver.disconnect());

    const sectionObserver = new IntersectionObserver(entries => {
      entries.forEach(entry => {
        if (!entry.isIntersecting) return;
        const key = entry.target.dataset.introSection;
        sectionButtons.forEach(button => button.classList.toggle("is-active", button.dataset.introSectionLink === key));
      });
    }, { rootMargin: "-45% 0px -45% 0px", threshold: 0 });
    sections.forEach(section => sectionObserver.observe(section));
    cleanup.push(() => sectionObserver.disconnect());

    sectionButtons.forEach(button => {
      on(button, "click", () => {
        const target = document.querySelector(`[data-intro-section="${button.dataset.introSectionLink}"]`);
        target?.scrollIntoView({ behavior: reducedMotion ? "auto" : "smooth", block: "start" });
      });
    });

    update();
  };

  const initKeyboard = () => {
    on(window, "keydown", event => {
      if (state.detailOpen) return;
      if (event.key === "ArrowLeft") goTo(state.currentIndex - 1);
      if (event.key === "ArrowRight") goTo(state.currentIndex + 1);
    });
  };

  const initParticles = () => {
    if (!canvas) return;
    const context = canvas.getContext("2d");
    if (!context) return;

    const particles = [];
    const ripples = [];
    const colors = ["#62F5D2", "#44D9FF", "#516CFF", "#8B5CFF", "#FF4FA3", "#F6D36B", "#F7F7FA"];
    let width = 0;
    let height = 0;
    let animationId = 0;

    const particleCount = () => {
      if (reducedMotion) return 90;
      if (window.innerWidth < 640) return 120;
      if (window.innerWidth < 1024) return 220;
      return 420;
    };

    const resize = () => {
      const ratio = Math.min(window.devicePixelRatio || 1, 2);
      width = canvas.clientWidth;
      height = canvas.clientHeight;
      canvas.width = Math.floor(width * ratio);
      canvas.height = Math.floor(height * ratio);
      context.setTransform(ratio, 0, 0, ratio, 0, 0);
    };

    const createParticles = () => {
      particles.length = 0;
      for (let i = 0; i < particleCount(); i += 1) {
        const depth = Math.random();
        particles.push({
          x: width * (0.54 + (Math.random() - 0.5) * 0.78),
          y: height * (0.5 + (Math.random() - 0.5) * 1.08),
          vx: (Math.random() - 0.5) * (0.2 + depth * 1.0),
          vy: (Math.random() - 0.5) * (0.18 + depth * 0.9),
          size: 0.55 + depth * 2.9,
          depth,
          color: colors[Math.floor(Math.random() * colors.length)],
          phase: Math.random() * Math.PI * 2
        });
      }
    };

    const addRipple = event => {
      const rect = canvas.getBoundingClientRect();
      ripples.push({
        x: event.clientX - rect.left,
        y: event.clientY - rect.top,
        radius: 2,
        life: 1
      });
    };

    on(page, "click", addRipple);

    const draw = time => {
      context.clearRect(0, 0, width, height);
      context.globalCompositeOperation = "lighter";
      const pointerX = state.pointer.x + (window.innerWidth * 0.5 - state.pointer.x) * 0.04;
      const pointerY = state.pointer.y + (window.innerHeight * 0.5 - state.pointer.y) * 0.04;

      particles.forEach(particle => {
        const pullX = width * 0.58 - (state.pointer.x / window.innerWidth - 0.5) * 38;
        const pullY = height * 0.48 - (state.pointer.y / window.innerHeight - 0.5) * 32;
        const dx = pullX - particle.x;
        const dy = pullY - particle.y;
        const dist = Math.max(70, Math.sqrt(dx * dx + dy * dy));
        const pointerDx = particle.x - pointerX;
        const pointerDy = particle.y - pointerY;
        const pointerDist = Math.max(1, Math.sqrt(pointerDx * pointerDx + pointerDy * pointerDy));
        const repel = pointerDist < 120 ? (120 - pointerDist) / 120 : 0;
        const speed = reducedMotion ? 0.22 : 1;
        const swirl = Math.sin(time * 0.0012 + particle.phase) * (0.32 + particle.depth * 0.68);

        particle.vx += ((dx / dist) * 0.014 + (-dy / dist) * 0.013 * swirl + (pointerDx / pointerDist) * repel * 0.22) * speed;
        particle.vy += ((dy / dist) * 0.008 + (dx / dist) * 0.011 * swirl + (pointerDy / pointerDist) * repel * 0.22) * speed;
        particle.vx *= 0.962;
        particle.vy *= 0.962;
        particle.x += particle.vx * (0.45 + particle.depth * 1.28);
        particle.y += particle.vy * (0.45 + particle.depth * 1.1);

        if (particle.x < -50 || particle.x > width + 50 || particle.y < -50 || particle.y > height + 50) {
          particle.x = width * (0.5 + (Math.random() - 0.5) * 0.34);
          particle.y = height * Math.random();
          particle.vx = (Math.random() - 0.5) * 0.5;
          particle.vy = (Math.random() - 0.5) * 0.5;
        }

        context.beginPath();
        context.fillStyle = particle.color;
        context.globalAlpha = clamp(0.2 + particle.depth * 0.44 + Math.sin(time * 0.002 + particle.phase) * 0.13 + (page.classList.contains("is-interactive") ? 0.08 : 0), 0.08, 0.78);
        context.shadowBlur = particle.depth > 0.72 ? 13 : 0;
        context.shadowColor = particle.color;
        context.arc(particle.x, particle.y, particle.size, 0, Math.PI * 2);
        context.fill();
      });

      for (let i = ripples.length - 1; i >= 0; i -= 1) {
        const ripple = ripples[i];
        context.beginPath();
        context.globalAlpha = ripple.life * 0.56;
        context.strokeStyle = "#62F5D2";
        context.lineWidth = 1.4;
        context.arc(ripple.x, ripple.y, ripple.radius, 0, Math.PI * 2);
        context.stroke();
        ripple.radius += 3.8;
        ripple.life -= 0.028;
        if (ripple.life <= 0) ripples.splice(i, 1);
      }

      context.shadowBlur = 0;
      context.globalAlpha = 1;
      animationId = requestAnimationFrame(draw);
    };

    resize();
    createParticles();
    on(window, "resize", () => {
      resize();
      createParticles();
    });
    animationId = requestAnimationFrame(draw);
    cleanup.push(() => cancelAnimationFrame(animationId));
  };

  initCards();
  initDrag();
  initButtons();
  initDetail();
  initSearch();
  initParallaxAndCursor();
  initScroll();
  initKeyboard();
  initParticles();
  renderCards();

  on(window, "beforeunload", () => cleanup.forEach(dispose => dispose()));
}());

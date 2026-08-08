(() => {
  document.querySelectorAll(".muva-marquee-track").forEach((track) => {
    if (track.dataset.cloned === "true") return;
    track.innerHTML += track.innerHTML;
    track.dataset.cloned = "true";
  });

  const revealItems = document.querySelectorAll("[data-muva-reveal]");
  if (revealItems.length) {
    const revealObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        entry.target.classList.add("in");
        revealObserver.unobserve(entry.target);
      });
    }, { threshold: 0.12 });

    revealItems.forEach((item) => revealObserver.observe(item));
  }

  const animateCount = (element) => {
    const target = Number.parseFloat(element.dataset.muvaCount || "0");
    let start = null;

    const step = (time) => {
      start ??= time;
      const progress = Math.min((time - start) / 1400, 1);
      const eased = 1 - Math.pow(1 - progress, 3);
      element.textContent = Math.round(target * eased).toString();
      if (progress < 1) requestAnimationFrame(step);
    };

    requestAnimationFrame(step);
  };

  const counters = document.querySelectorAll("[data-muva-count]");
  if (counters.length) {
    const countObserver = new IntersectionObserver((entries) => {
      entries.forEach((entry) => {
        if (!entry.isIntersecting) return;
        animateCount(entry.target);
        countObserver.unobserve(entry.target);
      });
    }, { threshold: 0.6 });

    counters.forEach((counter) => countObserver.observe(counter));
  }
})();

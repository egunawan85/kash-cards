/* ============================================================
   Kash v2 — scroll-driven motion engine
   ============================================================ */
(function () {
  'use strict';
  const reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

  /* ---------- Sticky nav shadow ---------- */
  const nav = document.querySelector('.nav');

  /* ---------- Scroll reveal ---------- */
  const reveals = document.querySelectorAll('.reveal');
  if ('IntersectionObserver' in window && reveals.length) {
    const io = new IntersectionObserver((entries) => {
      entries.forEach((e) => { if (e.isIntersecting) { e.target.classList.add('in'); io.unobserve(e.target); } });
    }, { threshold: 0.12, rootMargin: '0px 0px -8% 0px' });
    reveals.forEach((el) => io.observe(el));
  } else { reveals.forEach((el) => el.classList.add('in')); }

  /* ---------- Count-up stats ---------- */
  const counters = document.querySelectorAll('[data-count]');
  if ('IntersectionObserver' in window && counters.length) {
    const cio = new IntersectionObserver((entries) => {
      entries.forEach((e) => {
        if (!e.isIntersecting) return;
        cio.unobserve(e.target);
        const el = e.target;
        const target = parseFloat(el.dataset.count);
        const suffix = el.dataset.suffix || '';
        const prefix = el.dataset.prefix || '';
        const dec = (el.dataset.dec | 0);
        if (reduce) { el.textContent = prefix + target.toFixed(dec) + suffix; return; }
        const dur = 1400; let start = null;
        const tick = (t) => {
          if (start === null) start = t;
          const p = Math.min((t - start) / dur, 1);
          const eased = 1 - Math.pow(1 - p, 3);
          el.textContent = prefix + (target * eased).toFixed(dec) + suffix;
          if (p < 1) requestAnimationFrame(tick);
          else el.textContent = prefix + target.toFixed(dec) + suffix;
        };
        requestAnimationFrame(tick);
      });
    }, { threshold: 0.6 });
    counters.forEach((el) => cio.observe(el));
  }

  /* ---------- Hero card mouse tilt + idle float ---------- */
  const card = document.querySelector('.hero .card-3d');
  if (card && !reduce) {
    const stage = card.closest('.card-stage') || document.body;
    let raf = null, tx = 0, ty = 0, cx = 0, cy = 0;
    const apply = () => {
      cx += (tx - cx) * 0.12; cy += (ty - cy) * 0.12;
      card.style.transform = `rotateY(${cx}deg) rotateX(${-cy}deg)`;
      const face = card.querySelector('.qcard');
      if (face) { face.style.setProperty('--mx', `${cx * 2.2}%`); face.style.setProperty('--my', `${cy * 2.2}%`); }
      if (Math.abs(tx - cx) > 0.05 || Math.abs(ty - cy) > 0.05) raf = requestAnimationFrame(apply);
      else raf = null;
    };
    const queue = () => { if (!raf) raf = requestAnimationFrame(apply); };
    stage.addEventListener('mousemove', (e) => {
      const r = stage.getBoundingClientRect();
      tx = ((e.clientX - r.left) / r.width - 0.5) * 26;
      ty = ((e.clientY - r.top) / r.height - 0.5) * 26;
      card.classList.remove('card-float'); queue();
    });
    stage.addEventListener('mouseleave', () => { tx = 0; ty = 0; queue(); setTimeout(() => card.classList.add('card-float'), 400); });
    card.classList.add('card-float');
  }

  /* ---------- Unified scroll loop: progress bar + parallax + scrollytelling ---------- */
  const bar = document.querySelector('.progress-bar');
  const auras = document.querySelectorAll('.atmosphere .aura');
  const parallaxEls = document.querySelectorAll('[data-parallax]');
  const scrolly = document.querySelector('.scrolly-track');
  const scrollyCard = document.querySelector('.scrolly-card');
  const steps = document.querySelectorAll('.s-step');
  let ticking = false;

  function onFrame() {
    const y = window.scrollY;
    const docH = document.documentElement.scrollHeight - window.innerHeight;
    const vh = window.innerHeight;

    if (nav) nav.classList.toggle('scrolled', y > 12);

    if (bar) bar.style.width = (docH > 0 ? (y / docH) * 100 : 0) + '%';

    if (!reduce) {
      // background auras drift slowly with scroll
      auras.forEach((a, i) => {
        const speed = i === 0 ? 0.06 : -0.05;
        a.style.transform = `translateY(${y * speed}px)`;
      });
      // generic parallax elements
      parallaxEls.forEach((el) => {
        const speed = parseFloat(el.dataset.parallax) || 0.15;
        const rect = el.getBoundingClientRect();
        const offset = (rect.top + rect.height / 2) - vh / 2;
        el.style.transform = `translateY(${offset * -speed}px)`;
      });

      // scrollytelling: progress through the pinned track drives active step + card spin
      if (scrolly && steps.length) {
        const r = scrolly.getBoundingClientRect();
        const total = r.height - vh;
        let p = total > 0 ? (-r.top) / total : 0;
        p = Math.max(0, Math.min(1, p));
        const idx = Math.min(steps.length - 1, Math.floor(p * steps.length));
        steps.forEach((s, i) => s.classList.toggle('active', i === idx));
        if (scrollyCard) {
          const rot = p * 360;
          const lift = Math.sin(p * Math.PI) * 18;
          scrollyCard.style.transform = `rotateY(${rot}deg) translateY(${-lift}px)`;
        }
      }
    }
    ticking = false;
  }
  function onScroll() { if (!ticking) { ticking = true; requestAnimationFrame(onFrame); } }
  window.addEventListener('scroll', onScroll, { passive: true });
  window.addEventListener('resize', onScroll, { passive: true });
  onFrame();

  /* ---------- Password reveal toggles ---------- */
  const EYE = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M2 12s3.6-7 10-7 10 7 10 7-3.6 7-10 7-10-7-10-7z"/><circle cx="12" cy="12" r="3"/></svg>';
  const EYE_OFF = '<svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><path d="M3 3l18 18M10.6 10.6a3 3 0 004.2 4.2M9.4 5.2A9.6 9.6 0 0112 5c6.4 0 10 7 10 7a17 17 0 01-3.6 4.4M6.2 6.2A17 17 0 002 12s3.6 7 10 7a9.7 9.7 0 003.3-.6"/></svg>';
  document.querySelectorAll('[data-toggle-pw]').forEach((btn) => {
    btn.addEventListener('click', () => {
      const input = btn.parentElement.querySelector('input'); if (!input) return;
      const show = input.type === 'password';
      input.type = show ? 'text' : 'password';
      btn.innerHTML = show ? EYE_OFF : EYE;
    });
  });

  /* ---------- Password strength ---------- */
  const pw = document.querySelector('[data-strength-input]');
  const meter = document.querySelector('[data-strength]');
  if (pw && meter) {
    pw.addEventListener('input', () => {
      const v = pw.value; let s = 0;
      if (v.length >= 8) s++;
      if (/[A-Z]/.test(v) && /[a-z]/.test(v)) s++;
      if (/\d/.test(v)) s++;
      if (/[^A-Za-z0-9]/.test(v)) s++;
      meter.className = 'strength' + (v ? ' s' + s : '');
    });
  }

  /* ---------- Demo form states ---------- */
  document.querySelectorAll('[data-demo-form]').forEach((form) => {
    form.addEventListener('submit', (e) => {
      e.preventDefault();
      const btn = form.querySelector('button[type="submit"]'); if (!btn) return;
      const original = btn.innerHTML;
      btn.disabled = true; btn.innerHTML = '<span class="spin"></span> Processing…';
      setTimeout(() => { btn.innerHTML = '✓ Success'; setTimeout(() => { btn.disabled = false; btn.innerHTML = original; }, 1400); }, 1100);
    });
  });
})();

/* ============================================================
   Kash — Premium page interactions
   GSAP + ScrollTrigger + Lenis (UMD globals). Degrades safely.
   ============================================================ */
(function () {
  'use strict';
  var gsap = window.gsap, ST = window.ScrollTrigger;
  var reduce = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
  var animate = !!gsap && !reduce;

  if (animate) {
    document.documentElement.classList.add('anim');
    if (ST) gsap.registerPlugin(ST);
  }

  document.addEventListener('DOMContentLoaded', init);
  if (document.readyState !== 'loading') init();
  var started = false;
  function init() {
    if (started) return; started = true;

    // Lenis smooth scroll
    var lenis = null;
    if (animate && window.Lenis) {
      lenis = new window.Lenis({ lerp: 0.1, smoothWheel: true });
      if (ST) lenis.on('scroll', ST.update);
      gsap.ticker.add(function (t) { lenis.raf(t * 1000); });
      gsap.ticker.lagSmoothing(0);
    }

    // nav state + hide on scroll down
    var nav = document.querySelector('.nav');
    var lastY = 0;
    function onScroll() {
      var y = window.scrollY || window.pageYOffset;
      if (nav) {
        nav.classList.toggle('scrolled', y > 30);
        nav.style.transform = (y > lastY && y > 320) ? 'translateY(-130%)' : 'translateY(0)';
      }
      lastY = y;
    }
    window.addEventListener('scroll', onScroll, { passive: true });
    onScroll();

    // smooth anchor scroll
    document.querySelectorAll('a[href^="#"]').forEach(function (a) {
      a.addEventListener('click', function (e) {
        var id = a.getAttribute('href');
        if (id.length < 2) return;
        var el = document.querySelector(id);
        if (!el) return;
        e.preventDefault();
        if (lenis) lenis.scrollTo(el, { offset: -80 });
        else el.scrollIntoView({ behavior: 'smooth' });
      });
    });

    if (!animate) { document.querySelectorAll('.reveal').forEach(function (el) { el.classList.add('in'); }); return; }

    // reveals
    if (ST) {
      gsap.utils.toArray('.reveal').forEach(function (el) {
        ST.create({ trigger: el, start: 'top 96%', once: true, onEnter: function () { el.classList.add('in'); } });
      });

      // image parallax
      gsap.utils.toArray('[data-parallax]').forEach(function (el) {
        var sp = parseFloat(el.getAttribute('data-parallax')) || 0.12;
        gsap.fromTo(el, { yPercent: -sp * 50 }, {
          yPercent: sp * 50, ease: 'none',
          scrollTrigger: { trigger: el.closest('section') || el, start: 'top bottom', end: 'bottom top', scrub: true }
        });
      });

      // count-ups
      gsap.utils.toArray('[data-count]').forEach(function (el) {
        var target = parseFloat(el.getAttribute('data-count'));
        var dec = parseInt(el.getAttribute('data-dec') || '0', 10);
        var pre = el.getAttribute('data-prefix') || '', suf = el.getAttribute('data-suffix') || '';
        var o = { v: 0 };
        ST.create({
          trigger: el, start: 'top 92%', once: true, onEnter: function () {
            gsap.to(o, { v: target, duration: 1.6, ease: 'power3.out',
              onUpdate: function () { el.textContent = pre + o.v.toFixed(dec) + suf; },
              onComplete: function () { el.textContent = pre + target.toFixed(dec) + suf; } });
          }
        });
      });
    }

    // hero intro
    var tl = gsap.timeline();
    tl.from('.hero-badge', { y: 18, opacity: 0, duration: 0.6, ease: 'power3.out' })
      .from('.hero h1', { y: 30, opacity: 0, duration: 0.9, ease: 'power4.out' }, '-=0.3')
      .from('.hero .lede', { y: 24, opacity: 0, duration: 0.7 }, '-=0.55')
      .from('.hero-actions > *', { y: 22, opacity: 0, duration: 0.6, stagger: 0.1 }, '-=0.5')
      .from('.hero-note', { opacity: 0, duration: 0.5 }, '-=0.35')
      .from('.hero-stat', { y: 18, opacity: 0, duration: 0.6, stagger: 0.08 }, '-=0.4')
      .from('.hero-card img', { opacity: 0, scale: 0.86, rotation: -4, duration: 1.2, ease: 'power3.out' }, 0.1)
      .from('.scroll-cue', { opacity: 0, duration: 0.6 }, '-=0.2');

    // FAQ accordion
    document.querySelectorAll('.faq-item').forEach(function (item) {
      var q = item.querySelector('.faq-q');
      var a = item.querySelector('.faq-a');
      if (!q || !a) return;
      q.setAttribute('aria-expanded', 'false');
      q.addEventListener('click', function () {
        var open = item.classList.contains('open');
        // close siblings
        document.querySelectorAll('.faq-item.open').forEach(function (o) {
          if (o !== item) { o.classList.remove('open'); o.querySelector('.faq-a').style.height = '0px'; o.querySelector('.faq-q').setAttribute('aria-expanded', 'false'); }
        });
        if (open) { item.classList.remove('open'); a.style.height = '0px'; q.setAttribute('aria-expanded', 'false'); }
        else { item.classList.add('open'); a.style.height = a.firstElementChild.offsetHeight + 'px'; q.setAttribute('aria-expanded', 'true'); }
      });
    });

    // form demo
    document.querySelectorAll('[data-demo-form]').forEach(function (form) {
      form.addEventListener('submit', function (e) {
        e.preventDefault();
        var btn = form.querySelector('button[type="submit"]'); if (!btn) return;
        var orig = btn.innerHTML; btn.disabled = true; btn.textContent = 'Reserving...';
        setTimeout(function () { btn.textContent = 'Reserved'; setTimeout(function () { btn.disabled = false; btn.innerHTML = orig; }, 1500); }, 1000);
      });
    });
  }
})();

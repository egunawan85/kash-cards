/* ============================================================
   KASH — Dashboard interactions (front-end only)
   Mobile sidebar, copy-to-clipboard, freeze toggle (demo).
   ============================================================ */
(function () {
  'use strict';

  // Mobile sidebar
  var side = document.getElementById('side');
  var scrim = document.getElementById('scrim');
  var menu = document.getElementById('menu');
  function openNav() { side.classList.add('open'); scrim.classList.add('show'); }
  function closeNav() { side.classList.remove('open'); scrim.classList.remove('show'); }
  if (menu) menu.addEventListener('click', openNav);
  if (scrim) scrim.addEventListener('click', closeNav);

  // Copy buttons
  document.querySelectorAll('[data-copy]').forEach(function (btn) {
    btn.addEventListener('click', function () {
      var el = document.querySelector(btn.getAttribute('data-copy'));
      if (!el) return;
      var text = el.value || el.textContent;
      var done = function () {
        btn.classList.add('done');
        setTimeout(function () { btn.classList.remove('done'); }, 1400);
      };
      if (navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(text).then(done, function () { el.select && el.select(); done(); });
      } else {
        el.select && el.select(); try { document.execCommand('copy'); } catch (e) {} done();
      }
    });
  });

  // Freeze toggle (demo)
  var freeze = document.getElementById('freeze');
  if (freeze) {
    freeze.addEventListener('click', function () {
      var frozen = freeze.classList.toggle('done');
      freeze.style.borderColor = frozen ? 'var(--cyan)' : '';
      freeze.style.color = frozen ? 'var(--cyan-bright)' : '';
      freeze.lastChild.textContent = frozen ? ' Frozen' : ' Freeze';
    });
  }
})();

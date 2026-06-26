/* ============================================================
   KASH — OTP verification (front-end only; wire to your backend)
   6-digit code: auto-advance, backspace, paste-to-fill, auto-submit.
   ============================================================ */
(function () {
  'use strict';
  var wrap = document.getElementById('otp');
  if (!wrap) return;
  var inputs = Array.prototype.slice.call(wrap.querySelectorAll('input'));
  var form = document.getElementById('otp-form');
  var err = document.getElementById('err');
  var btn = document.getElementById('verify-btn');

  // Prefill destination email from ?email= if present
  try {
    var em = new URLSearchParams(location.search).get('email');
    if (em) document.getElementById('dest').textContent = em;
  } catch (e) {}

  function code() { return inputs.map(function (i) { return i.value; }).join(''); }
  function clearErr() { err.textContent = ''; }
  function focusAt(i) { if (inputs[i]) inputs[i].focus(); }

  inputs.forEach(function (input, idx) {
    input.addEventListener('input', function () {
      clearErr();
      // keep only the last typed digit
      input.value = input.value.replace(/\D/g, '').slice(-1);
      input.classList.toggle('filled', !!input.value);
      if (input.value && idx < inputs.length - 1) focusAt(idx + 1);
      if (code().length === 6) submit();
    });
    input.addEventListener('keydown', function (e) {
      if (e.key === 'Backspace' && !input.value && idx > 0) { focusAt(idx - 1); }
      else if (e.key === 'ArrowLeft' && idx > 0) { e.preventDefault(); focusAt(idx - 1); }
      else if (e.key === 'ArrowRight' && idx < inputs.length - 1) { e.preventDefault(); focusAt(idx + 1); }
    });
    input.addEventListener('paste', function (e) {
      e.preventDefault();
      var digits = (e.clipboardData || window.clipboardData).getData('text').replace(/\D/g, '').slice(0, 6);
      if (!digits) return;
      digits.split('').forEach(function (d, k) { if (inputs[k]) { inputs[k].value = d; inputs[k].classList.add('filled'); } });
      focusAt(Math.min(digits.length, inputs.length - 1));
      if (digits.length === 6) submit();
    });
  });

  focusAt(0);

  var submitting = false;
  function submit() {
    if (submitting) return;
    if (code().length !== 6) { err.textContent = 'Please enter all 6 digits.'; return; }
    submitting = true;
    var original = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<span class="spin"></span> Verifying…';
    // DEMO: replace with a real POST to your verify endpoint.
    setTimeout(function () {
      btn.innerHTML = 'Verified ✓';
      setTimeout(function () { window.location.href = 'dashboard.html'; }, 600);
    }, 1000);
  }
  if (form) form.addEventListener('submit', function (e) { e.preventDefault(); submit(); });

  /* Resend countdown */
  var resendBtn = document.getElementById('resend');
  var countEl = document.getElementById('count');
  var t = 30, timer;
  function tick() {
    t -= 1;
    if (t <= 0) {
      clearInterval(timer);
      resendBtn.disabled = false;
      resendBtn.textContent = 'Resend code';
    } else {
      countEl.textContent = t;
    }
  }
  function startCountdown() {
    t = 30; resendBtn.disabled = true;
    resendBtn.innerHTML = 'Resend in <span id="count">30</span>s';
    countEl = document.getElementById('count');
    clearInterval(timer); timer = setInterval(tick, 1000);
  }
  if (resendBtn) {
    startCountdown();
    resendBtn.addEventListener('click', function () {
      if (resendBtn.disabled) return;
      // DEMO: trigger your resend endpoint here.
      inputs.forEach(function (i) { i.value = ''; i.classList.remove('filled'); });
      focusAt(0); clearErr();
      startCountdown();
    });
  }
})();

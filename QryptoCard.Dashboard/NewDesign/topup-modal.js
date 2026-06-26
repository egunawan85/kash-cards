/* ============================================================
   KASH — Top up / Deposit popup (drop-in)
   Self-contained: injects its own styles + markup.
   Open it from any page by adding this script and triggering:
     - <button data-topup>            -> deposit to an existing card (with card picker)
     - <button data-topup data-card-last="8317">  -> preselect that card
     - <button data-topup-new data-product="Kash Pro" data-bin="471699"> -> deposit to a card you're buying
   It also intercepts clicks on <a href="topup.html..."> so existing links/nav open the popup.
   Front-end demo — wire Confirm + address/QR to your backend.
   ============================================================ */
(function () {
  'use strict';
  var FEE = 0.03;

  // Existing cards available to fund (replace with the user's real cards).
  var CARDS = [
    { name: 'Kash Global', last: '8317' }, { name: 'Kash Commerce', last: '4471' },
    { name: 'Kash Pro', last: '2093' }, { name: 'Kash Travel', last: '6628' },
    { name: 'Kash Lite', last: '9942' }, { name: 'Kash Max', last: '3351' }
  ];
  // apply any custom labels the user set on My Cards
  try {
    var saved = JSON.parse(localStorage.getItem('kash-card-labels') || '{}');
    CARDS.forEach(function (c) { if (saved[c.last]) c.label = saved[c.last]; });
  } catch (e) {}
  function lbl(c) { return c.label || c.name; }

  var ADDR = { TRON: 'TJYeQ8m2vF4kP9xLrN7sUqH3dWbZ6aXcKt', ETH: '0x71C7656EC7ab88b098defB751B7401B5f6d8976F' };

  /* ---- styles ---- */
  var css = '' +
    '.tu-modal{position:fixed;inset:0;z-index:300;display:none;place-items:center;padding:20px;background:rgba(3,5,8,.72);backdrop-filter:blur(7px)}' +
    '.tu-modal.open{display:grid}' +
    '.tu-card{width:100%;max-width:440px;max-height:92vh;overflow:auto;background:var(--panel,#0b1017);border:1px solid var(--line,rgba(255,255,255,.09));border-radius:22px;box-shadow:0 30px 80px rgba(0,0,0,.6);position:relative}' +
    '.tu-card::before{content:"";position:absolute;top:0;left:0;right:0;height:2px;background:linear-gradient(90deg,transparent,var(--cyan,#00e6ff),transparent);opacity:.6}' +
    '.tu-head{display:flex;align-items:center;justify-content:space-between;padding:22px 24px 16px;border-bottom:1px solid var(--line,rgba(255,255,255,.09))}' +
    '.tu-head h3{font-family:var(--font-display,sans-serif);font-weight:700;font-size:1.2rem;letter-spacing:-.01em;color:var(--ink,#eef3f8)}' +
    '.tu-x{width:32px;height:32px;border-radius:9px;border:1px solid var(--line-2,rgba(255,255,255,.16));background:rgba(255,255,255,.03);color:var(--ink-3,#6f7b89);cursor:pointer;display:grid;place-items:center}' +
    '.tu-x:hover{color:var(--ink,#fff);border-color:var(--cyan,#00e6ff)}' +
    '.tu-body{padding:22px 24px}' +
    '.tu-l{display:block;font-size:.8rem;color:var(--ink-2,#aab6c4);font-weight:600;margin:0 0 8px}' +
    '.tu-target{display:flex;align-items:center;gap:12px;padding:12px 14px;border-radius:13px;border:1px solid rgba(0,230,255,.25);background:rgba(0,230,255,.06);margin-bottom:18px}' +
    '.tu-target .tic{width:40px;height:27px;border-radius:6px;flex:0 0 auto;background:linear-gradient(135deg,#0c121a,#16222e 55%,#0b2b32);border:1px solid var(--line-2,rgba(255,255,255,.16));position:relative}' +
    '.tu-target .tic::after{content:"";position:absolute;left:6px;top:8px;width:10px;height:7px;border-radius:2px;background:linear-gradient(135deg,#e9c97a,#b8862f)}' +
    '.tu-target .tnm{font-weight:600;font-size:.95rem;color:var(--ink,#fff)}' +
    '.tu-target .tsub{font-size:.78rem;color:var(--ink-3,#6f7b89)}' +
    '.tu-target .tnew{margin-left:auto;font-family:var(--font-mono,monospace);font-size:.6rem;letter-spacing:.1em;text-transform:uppercase;color:var(--cyan,#00e6ff);background:rgba(0,230,255,.1);border:1px solid rgba(0,230,255,.3);padding:3px 8px;border-radius:999px}' +
    '.tu-sel,.tu-amtbox input{width:100%;color:var(--ink,#fff);background:rgba(255,255,255,.03);border:1px solid var(--line-2,rgba(255,255,255,.16));border-radius:13px;outline:none;font-family:inherit}' +
    '.tu-sel{appearance:none;-webkit-appearance:none;padding:.85em 2.4em .85em 1em;font-size:.95rem;cursor:pointer;margin-bottom:18px;background-image:url("data:image/svg+xml,%3Csvg xmlns=\'http://www.w3.org/2000/svg\' width=\'18\' height=\'18\' fill=\'none\' stroke=\'%236f7b89\' stroke-width=\'2\'%3E%3Cpath d=\'M4 7l5 5 5-5\'/%3E%3C/svg%3E");background-repeat:no-repeat;background-position:right 14px center}' +
    '.tu-sel:focus{border-color:var(--cyan,#00e6ff)}' +
    '.tu-sel option{background:#0b1017;color:#eef3f8}' +
    '.tu-amtbox{position:relative;margin-bottom:12px}' +
    '.tu-amtbox input{padding:.95em 1em .95em 3.4em;font-family:var(--font-display,sans-serif);font-weight:700;font-size:1.5rem;letter-spacing:-.02em;-moz-appearance:textfield}' +
    '.tu-amtbox input::-webkit-outer-spin-button,.tu-amtbox input::-webkit-inner-spin-button{-webkit-appearance:none;margin:0}' +
    '.tu-amtbox input:focus{border-color:var(--cyan,#00e6ff)}' +
    '.tu-amtbox .tu-ccy{position:absolute;left:1em;top:50%;transform:translateY(-50%);font-family:var(--font-mono,monospace);font-size:.85rem;color:var(--ink-3,#6f7b89);font-weight:600}' +
    '.tu-chips{display:grid;grid-template-columns:repeat(4,1fr);gap:9px;margin-bottom:20px}' +
    '.tu-chip{padding:.62em;border-radius:11px;border:1px solid var(--line-2,rgba(255,255,255,.16));background:rgba(255,255,255,.03);color:var(--ink-2,#aab6c4);font-family:var(--font-mono,monospace);font-weight:600;font-size:.86rem;cursor:pointer;transition:border-color .2s,color .2s,background .2s}' +
    '.tu-chip:hover{color:var(--ink,#fff)}' +
    '.tu-chip.on{border-color:var(--cyan,#00e6ff);color:var(--cyan-bright,#6ff6ff);background:rgba(0,230,255,.1)}' +
    '.tu-sum{border:1px solid var(--line,rgba(255,255,255,.09));border-radius:14px;background:rgba(255,255,255,.02);padding:4px 16px;margin-bottom:6px}' +
    '.tu-pay{display:flex;align-items:center;justify-content:space-between;padding:14px 0;border-bottom:1px solid var(--line,rgba(255,255,255,.09))}' +
    '.tu-pay span{color:var(--ink,#fff);font-weight:600}.tu-pay b{font-family:var(--font-display,sans-serif);font-weight:700;font-size:1.2rem;color:var(--cyan-bright,#6ff6ff)}' +
    '.tu-row{display:flex;align-items:center;justify-content:space-between;padding:10px 0;font-size:.88rem;color:var(--ink-3,#6f7b89)}.tu-row span:last-child{font-family:var(--font-mono,monospace);color:var(--ink-2,#aab6c4)}' +
    '.tu-foot{display:flex;gap:10px;padding:16px 24px 22px}' +
    '.tu-foot .btn{flex:1}' +
    /* step 2 */
    '.tu-step2{display:none;text-align:center}' +
    '.tu-card.s2 .tu-step1{display:none}.tu-card.s2 .tu-step2{display:block}' +
    '.tu-qr{width:172px;height:172px;margin:6px auto 18px;padding:14px;border-radius:16px;background:#fff;color:#05070a;box-shadow:0 8px 30px rgba(0,0,0,.35)}' +
    '.tu-sendamt{font-family:var(--font-display,sans-serif);font-weight:800;font-size:1.7rem;letter-spacing:-.02em;color:var(--ink,#fff);margin-bottom:4px}' +
    '.tu-sendnote{color:var(--ink-3,#6f7b89);font-size:.86rem;margin-bottom:18px}' +
    '.tu-addr{display:flex;gap:8px;margin-bottom:16px}' +
    '.tu-addr input{flex:1;min-width:0;padding:.8em 1em;border-radius:12px;border:1px solid var(--line-2,rgba(255,255,255,.16));background:rgba(255,255,255,.03);color:var(--ink,#fff);font-family:var(--font-mono,monospace);font-size:.82rem;outline:none}' +
    '.tu-copy{flex:0 0 auto;width:44px;border-radius:12px;border:1px solid var(--line-2,rgba(255,255,255,.16));background:rgba(255,255,255,.04);color:var(--ink-3,#6f7b89);cursor:pointer;display:grid;place-items:center}' +
    '.tu-copy:hover,.tu-copy.done{border-color:var(--cyan,#00e6ff);color:var(--cyan-bright,#6ff6ff)}' +
    '.tu-warn{display:flex;gap:10px;text-align:left;padding:12px 14px;border-radius:12px;border:1px solid rgba(255,176,32,.28);background:rgba(255,176,32,.07);color:var(--ink-2,#aab6c4);font-size:.82rem;line-height:1.45;margin-bottom:4px}' +
    '.tu-warn svg{flex:0 0 auto;color:#ffb020;margin-top:1px}';
  var style = document.createElement('style'); style.textContent = css; document.head.appendChild(style);

  /* ---- markup ---- */
  var wrap = document.createElement('div');
  wrap.className = 'tu-modal'; wrap.id = 'tu-modal';
  wrap.setAttribute('role', 'dialog'); wrap.setAttribute('aria-modal', 'true'); wrap.setAttribute('aria-label', 'Top up');
  wrap.innerHTML =
    '<div class="tu-card" id="tu-cardbox">' +
      '<div class="tu-head"><h3 id="tu-title">Top up card</h3><button class="tu-x" id="tu-x" aria-label="Close">' +
        '<svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M18 6L6 18M6 6l12 12"/></svg></button></div>' +
      // STEP 1
      '<div class="tu-step1">' +
        '<div class="tu-body">' +
          '<div id="tu-target-slot"></div>' +
          '<label class="tu-l" for="tu-amt">Deposit amount</label>' +
          '<div class="tu-amtbox"><span class="tu-ccy">USD</span><input id="tu-amt" type="number" inputmode="decimal" min="0" step="1" placeholder="0.00" value="100" autocomplete="off" /></div>' +
          '<div class="tu-chips" id="tu-chips"><button type="button" class="tu-chip" data-a="20">$20</button><button type="button" class="tu-chip on" data-a="100">$100</button><button type="button" class="tu-chip" data-a="250">$250</button><button type="button" class="tu-chip" data-a="500">$500</button></div>' +
          '<label class="tu-l" for="tu-net">Crypto transfer</label>' +
          '<select class="tu-sel" id="tu-net"><option value="TRON">USDT (TRC20)</option><option value="ETH">USDT (ERC20)</option></select>' +
          '<div class="tu-sum">' +
            '<div class="tu-pay"><span>You need to pay</span><b id="tu-pay">103.00 USDT</b></div>' +
            '<div class="tu-row"><span>You will deposit</span><span id="tu-dep">100.00 USD</span></div>' +
            '<div class="tu-row"><span>Deposit fee (3%)</span><span id="tu-fee">3.00 USD</span></div>' +
          '</div>' +
        '</div>' +
        '<div class="tu-foot"><button class="btn btn-line tu-close">Close</button><button class="btn btn-cyan" id="tu-confirm">Confirm deposit</button></div>' +
      '</div>' +
      // STEP 2
      '<div class="tu-step2">' +
        '<div class="tu-body">' +
          '<div class="tu-qr" id="tu-qr">' + qrSvg() + '</div>' +
          '<div class="tu-sendamt" id="tu-sendamt">103.00 USDT</div>' +
          '<div class="tu-sendnote" id="tu-sendnote">Send exactly this amount via USDT (TRC20)</div>' +
          '<label class="tu-l">Deposit address</label>' +
          '<div class="tu-addr"><input id="tu-addr" readonly value="' + ADDR.TRON + '" /><button class="tu-copy" id="tu-copy" aria-label="Copy address"><svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.8"><rect x="9" y="9" width="11" height="11" rx="2"/><path d="M5 15V5a2 2 0 012-2h10"/></svg></button></div>' +
          '<div class="tu-warn"><svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7"><path d="M10.3 3.9 1.8 18a2 2 0 0 0 1.7 3h17a2 2 0 0 0 1.7-3L13.7 3.9a2 2 0 0 0-3.4 0z"/><path d="M12 9v4M12 17h.01"/></svg><span>Only send <b style="color:var(--ink,#fff)">USDT</b> on the selected network. Other assets or networks will be lost.</span></div>' +
        '</div>' +
        '<div class="tu-foot"><button class="btn btn-line" id="tu-back">Back</button><button class="btn btn-cyan tu-close" id="tu-done">Done</button></div>' +
      '</div>' +
    '</div>';

  function qrSvg() {
    return '<svg viewBox="0 0 100 100" width="100%" height="100%"><rect x="6" y="6" width="22" height="22" rx="3" fill="none" stroke="currentColor" stroke-width="4"/><rect x="13" y="13" width="8" height="8" fill="currentColor"/><rect x="72" y="6" width="22" height="22" rx="3" fill="none" stroke="currentColor" stroke-width="4"/><rect x="79" y="13" width="8" height="8" fill="currentColor"/><rect x="6" y="72" width="22" height="22" rx="3" fill="none" stroke="currentColor" stroke-width="4"/><rect x="13" y="79" width="8" height="8" fill="currentColor"/><g fill="currentColor"><rect x="36" y="8" width="5" height="5"/><rect x="46" y="8" width="5" height="5"/><rect x="58" y="8" width="5" height="5"/><rect x="40" y="16" width="5" height="5"/><rect x="62" y="16" width="5" height="5"/><rect x="34" y="24" width="5" height="5"/><rect x="46" y="24" width="5" height="5"/><rect x="58" y="24" width="5" height="5"/><rect x="8" y="36" width="5" height="5"/><rect x="20" y="36" width="5" height="5"/><rect x="32" y="36" width="5" height="5"/><rect x="44" y="36" width="5" height="5"/><rect x="56" y="36" width="5" height="5"/><rect x="68" y="36" width="5" height="5"/><rect x="80" y="36" width="5" height="5"/><rect x="90" y="36" width="5" height="5"/><rect x="14" y="46" width="5" height="5"/><rect x="38" y="46" width="5" height="5"/><rect x="50" y="46" width="5" height="5"/><rect x="62" y="46" width="5" height="5"/><rect x="84" y="46" width="5" height="5"/><rect x="8" y="56" width="5" height="5"/><rect x="44" y="56" width="5" height="5"/><rect x="56" y="56" width="5" height="5"/><rect x="80" y="56" width="5" height="5"/><rect x="36" y="64" width="5" height="5"/><rect x="48" y="64" width="5" height="5"/><rect x="72" y="64" width="5" height="5"/><rect x="36" y="76" width="5" height="5"/><rect x="50" y="76" width="5" height="5"/><rect x="62" y="76" width="5" height="5"/><rect x="74" y="76" width="5" height="5"/><rect x="88" y="76" width="5" height="5"/><rect x="38" y="86" width="5" height="5"/><rect x="60" y="86" width="5" height="5"/><rect x="78" y="86" width="5" height="5"/><rect x="90" y="86" width="5" height="5"/></g></svg>';
  }

  document.addEventListener('DOMContentLoaded', boot);
  if (document.readyState !== 'loading') boot();
  var started = false;
  function boot() {
    if (started) return; started = true;
    document.body.appendChild(wrap);

    var box = document.getElementById('tu-cardbox');
    var title = document.getElementById('tu-title');
    var slot = document.getElementById('tu-target-slot');
    var amt = document.getElementById('tu-amt');
    var net = document.getElementById('tu-net');
    var elPay = document.getElementById('tu-pay'), elDep = document.getElementById('tu-dep'), elFee = document.getElementById('tu-fee');
    var sendAmt = document.getElementById('tu-sendamt'), sendNote = document.getElementById('tu-sendnote'), addr = document.getElementById('tu-addr');

    function fmt(n) { return n.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 }); }
    function netLabel() { return net.value === 'ETH' ? 'USDT (ERC20)' : 'USDT (TRC20)'; }
    function recompute() {
      var a = parseFloat(amt.value); if (!isFinite(a) || a < 0) a = 0;
      var fee = a * FEE, pay = a + fee;
      elPay.textContent = fmt(pay) + ' USDT';
      elDep.textContent = fmt(a) + ' USD';
      elFee.textContent = fmt(fee) + ' USD';
      sendAmt.textContent = fmt(pay) + ' USDT';
      sendNote.textContent = 'Send exactly this amount via ' + netLabel();
      addr.value = ADDR[net.value] || ADDR.TRON;
      Array.prototype.forEach.call(document.querySelectorAll('.tu-chip'), function (c) { c.classList.toggle('on', parseFloat(c.getAttribute('data-a')) === a); });
    }

    function renderTarget(opts) {
      if (opts.mode === 'new') {
        var sub = opts.bin ? ('New card · BIN ' + opts.bin) : 'A new card will be issued';
        slot.innerHTML = '<label class="tu-l">Depositing to</label><div class="tu-target"><span class="tic"></span><span><span class="tnm">' + (opts.product || 'New Kash card') + '</span><br><span class="tsub">' + sub + '</span></span><span class="tnew">New</span></div>';
      } else {
        var options = CARDS.map(function (c) { return '<option value="' + c.last + '"' + (c.last === opts.last ? ' selected' : '') + '>' + lbl(c) + ' · •••• ' + c.last + '</option>'; }).join('');
        slot.innerHTML = '<label class="tu-l" for="tu-card-sel">Card to fund</label><select class="tu-sel" id="tu-card-sel">' + options + '</select>';
      }
    }

    function open(opts) {
      opts = opts || {};
      title.textContent = opts.mode === 'new' ? 'Deposit to your new card' : 'Top up card';
      renderTarget(opts);
      box.classList.remove('s2');
      recompute();
      wrap.classList.add('open');
      setTimeout(function () { amt.focus(); amt.select(); }, 30);
    }
    function close() { wrap.classList.remove('open'); }
    window.KashTopup = { open: open, close: close };

    amt.addEventListener('input', recompute);
    net.addEventListener('change', recompute);
    document.getElementById('tu-chips').addEventListener('click', function (e) {
      var c = e.target.closest('.tu-chip'); if (!c) return;
      amt.value = c.getAttribute('data-a'); recompute(); amt.focus();
    });
    document.getElementById('tu-confirm').addEventListener('click', function () { box.classList.add('s2'); box.scrollTop = 0; });
    document.getElementById('tu-back').addEventListener('click', function () { box.classList.remove('s2'); });
    document.getElementById('tu-copy').addEventListener('click', function () {
      var b = this; if (navigator.clipboard) navigator.clipboard.writeText(addr.value).catch(function () {});
      b.classList.add('done'); setTimeout(function () { b.classList.remove('done'); }, 1300);
    });
    document.getElementById('tu-x').addEventListener('click', close);
    Array.prototype.forEach.call(document.querySelectorAll('.tu-close'), function (b) { b.addEventListener('click', close); });
    wrap.addEventListener('click', function (e) { if (e.target === wrap) close(); });
    document.addEventListener('keydown', function (e) { if (e.key === 'Escape') close(); });

    // global triggers
    document.addEventListener('click', function (e) {
      var nw = e.target.closest('[data-topup-new]');
      if (nw) { e.preventDefault(); open({ mode: 'new', product: nw.getAttribute('data-product'), bin: nw.getAttribute('data-bin') }); return; }
      var ex = e.target.closest('[data-topup]');
      if (ex) { e.preventDefault(); open({ mode: 'existing', last: ex.getAttribute('data-card-last') || CARDS[0].last }); return; }
      var link = e.target.closest('a[href^="topup.html"]');
      if (link) {
        e.preventDefault();
        var qs = {};
        try { new URLSearchParams(link.getAttribute('href').split('?')[1] || '').forEach(function (v, k) { qs[k] = v; }); } catch (err) {}
        if (qs.card === 'new') open({ mode: 'new', product: qs.product, bin: qs.bin });
        else open({ mode: 'existing', last: qs.last || CARDS[0].last });
      }
    });
  }
})();

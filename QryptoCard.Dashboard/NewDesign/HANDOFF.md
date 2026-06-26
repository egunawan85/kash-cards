# Kash (kash.cards) — Developer Handover

A complete, front-end prototype for the Kash product: marketing site + auth flow + full user dashboard. Dark premium theme, electric-cyan accent. **Vanilla HTML/CSS/JS — no build step.**

---

## Run it locally
ES modules aren't used on the shipping pages, but a few use fonts/CDN and relative assets, so serve over HTTP (don't double-click):

```bash
cd "Qryptocards web"
python3 -m http.server 8123
# open http://localhost:8123/premium.html
```

Any static host works (Netlify, Vercel, Cloudflare Pages, S3+CloudFront, nginx).

---

## File map

### Marketing (public, indexed)
| File | Purpose |
|------|---------|
| `premium.html` | **Home page** — hero, features, security, how-it-works, pricing, FAQ, waitlist |
| `terms.html` | Terms of Service |
| `privacy.html` | Privacy Policy |

### Auth flow
| File | Purpose |
|------|---------|
| `login-v2.html` | Sign in (email + password, **email only** — no social) |
| `register-v2.html` | Create account |
| `verify.html` | **Email OTP** (6-digit) — sits between login/register and the dashboard |

### App / dashboard (private, `noindex`)
| File | Purpose |
|------|---------|
| `dashboard.html` | Overview — balance, card, transactions, referral |
| `cards.html` | **My Cards** — scalable grid, statuses (active/frozen/blocked), filters, **editable labels** |
| `card.html` | Single card detail — **OTP-gated** sensitive details (number/CVC), transactions, deposit history, merchant fit |
| `buy-card.html` | **Buy a card** — master-detail list of all BINs + merchant compatibility |
| `topup.html` | Full-page top-up (fallback; the popup below is the primary flow) |
| `transactions.html` | Full transaction history (filters, search) |
| `referrals.html` | Referral program |
| `settings.html` | Profile, security, 2FA, notifications, danger zone |

### Shared assets
| File | Purpose |
|------|---------|
| `premium.css` | Design system (tokens, components) — used by every page |
| `app.css` | OTP + dashboard layout styles |
| `premium.js` | Marketing-page motion (GSAP/Lenis) |
| `dashboard.js` | Sidebar, copy buttons, freeze |
| `verify.js` | OTP input behaviour |
| `topup-modal.js` | **Top-up popup** (self-injecting; works on any page) |
| `kash-auth.css` / `kash-auth.js` | Styles + behaviour for the auth pages (`login-v2`, `register-v2`) |
| `images/` | Logo, card renders, app/lifestyle shots, avatars, og-image |

### SEO / legacy
- `robots.txt`, `sitemap.xml`, `images/og-image.png` (1200×630 share image)
- `_legacy/` — earlier design iterations / prototypes (`index`, `index-v2`, `kash`, `login`, `register`, `og`, `qrypto.*`). **Not part of the shipping product** — moved here to keep the root clean. Safe to delete entirely.

---

## Primary user flow
`premium.html` → **Sign up / Sign in** (`register-v2` / `login-v2`) → **OTP** (`verify.html`) → **`dashboard.html`** → My Cards / Buy a card / Top-up popup / Transactions / Referrals / Settings.

The **top-up is a popup** (`topup-modal.js`) that opens from: any "Top up" button, the nav, and "Select & deposit" on Buy a card. It carries the chosen card (existing, with a picker) or the new card (BIN) through to the deposit step.

---

## What the developer must wire to the backend
Everything is front-end with realistic demo data. Replace where marked `// DEMO`:

- **Auth:** real sign-in/sign-up; send + verify the email OTP (`verify.js`, and the OTP gate in `card.html`).
- **Cards:** real card list + statuses on `cards.html` (the `CARDS` array); card detail + **sensitive number/CVC** behind the OTP gate (`card.html`).
- **Card labels:** rename currently saves to `localStorage` → `PATCH /cards/:id { label }`.
- **Buy a card:** real **7 BINs** + merchant compatibility (the `CARDS` array in `buy-card.html`); issue card on confirm.
- **Top up:** real deposit address + amount + network, and the Confirm action (`topup-modal.js`); 3% fee is computed client-side.
- **Transactions / Deposit history / Referrals / Settings:** bind to real data + endpoints.
- **Freeze / Unfreeze / Block** actions.

---

## SEO (done)
- **Marketing pages** (`premium`, `terms`, `privacy`): title, meta description, **canonical**, **Open Graph + Twitter Card**, **JSON-LD Organization**, `theme-color`, favicon, `robots: index,follow`.
- **All app + auth + legacy pages:** `robots: noindex, nofollow` (kept out of search).
- **`robots.txt` + `sitemap.xml`** at the root; **`images/og-image.png`** (1200×630) for link previews.
- Favicon (`images/kash-logo.png`) on every page.

**Before launch:**
1. Serve `premium.html` as the site root (`/`) and `terms.html` / `privacy.html` as `/terms` and `/privacy` (the sitemap + canonicals assume these clean URLs). If you serve the literal `.html` files, update `sitemap.xml` and the `<link rel="canonical">` accordingly.
2. Confirm the production domain is `kash.cards` (used in canonicals, OG, JSON-LD, sitemap).
3. Optional: a proper multi-size `favicon.ico` / PWA manifest; the PNG logo is set as favicon for now.

---

## Important caveats before launch
1. **Legal pages are templates, not legal advice.** Have `terms.html` and `privacy.html` reviewed by an Australian financial-services lawyer and aligned with your card-issuing partner.
2. **BINs + merchant compatibility are placeholders.** Replace with your real 7 BINs and their exact accepted/blocked merchant categories.
3. **Demo data** (John Smith, balances, transactions, referral numbers, "12,400+ waitlist") must be replaced.
4. **Image artifacts:** the AI-generated renders are clean, but double-check the macro/security shots for any stray text before print/marketing use.

## Regulatory note
Earlier research (see your chat) concluded: an **AUSTRAC DCE/VASP registration alone is not sufficient** to operate this card — you also need an **ASIC AFSL** (typically via a licensed card-issuing partner such as Immersve). Confirm licensing with a lawyer before going live. Not legal advice.

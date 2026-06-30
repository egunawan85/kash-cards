# WasabiCard card types & usage limitations

Operational reference for which WasabiCard programs we can issue, what they can be used for,
and the gotchas that bit us in production. The catalog is sourced live from WasabiCard's
`getCardType` (Support BINs) endpoint via `CardCatalogService`; the fields below are theirs.

## Categories & holder model

Each card BIN has a **`category`** — `PURCHASE` (one-time / general spend), `SUBSCRIPTION`
(recurring billing), `GIFT`, or `PHYSICAL` — and a **cardholder model** (`metadata.cardHolderModel`):

- **B2C** (needs an individual cardholder): the buy flow calls `createHolder` first.
- **B2B** / `needCardHolder = false`: no holder step.

**Gotcha — "The BIN not support the model" (error 40002).** For our merchant account, the B2C
`createHolder` flow is **rejected on the B2C BINs** ("The BIN not support the model"). In
practice only the **no-holder** online card issues cleanly today; the B2C cards can't complete
a holder. This is a WasabiCard account/program constraint, not a code bug — confirm the
supported holder model with WasabiCard before relying on a B2C card.

**Gotcha — `town` must be a city CODE.** `createHolder.town` is a WasabiCard **city code** from
their `getCityList` catalog, NOT a city name. Sending a name fails with `40002 "town parameter
error"`. `CardholderGeoService` fetches a valid US city code live (cached) for the synthesized
holder. The legacy local geo tables (`tblM_Country_City` / `tblM_Address_Generator`) are unseeded.

## Minimum deposits (per BIN)

The minimum **initial deposit** to activate a card is per program (`depositAmountMinQuotaForActiveCard`),
ranging $1–$30 — it is **not** a flat $20. Recharge minimum (`rechargeMinQuota`) is likewise per
program. Read the live value per card; don't assume.

## Where cards can be used (and where they CANNOT)

Cards are **not** general-purpose — usage is scoped per BIN via three `getCardType` fields:

- **`support`** (`List<string>`) — accepted merchants, "for reference only".
- **`risk`** (`List<string>`) — high-risk merchants; "consumption will trigger card cancellation
  risk control". Consistently includes UBER, BOLT.EU, shenzhenshifenqil (+ MTR on some).
- **`cardDesc`** (free text) — where **hard-prohibited** merchants are written; using one
  **cancels and freezes the card immediately** (e.g. one BIN bans *PayPal, Apple, Netflix, Uber,
  Binance, OKX, OnlyFans, Patreon…*).

Platform-wide (ToS): prepaid USD topped up with USDT; **monthly + recharge limits** set per
program and adjustable by WasabiCard; blacklisted MCCs (gambling, adult, fuel) blocked;
WasabiCard may decline/reverse/suspend for fraud or ToS breach. Networks: Visa + MasterCard,
multiple issuing regions. Docs: https://wsb.gitbook.io/wasabicard-doc/api/card

**Data-quality caveat.** Treat the merchant data as **"reference + warnings," not a strict
allow/deny.** Support is genuinely per-card and contradictory across cards (PayPal/Apple are
*supported* on some BINs and *prohibited* on others). The `support` array and the `cardDesc`
text often disagree, and prohibited merchants in `cardDesc` are concatenated with no delimiters
(e.g. `"UberFood PandaStarlinkMTRBOLT.EU…"`), so the prohibition list is not cleanly parseable.

**Our-model gap.** `WCCardTypeResponseModel.Datum` captures `support` and `cardDesc` but **drops
`risk`** (no field) — to surface risk warnings in the cardholder UI, add `risk` to the model and
thread it through `CardCatalogService`.

## UI implication

Surface each card's `support` + `risk` + `cardDesc` on the buy page so cardholders pick the right
card for their merchant and don't get a card frozen by spending at a prohibited one.

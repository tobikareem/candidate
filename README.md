# Reconciliation Service — SWE Take-Home

Start with **`ASSIGNMENT.md`** — the full problem statement, output contract, and submission rules.

## What's in this folder

```
.
├── ASSIGNMENT.md            ← read this first
├── OUTPUT_CONTRACT.md       ← the exact answer files your service must produce (this is what we grade)
├── README.md                ← (this file)
├── docker-compose.yml       ← optional: a Postgres you may use as your store (or bring your own)
├── app-skeleton/            ← runnable hello-world services in Python / Node / Go — pick one or ignore
│   ├── python/  node/  go/
│   └── README.md
├── conformance/             ← self-check: validates your out/ files and scores them vs a public sample
│   ├── run_conformance.py
│   ├── schemas/             ← JSON Schemas for each required answer file
│   └── public-sample/       ← a small labeled subset so you can score yourself
└── documents/               ← ONE flat folder: every document for all 3 studies, unsorted
```

`documents/` is deliberately **not** organized — ~60 files (CTAs, CTMS exports, invoices, remittances,
portal exports, bank feeds, emails/Slack) for all three trials, mixed together like a real billing
inbox. Sorting them out (which study·site·investigator, what type) is part of the task.

## Quick start

1. Read `ASSIGNMENT.md`, then `OUTPUT_CONTRACT.md`.
2. Look at the pile:
   ```bash
   ls documents/ | head -40
   ```
3. Parse the PDFs/CSVs however you like (any LLM is fine — parsing is not what we're testing; some
   invoices are scanned images, so use a vision-capable model). Then build your service: ingest →
   classify each document by study → model → reconcile → serve.
4. Produce the canonical answer files under `out/<study>/` (schemas in `conformance/schemas/`).
5. Self-score against the public sample:
   ```bash
   python3 conformance/run_conformance.py --out ./out
   ```
6. Submit per `ASSIGNMENT.md`.

## A note on the inputs

The data is synthetic but modeled on real clinical-trial billing: real-world holdbacks,
lump-sum payments, invoice numbers that collide across studies, free-text visit naming, scanned-image invoices, and
documents you must scope to the right trial by their contents. Subject identifiers are synthetic; there
is no PHI. The three studies span variety — a large, messy study; a clean study with an autopay portal;
and a small third study on different vendors that will break anything you hard-coded to the first two.

---

# Solution

A .NET 10 reconciliation service. Three projects:

- **`Recon.Domain`** — entities, value objects, enums, the six chain records, `ReconciliationResult`,
  and the deterministic surrogate-key helper. Zero dependencies; the graded core.
- **`Recon.App`** — `Ingestion/` (header-signature CSV adapters + fixture loaders), `Classification/`
  (study resolver), `Reconciliation/` (the engine: six chains + derived products), `Output/`
  (canonical writer), `Api/` (Minimal API). Hosts the `--reconcile` CLI and the HTTP API.
- **`Recon.Tests`** — 58 xUnit tests: per-hard-case + a lock on every public-sample number.

**Status:** `make reconcile` → four valid files per study, **10/10 public-sample checks**, 58 tests green.

## How to run

Prereqs: **.NET 10 SDK**; **Python 3** for the conformance self-check.

```bash
make reconcile     # dotnet run --project src/Recon.App -- --reconcile --out ./out   (writes out/<slug>/*.json)
make selfcheck     # python3 conformance/run_conformance.py --out ./out              → 10/10 public sample
dotnet test        # 58 unit tests

# Query API (same in-memory result the files project from):
dotnet run --project src/Recon.App            # serves http://localhost:5000 (or --urls http://127.0.0.1:5099)
#   GET  /studies
#   GET  /studies/{slug}/{chains|dashboard|unbilled|unpaid|exceptions}
#   GET  /entities/{type}/{id}
#   GET  /reconcile           → per-study summary
#   POST /reconcile           → (re)writes out/<slug>/*.json and returns the summary
```

Extraction is **off the reproduce path**: PDFs (CTAs, invoices, remittances) and comms are extracted
**once** into committed `fixtures/*.json` (scanned PDFs via vision, text PDFs via `pdftotext`); the CSV
vendor feeds are committed as-is and parsed on the load path by **header-signature** adapters (never by
filename). So the graded run reads only committed inputs — deterministic, offline, no API keys.

## Architecture / data model

```
 documents/  (7 CSV feeds + ~55 PDFs + emails/Slack)
   │  CSVs → header-signature adapters          PDFs/comms → committed fixtures/*.json
   └───────────────────────┬──────────────────────────────┘
                           ▼
                   ReconStore  ── in-memory source of truth (lists + by-id dicts, fail-fast on dup id)
                           │
                           ▼
        StudyResolver + Classifier ── resolve every entity → study·site·investigator (chain 6)
                           │            (subject-prefix → payor/sponsor → printed code; prefix map DERIVED)
                           ▼
        ReconciliationEngine ── per study: 6 chains + consume-once bank matching + derived products
                           │
                           ▼
                  ReconciliationResult (per study)
                     │                         │
            CanonicalFileWriter            Minimal API
              out/<slug>/*.json     ◄── one projection ──►   GET/POST /reconcile, /studies/{slug}/...
```

**Entities** (each carries a deterministic surrogate `Id` + `SourceDocument` provenance):
`Study` · `Site` · `Investigator` · `CtaBudgetLine` · `Activity` · `Invoice`(+`InvoiceLine`) ·
`Payment` · `Remittance`(+`RemittanceLine`) · `Autopay` · `BankTransaction` · `Comm`(+`CommFact`).

**The six chains** (hold *ids*, serialize 1:1 to `chains.json`):

| # | Chain | Links |
|---|---|---|
| 1 | `PaymentToRemittance` | payment → remittance(s) |
| 2 | `InvoiceToPayment` | invoice → payments + `status` + `amount_settled` |
| 3 | `InvoiceToActivities` | invoice → activities |
| 4 | `RemittanceToActivities` | remittance → line allocations |
| 5 | `ActivityToCta` | activity → CTA budget line + confidence |
| 6 | `EntityScope` | entity → study·site·investigator |

**Surrogate keys:** `id = shortHash(entityType, …natural key…)` — stable across runs (SHA-256, *not*
`GetHashCode`). An invoice's natural key is `(study, printed number)`, so the reused `INV-001` in HORIZON
and NORTHSTAR get distinct ids.

## How each derived number is computed (per study)

- **`total_billed`** = Σ invoice face + Σ per-visit autopay scheduled (autopays are billed through the
  portal with no site invoice).
- **`total_collected`** = bank deposits matched to the study's remittances/autopays, **consuming each
  deposit at most once**, so a cross-study duplicate or stray deposit is never counted.
- **`outstanding_ar`** = Σ face of invoices with no settling remittance line **and** no matching direct
  payment/deposit.
- **`holdback_withheld`** = Σ |remittance-line adjustment|. A holdback is *paid in full per terms* — a
  90%-settled invoice is `paid`, never `partial`.
- **`unbilled`** = CTMS activities with no invoice (±10 days) and no autopay **for the same visit**;
  estimate = matched CTA base × (1 + study overhead).
- **`unpaid`** = invoices issued but never settled (`sent_not_paid`) + authorized autopays with no
  deposit (`autopay_no_deposit`, via consume-once amount matching).
- **`exceptions`** = a deposit matching no scheduled autopay/remittance (wrong amount) + invoices
  breaching a CTA cap.

All rates (holdback %, overhead %, caps) are **read from the CTA**, never hard-coded.

## Hard cases & evidence trails

Each row is a deliberate trap in the data and how the engine resolves it, with the document chain that
proves it:

1. **Holdback = paid in full, not partial.** INV-002 face **$2,153.13** → remittance R-002 line
   (Gross 2153.13, Withholding −215.31, **Net 1937.82**) → bank `BT-002` deposit $1,937.82. Status
   `paid`, `amount_settled` 1937.82.  *INV-002 PDF → R-002 advice → Plaid BT-002.*
2. **One payment, many invoices (line-level).** Remittance R-001 (**$7,354.69**) splits across
   INV-001 / INV-003 / INV-D003 / INV-004, each with its own holdback; `NetPaid` ties to `BT-001`.
   Reconciled at the line, not the payment.  *R-001 lines → invoices by (number, gross) → BT-001.*
3. **Reused invoice numbers across studies.** `INV-001` exists for HORIZON (S-12-001) and NORTHSTAR
   (S-03-001); surrogate key `hash(study, number)` keeps them distinct.  *two INV-001 PDFs.*
4. **Overhead markup.** HORIZON visit invoices = CTA base × 1.25 (Screening 1722.50 → 2153.13); the
   engine matches the CTA line despite the gap.  *CTA grid vs invoice face.*
5. **Misfiled study code → trust the paying chain.** `INV-021` prints "Study: ASCEND", but the subject
   is **S-12-021** (HORIZON), the payor is **Meridian** (HORIZON's sponsor), and it's settled by Meridian
   remittance R-011 with a 10% holdback (ASCEND has none). Classification → **HORIZON**, so it lands in
   HORIZON's totals — *not* NORTHSTAR's or ASCEND's.  *subject prefix + payor + R-011 holdback.*
6. **Settled with no remittance advice.** HORIZON `INV-100` ($14,317.50) has no remittance (there is no
   R-005), but ledger `LR-005` and bank `BT-005` both equal its face → settled, **not** outstanding. So
   `outstanding_ar` = only INV-105.  *LR-005 + BT-005 = face.*
7. **Sent-not-paid, confirmed by comms.** `INV-105` (pharmacy maintenance, 4 yrs, **$13,061.60**) — no
   remittance, no deposit → `sent_not_paid`; email (2026-05-19) + Slack (2026-06-02) confirm the sponsor
   won't pay.  *no R/deposit + comm corroboration.*
8. **Autopay no-deposit vs wrong-amount.** ASCEND `AP-009` ($523.10) and `AP-011` ($418.75) authorized
   but never landed → `autopay_no_deposit` (found by consume-once counting: 4 × 418.75 scheduled vs 3
   deposits → 1 unpaid). `AP-006` (scheduled $685.40) deposited **$627.55** → that's an **exception**
   (wrong amount), not unpaid.  *eClinicalGPS register vs Plaid feed + email 2026-04-28.*
9. **Consume-once collections.** $685.40 appears 5× in the register and 5× in the bank, but one bank
   line (`PL-N2`) is a cross-study duplicate — consume-once matching stops it double-counting.
10. **Unbilled, precision both ways.** S-12-037 (RealTime screening, no invoice), S-03-002 Visit 2
    (Clinical Conductor 4/15, never invoiced — email 2026-04-22), ASCEND unscheduled repeat-hematology
    (no autopay, per-procedure — Slack 2026-04-29). Coverage is matched **per visit**, so a draw near a
    scheduled autopay still surfaces; anything billed/autopaid is never included.
11. **ClinCard excluded (precision).** The 7 ClinCard receipts are subject reimbursements — "*not a site
    charge to the sponsor*" — so they're deliberately kept out of billing totals.  *receipt text.*

## What's next (beyond MVP)

- **Persistence:** swap the in-memory `ReconStore` for EF Core + Postgres (compose already ships one).
  The engine is pure functions over POCOs, so this is an adapter change, not a rewrite.
- **Comms corroboration:** `CommFact`s are loaded but not yet *consumed* — wire them to confirm/flag
  statuses (they may only corroborate, never originate).
- **`MatchingOptions`:** centralize the amount/date tolerances (currently named constants in the engine).
- **Smarter activity→CTA matching:** current label matching is heuristic (exact → most-specific fuzzy →
  null), so a few unbilled *estimates* are approximate. An alias table / embeddings would tighten them.
- **Packaging:** multi-stage Dockerfile + compose wiring; full HTTP integration tests
  (`WebApplicationFactory`); a packaged `tools/Recon.Extractor` to regenerate `fixtures/` from documents.

## Time spent

About **20 hours**, roughly: scaffold + domain model (4 hours), ingestion (CSV adapters + fixtures, incl.
PDF extraction) (6 hours), classification + the six chains + derived products (6 hours), and API +
hardening + tests + this writeup (4 hours). The bulk went into the reconciliation rules and verifying
every public-sample number, not the plumbing.


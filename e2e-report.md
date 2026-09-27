# CompanyERP — End-to-End User Flow Test Report

- **Date:** 2026-09-27
- **Environment:** Local (Win), app on `https://localhost:7168`, **rebuilt clean** SQL Server Express DB `CompanyERP` (`DESKTOP-H7V9030\SQLEXPRESS`, user `vs`)
- **DB reset:** Old DB backed up to `F:\ERPbackup`, then `CompanyERP` dropped and rebuilt from EF migrations before this run.
- **Method:** All business records created/edited through the **web UI** as a real user. Only the **security bootstrap** (roles/permissions/menus/admin) was inserted with a direct SQL script, because the app has no first-run seeder wired up (see finding 9).
- **Test user:** `admin` / `Pass@1234` (SUPERADMIN)
- **Harness:** `e2e.ps1` (GET page → reuse server-prefilled numbers/hidden values → POST with `__RequestVerificationToken`, submitting the fields the rendered form actually contains).
- **Build:** `dotnet build` — 0 warnings, 0 errors.

---

## 1. Security bootstrap (direct SQL, documented exception)

`SecuritySeederService.SeedAsync()` exists but is never registered or called, so a fresh DB has no login possible. Seeded once, then all security pages exercised through the UI.

| Table | Rows |
|---|---|
| Roles | 6 (Superadmin + 5 business roles) |
| Permissions | 59 |
| RolePermissions | 111 (Superadmin holds all) |
| Menus | 7 (1 group + 6 security items) |
| Users | 1 (`admin`) |
| UserRoles | 1 |

Login works; `/User`, `/Role`, `/Permission`, `/Menu`, `/ActivityLog`, `/LoginHistory` all render for SUPERADMIN even though only the security subtree is in the sidebar menu set.

---

## 2. Fixes verified in this clean run

| # | Fix | Verification on clean DB |
|---|---|---|
| 1 | Posted-date document numbering (8 controllers) | Two sales orders both dated `2026-05-10` → **`SO-20260510-001`** and **`SO-20260510-002`** (previously the second collided with the first). `PI-20260210-001`, `RCV-20260210-001`, `PRT-20260328-001`, `SI-20260305-001`, `SR-20260325-001`, `EXP-20260310-001`, `PAY-20260215-001`, `AST-20260201-001` all carry the posted date. |
| 2 | `Warehouse.BranchId` server-side validation | POST without `BranchId` → **rejected, no row created** (old behaviour: "Selected branch does not exist."). |
| 3a | Payment invoice picker | `GET /Payment/GetCustomerInvoices`, `GET /Payment/GetSupplierInvoices` return outstanding invoices with due amounts; both selects render in the form. |
| 3b | Payment "Opening" option | New **Opening (no invoice)** option in both selects, no longer disabled when there are no outstanding invoices. `PAY-20260930-001` posted against invoice-less customer `CUST-002` with `SourceReferenceNo=Opening` → journal `JR-20260930-001` (DR Cash 100 / CR AR 100). Required a narrow service change: the over-payment cap is skipped for the `Opening` sentinel, otherwise a party with no invoice could never be paid. |
| 4 | `ServiceDelivery` create form | Form had **no `companyId` field**, so every real submission failed with *"Company does not exist."* Hidden `companyId` added (sourced from `ViewData`) → the flow now reaches real business validation. |

---

## 3. Masters (all via UI)

| Entity | Code | Name | Notes |
|---|---|---|---|
| Company | — | E2E Trading Ltd | id=1 |
| FinancialYear | FY2026 | — | 4 periods P01–P04 |
| AccountingPeriod | P01 | Jan–Mar 2026 | **Status 1 = Open / 2 = Closed**; posting `0` is rejected by model binding (validation working as designed) |
| BranchType | HO | — | |
| Branch | BR-HO | Head Office | |
| Department | ACC | | |
| Designation | MGR | | |
| PaymentMethod | CASH | | |
| Employee | EMP-001 | Md. Rahman | |
| Supplier | SUP-001 | ABC Traders | |
| Customer | CUS-001 | XYZ Retail | + CUST-002 Future Corp (added for the Opening-payment test) |
| CategoryType | CT-GOODS | | |
| Category | CAT-GOODS | | |
| ProductCategory | — | Finished Goods | |
| Product | PRD-001 | USB Cable | Cost 10, Sale 15 |
| Warehouse | WH-HO | Head Office Warehouse | + WH-ST Store Room (both branch-bound) |
| CashAccount | CA-001 | HO Cash | |
| BankAccount | BA-001 | HO Bank Account | |
| AssetCategory | AC-EQP | Equipment | |
| AssetType | AT-LAPTOP | Laptop | StraightLine, 60 months, salvage 0 |

---

## 4. Chart of Accounts

- Auto-seeded on first posting (`TransactionPostingService.EnsureDefaultsAsync`): 61 accounts, 4-layer hierarchy (`1` → `10` → `100` → `1000`).
- Created through the UI: **5310 Bank Charges** under group `530 General & Administrative`, postable.
- Final count: **62**.

Key IDs: `1000 Cash`, `1100 Bank`, `1200 AR`, `1300 Inventory`, `1400 Fixed Assets`, `1410 Accumulated Depreciation`, `2000 AP`, `4000 Product Sales Revenue`, `4100 Software Revenue`, `4200 Service Revenue`, `5000 COGS`, `5200 Depreciation Expense`, `5300 Operating Expenses`, `5410 Loss on Asset Disposal`.

---

## 5. Purchase chain

| Step | Document | Qty/Cost | Result |
|---|---|---|---|
| Request | `PRQ-20260927-001` | 100 @ 0 | Draft → Approved → **Converted (2)** |
| Quotation | `QT-20260927-001` | — | Draft → **Sent to supplier (1)** |
| Purchase Order | `PO-20260927-001` | — | Created from quote → Approved → **Received (2)** |
| Receiving | `RCV-20260210-001` | 100 @ 8 = 800 | Auto-created `PI-20260210-001` |
| Purchase Invoice | `PI-20260210-001` | 800, On Account | `JR-20260210-001` |
| Supplier payment | `PAY-20260215-001` (Cash) | 800 | `JR-20260215-001` |
| Purchase Return | `PRT-20260328-001` | 20 @ 8 = 160 | `JR-20260328-001`, stock −20 |
| Request #2 | `PRQ-20260927-002` | — | **Cancelled (3)** |

```
JR-20260210-001  DR 1300 Inventory          800   CR 2000 Accounts Payable    800
JR-20260215-001  DR 2000 Accounts Payable   800   CR 1000 Cash               800
JR-20260328-001  DR 2000 Accounts Payable   160   CR 1300 Inventory          160
```

Stock after purchase chain: **80 @ 8.00**.

---

## 6. Sales chain

| Step | Document | Qty/Price | Result |
|---|---|---|---|
| Sales Order | `SO-20260927-001` | 40 @ 15 = 600 | Draft → Confirm → Invoiced (2) |
| Sales Invoice | `SI-20260305-001` | On Account | `JR-20260305-001` |
| Customer payment | `PAY-20260320-001` (Cash) | 600 | `JR-20260320-001` |
| Sales Return | `SR-20260325-001` | 10 @ 15 = 150 | `JR-20260325-001` |

```
JR-20260305-001  DR 1200 Accounts Receivable  600
                DR 5000 COGS                  400   <- 40 x Product.CostPrice (10)
                CR 1300 Inventory             400
                CR 4000 Product Sales Revenue 600

JR-20260320-001  DR 1000 Cash    600          CR 1200 Accounts Receivable 600

JR-20260325-001  DR 4000 Product Sales Revenue 150   <- revenue reversal
                CR 1200 Accounts Receivable   150   <- refund owed (invoice already paid)
                DR 1300 Inventory             100   <- 10 x CostPrice
                CR 5000 COGS                  100
```

Stock after sales chain: **50 @ 8.40**.

---

## 7. Expense

| Step | Code | Result |
|---|---|---|
| ExpenseCategory | `EXC-OPR` — Office Rent & Utilities | ok |
| ExpenseType | `EXT-RENT` — Office Rent | ok |
| ExpenseEntry | `EXP-20260310-001` — 500.00 cash, 2026-03-10 | `JR-20260310-001` |

```
JR-20260310-001  DR 5300 Operating Expenses 500   CR 1000 Cash 500
```

---

## 8. Inventory (stock-only module)

| Step | Details | Result |
|---|---|---|
| 2nd warehouse | `WH-ST` — Store Room | ok, branch required (fix 2) |
| Transfer | 40 pcs WH-HO → WH-ST @ 8.40 | Paired rows, ref `TRF-20260927231625` |
| Adjustment | −1 in WH-ST | ref `ADJ-001` |
| Stock In (Opening) | +5 @ 9.00 in WH-ST | ref `OPEN-001` |

**Final balances:** WH-HO **5 @ 8.40**, WH-ST **44 @ 8.47** (9 stock transactions, 1 transfer).

**Observation (finding 5):** transfers, adjustments and opening stock-in create **no journal entry** — stock rows only, while a parallel transfer transaction ID exists on both legs.

---

## 9. Service module + service on the same invoice

| Step | Document | Result |
|---|---|---|
| Service | `SRV-WEB` — Web Development (200 / cost 50) | ok |
| Sales Order (mixed) | `SO-20260405-001`: 5 × USB @ 18 + 1 × `SRV-WEB` @ 200 = 290 | Draft → Confirm |
| Sales Invoice | `SI-20260405-001` (On Account) | `JR-20260405-001` |
| Customer payment | `PAY-20260410-001` (Cash) | 290, `JR-20260410-001` |

```
JR-20260405-001  DR 1200 Accounts Receivable  290
                DR 5000 COGS                   50   <- 5 x CostPrice
                CR 1300 Inventory              50
                CR 4000 Product Sales Revenue   90   <- product leg
                CR 4200 Service Revenue       200   <- service leg, separate revenue account

JR-20260410-001  DR 1000 Cash 290              CR 1200 Accounts Receivable 290
```

Service line splits correctly across `4000` / `4200`; stock drops only for the product leg.

---

## 10. Asset lifecycle

| Step | Document | Result |
|---|---|---|
| Registration | `AST-20260201-001` — Laptop, 1500, purchase 2026-02-01, acquired 1500 | `JR-20260201-001` (DR 1400 / CR 1100) |
| Depreciation run | period `2026-02` | 25.00 = 1500 / 60, `JR-20260228-001` |

```
JR-20260201-001  DR 1400 Fixed Assets 1500          CR 1100 Bank 1500
JR-20260228-001  DR 5200 Depreciation Expense 25     CR 1410 Accumulated Depreciation 25
```

Depreciation is period-locked (a period can post once per company) and re-running the same period is rejected. Asset acquisition defaulted to the **Bank** account even though no bank account was chosen on the form — worth confirming intended behaviour.

---

## 11. ServiceOrder / ServiceDelivery

| Step | Document | Result |
|---|---|---|
| ServiceOrder | `SVO-20260927-001` (`SRV-WEB` × 1 @ 200) | created |
| ServiceDelivery | 1 qty, 2026-09-28, by Rahim | recorded; order status → **Delivered (2)** |

A second delivery on the same order is correctly rejected (*"Only pending or in-progress service orders can be delivered."*). Before fix 4 this screen always failed with *"Company does not exist."* because the form never posted `companyId`.

---

## 12. Opening-balance payment (customer with no invoices)

| Step | Document | Result |
|---|---|---|
| Customer | `CUST-002` Future Corp, opening receivable 0 | created |
| Invoice list | `GET /Payment/GetCustomerInvoices?customerId=2` | `{"invoices":[]}` |
| Payment | `PAY-20260930-001` — 100 cash, ref **Opening** | `JR-20260930-001` (DR 1000 / CR 1200) |

This is the case the original report flagged as unusable: previously the select had no options and was left disabled, and even a hand-typed reference was blocked by the over-payment cap (`outstanding 0.00`).

---

## 13. Reports and page sweep (all HTTP 200)

39 routes probed plus the home page, **all 200**:

Accounting Report: TrialBalance, Ledger, CashBook, BankBook, ProfitAndLoss, BalanceSheet, ReceivablesPayables, GeneralJournal, ComparativePAndL, CashFlow, BankCashSummary, CoaReport · Aging Report: Receivables, Payables · Asset Report: Register, Depreciation, Disposals, Groups · Expense / Inventory / Payment / Purchase / Sales / Statement / HR Report index · Inventory: Balance, Transactions, Transfers · Security: Users, Roles, Permissions, Menus, ActivityLog, LoginHistory · module indexes: SalesOrder, PurchaseOrder, Payment, Expense, Asset · `/Home/Index` (200).

`/Dashboard` returns 404 — there is no such route; the dashboard is `/Home/Index`.

Audit trail: `LoginHistory` 43 rows, `ActivityLog` 67 rows at report time.

---

## 14. Final ledger

**12 journal entries, every one balanced. Total D = Total C = 6,365.00.**

| Code | Account | Debit | Credit | Net |
|---|---|---|---|---|
| 1000 | Cash | 990.00 | 1,300.00 | −310.00 |
| 1100 | Bank | 0.00 | 1,500.00 | −1,500.00 |
| 1200 | Accounts Receivable | 890.00 | 1,140.00 | −250.00 |
| 1300 | Inventory | 900.00 | 610.00 | 290.00 |
| 1400 | Fixed Assets | 1,500.00 | 0.00 | 1,500.00 |
| 1410 | Accumulated Depreciation | 0.00 | 25.00 | −25.00 |
| 2000 | Accounts Payable | 960.00 | 800.00 | 160.00 |
| 4000 | Product Sales Revenue | 150.00 | 690.00 | 540.00 |
| 4200 | Service Revenue | 0.00 | 200.00 | 200.00 |
| 5000 | COGS | 450.00 | 100.00 | 350.00 |
| 5200 | Depreciation Expense | 25.00 | 0.00 | 25.00 |
| 5300 | Operating Expenses | 500.00 | 0.00 | 500.00 |
| 5310 | Bank Charges (custom) | 0.00 | 0.00 | unused |

| Journal list | | |
|---|---|---|
| `JR-20260201-001` Asset Acquisition 1500 | `JR-20260210-001` Purchase Invoice 800 | `JR-20260215-001` Supplier Payment 800 |
| `JR-20260228-001` Depreciation 25 | `JR-20260305-001` Sales Invoice 1000 | `JR-20260310-001` Expense Entry 500 |
| `JR-20260320-001` Customer Payment 600 | `JR-20260325-001` Sales Return 250 | `JR-20260328-001` Purchase Return 160 |
| `JR-20260405-001` Sales Invoice (mixed) 340 | `JR-20260410-001` Customer Payment 290 | `JR-20260930-001` Opening Receipt 100 |

**Trial Balance:** 6,365.00 = 6,365.00, *Balanced*.
**P&L:** Revenue **740.00** (Product 540, Service 200) − Expenses **875.00** = **Net −135.00**.
**Balance Sheet:** Total Assets **−245.00**, Total Liabilities **−160.00**, L+E **−295.00**.

Negative cash/bank is a **data-setup artefact of this run**, not a bug: no opening capital or opening cash/bank balances were loaded, while the asset purchase of 1500 defaulted to Bank. Ledger integrity (D = C) holds.

---

## 15. Row counts (end of run)

COA 62 · Menus 7 · Permissions 59 · Roles 6 · RolePermissions 111 · Users 1 · Branches 1 · PaymentMethods 1 · Products 1 · Customers 2 · Suppliers 1 · Warehouses 2 · StockTransactions 9 · StockTransfers 1 · PurchaseRequests 2 · PurchaseQuotations 1 · PurchaseOrders 1 · PurchaseReceivings 1 · PurchaseInvoices 1 · PurchaseReturns 1 · SalesOrders 4 · SalesInvoices 2 · SalesReturns 1 · Payments 4 · ExpenseEntries 1 · AssetRegisters 1 · AssetDepreciations 1 · ServiceOrders 1 · ServiceDeliveries 1.

---

## 16. Findings

**Fixed and verified in this run**
1. Document numbers were generated from `DateTime.Today` while counting by stored date → duplicate numbers for backdated documents (Payment, PurchaseRequest, SalesOrder, and 5 more controllers). Fixed by regenerating from the posted date.
2. `Warehouse` create required `BranchId` but had no validation → misleading *"Selected branch does not exist."*; now rejected server-side with `[Range(1, int.MaxValue)]`.
3. Payment source reference was free text and the "pick an invoice" control could not express an opening receipt. Now a server-fed picker plus an explicit Opening option, with the over-payment cap skipped only for that sentinel.
4. `ServiceDelivery/Create.cshtml` never posted `companyId` → the screen was unusable (*"Company does not exist."*).

**Open / design questions**
5. Stock transfer, adjustment and opening stock-in post **no journal entry** (stock rows only). If the ERP targets full double entry, these should post Inventory ↔ Opening-Balance/adjustment contra.
6. COGS uses `Product.CostPrice` (10) while the running stock average is 8.00–8.47 → COGS overstated (400 instead of 320 on the first invoice). Stock-out unit cost should come from `StockBalance.AverageCost` if moving-average valuation is intended.
7. A sales return against a fully-paid invoice leaves an **AR credit** (refund owed) with no credit-note/refund workflow in the UI.
8. Asset acquisition silently used the **Bank** account although the form offers no bank-account choice for acquisitions.
9. No first-run seeder: `SecuritySeederService` is never invoked, so a fresh DB has no way to create the first user. Consider calling it at startup or shipping a migration seed.
10. The warehouse "branch required" error surfaces only as a generic EF message (*"The value '' is invalid."*) rather than a friendly field message.
11. Asset `Status` persisted as `Registered (0)` although the payload requested `Active (1)` — confirm the intended lifecycle transition.

---

## 17. Manual E2E notes

- Always GET the create page first and reuse server-prefilled numbers (`RequestNo`, `OrderNo`, `InvoiceNo`, `PaymentNo`, `AssetNo`, `ExpenseNo`) — they are regenerated from the posted date on submit.
- Status actions (Approve, SendToSupplier, Confirm) are **POST-only**; a GET returns 200 but changes nothing.
- Enum values used by the forms: PR Draft 0/Approved 1/Converted 2/Cancelled 3 · Quotation 0/1/2 · PO 0/1/2 · SalesOrder 0/1/2 · SI PaymentType Cash 0/Bank 1/OnAccount 2 · Payment Category Customer 0/Supplier 1/… · AccountType Cash 0/Bank 1 · AccountingPeriod Open 1/Closed 2 · AssetStatus 0..3 · DepreciationMethod StraightLine 0/ReducingBalance 1 · StockIn Type Opening 0.
- The app must be started with `ASPNETCORE_URLS=https://localhost:7168` — running the built exe directly falls back to port 5000 because `launchSettings.json` is not applied.

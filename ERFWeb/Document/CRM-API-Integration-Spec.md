# ERFWeb ← CRM API: Data Contract (Draft v0.2)

**Purpose.** When an employee creates an Expense Report (ERF) in ERFWeb, they can pick the CRM
activities (service calls, PMs, installations, customer visits) they did in the report period.
ERFWeb pre-fills each expense line's date, particulars, location, service number, instrument,
serial number and "Work With" from the CRM. The employee still enters the money: category,
amount, Invoice/OR number, VAT and receipts. **CRM is the source of _what, where, when, with whom_;
ERFWeb stays the source of _how much_.**

**Direction.** ERFWeb backend → CRM API (server to server). The browser never calls the CRM.
Read-only, except the optional "claimed" callback in section 5.

---

## 1. General conventions

| Item | Requirement |
| --- | --- |
| Transport | HTTPS only (TLS 1.2+). Reachable from the ERFWeb API server. |
| Format | JSON, UTF-8, `camelCase` property names. |
| Base path | Production `https://inventory.mdmpi.com.ph/api2/erf/v1/` (host to be confirmed); local development `https://localhost:<port>/api/erf/v1/`. |
| Authentication | Server-to-server API key in header `X-Api-Key`, one key issued to ERFWeb, revocable. ERFWeb keeps it in the server's `appsettings.json` (gitignored), never in the browser. |
| Dates | `YYYY-MM-DD` (e.g. `2026-10-21`). |
| Date-times | ISO 8601 **with offset**, e.g. `2026-10-21T08:30:00+08:00`. |
| Money (if any) | JSON number, 2 decimals, PHP. |
| Nulls | Missing values are `null`, not `""` or `"N/A"`. |
| IDs | Strings, stable forever (never reused, never changed after creation). |
| Paging | `page` (1-based) and `pageSize` (max 100); response carries `totalCount`. |
| Errors | HTTP status + JSON body `{ "status": 404, "title": "...", "detail": "..." }` (RFC 7807 ProblemDetails). |
| Performance | Response within 5 seconds; ERFWeb times out at 10 s. |
| Data hygiene | No passwords, tokens or customer personal data beyond what is listed below. |

## 2. How an employee is identified

The CRM database holds no employee email, so the **CRM `employeeId`** (e.g. `CNT-001`) is the link.
An ERFWeb admin links each ERFWeb user to one CRM `employeeId` with the CRM's employee lookup
(`GET /employees?search=` and `GET /employees/{employeeId}`, specified in the CRM team's
"CRM Employee Lookup API for ERFWeb"). Every activity call below takes and returns `employeeId`;
companions in `companions[]` are matched to ERFWeb users the same way.

The lookup also returns `workflows` (`Sales`, `PS`, `TSG`), which says which kinds of activities the
employee can claim.

## 3. Endpoints ERFWeb needs

### 3.1 List an employee's activities in a date range

```
GET /api2/erf/v1/activities?employeeId=CNT-001&dateFrom=2026-10-01&dateTo=2026-10-15&status=Completed&page=1&pageSize=50
```

| Query parameter | Required | Notes |
| --- | --- | --- |
| `employeeId` | yes | CRM employee ID of the employee who did the activity (as lead or as companion). |
| `dateFrom`, `dateTo` | yes | Inclusive. Range at most 62 days. |
| `status` | no | Default `Completed`. Only completed activities are claimable. |
| `page`, `pageSize` | no | Defaults 1 and 50. |

**Response 200**

```json
{
  "page": 1,
  "pageSize": 50,
  "totalCount": 2,
  "items": [
    {
      "activityId": "SR-2026-004512",
      "workflow": "TSG",
      "activityType": "Corrective Maintenance",
      "status": "Completed",
      "activityDate": "2026-10-03",
      "startAt": "2026-10-03T07:45:00+08:00",
      "endAt": "2026-10-03T19:20:00+08:00",
      "subject": "Repair of hematology analyzer - no power",
      "serviceNumber": "SR-2026-004512",
      "customer": {
        "customerId": "CUST-00871",
        "name": "St. Luke's Medical Center",
        "branch": "Global City",
        "city": "Taguig",
        "province": "Metro Manila"
      },
      "instrument": {
        "name": "Sysmex XN-1000",
        "serialNumber": "XN12345678"
      },
      "employee": {
        "employeeId": "CNT-001",
        "name": "Juan Dela Cruz",
        "role": "Lead"
      },
      "companions": [
        { "employeeId": "CNT-014", "name": "Pedro Santos" }
      ],
      "travel": {
        "origin": "Makati Office",
        "destination": "St. Luke's BGC",
        "distanceKm": 7.5,
        "isOutOfTown": false,
        "isOvernight": false
      },
      "lastModifiedAt": "2026-10-03T20:02:11+08:00"
    }
  ]
}
```

### 3.2 Get one activity

```
GET /api2/erf/v1/activities/{activityId}
```

Same object as one `items` entry above. `404` if it does not exist. ERFWeb calls this again when
the employee saves the line, to make sure the activity is still valid and not cancelled.

### 3.3 (Optional) Customer search

```
GET /api2/erf/v1/customers?search=luke&page=1&pageSize=20
```

Returns `{ customerId, name, branch, city, province }` items, for typing a location when the expense is
not tied to a specific activity.

## 4. Field dictionary and how ERFWeb uses each field

| CRM field | Type | Required | Max length | ERFWeb expense line field | Notes |
| --- | --- | --- | --- | --- | --- |
| `activityId` | string | **yes** | 50 | stored as the CRM link | Unique, stable. Used to stop the same activity from being claimed twice. |
| `workflow` | string | **yes** | 10 | (filter) | `Sales` (itinerary visit), `PS` or `TSG` (service work), matching the lookup's `workflows`. |
| `activityType` | string | **yes** | 50 | part of Particulars | e.g. Corrective Maintenance, Preventive Maintenance, Installation, Training, Sales Call, Demo. Please send the full list of values. |
| `status` | string | **yes** | 20 | filter | `Completed`, `Cancelled`, `Open`. Only `Completed` is claimable. |
| `activityDate` | date | **yes** | | Transaction Date | Must fall inside the report's Date From/To. |
| `startAt`, `endAt` | date-time | recommended | | meal per-diem eligibility | Lets ERFWeb suggest breakfast/lunch/dinner/OT meal. |
| `subject` | string | **yes** | 150 | Particulars | ERFWeb line limit is 200 chars incl. activity type. |
| `serviceNumber` | string | recommended | 100 | Service Number | Service report / ticket number printed on R47. |
| `customer.customerId` | string | **yes** | 50 | (reference) | |
| `customer.name` | string | **yes** | 80 | Location | Location = `name - branch` (max 100). |
| `customer.branch` | string | no | 50 | Location | |
| `customer.city`, `customer.province` | string | no | 50 | (display) | |
| `instrument.name` | string | recommended | 100 | Instrument | Printed on R47 as "Location - Instrument - Serial". |
| `instrument.serialNumber` | string | recommended | 100 | Serial Number | |
| `employee.employeeId` / `name` / `role` | string | **yes** | 100 | (check) | `role`: `Lead` or `Companion`. |
| `companions[].employeeId`, `name` | string | no | 100 | Work With | ERFWeb matches by `employeeId` to its linked users. |
| `travel.origin`, `destination` | string | no | 100 | Fare leg From/To | Pre-fills a transportation leg. |
| `travel.distanceKm` | number | no | | per-km rate | Used where the employee's rate is per km. |
| `travel.isOutOfTown`, `isOvernight` | boolean | no | | per-diem hints | |
| `lastModifiedAt` | date-time | **yes** | | (audit) | |

**Not needed from the CRM** (the employee enters these in ERFWeb): expense category, amount, VAT,
Invoice/OR number, receipts, cash advances, approvers.

## 5. (Optional) Tell the CRM an activity was claimed

If the CRM wants to show "expense claimed" on its side:

```
PUT /api2/erf/v1/activities/{activityId}/expense-claim
{
  "erfReferenceNo": "ERF-2026-000123",
  "status": "Filed",          // Filed | Approved | Rejected | Withdrawn
  "claimedAt": "2026-10-16T09:12:00+08:00"
}
```

`204` on success. Must be idempotent (sending the same call twice is fine). ERFWeb never
blocks an employee's filing if this call fails; it retries later.

## 6. Status codes ERFWeb handles

| Code | Meaning to ERFWeb |
| --- | --- |
| 200 / 204 | OK |
| 400 | Bad query (e.g. range too long); shown to the employee as "CRM rejected the search". |
| 401 / 403 | Credential problem; ERFWeb logs it and says "CRM unavailable". |
| 404 | Employee or activity not found. |
| 429 | Too many calls; include `Retry-After`. |
| 5xx / timeout | ERFWeb lets the employee continue by typing the line manually. |

## 7. What we need back from the CRM team

1. Production host confirmation, a test environment, and the `X-Api-Key` handed over securely.
2. The list of `activityType` and `status` values.
3. Whether one activity can span several days, and if so whether they send one record per day.
4. Whether they want the claimed callback (section 5).
5. A test employee with a few completed activities in the test environment.

The employee lookup returns `mobileNumber` (the CRM `CNTNUM` column, `null` once the person has left).
ERFWeb copies it into an empty Mobile No. on the Add User screen, for SMS notifications.

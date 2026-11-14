# Operations UI

Primary archetype: enterprise-dashboard. Continue the existing internal stockroom
sidebar, neutral palette, compact tables and form panels. New screens cover counts,
supplier/accounts administration, summaries and backup status. Do not introduce a
new design system or UI library.

Use Field, Notice, Empty, Pagination and existing CSS tokens. Always label the
business date and snapshot time of reports. A queued job is not a completed result.
Confirm mutations explicitly; preserve form data on failures and allow refresh.
Search selectors must query beyond the first 100 records and preserve selections.
Verify loading/empty/error states, keyboard labels and 390/1440px layouts.

## 14 November 2026 — whole-workspace visual refresh

Retain the enterprise-dashboard archetype and existing React/plain-CSS components.
The revised shared palette uses forest navigation, warm neutral backgrounds,
white working surfaces and a restrained green accent. This supersedes the older
navy/cyan styling while retaining the documented operational layout.

Navigation is grouped by workspace, stock/trading and management. At 900px and
below it becomes an explicit drawer, with keyboard focus containment, Escape
closure and focus return. Wide tables scroll within their own container.
Login, tables, forms, search selectors, notices, empty states and pagination
share the revised typography, borders, spacing and focus treatment. Destructive
trade confirmations use a distinct danger action. Dashboard adds actual API-backed
quick date ranges and labelled zero-transaction periods; no invented metrics.

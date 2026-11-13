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

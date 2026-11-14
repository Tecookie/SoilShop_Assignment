# Review checklist

- [ ] 390px and 1440px viewports: no page overflow or clipped actions.
- [ ] Default, loading, empty and failed request states.
- [ ] Employee cannot reach Admin actions; API also enforces roles.
- [ ] Stale versions display actionable reload/recount feedback.
- [ ] Backup/report pending and completed states are distinct.
- [ ] Existing trading and advice workflows remain usable.

## 14 November 2026 visual review

- [x] Production TypeScript/Vite build succeeds.
- [x] Real-browser Admin navigation across all 14 screens at 1440/768/390px.
- [x] No whole-page horizontal overflow in these viewports.
- [x] Password toggle, mobile keyboard navigation and sign-out checked.
- [x] Dashboard quick range uses the real local API.
- [x] Inventory empty/error responses injected in the browser, then recovery checked.
- [ ] A full mutation/recovery regression suite is separate from this visual review.

Repeatable check: `scripts/browser_ui_review.py`, using the existing browser Python
environment and Edge, with local Vite/API running. Screenshots and results are
written under ignored `test-results/ui-redesign/`. It opens forms without saving
and does not confirm trades or alter inventory/administration data.

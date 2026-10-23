# Initial runnable application

Status: In progress

Implement the approved design incrementally. This first runnable increment covers configuration, the 18-table EF model/migration, JWT login with active-account checks, Admin catalogue/SKU/supplier maintenance, Employee receipts/sales and transactional confirmation, gRPC totals, Redis outbox/low-stock consumer, React screens, and six-service Compose deployment. Verify real PostgreSQL concurrency, rollback and idempotency before browser verification.

Remaining later increments: complete source-reviewed advice, physical counts/adjustments, cancellation/draft editing and deletion, account management, suggestion conversion, daily summaries and scheduled/manual backups. Do not expose unfinished controls or claim those features are implemented.

Trust boundaries: browser input cannot assign roles, stock, status or totals. JWT signing key and bootstrap credentials come from ignored local environment. API owns stock transactions; calculation RPC occurs before locks. PostgreSQL constraints remain authoritative; Redis failure cannot undo a confirmed sale. Tokens stay in React memory. No remote publication.

UI contract: navy/cyan workspace with compact navigation, searchable inventory table, explicit low-stock labels, labelled forms, document draft/confirm actions, loading/empty/error/conflict states, and local horizontal table scrolling on narrow displays.

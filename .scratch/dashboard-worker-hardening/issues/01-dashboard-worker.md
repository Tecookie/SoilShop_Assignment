# Dashboard and worker reliability

Status: Complete

## Comments

- Preserve uncommitted previous workflow work on the existing feature branch.
- Daily/weekly aggregates, settings validation and supplier deactivation are covered
  by integration tests. Real Redis quarantine and responsive dashboard were checked
  in the separate `soilshop-worker-verification` Compose project.

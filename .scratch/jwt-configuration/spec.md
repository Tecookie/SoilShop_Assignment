# Distinguish missing and short JWT configuration

Status: Complete

The local Development JSON has a 44-byte key, and its launch profile selects Development. Native launch with that profile passes JWT validation and reaches PostgreSQL initialization. Running without a launch profile defaults to Production and reproduces the original misleading minimum-length error because Development JSON is not loaded. Environment overrides were checked by presence/byte length without printing secret values.

Startup now distinguishes missing versus short keys and reports environment/content root plus configuration instructions. The minimum 32-byte check remains enforced; local secrets/profile files remain outside the commit. Native solution build passed (zero warnings/errors), and three subprocess checks passed: absent key, 31-byte key with no leakage, and Development file loading against an intentionally unavailable probe database. Full PostgreSQL suite could not start because Docker Desktop's engine is not running; no full-suite pass is claimed.

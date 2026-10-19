# Contracts

Shared between the backend and the frontend (ADR-03).

| Path | Content |
|---|---|
| `openapi.json` | The API contract. **Generated from the API at build time and committed**; never edited by hand once generation exists. The frontend generates its TypeScript types from it (`npm run contracts:generate` in `/frontend`). |
| `test-vectors/` | JSON test vectors for rules implemented on both sides (e.g., Egyptian phone normalization), run by both the .NET and the Angular test suites so the two implementations cannot drift (ADR-03). |

> **Temporary state (M0 step 5, 2026-10-19):** `openapi.json` is a **hand-written stand-in** (`info.version: 0.0.0-temporary`, `x-platform-temporary: true`) because backend generation is not ready yet. It describes only the health endpoints that exist on `main`. When the backend generates the document, the generated file replaces it, the frontend regenerates its types, and the `x-platform-temporary` marker disappears.

## Drift check

- **Frontend (now):** `npm run contracts:check` in `/frontend` fails when `projects/api-contracts/src/lib/schema.d.ts` does not match `openapi.json`.
- **Full check (with backend generation, CI in M0 step 6):** `dotnet build` (regenerates `openapi.json`) → `npm run contracts:generate` → `git diff --exit-code -- contracts/ frontend/projects/api-contracts/` (M0-13).

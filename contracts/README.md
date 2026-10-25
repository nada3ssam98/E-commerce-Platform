# Contracts

Shared between the backend and the frontend (ADR-03).

| Path | Content |
|---|---|
| `openapi.json` | The API contract. **Generated from the API at build time and committed**; never edited by hand. The frontend generates its TypeScript types from it (`npm run contracts:generate` in `/frontend`). |
| `test-vectors/` | JSON test vectors for rules implemented on both sides (e.g., Egyptian phone normalization), run by both the .NET and the Angular test suites so the two implementations cannot drift (ADR-03). |

## Drift check

Every build of `backend/src/Api` rewrites `openapi.json` from the running endpoints (`Microsoft.Extensions.ApiDescription.Server`; configuration in `Platform.Api/OpenApiContract.cs`). The document is not served by the API at runtime.

After changing an endpoint:

```bash
dotnet build backend/src/Api/Platform.Api.csproj   # regenerates contracts/openapi.json
cd frontend && npm run contracts:generate          # regenerates the TypeScript types
```

Commit both files. CI fails otherwise (M0-13):

- **Contract drift check** job: builds the API, regenerates the types, then `git diff --exit-code -- contracts/ frontend/projects/api-contracts/`.
- **Frontend** job: `npm run contracts:check` fails when the committed types do not match `openapi.json`.

# Repository Guidelines

## Project Structure & Module Organization
Product Catalog enforces a strict Vertical Slice Architecture: `src/ProductCatalog/Features/<Feature>` hosts autonomous slices split into `Application` (CQRS handlers, validators) and `UI` (DTOs, components). Shared rules and persistence live in `src/ProductCatalog/Shared`, while global kernels sit under `src/Shared/{Kernel,Application,Domain,Infrastructure}`. `src/ProductCatalog.Host` is the Blazor Server entry point that wires DI, middleware, and SignalR. Tests stay under `tests/ProductCatalog.*Tests`. Run `scripts/validate-vsa-structure.sh` after scaffolding to keep folder contracts honest.

## Build, Test, and Development Commands
- `podman compose up -d` (or `podman run …postgres…`): start the PostgreSQL defined in `compose.yml`.
- `dotnet build ProductCatalog.sln`: compile all projects and surface analyzer warnings.
- `dotnet run --project src/ProductCatalog.Host/ProductCatalog.Host.csproj`: launch the Blazor host (first run applies EF migrations).
- `dotnet ef database update --project src/ProductCatalog/Shared/Infrastructure/Persistence/ProductCatalog.Shared.Infrastructure.Persistence.csproj --startup-project src/ProductCatalog.Host/ProductCatalog.Host.csproj`: update the schema without serving the UI.
- `dotnet test ProductCatalog.sln` or `dotnet test tests/ProductCatalog.Application.UnitTests/ProductCatalog.Application.UnitTests.csproj`: run xUnit suites with coverlet.
- `scripts/validate-vsa-structure.sh`: lint the folder layout before opening a PR.

## Coding Style & Naming Conventions
Default to file-scoped namespaces, `sealed` classes or records, and immutable constructor parameters as shown in `CreateProductCommand`. Follow CQRS naming: `*Command`/`*Query`, paired `*Handler` and `*Validator`. DTOs end with `Dto`, store states with `State`, and page actions belong in `UI/Actions`. Keep FluentValidation rules declarative, respect PascalCase for public members, camelCase for locals, and retain Japanese XML doc comments so the AI-facing documentation stays accurate.

## Testing Guidelines
xUnit + FluentAssertions + Moq cover unit scenarios, and `tests/ProductCatalog.Web.IntegrationTests` spins up the host via `Microsoft.AspNetCore.Mvc.Testing`. Playwright-backed `ProductCatalog.E2ETests` walks the UI, so keep referenced components routable. Follow the `Method_ShouldOutcome_WhenCondition` naming pattern (`CreateProductCommandValidatorTests`) and run `dotnet test -p:CollectCoverage=true` whenever you change commands, handlers, or store actions.

## Commit & Pull Request Guidelines
Recent history sticks to Conventional Commit prefixes (`fix: …`, `feat: …`). Keep titles short (English or Japanese) and call out slice-level impact in the body. Every PR should summarize the feature touched, link issues, list the commands you ran, and attach screenshots or Playwright artifacts for UI changes.

## Security & Configuration Tips
Database settings live in `appsettings.Development.json`; keep tenant-specific secrets in user secrets or env vars instead of git. When persistence or infrastructure steps change, refresh `README_DATABASE.md` so others can reproduce Postgres locally, and prefer `podman compose down -v` before switching connection strings to avoid stale volumes.

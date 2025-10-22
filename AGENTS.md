# Repository Guidelines

ClarityDesk runs on ASP.NET Core 8 Razor Pages. Use this guide to align changes with existing workflows and reviewer expectations.

## Project Structure & Module Organization
- `Pages/` holds Razor Pages and PageModel classes; keep handlers thin and delegate work to `Services/`.
- `Services/` contains domain logic and external integrations; add matching interfaces under `Services/Interfaces/` when introducing dependencies.
- `Models/` defines EF Core entities and DTOs kept in sync with `Migrations/`.
- `Infrastructure/` hosts persistence helpers and DI extensions; static assets live in `wwwroot/`.

## Build, Test, and Development Commands
- `dotnet restore` — install NuGet dependencies.
- `dotnet build ClarityDesk.sln -c Release` — compile the solution and surface warnings before review.
- `dotnet ef database update` — apply migrations to the active connection.
- `dotnet run --project ClarityDesk.csproj` — run locally at `https://localhost:5001`.
- `dotnet test` — execute unit and integration suites; append `--collect:"XPlat Code Coverage"` when coverage is required.

## Coding Style & Naming Conventions
- Use 4-space indentation, trim trailing whitespace, and prefer `var` when the type is evident.
- Apply PascalCase to classes, Razor Pages, folders, and public members; camelCase for locals; prefix private fields with `_`.
- Keep PageModels lean by pushing business rules into services, reusing DTOs, and pairing data annotations with client validation in `wwwroot/js/`.

## Testing Guidelines
- Keep unit tests in `Tests/ClarityDesk.UnitTests` and integration tests in `Tests/ClarityDesk.IntegrationTests`; mirror the namespace of the code under test.
- Name tests `[Method]_[Scenario]_[Result]`, assert with FluentAssertions, and isolate dependencies with Moq.
- Target ≥80% coverage for touched services and run `dotnet test` before a PR, noting new data setup when integration workflows change.

## Commit & Pull Request Guidelines
- Branch names follow `feature/*`, `bugfix/*`, `docs/*`, or `refactor/*`.
- Commits use conventional prefixes (`feat:`, `fix:`, `docs:`, `test:`, `chore:`) and group related work.
- PRs should summarize changes, link issues, flag migrations (`dotnet ef database update`), and attach UI screenshots when Razor Pages move.
- Confirm CI status and tag reviewers closest to the affected area (services, pages, infrastructure) before requesting approval.

## Configuration & Security Tips
- Store secrets with `dotnet user-secrets` locally and environment variables or Key Vault in production; keep `appsettings.Production.json` sanitized.
- Document authentication or caching adjustments in `DEPLOYMENT.md` and highlight required settings inside the PR discussion.

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ClarityDesk is an Issue Tracking and Help Desk Management System built with **ASP.NET Core 8.0** using Razor Pages architecture. The application provides department-based issue assignment workflows with LINE Login authentication integration for the Asian market.

## Technology Stack

- **Framework**: ASP.NET Core 8.0 (Razor Pages, not MVC)
- **Language**: C# with nullable reference types enabled
- **Database**: SQL Server with Entity Framework Core 8.0
- **Authentication**: LINE Login OAuth 2.0 with cookie-based sessions
- **Testing**: xUnit, Moq, FluentAssertions, EF Core InMemory
- **Frontend**: Bootstrap 5, jQuery, Bootstrap Icons

## Essential Commands

### Build and Run
```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application (default: http://localhost:5191)
dotnet run

# Publish for deployment
dotnet publish -c Release
```

### Testing
```bash
# Run all tests (unit + integration)
dotnet test

# Run only unit tests
dotnet test Tests/ClarityDesk.UnitTests/ClarityDesk.UnitTests.csproj

# Run only integration tests
dotnet test Tests/ClarityDesk.IntegrationTests/ClarityDesk.IntegrationTests.csproj

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Database Management
```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations to database
dotnet ef database update

# Rollback to specific migration
dotnet ef database update <MigrationName>

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script from migrations
dotnet ef migrations script
```

## Project Architecture

### Layered Architecture Pattern

```
Pages (Razor Pages - Presentation Layer)
    ↓
Services (Business Logic Layer)
    ↓
Data/ApplicationDbContext (Data Access Layer)
    ↓
SQL Server Database
```

### Key Directories

- **`Data/`** - EF Core DbContext, entity configurations (Fluent API), database seeding
- **`Models/`** - Domain entities, DTOs, ViewModels, enums, extensions
  - `Entities/` - Database entities (User, IssueReport, Department, etc.)
  - `Enums/` - UserRole, IssueStatus, PriorityLevel
  - `ViewModels/` - Razor Page-specific view models
- **`Services/`** - Business logic with interface-based design
  - All services follow `IServiceName` / `ServiceName` pattern
- **`Pages/`** - Razor Pages (.cshtml/.cs pairs)
  - `Issues/` - Issue CRUD operations
  - `Admin/` - User and department management
  - `Account/` - Authentication flows
- **`Infrastructure/`** - Cross-cutting concerns (middleware, auth handlers, filters, tag helpers)
- **`Migrations/`** - EF Core database migrations
- **`Tests/`** - Separate projects for unit and integration tests

### Domain Model Relationships

**Core Entities:**
- **User**: LINE-based authentication, role-based access (Admin/User)
- **IssueReport**: Main tracking entity with status (Pending/Completed) and priority (Low/Medium/High)
- **Department**: Organizational units
- **DepartmentAssignment**: Join table linking Issues to Departments (many-to-many)
- **DepartmentUser**: Join table linking Users to Departments (many-to-many)

**Key Relationships:**
- IssueReport → User (AssignedUser): Many-to-One
- IssueReport → User (LastModifiedBy): Many-to-One for audit trail
- IssueReport ↔ Department: Many-to-Many via DepartmentAssignment
- Department ↔ User: Many-to-Many via DepartmentUser

## Configuration

### Required Settings (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=<server>;Database=ClarityDesk;User Id=<user>;Password=<password>;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true"
  },
  "LineLogin": {
    "ChannelId": "YOUR_LINE_CHANNEL_ID",
    "ChannelSecret": "YOUR_LINE_CHANNEL_SECRET",
    "CallbackPath": "/signin-line"
  }
}
```

### Application Entry Point

`Program.cs` configures:
- Razor Pages with page-level authorization conventions
- EF Core with SQL Server
- LINE OAuth authentication (365-day sessions, sliding expiration)
- DI container with scoped services
- Global exception handling middleware
- Response compression (Brotli + Gzip)

## Development Patterns and Conventions

### Service Layer Pattern
All business logic is encapsulated in services with interface contracts:
- `IIssueReportService` - Issue CRUD, filtering, statistics, Excel export
- `IAuthenticationService` - LINE OAuth integration
- `IDepartmentService` - Department management
- `IUserManagementService` - User administration

Services are registered as **scoped** in DI container.

### Repository Pattern
Implicit through EF Core:
- `ApplicationDbContext` acts as Unit of Work
- `DbSet<T>` properties provide repository-like access
- Use `.AsNoTracking()` for read-only queries

### Automatic Timestamp Management
`ApplicationDbContext` overrides `SaveChanges`/`SaveChangesAsync` to automatically:
- Set `CreatedAt` on new entities
- Update `UpdatedAt` on modified entities
- Track `LastModifiedById` for audit trail

### Authorization Model
- **Page-level authorization** configured via Razor Pages conventions in `Program.cs`
- **Role-based policies**: "Admin" policy restricts `/Admin/*` pages
- **Claims-based**: User role stored in claims (UserRole.Admin or UserRole.User)
- Cookie authentication with custom access denied page

### Error Handling
- **Global exception middleware** (`ExceptionHandlingMiddleware`) catches unhandled exceptions
- Services use try-catch with `ILogger<T>` for structured logging
- User feedback via `TempData` messages in Razor Pages

### Caching Strategy
- **IMemoryCache** for statistics dashboard (5-minute TTL)
- Cache invalidation on data changes
- Reduces database load for frequently accessed data

### Code Style
- **PascalCase**: Classes, methods, properties, public fields
- **camelCase with `_` prefix**: Private fields
- **Async suffix**: All async methods (e.g., `GetIssuesAsync`)
- **Chinese comments**: Used for domain-specific business logic
- **XML documentation**: Required on public service interfaces and methods

## Important Implementation Details

### LINE Authentication Flow
1. User clicks "LINE Login" → redirects to LINE OAuth
2. LINE callback → `AuthenticationService.HandleLineLoginAsync()`
3. First-time users auto-registered with "User" role
4. Profile sync on each login (DisplayName, PictureUrl from LINE)
5. Claims created with UserId, Name, Role
6. Cookie issued with 365-day expiration

### Issue Filtering System
`IssueReportService.GetFilteredIssuesAsync()` supports:
- Status, Priority, Department, AssignedUser filters
- Date range filtering (StartDate/EndDate)
- Keyword search (Title, Content, CustomerName)
- Sorting (Title, Status, Priority, RecordDate)
- Pagination (20 items per page)

Use `.AsNoTracking()` for read operations to improve performance.

### Excel Export
- Uses **EPPlus** library (Version 7.0.5)
- Generates `.xlsx` files with formatted headers
- Includes all issue fields with Chinese column names
- Auto-sized columns for readability

### Database Seeding
`ApplicationDbContextSeed.SeedAsync()` creates:
- Default admin user (LineUserId: "system_admin")
- Three default departments: 客服部, 技術部, 業務部

Seeding runs on application startup after migrations are applied.

## Testing Strategy

### Unit Tests (ClarityDesk.UnitTests - .NET 8.0)
- **Target**: Service layer business logic
- **Mocking**: Use Moq for dependencies (DbContext, ILogger, etc.)
- **Database**: EF Core InMemory provider for isolated tests
- **Assertions**: FluentAssertions for readable test assertions
- **Test files**: Named `<ServiceName>Tests.cs`

Example test structure:
```csharp
public class IssueReportServiceTests
{
    private readonly Mock<ApplicationDbContext> _mockDbContext;
    private readonly Mock<ILogger<IssueReportService>> _mockLogger;
    // Setup in constructor, test methods follow AAA pattern
}
```

### Integration Tests (ClarityDesk.IntegrationTests - .NET 9.0)
- **Target**: End-to-end scenarios across layers
- **Framework**: `Microsoft.AspNetCore.Mvc.Testing` with `WebApplicationFactory`
- **Database**: Test database or InMemory provider
- Test authentication flows, page navigation, data persistence

## Security Considerations

When adding new features:
- **Validate all user input** to prevent XSS, SQL injection (EF Core parameterizes queries)
- **Check authorization** on all admin operations (use `[Authorize(Policy = "Admin")]` or page conventions)
- **Never log sensitive data** (passwords, LINE tokens, personal info)
- **Use HTTPS** in production (enforced via middleware)
- **Anti-forgery tokens** on all POST/PUT/DELETE forms (automatic in Razor Pages)

## Common Gotchas

1. **Tests directory exclusion**: The main `.csproj` excludes `Tests/**` from compilation. Test projects reference the main project via `<ProjectReference>`.

2. **LINE Login requires HTTPS**: LINE OAuth callback only works with HTTPS. Use `https://` URLs in production and for LINE Console configuration.

3. **Nullable reference types**: Project uses `<Nullable>enable</Nullable>`. Always handle null cases or use nullable annotations (`?`, `!`).

4. **Many-to-many relationships**: When adding/removing Department assignments, use join tables (`DepartmentAssignment`, `DepartmentUser`) explicitly. EF Core 8.0 skip navigation properties are configured in Fluent API.

5. **Async all the way**: All database operations use async/await. Avoid blocking calls like `.Result` or `.Wait()`.

6. **Migrations vs Database**: After creating a migration, **always run `dotnet ef database update`** to apply changes. Migrations are code-only until applied.

## Performance Tips

- Use `.AsNoTracking()` for read-only queries (already implemented in service layer)
- Eager load related entities with `.Include()` to avoid N+1 queries
- Paginate large result sets (already implemented with 20 items/page)
- Cache expensive queries (statistics already cached for 5 minutes)
- Index frequently queried columns (defined in entity configurations)

## Deployment Notes

- **IIS Deployment**: `web.config` configured for ASP.NET Core Module V2
- **Environment Variables**: Set `ASPNETCORE_ENVIRONMENT` to `Production`
- **Connection String**: Override in `appsettings.Production.json` (never commit production secrets)
- **LINE Login**: Update `CallbackPath` and redirect URIs in LINE Developers Console
- **Database Migrations**: Run `dotnet ef database update` on production server or generate SQL script
- **Static Files**: wwwroot serves Bootstrap, jQuery from local files (not CDN) except Bootstrap Icons

## Resources

- **LINE Login Documentation**: https://developers.line.biz/en/docs/line-login/
- **Entity Framework Core Docs**: https://learn.microsoft.com/en-us/ef/core/
- **ASP.NET Core Razor Pages**: https://learn.microsoft.com/en-us/aspnet/core/razor-pages/
- **xUnit Testing**: https://xunit.net/

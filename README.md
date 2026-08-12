# Product Catalog API

A RESTful CRUD API for **Products** (and their related **Items** / stock records), built with
.NET 8, ASP.NET Core Web API, EF Core, JWT authentication, FluentValidation and Serilog,
following a Clean Architecture layout (Domain / Application / Infrastructure / API).

> **Heads-up on how this repo was produced.** This solution was scaffolded and written with
> the help of Claude (Anthropic's AI assistant) in a sandbox that has no outbound access to
> NuGet, so the code below has **not** been compiled or run in that sandbox — only visually
> reviewed. Before you submit this as your own take-home assessment, please read it end to
> end, run it locally (instructions below), fix anything that doesn't build or behave the way
> you'd want, and make sure you can explain every design decision — that's the point of a
> technical assessment. Treat this as a strong starting point / reference implementation, not
> a drop-in submission.

## Architecture

```
Solution/
├── src/
│   ├── API/              ASP.NET Core Web API - controllers, middleware, DI wiring, Program.cs
│   ├── Application/      DTOs, service interfaces + implementations, validators, AutoMapper profile
│   ├── Domain/            Entities (Product, Item), custom exceptions
│   └── Infrastructure/   EF Core DbContext, repositories, Unit of Work, JWT token service
├── tests/
│   ├── Application.Tests/     xUnit + Moq unit tests for the service layer
│   ├── Infrastructure.Tests/  Repository tests against a real (SQLite in-memory) provider
│   └── API.Tests/             WebApplicationFactory integration tests (auth + Products CRUD)
├── Dockerfile
└── docker-compose.yml    API + SQL Server
```

Dependencies point inward: `API → Application/Infrastructure → Domain`. `Application` defines
interfaces (`IProductRepository`, `IUnitOfWork`, `ITokenService`, ...) that `Infrastructure`
implements, so the service layer never depends on EF Core or ASP.NET Core directly.

### Database

```sql
CREATE TABLE [dbo].[Product] (
  [Id] INT NOT NULL PRIMARY KEY IDENTITY (1,1),
  [ProductName] NVARCHAR(255) NOT NULL,
  [CreatedBy] NVARCHAR(100) NOT NULL,
  [CreatedOn] DATETIME NOT NULL,
  [ModifiedBy] NVARCHAR(100) NULL,
  [ModifiedOn] DATETIME NULL
  -- + IsDeleted BIT NOT NULL DEFAULT 0, added for a non-destructive DELETE
)

CREATE TABLE [dbo].[Item] (
  [Id] INT NOT NULL PRIMARY KEY IDENTITY (1,1),
  [ProductId] INT NOT NULL FOREIGN KEY REFERENCES Product(Id),
  [Quantity] INT NOT NULL
)
```

No hand-written EF Core migrations are checked in (generating them needs the `dotnet-ef`
tool, which requires NuGet access this sandbox didn't have). Instead, `Program.cs` calls
`Database.EnsureCreatedAsync()` on startup, which builds the schema straight from the EF Core
model and seeds 3 demo products. **Once you have NuGet access**, swap this for real migrations:

```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add InitialCreate -p src/Infrastructure -s src/API
dotnet ef database update -p src/Infrastructure -s src/API
```

then replace `EnsureCreatedAsync()` in `Program.cs` with `context.Database.MigrateAsync()`.

## API Endpoints (v1)

| Method | Route                          | Auth              | Description                          |
|--------|---------------------------------|-------------------|---------------------------------------|
| POST   | `/api/v1/auth/login`            | none              | Exchange credentials for a JWT pair   |
| POST   | `/api/v1/auth/refresh`          | none              | Exchange a refresh token for a new pair |
| GET    | `/api/v1/products`              | none              | Paged list, `?pageNumber=&pageSize=&search=` |
| GET    | `/api/v1/products/{id}`         | none              | Product detail incl. items            |
| GET    | `/api/v1/products/{id}/items`   | none              | Items belonging to a product          |
| POST   | `/api/v1/products`              | Bearer            | Create a product                      |
| PUT    | `/api/v1/products/{id}`         | Bearer            | Update a product                      |
| DELETE | `/api/v1/products/{id}`         | Bearer, role=Admin| Soft-delete a product                 |
| GET    | `/api/v1/items/{id}`            | none              | Item detail                           |
| POST   | `/api/v1/items`                 | Bearer            | Create an item                        |
| PUT    | `/api/v1/items/{id}`            | Bearer            | Update an item's quantity             |
| DELETE | `/api/v1/items/{id}`            | Bearer, role=Admin| Delete an item                        |
| GET    | `/health`                       | none              | Liveness check                        |

Demo credentials seeded in `Infrastructure/Identity/AuthService.cs`: **admin / Passw0rd!**
(swap this in-memory user store for ASP.NET Core Identity or an external IdP for real use).

### Auth flow (high level)

1. `POST /api/v1/auth/login` with username/password → returns a short-lived (15 min) JWT
   **access token** + a longer-lived (7 day) opaque **refresh token**.
2. Call protected endpoints with `Authorization: Bearer <accessToken>`.
3. When the access token expires, `POST /api/v1/auth/refresh` with the *expired* access
   token + the refresh token → returns a new pair. The old refresh token is invalidated
   (single-use / rotation).

### Errors

All errors return a `application/problem+json` body via a global exception-handling
middleware (`API/Middleware/ExceptionHandlingMiddleware.cs`):

```json
{
  "title": "Resource not found",
  "status": 404,
  "detail": "Entity \"Product\" (99) was not found.",
  "instance": "/api/v1/products/99",
  "traceId": "0HN..."
}
```

Request payload validation (FluentValidation) runs automatically for every action via a
global MVC filter (`API/Filters/ValidateModelFilter.cs`) and returns `400` with a per-field
error dictionary.

## Running locally

### Option A — `dotnet run`

```bash
dotnet restore
dotnet build
dotnet run --project src/API
```

Swagger UI opens at the app root: **http://localhost:5203/** (or whatever port
`dotnet run` prints). Uses SQLite (`productcatalog.db`, created automatically) by default —
no external database required.

### Option B — Docker Compose (API + SQL Server)

```bash
docker compose up --build
```

API available at **http://localhost:8080/**, using SQL Server in the `sqlserver` container.

### Running the tests

```bash
dotnet test
```

## Configuration

Settings live in `src/API/appsettings.json` (and `appsettings.Development.json`), all
overridable via environment variables (e.g. `Jwt__Secret`, `ConnectionStrings__SqlServer`) or
`dotnet user-secrets` — **do not commit a real JWT secret or DB password**; the ones in this
repo are placeholders for local/demo use only.

| Key                          | Purpose                                      |
|-------------------------------|-----------------------------------------------|
| `DatabaseProvider`            | `Sqlite` (default, local) or `SqlServer`     |
| `ConnectionStrings:Sqlite`    | SQLite file path                              |
| `ConnectionStrings:SqlServer` | SQL Server connection string                  |
| `Jwt:Secret`                  | HMAC-SHA256 signing key (32+ chars)           |
| `Jwt:Issuer` / `Jwt:Audience` | JWT `iss`/`aud` claims                        |

## Performance & security notes

- Read-only queries use `AsNoTracking()`; list endpoints are paginated (`PageSize` capped
  at 100) and support server-side search.
- JWT bearer auth with short-lived access tokens + rotating refresh tokens; role-based
  `[Authorize(Roles = "Admin")]` on delete endpoints.
- FluentValidation on every write DTO; a correlation-id middleware
  (`X-Correlation-Id`) ties every log line for a request together via Serilog's LogContext.
- CORS, response compression, and HSTS/HTTPS redirection (outside Development) are wired up
  in `Program.cs`.
- Swagger/OpenAPI (with a bearer-token "Authorize" button) is served at the app root so it
  doubles as quick manual-testing UI.

## What's intentionally simplified

This is a take-home-sized project, not a production system. A few corners were cut on
purpose, called out here rather than hidden:

- **Auth store**: a single hardcoded demo user + an in-memory refresh-token dictionary
  instead of ASP.NET Core Identity / a real user table.
- **Migrations**: `EnsureCreatedAsync()` instead of versioned EF Core migrations (see above
  for how to add them once you have NuGet access).
- **Soft delete**: `Product.IsDeleted` isn't in the original schema in the assessment brief;
  it was added so `DELETE` doesn't orphan `Item` rows or destroy history.

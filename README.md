# 🌙 Mooncake E-Commerce Platform

A **Clean Architecture** (Onion/Hexagonal) ASP.NET Core 8 backend starter template.

---

## 📁 Project Structure

```text
mooncake-ecommerce-platform/
├── .agents/
│   └── AGENTS.md               # Coding guidelines & conventions
├── .vscode/
│   ├── launch.json                   # VS Code debugger settings
│   └── tasks.json                    # VS Code build tasks
├── docker/
│   ├── dev/
│   │   ├── docker-compose.dev.yml    # MySQL 8.0 + Adminer (dev)
│   │   └── .env.dev                  # Dev environment variables (BACKEND_PORT, DB config)
│   ├── stg/
│   │   ├── docker-compose.stg.yml    # MySQL 8.0 (staging, with healthcheck)
│   │   └── .env.stg                  # Staging environment variables
│   └── mysql/
│       └── init.sql                  # UTF-8 & UTC initialization
├── src/
│   ├── Mooncake.EcommercePlatform.Domain/
│   │   ├── Common/BaseEntity.cs
│   │   ├── Entities/User.cs
│   │   └── Enums/UserRole.cs
│   ├── Mooncake.EcommercePlatform.Application/
│   │   ├── Common/DTOs/ApiResponse.cs
│   │   ├── Common/Exceptions/HttpException.cs
│   │   ├── Common/Helpers/IUserHelper.cs
│   │   ├── Common/Helpers/UserHelper.cs
│   │   ├── Common/Interfaces/IDateTimeProvider.cs
│   │   ├── Common/Interfaces/ITraceContext.cs
│   │   ├── Common/Interfaces/IUserRepository.cs  ← Repository contract lives here
│   │   ├── Common/Utils/ResponseHelper.cs
│   │   ├── DTOs/Users/Requests/
│   │   ├── DTOs/Users/Responses/
│   │   ├── Services/Interfaces/IUserService.cs
│   │   ├── Services/Implementations/UserService.cs
│   │   └── DependencyInjection.cs
│   ├── Mooncake.EcommercePlatform.Infrastructure/
│   │   ├── Migrations/
│   │   ├── Persistence/ApplicationDbContext.cs
│   │   ├── Persistence/Configurations/UserConfiguration.cs
│   │   ├── Repositories/UserRepository.cs
│   │   ├── Services/DateTimeProvider.cs
│   │   ├── Services/TraceContext.cs
│   │   └── DependencyInjection.cs
│   └── Mooncake.EcommercePlatform.WebApi/
│       ├── Configurations/SerilogSetup.cs
│       ├── Controllers/BaseApiController.cs
│       ├── Controllers/UsersController.cs
│       ├── Middleware/GlobalExceptionHandler.cs
│       ├── Middleware/TraceIdMiddleware.cs
│       ├── logs/                         # Generated Serilog files (debug, info, warn, error)
│       ├── appsettings.json
│       ├── appsettings.Development.json
│       └── Program.cs
├── Mooncake.EcommercePlatform.sln
└── README.md
```

---

## 🏗️ Architecture

```
Domain  ←  Application  ←  Infrastructure  ←  WebApi
(arrow = "knows about / depends on")
```

### Dependency Flow Rules

Each layer may only reference the layer(s) to its **left**. Violations break the architecture.

| Layer | Depends On | Implemented By |
|---|---|---|
| **Domain** | *(nothing)* — zero external dependencies | Entities, Enums, `BaseEntity` |
| **Application** | Domain only | Service interfaces (`IUserService`), repository & provider contracts (`IUserRepository`, `IDateTimeProvider`, `ITraceContext`), business logic (`UserService`), DTOs, Helpers |
| **Infrastructure** | Application + Domain | EF Core `DbContext`, `UserRepository`, `DateTimeProvider`, `TraceContext`, Migrations |
| **WebApi** | Application + Infrastructure | Controllers, Middleware, `Program.cs`, Serilog setup |

### Data Flow per Request

```
HTTP Request
    → WebApi (Controller receives request, validates DTO)
        → Application (Service executes business logic via interfaces)
            → Infrastructure (Repository/Provider fulfils the contract against DB/HTTP)
                → Domain (Entities are created / mutated)
            ← Infrastructure (Returns Domain Entity)
        ← Application (Maps Entity → Response DTO)
    ← WebApi (Wraps result in ApiResponse<T> and returns HTTP response)
```

### Key Invariants
- **Domain has zero project references** — it must compile standalone.
- **Application never references Infrastructure** — it only depends on interfaces.
- **Infrastructure never contains business logic** — it only fulfils contracts.
- **WebApi never calls repositories directly** — it always goes through a Service.

---

## ⚡ Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [dotnet-ef CLI](https://learn.microsoft.com/en-us/ef/core/cli/dotnet): `dotnet tool install --global dotnet-ef`

---

### Step 1 — Start the Development Database

```bash
cd docker/dev
docker compose -f docker-compose.dev.yml --env-file .env.dev up -d
```

| Service | URL |
|---|---|
| MySQL 8.0 | `localhost:3306` |
| Adminer UI | http://localhost:8080 |

**Adminer login:**
*(Refer to `.env.dev` for latest credentials)*
- Server: `mysql-dev`
- Username: `mooncake_user`
- Password: `mooncake_dev_password`
- Database: `mooncake_db`

---

### Step 2 — Run Migrations

```bash
# Apply migrations to the database
dotnet ef database update \
  --project src/Mooncake.EcommercePlatform.Infrastructure \
  --startup-project src/Mooncake.EcommercePlatform.WebApi
```

---

### Step 3 — Launch the Application

The application reads port and database connections dynamically from `.env.dev` using **DotNetEnv**, following 12-Factor App principles.

**Option A — VS Code (F5):**  
Open the project in VS Code. Press **F5** to start with the `.vscode/launch.json` debugger attached.

**Option B — CLI:**
```bash
cd src/Mooncake.EcommercePlatform.WebApi
dotnet run
```

**Swagger UI:** http://localhost:8089/swagger
*(Port dynamically assigned by `BACKEND_PORT` in `.env.dev`)*

---

## 🔍 API Response Format

Every endpoint returns a standardised `ApiResponse<T>` envelope.

### ✅ Success — List All Users

**Request:**
```
GET /api/v1/users
```

**Response:**
```json
{
  "status": 200,
  "message": "Users retrieved successfully.",
  "data": [
    {
      "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
      "username": "john.doe",
      "email": "john.doe@example.com",
      "fullName": "John Doe",
      "role": "Customer",
      "createdAtUtc": "2026-09-19T13:00:00Z",
      "updatedAtUtc": "2026-09-19T13:00:00Z"
    }
  ],
  "path": "/api/v1/users",
  "traceId": "a1b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4",
  "time": "2026-09-19T13:05:00Z"
}
```

---

### ✅ Success — Create User

**Request:**
```json
POST /api/v1/users
{
  "username": "jane.smith",
  "email": "jane.smith@example.com",
  "fullName": "Jane Smith",
  "password": "Secure@1234"
}
```

**Response:**
```json
{
  "status": 200,
  "message": "User created successfully.",
  "data": {
    "id": "7fa85f64-0000-4562-b3fc-2c963f66afa6",
    "username": "jane.smith",
    "email": "jane.smith@example.com",
    "fullName": "Jane Smith",
    "role": "Customer",
    "createdAtUtc": "2026-09-19T13:10:00Z",
    "updatedAtUtc": "2026-09-19T13:10:00Z"
  },
  "path": "/api/v1/users",
  "traceId": "b2c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5",
  "time": "2026-09-19T13:10:00Z"
}
```

---

### ❌ Error — User Not Found (404)

```json
{
  "status": 404,
  "message": "User with id '00000000-0000-0000-0000-000000000000' was not found.",
  "path": "/api/v1/users/00000000-0000-0000-0000-000000000000",
  "traceId": "c3d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6",
  "time": "2026-09-19T13:12:00Z"
}
```

---

### ❌ Error — Validation Failed (400)

```json
{
  "status": 400,
  "errors": [
    "The Email field is not a valid e-mail address.",
    "The Password field must be at least 8 characters long."
  ],
  "path": "/api/v1/users",
  "traceId": "d4e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1",
  "time": "2026-09-19T13:14:00Z"
}
```

---

### ❌ Error — Internal Server Error (500)

```json
{
  "status": 500,
  "message": "Internal server error",
  "path": "/api/v1/users",
  "traceId": "e5f6a1b2c3d4e5f6a1b2c3d4e5f6a1b2",
  "time": "2026-09-19T13:15:00Z"
}
```

> **Trace Verification**: The `traceId` in every JSON response is **identical** to the `X-Trace-Id` HTTP response header, and is embedded in every structured log line emitted by **Serilog** during that request, enabling end-to-end distributed tracing.

---

## 📝 Logging (Serilog)

Logs are generated in `src/Mooncake.EcommercePlatform.WebApi/logs/` and split by log level:
- `/debug/` — Detailed debugging events, including EF Core SQL queries (`CommandExecuting` / `CommandExecuted`).
- `/info/` — Application lifecycle events and API request traces.
- `/warn/` — Recoverable errors and warnings.
- `/error/` — Unhandled exceptions and critical errors.

SQL queries are explicitly mapped to the `Debug` level to ensure granular tracking of database operations.

---

## 🌐 Timezone Policy

| Layer | Setting |
|---|---|
| C# Runtime | `DateTime.UtcNow` only; `TZ=Etc/UTC` in environment variables |
| Docker containers | `TZ: Etc/UTC` environment variable |
| MySQL server | `--default-time-zone=+00:00` command flag + `init.sql` |
| EF Core entity fields | `CreatedAtUtc`, `UpdatedAtUtc` — always UTC |

**`DateTime.Now` is PROHIBITED.** All timestamps MUST use `DateTime.UtcNow`.

---

## 📦 Tech Stack

| Component | Technology |
|---|---|
| Framework | ASP.NET Core 8.0 |
| Language | C# 12 |
| ORM | Entity Framework Core + Pomelo.MySql |
| Database | MySQL 8.0 |
| Logging | Serilog (Async File Appenders) |
| Configuration | DotNetEnv (12-Factor App) |
| API Docs | Swagger / OpenAPI (Swashbuckle) |
| Containerization | Docker Compose |

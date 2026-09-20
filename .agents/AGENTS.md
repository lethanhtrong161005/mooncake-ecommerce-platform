# C# Coding Rules — Mooncake E-Commerce Platform

> **Scope**: These rules apply to every file under `src/` and any auto-generated code that is checked into the repository.

---

## 1. Language Enforcement

| Rule | Detail |
|---|---|
| **English Only** | 100 % of identifiers, method names, class names, XML documentation, exception messages, log strings, API response messages, and commit messages MUST be written in **English**. Vietnamese (or any other language) is strictly prohibited in source code. |
| **Extract Private Helpers** | If a service or controller contains private helper methods with complex or repetitive logic (e.g., mapping entities to DTOs, string parsing), these MUST be extracted into a separate static class suffixed with `Helper` (e.g., `UserHelper`, `StringHelper`) to keep the primary class focused and readable. Do not use `Mapper` for class names. |
| **No Magic Strings** | All user-facing or log strings must be defined as `const string` or `static readonly string` fields, never inline. |

---

## 2. Naming Conventions

### 2.1 Solution & Project Names
- Solution: `Mooncake.EcommercePlatform.sln`
- Projects use `PascalCase` with the prefix `Mooncake.EcommercePlatform.*`:
  - `Mooncake.EcommercePlatform.Domain`
  - `Mooncake.EcommercePlatform.Application`
  - `Mooncake.EcommercePlatform.Infrastructure`
  - `Mooncake.EcommercePlatform.WebApi`

### 2.2 Identifiers
| Item | Convention | Example |
|---|---|---|
| Classes / Records | PascalCase | `UserService`, `ApiResponse<T>` |
| Interfaces | `I` prefix + PascalCase | `IUserRepository`, `IUserService` |
| Async methods | Suffix `Async` | `GetByIdAsync`, `CreateUserAsync` |
| Private fields | `_camelCase` | `_userRepository` |
| Local variables | `camelCase` | `userId`, `traceId` |
| Constants | `PascalCase` | `TraceIdHeader` |
| DTOs — request | `*Request` suffix | `CreateUserRequest`, `UpdateUserRequest` |
| DTOs — response | `*Response` suffix | `UserResponse` |
| Enums | PascalCase members | `UserRole.Admin`, `UserRole.Customer` |

---

## 3. Standard `ApiResponse<T>` DTO

File: `src/Mooncake.EcommercePlatform.Application/Common/DTOs/ApiResponse.cs`

```csharp
namespace Mooncake.EcommercePlatform.Application.Common.DTOs;

using System.Text.Json.Serialization;

public class ApiResponse<T>
{
    public int Status { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Message { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public T? Data { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Errors { get; set; }

    public string Path { get; set; } = string.Empty;

    public string TraceId { get; set; } = string.Empty;

    public DateTime Time { get; set; } = DateTime.UtcNow;
}
```

---

## 4. `ResponseHelper` — Static Response Builders

File: `src/Mooncake.EcommercePlatform.Application/Common/Utils/ResponseHelper.cs`

```csharp
namespace Mooncake.EcommercePlatform.Application.Common.Utils;

using Mooncake.EcommercePlatform.Application.Common.DTOs;

public static class ResponseHelper
{
    private static ApiResponse<T> BuildSuccess<T>(string message, T? data, string path, string traceId)
    {
        return new ApiResponse<T>
        {
            Status = 200,
            Message = message,
            Data = data,
            Path = path,
            TraceId = traceId,
            Time = DateTime.UtcNow
        };
    }

    public static ApiResponse<T> SuccessWithData<T>(T data, string message, string path = "", string traceId = "") =>
        BuildSuccess(message, data, path, traceId);

    public static ApiResponse<object> Success(string message, string path = "", string traceId = "") =>
        BuildSuccess<object>(message, null, path, traceId);

    public static ApiResponse<T> Error<T>(int httpStatus, string message, string path = "", string traceId = "") =>
        new()
        {
            Status = httpStatus,
            Message = message,
            Path = path,
            TraceId = traceId,
            Time = DateTime.UtcNow
        };

    public static ApiResponse<T> Error<T>(int httpStatus, List<string> errors, string path = "", string traceId = "") =>
        new()
        {
            Status = httpStatus,
            Errors = errors,
            Path = path,
            TraceId = traceId,
            Time = DateTime.UtcNow
        };
}
```

---

## 5. MDC Pattern — `TraceIdMiddleware`

File: `src/Mooncake.EcommercePlatform.WebApi/Middleware/TraceIdMiddleware.cs`

```csharp
namespace Mooncake.EcommercePlatform.WebApi.Middleware;

public class TraceIdMiddleware(RequestDelegate next, ILogger<TraceIdMiddleware> logger)
{
    private const string TraceIdHeader = "X-Trace-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = context.Request.Headers[TraceIdHeader].FirstOrDefault()
                      ?? Guid.NewGuid().ToString("N");

        context.Items["TraceId"] = traceId;
        context.Response.Headers[TraceIdHeader] = traceId;

        // MDC equivalent: Attach TraceId to all subsequent logs in this request scope
        using (logger.BeginScope(new Dictionary<string, object> { ["TraceId"] = traceId }))
        {
            await next(context);
        }
    }
}
```

---

## 6. `HttpException` & `GlobalExceptionHandler`

### HttpException
```csharp
public sealed class HttpException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}
```

### GlobalExceptionHandler Rules
- Extract `traceId = httpContext.Items["TraceId"]?.ToString() ?? httpContext.TraceIdentifier`
- Extract `path = httpContext.Request.Path.Value ?? string.Empty`
- `HttpException` → `ResponseHelper.Error<object>(ex.StatusCode, ex.Message, path, traceId)`
- Validation / bad request errors → status `400` with `errorsList`, `path`, `traceId`
- Unhandled `Exception` → status `500`, `"Internal server error"`, `path`, `traceId`

---

## 7. `BaseApiController` Convenience Methods

```csharp
protected IActionResult Success<T>(T data, string message) =>
    Ok(ResponseHelper.SuccessWithData(data, message, Request.Path.Value ?? "", HttpContext.Items["TraceId"]?.ToString() ?? ""));

protected IActionResult Success(string message) =>
    Ok(ResponseHelper.Success(message, Request.Path.Value ?? "", HttpContext.Items["TraceId"]?.ToString() ?? ""));
```

---

## 8. Date & Time Standards

| Rule | Enforcement |
|---|---|
| `DateTime.Now` | PROHIBITED — fails code review |
| `DateTime.UtcNow` | Required everywhere |
| Docker / MySQL TZ | `TZ=Etc/UTC` in every container |
| C# runtime | `"TZ": "Etc/UTC"` in `launchSettings.json` |
| `IDateTimeProvider` | Inject into all production services |

---

## 9. Modern C# Style (C# 12)

| Feature | Required |
|---|---|
| File-scoped namespaces | `namespace Foo.Bar;` |
| Primary constructors | `public class Svc(IDep dep)` |
| Target-typed `new()` | When type is inferred |
| `<Nullable>enable</Nullable>` | Every `.csproj` |
| `<ImplicitUsings>enable</ImplicitUsings>` | Every `.csproj` |
| XML docs `/// <summary>` | All public APIs |

---

## 10. Configurations & Environment

- **Do Not Touch Configs**: Do not modify existing configuration files (`.env`, `appsettings.json`, `appsettings.Development.json`, `launch.json`, or DI configuration) unless explicitly requested by the user.

---

## 11. Clean Architecture — Mandatory Dependency & Data Flow

### Dependency Direction (MUST be strictly enforced)

```
Domain  ←  Application  ←  Infrastructure  ←  WebApi
(arrow = "knows about / depends on")
```

Each layer may only reference the layer(s) to its **left**. Any violation is a hard rule break.

| Layer | Allowed Dependencies | Contains |
|---|---|---|
| **Domain** | *(none)* — zero project references | Entities, Enums, `BaseEntity` |
| **Application** | Domain only | `IUserRepository`, `IUserService`, `IDateTimeProvider`, `ITraceContext`, `UserService`, DTOs, Helpers, Utils, Exceptions |
| **Infrastructure** | Application + Domain | `UserRepository`, `DateTimeProvider`, `TraceContext`, EF Core `DbContext`, Configurations, Migrations |
| **WebApi** | Application + Infrastructure | Controllers, Middleware, Serilog setup, `Program.cs` |

### Data Flow per HTTP Request (READ THIS before writing new code)

```
HTTP Request
    → WebApi (Controller receives request, validates DTO)
        → Application (Service executes business logic via interfaces)
            → Infrastructure (Repository/Provider fulfils the contract against DB/HTTP)
                → Domain (Entities are created / mutated)
            ← Infrastructure (Returns Domain Entity)
        ← Application (Maps Entity → Response DTO via IUserHelper)
    ← WebApi (Wraps result in ApiResponse<T> and returns HTTP response)
```

### Key Invariants (MANDATORY — never violate)

- **Domain has zero project references** — it must compile completely standalone.
- **Application NEVER references Infrastructure** — it only depends on abstractions (interfaces).
- **Infrastructure NEVER contains business logic** — it only fulfils contracts defined in Application.
- **WebApi NEVER calls repositories directly** — all data access must go through a Service in Application.
- **Repository interfaces** (e.g., `IUserRepository`) MUST reside in `Application/Common/Interfaces/`, NOT in `Domain/`.



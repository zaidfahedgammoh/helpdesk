# HelpDesk Live

HelpDesk Live is an ASP.NET Core solution split into Domain, Application, Infrastructure, API, and MVC projects. The API owns persistence and JWT authentication; the MVC application calls the API and keeps its short-lived access token in server-side session storage.

## Configuration

Do not commit connection strings, JWT signing keys, or test passwords. Configure these values through user secrets, a secret manager, or environment variables.

Required API settings:

| Setting | Purpose |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string |
| `Jwt__Issuer` | JWT issuer |
| `Jwt__Audience` | JWT audience |
| `Jwt__SigningKey` | Random signing key of at least 32 UTF-8 bytes |
| `Cors__AllowedOrigins__0` | Exact MVC origin, for example `https://localhost:7082` |

The MVC application also requires `ApiSettings__BaseUrl` to point to the API base URL.

Example PowerShell setup for a local development shell (replace values with your own):

```powershell
$env:ConnectionStrings__DefaultConnection = "Server=localhost;Database=HelpDeskLiveDb;Trusted_Connection=True;TrustServerCertificate=True;"
$env:Jwt__Issuer = "HelpDeskLive.API"
$env:Jwt__Audience = "HelpDeskLive.Client"
$env:Jwt__SigningKey = "<generate-a-random-secret-at-least-32-bytes-long>"
$env:Cors__AllowedOrigins__0 = "https://localhost:7082"
$env:ApiSettings__BaseUrl = "https://localhost:7001"
```

For development-only sample accounts, explicitly opt in with `SeedTestUsers__Enabled=true` and provide `SeedTestUsers__Password` (at least 12 characters). Test accounts are not seeded in non-development environments. Use unique passwords and never use this mechanism in production.

## Ticket and chat behavior

- Normal ticket lifecycle: Open → InProgress → Resolved → Closed.
- Escalated is a separate queue state. Escalating releases the assigned agent; a department agent can pick up the escalated ticket.
- Live chat is limited to approved High and Critical tickets in Open or InProgress states.
- Customers can join an eligible waiting ticket; only its assigned agent can join as an agent.
- Chat messages are persisted before they are broadcast.
- Customer closure, agent resolution, closure, and escalation publish SignalR status events where currently implemented.

## Authentication

- Login issues a short-lived JWT access token.
- Refresh tokens are random, stored as SHA-256 hashes, expire after seven days, and rotate on successful refresh.
- The refresh token is issued in an HttpOnly cookie; logout revokes the stored refresh token.
- The MVC app stores the access token in server-side session rather than exposing it to page JavaScript.

## Current implementation notes and known gaps

This README records the implementation observed during the current code review; it is not a claim that every requirement in the full project audit has been completed.

- API controllers and the SignalR hub still contain direct EF Core business/data access in several workflows; full CQRS extraction is outstanding.
- A complete global ProblemDetails/exception middleware and MediatR validation/logging pipeline have not been verified or completed.
- A dedicated escalation-history entity/table and required escalation-reason capture are not implemented; the current escalation audit is a chat message.
- Department-level SignalR group subscription/authorization and push-driven queue updates are not complete; the agent queue currently refreshes periodically.
- Automatic access-token refresh from MVC when API requests receive 401 responses still needs end-to-end implementation.
- Reporting implementations using LINQ, stored procedures, Dapper, and ADO.NET need a full requirements audit.
- MessagePack configuration, benchmarks, and compression experiments have not been verified.
- Automated unit, integration, SignalR, authorization, and localization tests have not been added/verified as a complete suite.
- Arabic/English resource coverage and all frontend strings still need a full view-by-view audit.
- The API still applies EF Core migrations automatically at startup; review this operational choice for production deployment.

## Build and test

Build and test locally after configuring the required settings:

```powershell
dotnet restore
dotnet build
dotnet test
```

No build or test command was run as part of the current code changes.
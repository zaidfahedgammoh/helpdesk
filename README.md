# HelpDesk Live

ASP.NET Core HelpDesk application using Domain, Application, Infrastructure, API, and MVC projects. The solution keeps business entities in Domain, use cases and abstractions in Application, persistence in Infrastructure, HTTP/SignalR endpoints in API, and the browser-facing UI in MVC.

## Configuration

Set secrets through user-secrets, environment variables, or a secret manager. Do not commit credentials.

API:
- `ConnectionStrings__DefaultConnection`: SQL Server connection string.
- `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__SigningKey`: JWT settings. Signing key must be at least 32 UTF-8 bytes.
- `Jwt__AccessTokenMinutes`: access token lifetime; project requirement is 15 minutes.
- `Cors__AllowedOrigins__0`: exact MVC origin.

MVC:
- `ApiSettings__BaseUrl`: API base URL.

Development-only sample users require both `SeedTestUsers__Enabled=true` and `SeedTestUsers__Password` (at least 12 characters). They are not seeded outside Development.

## Ticket rules

- Normal lifecycle: Open → InProgress → Resolved → Closed.
- Escalated is a separate queue state; escalation releases the assigned agent and a different agent can pick it up.
- Customers may have at most three Open tickets.
- Agents may have at most five InProgress tickets.
- Approved High and Critical tickets use live chat.
- Closed tickets cannot accept new messages.
- Escalations require a reason and are stored in the TicketEscalations table, with the reason also visible in the ticket conversation history.

## Authentication

- Passwords are verified using ASP.NET Core PasswordHasher (PBKDF2-based).
- Login issues a short-lived JWT access token and a seven-day random refresh token.
- The database stores a SHA-256 hash of the refresh token.
- Refresh rotates the token; logout revokes the current refresh token.
- Refresh tokens are transported in an HttpOnly cookie. The MVC application stores the access token in server-side session.

## API routes

- `POST /api/auth/login`, `POST /api/auth/refresh`, `POST /api/auth/logout`
- `POST /api/tickets`
- `GET /api/customer/tickets`, `POST /api/customer/tickets/{id}/close`
- `GET /api/agent/tickets`, `POST /api/agent/tickets/{id}/start|resolve|close|escalate`
- `GET /api/chat/{id}/messages`
- `GET /api/admin/reports/department-ticket-status` (Dapper stored procedure)
- `GET /api/admin/reports/status-summary` (raw ADO.NET)
- SignalR hub: `/hubs/chat`

## Reporting implementations

1. LINQ: dashboard query handlers/repositories.
2. Dapper: `dbo.GetDepartmentTicketStatusReport` returns ticket counts by department/status and average close duration in hours.
3. Raw ADO.NET: status summary endpoint uses SqlConnection, SqlCommand, and SqlDataReader.

Dapper is suitable for compact, high-performance SQL projections and stored procedures; raw ADO.NET gives explicit control but requires more boilerplate; LINQ is maintainable and composable for normal application queries, though complex reports may generate less predictable SQL. Compare actual query plans and timings on representative data before making performance claims.

## Error handling and request pipeline

API middleware order: ProblemDetails exception middleware first, then HSTS/HTTPS redirection, routing, CORS, authentication, authorization, endpoint mapping. Exception middleware must wrap downstream middleware so controller and authorization errors can be normalized. MediatR registers request handlers, FluentValidation validators, validation behavior, and logging behavior (including elapsed execution time).

## Database

EF Core SQL Server migrations are under `HelpDeskLive.Infrastructure/Migrations`. A new migration `20261104140000_AddTicketEscalations` creates durable escalation history. `20261104143000_AddDepartmentTicketReportProcedure` creates the Dapper report procedure. Review the EF model snapshot when generating future migrations.

## Localization

MVC supports English and Arabic resource files and a culture cookie / Accept-Language provider. Arabic/English resource coverage for every page and API validation message still needs a full audit. User PreferredLanguage is not yet wired as a per-user culture preference.

## SignalR

JWT authentication is accepted from the `access_token` query parameter only for `/hubs/chat`. Ticket groups enforce customer ownership and assigned-agent access. Agents automatically join their department group on hub connection. Chat messages are persisted before broadcast; typing and participant events are available.

The hub still accesses EF Core directly for ticket authorization and message persistence rather than routing all hub operations through MediatR. MessagePack, JSON-vs-MessagePack benchmarks, compression experiments, and the separate MessagePack Notification API from the training document are not yet implemented.

## Tests

A domain test project covers normal ticket lifecycle, invalid transitions, escalation/reassignment, closure, and escalation reason validation. Add the test project to the solution and run it using:

```powershell
dotnet restore
dotnet test
dotnet build
```

No build or tests were run during the current repository edits, as requested. API integration, authentication/authorization, database, MVC, localization, and SignalR end-to-end tests remain outstanding.

## Remaining work / limitations

- Finish extracting controller and SignalR database/business operations into Application commands/queries and repository abstractions.
- Add GetTicketById and paged/filterable GetMyTickets queries; standardize all API responses as DTO records and add the documented AutoMapper profiles/custom mapping.
- Complete refresh-token rotation concurrency protection and implement automatic refresh/retry for MVC API calls after access-token expiry.
- Add complete API and MVC registration flow if public/self-service registration is intended; current account creation is admin-managed.
- Update the EF Core model snapshot for the new escalation entity and add integration coverage for both migrations.
- Add MessagePack server/client negotiation, payload DTO attributes, serialization-size benchmark, and compression experiment.
- Implement the separate minimal Notification API and call it when a ticket closes.
- Complete Arabic/English resource coverage, validation/error localization, and per-user PreferredLanguage selection.
- Add integration and SignalR tests for multi-session customer/agent chat, unauthorized access, queue notification, escalation, and closed-ticket behavior.
- Review MVC token exposure in the chat page: SignalR currently receives the short-lived JWT through the browser's SignalR accessTokenFactory. Keep its lifetime short and apply a strong Content Security Policy.
- Consider moving production database migrations out of application startup into a controlled deployment step.

Do not treat the remaining list as completed functionality.
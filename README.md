# HelpDesk Live

HelpDesk Live is a support ticketing system with real-time live chat built on ASP.NET Core 10.0, Clean Architecture, CQRS with MediatR, SignalR, MessagePack, and Entity Framework Core.

---

## Architecture Overview

The solution adheres to Clean Architecture principles with clear separation of concerns across projects:

```
HelpDeskLive.Domain          -> Domain entities, enums, business invariants
HelpDeskLive.Application     -> CQRS commands and queries, MediatR pipeline behaviors, DTOs, FluentValidation
HelpDeskLive.Infrastructure  -> EF Core DbContext, repositories, Dapper, ADO.NET, stored procedures, external services
HelpDeskLive.API             -> REST controllers, SignalR hub, MessagePack protocol, JWT authentication, ProblemDetails
HelpDeskLive.MVC             -> Customer, Agent, and Admin dashboards, real-time chat UI, cookie-to-session bridge
HelpDeskLive.NotificationAPI  -> Minimal API receiving binary MessagePack notifications on ticket closure
```

Dependency direction is strictly enforced: `API` -> `Infrastructure` -> `Application` -> `Domain`. The Domain project depends on nothing.

---

## Core Business Rules

- **Ticket Status Lifecycle**: Follows a strict linear sequence: `Open` -> `InProgress` -> `Resolved` -> `Closed`. Status skipping and backward transitions are prohibited.
- **Closed Tickets**: Once closed, no further messages can be sent.
- **Agent Workload Limit**: An agent cannot hold more than 5 `InProgress` tickets simultaneously.
- **Customer Limit**: A customer cannot have more than 3 `Open` tickets simultaneously.
- **Live Chat Eligibility**: Live chat is reserved for approved tickets with `High` or `Critical` priority in `Open` or `InProgress` status.

---

## Configuration & Local Setup

### Local Database Configuration
In development, SQL Server LocalDB is configured by default in `HelpDeskLive.API/appsettings.Development.json`:
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=HelpDeskLiveDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True;"
}
```

### Seeding Credentials (Development)
When `SeedTestUsers:Enabled` is set to `true`, initial accounts are automatically seeded:
- **Admin**: `admin@helpdesklive.local` / `Password123!@#`
- **Agent**: `agent@helpdesklive.local` / `Password123!@#`
- **Customer**: `customer@helpdesklive.local` / `Password123!@#`

### Running the Application
From the solution root:
```powershell
# Run the API
dotnet run --project HelpDeskLive.API

# In a separate terminal, run the MVC client
dotnet run --project HelpDeskLive.MVC

# Optional: run the Notification API pipeline
dotnet run --project HelpDeskLive.NotificationAPI
```

---

## Technical Documentation & Discussion Points

### Part 1: Clean Architecture, Dependency Injection & Inversion of Control

#### Why Application Defines Interfaces and Infrastructure Implements Them
The Dependency Inversion Principle states that high-level modules should not depend on low-level modules; both should depend on abstractions. In Clean Architecture:
- The **Application layer** encapsulates business logic, use cases, and workflow orchestration. It specifies what capabilities it requires from the external world via interfaces (`ITicketRepository`, `IUserRepository`, `ITicketNotificationService`).
- The **Infrastructure layer** provides the technical implementation details (EF Core, SQL Server, Dapper, HTTP clients).
- By having Application define interfaces and Infrastructure implement them, the core domain and application logic remain completely independent of external databases, libraries, or frameworks. The database or external notification service can be changed without altering business rules.

#### Inversion of Control (IoC) vs Dependency Injection (DI)
- **Inversion of Control (IoC)**: An architectural design principle where control over program flow and object lifecycle is inverted and handed over to a framework or container, rather than having custom code directly instantiate dependencies.
- **Dependency Injection (DI)**: A concrete design pattern implementing IoC. Dependent objects receive their collaborators from an external injector (such as the ASP.NET Core `IServiceCollection` container) via constructor injection.

#### Service Lifetimes
- **Transient** (`AddTransient`): A new instance is created every time the service is requested. Best suited for lightweight, stateless services where no shared state is needed.
- **Scoped** (`AddScoped`): A single instance is created per HTTP request scope and shared across all consumers within that request. Essential for `DbContext`, repositories, and Unit of Work where operations within a single request must share a transaction or change tracker.
- **Singleton** (`AddSingleton`): Created once when first requested and reused for the entire application lifetime. Suitable for thread-safe utilities, application-wide caches, configuration providers, or token signers.

---

### Part 2: Data Layer (EF Core, Repository, IQueryable vs IEnumerable)

#### IQueryable vs IEnumerable
- **IQueryable<T>**: Represents an unexecuted query against an external data source. It maintains an Expression Tree that is translated by the EF Core provider into SQL. The query executes on the database server only when materialized (`ToListAsync()`, `FirstAsync()`, `CountAsync()`, or `foreach`).
- **IEnumerable<T>**: Represents an in-memory collection. LINQ operations on `IEnumerable` execute in application memory.

**Execution Example**:
```csharp
// 1. IQueryable - Filters executed on SQL Server
// SQL: SELECT [t].[Id], [t].[Title] FROM [Tickets] AS [t] WHERE [t].[Status] = 1
var queryable = _context.Tickets
    .Where(t => t.Status == TicketStatus.Open)
    .Select(t => new { t.Id, t.Title });
var results = await queryable.ToListAsync();

// 2. IEnumerable - Entire table pulled into memory before filtering
// SQL: SELECT [t].[Id], [t].[Title], [t].[Description], ... FROM [Tickets] AS [t]
var enumerable = _context.Tickets
    .AsEnumerable()
    .Where(t => t.Status == TicketStatus.Open);
```

#### Why a Repository on top of EF Core?
Although `DbContext` already implements the Unit of Work pattern and `DbSet<T>` acts as a repository, an explicit repository layer provides several key advantages:
1. It encapsulates complex data retrieval queries, specifications, and navigation property includes behind business-meaningful methods (`CountActiveTicketsForAgentAsync`).
2. It completely decouples MediatR command and query handlers from EF Core types, preventing database leakage into the Application layer.
3. It allows switching query implementations (e.g. replacing a complex EF query with Dapper or raw ADO.NET) without touching use-case handlers.

#### Is DbContext already a Unit of Work?
Yes. `DbContext` maintains an internal `ChangeTracker` that tracks state changes (Added, Modified, Deleted) across all entities during an operation. Calling `SaveChangesAsync()` commits all staged changes within an atomic database transaction.

#### When does ToList() pull everything into memory?
Calling `ToList()` or `ToListAsync()` immediately executes the underlying expression tree against the database, materializes the result set into .NET objects, and stores them in process memory. Any subsequent LINQ chaining executes in memory via LINQ to Objects rather than on the database engine.

---

### Part 3: DTOs, Security & Over-Posting

#### Why PasswordHash Must Never Be Exposed
Password hashes must never be serialized or returned in DTOs:
1. Even salted and hashed passwords (e.g. PBKDF2/BCrypt) can be subjected to offline dictionary and rainbow-table attacks if captured by unauthorized parties.
2. Exposing internal security fields in API responses leaks implementation details (such as algorithm iteration counts or formats) and violates the principle of least privilege.
3. Keeping `PasswordHash` internal to the database and authentication service ensures credentials cannot be intercepted in transit or cached in proxy logs.

#### What is Over-Posting?
Over-posting (mass assignment) occurs when an HTTP endpoint automatically binds client-supplied request fields directly to a domain entity. A malicious user can craft a JSON payload containing properties they should not modify (such as `Role: "Admin"`, `CustomerId: 99`, or `ApprovalStatus: "Approved"`). If the model binder maps these properties directly into the entity, unintended state mutations occur. Using strict request DTOs (`record`) ensures only explicitly allowed properties are bound and processed.

---

### Part 4: Security (JWT, Refresh Tokens & Status Codes)

#### How JWT and Cookies Work Together
- **Access Token (JWT)**: Short-lived (15 minutes), digitally signed with HMAC-SHA256. It contains user claims (Id, Role, Name) and is passed in HTTP Authorization headers or SignalR query strings for stateless validation without database lookups.
- **Refresh Token**: Long-lived (7 days), cryptographically secure random token stored as a SHA-256 hash in the database and delivered to the browser inside an `HttpOnly`, `SameSite=Strict`, `Secure` cookie.
- **Workflow**: Client requests authenticate with the short-lived access token. When expired, the MVC backend transparently exchanges the HttpOnly refresh cookie for a newly rotated access token and refresh token via the API `/api/auth/refresh` endpoint. Client-side JavaScript has zero access to the refresh cookie, preventing cross-site scripting (XSS) token theft.

#### Why the Access Token is Short-Lived
Because JWTs are self-contained and stateless, revocation cannot be verified without querying a database on every request. Keeping access tokens short-lived (15 minutes) ensures that if a token is ever compromised, the exposure window is minimal, while refresh token rotation guarantees ongoing active sessions are re-validated regularly.

#### Difference Between 401 Unauthorized and 403 Forbidden
- **401 Unauthorized**: The caller has not provided valid authentication credentials ("Who are you?"). The request lacks a valid token or authentication ticket.
- **403 Forbidden**: The caller is authenticated and recognized, but lacks sufficient permissions to access the requested resource ("You are not allowed here"). For example, a customer attempting to access the Admin dashboard or an agent trying to access tickets belonging to another department.

---

### Part 5: Application Layer (CQRS and MediatR)

#### How DI and MediatR Work Together (Automatic Handler Registration)
MediatR utilizes reflection-based assembly scanning. During startup, `services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly))` scans the Application assembly for all implementations of `IRequestHandler<TRequest, TResponse>`. Each handler is registered in the DI container with `Scoped` lifetime. When `ISender.Send(command)` is invoked, MediatR resolves the corresponding handler and any registered open pipeline behaviors (`ValidationBehavior<,>`, `LoggingBehavior<,>`) dynamically.

#### Why Reads and Writes are Separated (CQRS)
- **Command Path (Writes)**: Focuses on domain logic, transactional boundaries, validation, business invariants (e.g. ticket workload limits, state machine transitions), and tracking updates.
- **Query Path (Reads)**: Optimized for fast data projection and retrieval. Queries use `AsNoTracking()`, customized DTO projections, or high-performance SQL tools (Dapper, ADO.NET) without the overhead of domain entity tracking.
- Separating reads from writes allows each side to be optimized, scaled, and maintained independently.

---

### Part 6: Middleware & Error Handling Pipeline

#### Pipeline Order in ASP.NET Core
The request pipeline in `HelpDeskLive.API/Program.cs` is configured in the following order:

```
1. UseExceptionHandler()     -> Catches unhandled exceptions from all downstream middleware
2. UseHsts() / UseHttps()    -> Enforces secure transport
3. UseRouting()              -> Matches the incoming request URL to endpoint metadata
4. UseCors()                 -> Evaluates cross-origin policies and handles CORS preflight
5. UseAuthentication()       -> Validates JWT Bearer tokens and establishes ClaimsPrincipal
6. UseAuthorization()        -> Evaluates role and policy authorization attributes
7. MapControllers() / Hubs   -> Executes the target action method or SignalR hub
```

#### Why Middleware Order Matters
- **Exception Handler must be first**: Middleware placed before the exception handler will not have its exceptions caught. By wrapping the pipeline at the very top, any error thrown downstream is intercepted and formatted into a standard RFC 7807 `ProblemDetails` response.
- **Routing must precede CORS, Auth, and Endpoints**: Authentication and authorization require endpoint metadata (like `[Authorize]`) established by `UseRouting()`.
- **Authentication must precede Authorization**: The system must establish identity before evaluating access permissions. Placing authorization first would cause all protected requests to fail with 401 or 403.
- **CORS must precede Authorization**: Browsers send unauthenticated `OPTIONS` preflight requests; if authorization ran before CORS, preflight requests would be rejected before the browser could evaluate CORS headers.

---

### Part 7: Data Access Technologies Comparison

| Approach | Performance | Code Size & Boilerplate | Maintainability | When to Choose |
| --- | --- | --- | --- | --- |
| **EF Core LINQ** | Moderate (query translation + change tracking overhead) | Minimal (clean LINQ expressions and entity navigation) | High (strongly typed, compile-time checked, auto-migrations) | Standard CRUD, complex domain entity workflows, transactional operations |
| **Dapper** | Very High (direct SQL execution, fast micro-ORM mapping) | Low (SQL strings + straightforward mapping) | High (raw SQL flexibility, easy to profile and index) | High-throughput reporting, analytical queries, aggregated dashboards |
| **Stored Procedures** | Maximum (pre-compiled execution plans on SQL Server) | Low in C# / Moderate in SQL (requires database scripts) | Moderate (schema changes require updating DB procedures and C# models) | Critical analytics, complex multi-table aggregations, DBA-managed enterprise environments |
| **Raw ADO.NET** | Maximum (zero abstraction, direct streaming via `SqlDataReader`) | High (manual connection lifecycle, command creation, index reading) | Low (verbose boilerplate, manual type conversion) | High-volume streaming, memory-constrained environments, bulk data export |

---

### Part 8: Localization & Globalization

#### Localization vs Globalization
- **Globalization**: Designing and developing an application to support various cultures, languages, numeric formats, and date conventions without engineering modifications.
- **Localization**: Adapting an already-globalized application to a specific culture or locale by translating user interface text and providing region-specific content.

#### CultureInfo vs UICulture
- **CurrentCulture (`CultureInfo`)**: Controls locale-specific formatting rules, such as date formats (`dd/MM/yyyy` vs `MM/dd/yyyy`), currency symbols, decimal separators, and calendar calculations.
- **CurrentUICulture**: Controls resource manager lookups for localized strings (`.resx` files), determining which language resource is loaded for the user interface.

---

### Part 9: Real-Time SignalR, MessagePack & Notification Pipeline

#### SignalR Hub Architecture
- The `TicketChatHub` is protected with JWT authentication. Because WebSocket connections cannot supply custom HTTP headers, the client passes the token as a query string parameter (`?access_token=...`), parsed during `OnMessageReceived` in `Program.cs`.
- Active tickets are isolated into SignalR groups (`ticket-{id}`).
- Events broadcast across the real-time hub:
  - `ReceiveMessage`: Sends real-time chat messages to all participants in the ticket group.
  - `UserTyping`: Broadcasts typing indicators to active participants.
  - `TicketStatusChanged`: Notifies connected clients when a ticket moves to `Resolved` or `Closed`.
  - `TicketAssigned`: Notifies department agents when a new ticket is assigned or available in their queue.

#### MessagePack Serialization & Performance
- The SignalR hub supports MessagePack binary protocol via `.AddMessagePackProtocol()`.
- `ChatMessageDto` is annotated with `[MessagePackObject]` and indexed `[Key(n)]` properties.
- **Benchmark** (100 chat messages):
  - JSON payload size: ~23,077 bytes
  - MessagePack binary size: ~14,095 bytes
  - **Payload reduction: ~38.9% smaller** with MessagePack.
  - With Brotli compression on large batches: further reduces payload by over 90%.

#### Notification API Pipeline
HelpDesk Live integrates with an external Notification Minimal API:
1. When a ticket is closed, `CloseTicketCommandHandler` / `CloseAgentTicketCommandHandler` invokes `ITicketNotificationService`.
2. The service serializes a `TicketClosedNotificationDto` into binary MessagePack bytes using `MessagePackSerializer.Serialize()`.
3. The binary payload is sent via `POST` (`application/x-msgpack`) to `HelpDeskLive.NotificationAPI`.
4. The Notification API deserializes the binary message into a strongly-typed object and returns it as JSON.
```
SignalR -> MessagePack -> Binary Data -> Other API -> MessagePack Deserialization -> Object -> JSON
```

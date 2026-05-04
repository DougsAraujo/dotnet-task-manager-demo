# GenAI usage — Task Manager API

## Prompt used (example)

> You are a senior .NET engineer. Scaffold an ASP.NET Core 8 **Web API** for a **task management** app with Clean Architecture (Domain, Application, Infrastructure, Api).  
> **Constraints:** do **not** use Entity Framework, Dapper, or MediatR. Use **ADO.NET** (e.g. Microsoft.Data.Sqlite) for persistence.  
> **Domain:** `User` (Id, Email, PasswordHash, DisplayName), `TaskItem` (Id, UserId, Title, Description, Status enum Pending/InProgress/Completed, DueDate nullable).  
> **Features:**  
> - CRUD REST endpoints for tasks scoped to the authenticated user.  
> - Auth: register, login, JWT bearer; one public endpoint (e.g. GET `/api/info`).  
> - Application layer: validation and business rules independent of SQL and controllers.  
> - Infrastructure: schema creation (CREATE TABLE IF NOT EXISTS), repositories.  
> - Tests: xUnit for application services (mocks) and at least one API integration test.  
> Output: project layout, key interfaces, sample `Program.cs` wiring, and security notes (password hashing, JWT key length).

## Representative sample

The repository implements the above manually (not pasted from a model verbatim). A typical AI scaffold might include controllers and `DbContext`-style patterns — those were **rejected** because they violate the “no EF/Dapper” rule.

## How AI output was validated

1. **Requirement fit:** confirm no banned packages; persistence is parameterized SQL via `SqliteCommand`.  
2. **Security:** passwords hashed; JWT signing key length enforced; tasks always filtered by `UserId` from the token.  
3. **Auth edge cases:** wrong password, duplicate email, missing/invalid token → 400/401/409 as appropriate.  
4. **Integration tests:** end-to-end register → login → CRUD against a temporary SQLite file and in-memory JWT configuration.  
5. **Corrections made:** e.g. JWT validation parameters bound via `IOptions<JwtOptions>` so **test configuration overrides** apply after `WebApplicationFactory` merges settings; HTTPS redirection disabled in `Testing` to avoid dropping `Authorization` on redirect.

## If the model suggested EF or MediatR

Replace with explicit application services + ADO.NET repositories, and document the trade-off: more boilerplate, but full control over SQL and no hidden query patterns.

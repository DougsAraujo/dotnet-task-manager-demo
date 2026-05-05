# Task Manager

Full-stack **task management** sample: **ASP.NET Core 8 Web API** (Clean Architecture, **ADO.NET** + SQLite, **JWT** authentication) and an **Angular 19** single-page application.

This repository is structured so reviewers can **clone, run, and test** the solution locally or with **Docker**, and cross-check it against the interview brief (backend, frontend, tests, GenAI documentation).

---

## Project overview (assignment scope)

The exercise asks for a simple web application with a **.NET** API and data layer, **Clean Architecture**, awareness of **TDD**, an **informal user story**, **CRUD** over persisted data, **user registration and login** with users stored in the data store, and **no Entity Framework, Dapper, or MediatR**. A **frontend** must consume the API (responsive, CRUD for the same use case, organized code). **README** and **demo credentials / seeded data** are required. A **Generative AI** section documents the prompt, representative output, and how AI suggestions were validated.

**Stack choice:** **ASP.NET Core Web API + Angular** (instead of server-rendered ASP.NET MVC). The API remains the contract-first layer; the SPA is the “web application” UI. If your brief explicitly requires Razor MVC, call that out in review — here the focus is **Web API + SPA**, which is a common modern interpretation of “web application + API.”

---

## What is implemented (quick checklist)

| Area | Implementation |
|------|----------------|
| **Persistence** | SQLite: `Users` and `TaskItems` tables with primary keys and multiple columns (see `TaskManager.Infrastructure` migrator). |
| **CRUD API** | `/api/tasks` — list, get by id, create, update, delete; correct HTTP verbs and problem-details style errors where applicable. |
| **Auth API** | `POST /api/auth/register`, `POST /api/auth/login`; JWT protects task routes; `GET /api/info` is **public**. |
| **Data layer** | `IUserRepository`, `ITaskRepository` + ADO.NET implementations (`SqliteCommand`, parameterized SQL). |
| **Business logic** | `TaskManager.Application` — validation (`TaskValidation`, `UserValidation`), `AuthService`, `TaskItemService`; **no** reference to SQL or HTTP. |
| **Banned stack** | No EF Core, Dapper, or MediatR. |
| **Frontend** | Angular: login/register, task list, create/edit, delete; responsive layout; feature-based folders and core services. |
| **Demo data** | Seeded demo user from configuration (`DemoUser` in `appsettings.json` / Docker environment). |
| **Tests** | xUnit: application unit tests (mocked repositories), infrastructure tests (real SQLite file), API **integration** tests (`WebApplicationFactory`). |
| **GenAI write-up** | [`docs/genai-prompt.md`](docs/genai-prompt.md) — prompt, sample / rejection rationale, validation and edge cases. |

**Testing note:** The brief asks for unit tests including **API endpoints**. Here, **HTTP endpoints are covered by integration tests** (full pipeline, auth, and persistence). Application and infrastructure layers have **focused unit-style tests**. Strict **test-first TDD** across every commit is **not** claimed; tests are used to lock behaviour and regression-safety.

---

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js 20+](https://nodejs.org/) and npm  
  This repo pins public registries where relevant (`nuget.config`, `client/task-manager-web/.npmrc`) in case your machine defaults to private feeds.

---

## Repository layout (high level)

| Path | Role |
|------|------|
| `src/TaskManager.Domain` | Entities, enums |
| `src/TaskManager.Application` | Services, validation, DTOs/contracts, abstractions |
| `src/TaskManager.Infrastructure` | SQLite migration, ADO.NET repositories, JWT/password helpers |
| `src/TaskManager.Api` | Controllers, middleware, `Program.cs` |
| `client/task-manager-web` | Angular SPA |
| `tests/*` | xUnit test projects |
| `docs/genai-prompt.md` | GenAI assignment answers |
| `docker/` | Dockerfiles + nginx config for the SPA |

---

## Run the backend (API)

```bash
dotnet restore TaskManager.slnx
dotnet run --project src/TaskManager.Api/TaskManager.Api.csproj --launch-profile https
```

- **Swagger UI:** `https://localhost:7282/swagger` (host/port from `Properties/launchSettings.json`)
- **Database file:** `taskmanager.db` in the API working directory (created on first run; schema migrated at startup)
- **Demo user** (`appsettings.json` → `DemoUser`):

  | Field | Value |
  |--------|--------|
  | Email | `demo@taskmanager.local` |
  | Password | `Demo123!` |

**JWT:** configure the `Jwt` section; `SigningKey` must be **at least 32 characters** for local development.

### API summary

| Area | Method | Route | Auth |
|------|--------|-------|------|
| Public | GET | `/api/info` | No |
| Auth | POST | `/api/auth/register` | No |
| Auth | POST | `/api/auth/login` | No |
| Tasks | GET / POST | `/api/tasks` | Bearer JWT |
| Tasks | GET / PUT / DELETE | `/api/tasks/{id}` | Bearer JWT |

---

## Run the frontend (Angular)

```bash
cd client/task-manager-web
npm install
npm start
```

- **Dev server:** `http://localhost:4200`
- **API base URL:** `src/environments/environment.ts` (default `https://localhost:7282/api`). Adjust if your API port or profile differs.

Trust the **ASP.NET development HTTPS certificate** so the browser can call the API from the Angular dev origin.

### Internationalization (optional UX)

- Catalogs: `client/task-manager-web/public/i18n/en.json`, `pt.json` (loaded from `/i18n/{lang}.json`).
- Language toggle on sign-in / register and in the shell header for guests; preference stored as `tm_lang` in `localStorage`.

---

## Run with Docker (API + SPA)

Requires [Docker Engine](https://docs.docker.com/engine/install/) with **Compose v2**.

```bash
docker compose up --build
```

| Service | URL / notes |
|---------|-------------|
| **Web (nginx + Angular)** | [http://localhost:8080](http://localhost:8080) — `/api` reverse-proxied to the API container |
| **API (Swagger)** | [http://localhost:5011/swagger](http://localhost:5011/swagger) — `Swagger__Enabled=true` in Compose while the host environment remains `Production` |
| **SQLite** | Named volume `sqlite-data` → `/data/taskmanager.db` inside the API container |

The SPA production build uses `apiUrl: '/api'` so the browser calls same-origin; nginx forwards to the `api` service.

**Important:** after code changes, use `docker compose up --build` (or rebuild the `web` image). `docker compose up --force-recreate` alone **does not** rebuild images.

Override `Jwt__SigningKey` (and other secrets) for anything beyond local demos.

---

## Tests and coverage

```bash
dotnet test TaskManager.slnx
```

- **TaskManager.Application.Tests** — validation and application services with **Moq** repositories  
- **TaskManager.Infrastructure.Tests** — SQLite repositories against a temporary database file  
- **TaskManager.Api.Tests** — end-to-end HTTP flows (register → login → CRUD, auth failures, isolation between users). The `Testing` host environment **disables HTTPS redirection** so the `Authorization` header is not lost on redirect.

**Coverage (Coverlet):**

```bash
dotnet test TaskManager.slnx -p:CollectCoverage=true
```

Cobertura/JSON output is written under `TestResults/coverage/<TestProject>/` (ignored by git). For HTML reports, pipe OpenCover/Cobertura into [ReportGenerator](https://github.com/danielpalme/ReportGenerator) or your CI viewer of choice.

---

## Clean Architecture (short)

- **Domain** — `User`, `TaskItem`, `TaskItemStatus`; no infrastructure references.  
- **Application** — use cases, validation, contracts; depends only on Domain + abstractions.  
- **Infrastructure** — SQLite, ADO.NET, password hashing, JWT token creation, implementations of repository interfaces.  
- **Api** — thin controllers, JWT bearer authentication, CORS for `http://localhost:4200`, global exception → RFC 7807-style JSON (`application/problem+json`), startup migration + optional demo seed.

---

## Generative AI (assignment deliverable)

All required narrative — **prompt**, **representative sample / what was rejected**, **validation**, **corrections**, **auth and edge cases** — lives in:

**[`docs/genai-prompt.md`](docs/genai-prompt.md)**

## License

Sample code for **interview / evaluation purposes** only.

# Germany Applications Platform

Initial monorepo scaffold for a self-service platform that helps Sri Lankan students discover verified German Master's programmes and organise applications they submit themselves.

This scaffold contains infrastructure and health verification only. It does not contain university/course data or product functionality. Read `AGENTS.md` and the planning documents in `docs/` before contributing.

## Repository layout

```text
frontend/        React, TypeScript, Vite, Tailwind CSS
backend/         ASP.NET Core 10 Web API and xUnit tests
docs/            product, architecture, scope, security, and delivery plans
infrastructure/  Docker Compose local dependencies
```

## Prerequisites

- Node.js 24 LTS-compatible runtime and npm
- .NET 10 SDK
- Docker Engine with Docker Compose v2 (or a compatible container runtime)

## Environment setup

Only examples are committed. Never commit populated `.env` files or real credentials.

```bash
cp infrastructure/.env.example infrastructure/.env
cp backend/.env.example backend/.env
cp frontend/.env.example frontend/.env
```

Change both example database password placeholders to the same local-only value. ASP.NET Core does not automatically load `.env`; either export the backend values in your shell or use your IDE/container environment. Development defaults in `appsettings.Development.json` are local-only and must never be used outside a developer workstation.

## Start PostgreSQL

```bash
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml up -d
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml ps
```

Stop it without deleting data:

```bash
docker compose --env-file infrastructure/.env -f infrastructure/compose.yaml down
```

Add `--volumes` only when intentionally deleting the local database.

## Run the backend

```bash
dotnet restore backend/GermanyApplications.slnx
dotnet build backend/GermanyApplications.slnx --no-restore
dotnet run --project backend/src/GermanyApplications.Api
```

The development API listens on `http://localhost:5080`. Swagger UI is available at `http://localhost:5080/swagger` only in Development.

Health endpoints:

- `GET http://localhost:5080/health/live` verifies the API process.
- `GET http://localhost:5080/health/ready` verifies PostgreSQL connectivity.

## Run the frontend

```bash
cd frontend
npm ci
npm run dev
```

Open `http://localhost:5173`. The landing page calls the backend liveness endpoint and reports whether the API is reachable.

## Build, format, lint, and test

Backend:

```bash
dotnet format backend/GermanyApplications.slnx --verify-no-changes
dotnet build backend/GermanyApplications.slnx
dotnet test backend/GermanyApplications.slnx --no-build
```

Frontend:

```bash
cd frontend
npm run format
npm run lint
npm run typecheck
npm run test:run
npm run build
```

## Local CORS and secrets

The API accepts browser requests only from origins explicitly listed in `Cors:AllowedOrigins`; the development configuration permits `http://localhost:5173`. It does not use wildcard origins or permit credentials by default. Configure deployed origins using environment-specific configuration.

Vite variables are public browser configuration. Never put API keys, passwords, tokens, connection strings, or other secrets in `VITE_*` variables. Backend secrets belong in environment-specific secret storage.

## Database migrations

No initial application migration exists because no product entities have been implemented. Once migrations are introduced, create forward migrations from the designated project, review generated SQL/model snapshots, and test against PostgreSQL. Never edit or delete an existing shared migration without an explicit explanation and approval as required by `AGENTS.md`.

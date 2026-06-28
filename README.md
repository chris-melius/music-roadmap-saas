# MusicRoadmap SaaS Portal

![CI/CD Build Status](https://github.com)
[![License: MIT](https://shields.io)](https://opensource.org)
[![.NET Core](https://shields.io)](https://microsoft.com)

An enterprise-grade, cloud-native B2B multi-tenant SaaS application built for music studios and independent music educators to manage students and dynamically generate customized curriculum roadmaps. Architected with strict data isolation boundaries, robust defense-in-depth security gates, and an automated cloud-delivery container pipeline.

## 🏗️ Core Architecture & Design Patterns

*   **Fail-Closed Multi-Tenancy:** Implements an unconditional, global query filter at the entity framework core database level (`AppDbContext`). This restricts all data operations strictly to the active `InstructorId` tenant extracted from the HTTP claims pipeline, eliminating cross-tenant data leaks.
*   **Cryptographic Asymmetry (Hashed Sessions):** Access tokens are stateless, short-lived 15-minute JWT structures to minimize blast radius exposure. Refresh tokens are recorded inside SQL Server exclusively as **SHA-256 cryptographic hashes**, neutralizing active session hijackings even in the event of a full database compromise.
*   **Atomic Idempotent Ingestion:** The client-side form submissions pass a unique `X-Ingestion-Id` network header. The API wraps ingestion validation logs and business entity initialization routines inside an **EF Core Database Transaction**, ensuring an all-or-nothing rollback to protect data integrity during transient container crashes.
*   **Enterprise Startup Sanity Guards:** Implements a strict fail-fast check at initialization time. If the hosting environment boots up under a `Production` context but detects hardcoded dev credentials or connection placeholders, it immediately executes an exit crash (`Environment.Exit(1)`) before opening any TCP ports.
*   **Resilient Database Bootstrapper:** Database migrations are automated on container startup via a resilient, exponential backoff retry loop. This completely mitigates cloud orchestration/Docker network race conditions where the API boots faster than the underlying database cluster can initialize.
*   **Administrative Security Perimeter:** The registration gateway implements a strict, server-side firewall whitelist restricted entirely to administrative IP address footprints mapped via proxy forwarded headers.

---

## 🛠️ Tech Stack & Ecosystem

*   **Backend:** ASP.NET Core Web API (.NET 10), Entity Framework Core, ASP.NET Core Identity
*   **Frontend:** Blazor WebAssembly (.NET 10), Bootstrap CSS (Custom Slate Theme)
*   **Database:** Microsoft SQL Server 2022
*   **Containerization & Proxy:** Docker, Docker Compose, Nginx
*   **Cloud Infrastructure:** Azure Container Apps (ACA), Azure SQL Serverless, Azure Container Registry (ACR)
*   **Testing & CI/CD:** xUnit, WebApplicationFactory Integration Tests, GitHub Actions Pipelines

---

## 🚀 Local Development Quickstart

The entire full-stack environment is fully containerized. You do not need SQL Server or local hosting utilities installed on your machine to test the workspace.

### Prerequisites
*   Docker Desktop installed and active.
*   .NET 10 SDK (only if running native hot-reload tests).

### 1. Step Setup Configurations
Clone the project and create your un-tracked local override settings file right next to the base `docker-compose.yml`:
```powershell
cp docker-compose.override.example.yml docker-compose.override.yml
```

### 2. Launch the Development Stack
Execute the following single-step terminal instruction from your root directory:
```powershell
docker-compose up --build
```
Once the containers synchronize, the infrastructure boots up automatically:
*   **Blazor WebAssembly App:** `http://localhost:5001`
*   **ASP.NET Core Web API Engine:** `http://localhost:5213`
*   **SQL Server Engine Local Instance:** `localhost,1433`

---

## ⚡ High-Velocity Frontend Styling (Hot-Reload)

To tweak CSS or adjust Blazor views without suffering through a full Docker container compilation loop, you can execute the frontend UI natively on your laptop machine against the background Docker data layer:

1. Comment out the `music-client` container block from your base `docker-compose.yml`.
2. Boot just the database and backend API nodes: `docker-compose up --build`.
3. Open a separate terminal, navigate to the client folder, and activate Microsoft's hot-reload engine:
```powershell
cd MusicRoadmap.Client
dotnet watch run
```
Any modifications saved to your razor views or custom stylesheets will inject directly into your active browser frame in under **0.5 seconds**.

---

## 🛢️ Secure Runtime Environment Variables

The system enforces complete credential separation. Production secrets are never committed to source files. The application expects the following configuration mapping schema at runtime:

| Variable Context Key | Target Layer | Functional Intent / Description |
| :--- | :--- | :--- |
| `ASPNETCORE_ENVIRONMENT` | Core Host API | Set to `Development` or `Production` to govern fail-fast behaviors. |
| `ConnectionStrings__DefaultConnection` | Infrastructure DB | Direct network path target routing to the active SQL Server engine catalog. |
| `Jwt__Key` | Token Engine | Cryptographic signing key (minimum 32 characters long) used to lock JWT headers. |
| `Jwt__Issuer` | Token Engine | The trusted security token service domain URI address generating payloads. |
| `Jwt__Audience` | Token Engine | The targeted system component domain URI authorized to consume tokens. |
| `AdminSettings__AllowedIp` | Gateway Protection | Strict explicit IPv4 address allowed to register new instructor accounts. |
| `CorsSettings__AllowedOrigins__0` | Browser Networking | White-listed cross-origin domains permitted to pass secure credential headers. |
| `OpenAI__ApiKey` | Semantic Kernel | Secure platform token allocated to drive automated roadmap creations. |

---

## 🏛️ Repository Hygiene & Clean-up

To run automated architectural health and testing checks across your machine natively, invoke the standard testing matrix CLI:
```powershell
dotnet test MusicRoadmap.IntegrationTests/
```
All production builds are validated remotely via GitHub Actions before container image pushes are committed to the Azure registry ecosystem, maintaining an absolute green-check operational standard.

## 📄 License
This project is open-source software licensed under the terms of the [MIT License](LICENSE).
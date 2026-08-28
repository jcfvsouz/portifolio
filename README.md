# Campaign Publication & Batch Email Dispatch

> Portfolio project: an event-driven campaign publishing pipeline in .NET 10 — Clean Architecture, RabbitMQ, Docker & Kubernetes, with local NuGet packaging and xUnit tests.

A small, self-contained system that models a real problem from CRM/marketing platforms: a marketer publishes a **campaign** targeted at a **buyer group**, and the platform reacts asynchronously — dispatching a batch of e-mails to every buyer in that group — without making the publish request wait on the sending itself.

This project exists as a portfolio piece to demonstrate, end to end and with working code: Clean Architecture in .NET, an event-driven pipeline (API → message broker → worker), containerization with Docker, and orchestration with Kubernetes. It intentionally mirrors an architecture pattern from my own production experience (a CRM campaign-segmentation system), rebuilt from scratch with synthetic data — no proprietary code or business data involved.

## Why this project

Most portfolio repositories show one thing well — a CRUD API, a UI component, an algorithm exercise. This one is deliberately broader: a single, cohesive scenario used as scaffolding to demonstrate the stacks I work with — Clean Architecture in .NET, SQL Server via Dapper, asynchronous messaging, containerization, and cluster orchestration — inside one small codebase, instead of spreading that evidence across several disconnected toy repos. Every piece here is intentionally small; the point is showing how the pieces fit together end to end, not depth in any single one.

## Architecture

```
                    POST /campaigns/{id}/publish
                              │
                              ▼
                    ┌───────────────────┐
                    │   Campaign API    │   ASP.NET Core, Clean Architecture
                    │  (Promo.API)      │   SQL Server via Dapper
                    └─────────┬─────────┘
                              │ publishes "CampaignPublished" event
                              ▼
                    ┌───────────────────┐
                    │     RabbitMQ      │   message broker
                    └─────────┬─────────┘
                              │ consumed by
                              ▼
                    ┌───────────────────┐
                    │  Campaign Worker   │   background service
                    │  (Promo.Worker)    │   fetches target buyers, sends batch e-mail
                    └─────────┬─────────┘
                              │ SMTP
                              ▼
                    ┌───────────────────┐
                    │      Mailhog       │   fake SMTP server + web UI —
                    │                    │   lets you *see* every e-mail the
                    └───────────────────┘   Worker sent, with no real inbox involved
```

Everything above the "SMTP" arrow runs as its own container; `docker-compose` wires the five of them together, and the Kubernetes manifests do the same job for a cluster (`kind`/`minikube` locally).

## Repository layout

```
portfolio-campaign-pipeline/
├── components/     # Shared class libraries, packed as local NuGet packages — see components/README.md
├── api/             # Promo.API — Clean Architecture, publishes CampaignPublished          [planned]
├── worker/           # Promo.Worker — consumes the queue, sends batch e-mail via Mailhog     [planned]
├── docker/           # docker-compose.yml wiring API + Worker + RabbitMQ + SQL Server + Mailhog [planned]
└── k8s/              # Deployment/Service manifests for a local cluster                       [planned]
```

## Status

| Piece | Status |
|---|---|
| `components/` — Result, FluentResult extensions, SQL Server repository | ✅ done |
| `components/` — xUnit tests (builders + fixtures) | ✅ done |
| `components/` — sample console apps per package | ✅ done |
| `components/` — RabbitMQ messaging abstraction | ⏳ next |
| `api/` — Campaign domain, publish endpoint | ⏳ planned |
| `worker/` — queue consumer, batch e-mail dispatch | ⏳ planned |
| `docker/` — compose file for all 5 containers | ⏳ planned |
| `k8s/` — manifests for a local cluster | ⏳ planned |

## Tech stack

- **.NET 10**, C#, ASP.NET Core (Minimal APIs / Controllers)
- **SQL Server** (via Dapper), packaged behind an engine-agnostic repository interface
- **RabbitMQ** for asynchronous messaging
- **Mailhog** as a safe, local stand-in for a real SMTP provider
- **Docker** / **docker-compose** for local multi-container orchestration
- **Kubernetes** for cluster orchestration
- **NuGet**, packaged and served from a local folder feed (no nuget.org publish involved)

## Related experience

This project's shape — segment an audience, publish a campaign, process the fan-out asynchronously — is a smaller, public rebuild of production work I led as Tech Lead on a CRM team (campaign segmentation architecture migration, incremental delivery of a personalized-campaigns product). Details of that real-world work are in my résumé and LinkedIn, not in this repository.

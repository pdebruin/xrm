# XRM — Extensible Relationship Management

![XRM Screenshot](docs/Screenshot%202026-04-21%20091241.png)

## Why

"SaaS is dead" — or at least, AI lets us generate software faster than ever.
XRM is an experiment to test that claim: can we build a usable, self-hosted
platform from requirements to working app in a single AI-assisted session?

The result is a **meta-CRM** — a platform where you define your own entities,
fields, and relationships, then manage records through a generated UI. It could
be a CRM, a sports team tracker, an inventory system, or anything else.

## How

This project was built almost entirely through conversation with GitHub Copilot
(Claude Opus). The process:

1. **Requirements gathering** — interactive Q&A to define scope, tech stack,
   and design decisions, captured in [requirements.md](requirements.md)
2. **MVP implementation** — scaffolded .NET 10 Blazor Server app with EF Core
   and SQLite in a single pass
3. **Iterative refinement** — field/relationship designers, service layer
   refactor, demo data, child record grids, sorting, parent navigation
4. **Peer review** — AI-driven code review identifying 13 issues across
   critical/important/minor severity; critical and top important items fixed
5. **Testing** — manual [test plan](tests.md) mapped to requirements, plus
   40 automated tests (unit + API integration)
6. **Documentation** — user guide, technical docs, this README

Each step was a conversation turn. The AI wrote the code, ran builds and tests,
and committed. Human input steered decisions, caught UX issues, and set priorities.

## What

### Artifacts

| File | Description |
|------|-------------|
| [requirements.md](requirements.md) | Full requirements, decisions, color palette |
| [docs/guide.md](docs/guide.md) | User guide with screenshots |
| [docs/architecture.md](docs/architecture.md) | Architecture decisions and technical design |
| [tests.md](tests.md) | Manual test validation document |
| [LICENSE](LICENSE) | MIT License |

### Project Structure

```
src/Xrm.Server/          → .NET 10 Blazor Server app
  Models/                 → EF Core domain models
  Data/                   → DbContext + demo data seeder
  Services/               → Business logic (entity, field, relationship, record)
  Controllers/            → REST API (thin wrappers over services)
  Components/Pages/       → Blazor UI (record grid, detail, schema designer)
tests/Xrm.Tests/         → xUnit tests (unit + API integration)
docs/                     → User guide + screenshots
```

### Quick Start

```bash
cd src/Xrm.Server
dotnet run
```

Open http://localhost:5186 — demo data loads automatically on first run.

## License

[MIT](LICENSE)

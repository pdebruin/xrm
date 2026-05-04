# XRM — Extensible Relationship Management

![XRM Screenshot](docs/Screenshot%202026-04-21%20091241.png)

## What

XRM is a **meta-CRM** — a platform where you define your own entities, fields, and
relationships at runtime, then manage records through a generated UI and REST API.
Think "web-based MS Access" — you design your tables and relationships, then the UI
is generated for you. The difference is it's a real web app with a proper API.

It could be a CRM, a project tracker, an inventory system, or anything else.

## Quick Start

```bash
cd src/Xrm.Server
dotnet run
```

Open http://localhost:5186 — demo data loads automatically on first run.

## Documentation

| Document | Audience | Description |
|----------|----------|-------------|
| [User Guide](docs/guide.md) | End users | How to use the app — schema design, record management, navigation |
| [Architecture & Implementation](docs/architecture.md) | Developers | Technical design, reuse pattern, and links to all feature docs |
| [Project Evolution](docs/evolution.md) | Everyone | How a 3-hour hack became a production framework |
| [Backlog](docs/backlog.md) | Contributors | Planned features and improvements |

## Technology

.NET 10 · Blazor Server · EF Core · SQLite · xUnit · GitHub Actions

## License

[MIT](LICENSE)

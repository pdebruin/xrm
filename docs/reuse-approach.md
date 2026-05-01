# Reuse Approach — XRM Framework & Domain Implementations

## Problem Statement

XRM is a generic, extensible relationship management framework. Specific
implementations (e.g., the ERP for Dutch housing corporations) need to add
domain-specific schema seeds, branding, and features — but currently this is done
by **copying the entire `src/Xrm.Server`** folder. This creates diverging
versions that are painful to maintain and miss upstream bug fixes or features.

We need a layered approach where:

1. XRM remains a reusable framework/platform.
2. Domain implementations only define their own additions (schema, seed data,
   UI customization, domain logic) without forking the core.

---

## Current State

| Repo | Contents |
|------|----------|
| `xrm` | Generic framework: EAV models, services, Blazor UI, REST API |
| `erp` | Full copy of `Xrm.Server` + housing corp seed data in `DemoDataSeeder.cs` |

The only meaningful difference in `erp` today is the domain seed — but because
the entire project is copied, any fix in `xrm` must be manually re-applied.

---

## Options

### Option A: NuGet Package (Class Library)

Extract the framework into one or more NuGet packages that domain projects
reference.

**Structure:**

```
xrm/
  src/
    Xrm.Core/           ← Models, Services, DbContext (class library → NuGet)
    Xrm.Blazor/         ← Razor components, layouts (RCL → NuGet)
    Xrm.Server/         ← Host app (reference project, demo/dev)

erp/
  src/
    Erp.Server/          ← ASP.NET host, references Xrm.Core + Xrm.Blazor via NuGet
      DomainSeeder.cs    ← Housing corp entities/fields/relationships
      Program.cs         ← Composes services, adds custom middleware
```

**Pros:**

- Clean separation; framework is versioned and published.
- Domain projects are small — only custom code.
- Standard .NET pattern; familiar to any C# developer.
- Framework can be independently tested, released, documented.
- Multiple domain implementations can target different framework versions.

**Cons:**

- Requires NuGet infrastructure (feed — GitHub Packages, Azure Artifacts, or
  local folder).
- Breaking changes in the framework require a new package version + consumer
  update.
- More upfront work to split the current monolith into library + host.
- Blazor components must be in a Razor Class Library (RCL), requiring some
  restructuring of static assets and routing.

---

### Option B: Git Submodule / Subtree

Include `xrm` as a Git submodule (or subtree) in the domain repo.

**Structure:**

```
erp/
  xrm/                  ← git submodule pointing at pdebruin/xrm
  src/
    Erp.Server/          ← Host app, project-references xrm/src/Xrm.Server
      DomainSeeder.cs
      Program.cs
```

**Pros:**

- Least restructuring of the framework project itself.
- Always builds from source — no publish step needed.
- Easy to track which framework commit a domain project is pinned to.

**Cons:**

- Git submodules are notoriously awkward (clone --recurse, update, detached HEAD).
- Tight coupling: domain project references framework internals directly.
- Harder to have a clean public API boundary.
- CI/CD needs extra steps to fetch submodules.
- Contributors unfamiliar with submodules will hit friction.

---

### Option C: Project Reference (Mono-repo or Multi-repo with Shared Path)

Keep both repos separate but use a relative project reference during development,
publishing as NuGet for release/CI.

**Structure (development):**

```
_projects/
  xrm/
    src/Xrm.Core/
    src/Xrm.Blazor/
    src/Xrm.Server/          ← standalone demo host
  erp/
    src/Erp.Server/
      Erp.Server.csproj      ← <ProjectReference Include="../../xrm/src/Xrm.Core/..." />
```

**Pros:**

- Fastest inner-loop development: change framework + domain in one IDE session.
- No NuGet publish overhead during development.
- Can still publish packages for CI/production.

**Cons:**

- Relies on a specific folder layout on disk.
- CI must either clone both repos into the right structure, or fall back to NuGet.
- Conditional project/package references add msbuild complexity.
- Others contributing to the domain repo need the framework checked out locally.

---

### Option D: Template / Scaffold (dotnet new)

Ship XRM as a `dotnet new` template. Running `dotnet new xrm-domain` scaffolds a
new project pre-wired to the framework packages.

**Pros:**

- Great first-run experience for new domain projects.
- Combines well with Option A (template references NuGet packages).

**Cons:**

- Not a reuse mechanism by itself — it's scaffolding, not ongoing linkage.
- Must be combined with another option (A or C) for continued framework updates.

---

## Comparison Matrix

| Criterion                        | A: NuGet | B: Submodule | C: ProjectRef | D: Template |
|----------------------------------|:--------:|:------------:|:-------------:|:-----------:|
| Clean API boundary               | ✅       | ❌           | ✅            | ✅          |
| Simple contributor experience    | ✅       | ❌           | ⚠️            | ✅          |
| Fast inner-loop development      | ⚠️       | ✅           | ✅            | N/A         |
| Independent versioning           | ✅       | ⚠️           | ⚠️            | ✅          |
| CI/CD simplicity                 | ✅       | ❌           | ⚠️            | ✅          |
| Minimal framework restructuring  | ❌       | ✅           | ❌            | ❌          |
| Multiple domain projects         | ✅       | ⚠️           | ⚠️            | ✅          |

---

## Recommendation: Option A (NuGet) + C (ProjectRef for dev)

Combine **Option A** for the release/distribution model with **Option C** for
the local development inner loop. Optionally add **Option D** later for
onboarding new domain projects.

### Why?

- **NuGet** gives a clean boundary, proper versioning, and easy consumption in
  CI. It's the idiomatic .NET pattern.
- **Project references** during local development avoid the publish-restore
  cycle, enabling fast iteration across framework and domain simultaneously.
- The conditional reference pattern (`Condition="'$(UseProjectReferences)'=='true'"`)
  in csproj keeps both modes working cleanly.

### Implementation Steps

1. **Split `Xrm.Server` into layers:**
   - `Xrm.Core` — Models, DbContext, Services, interfaces.
   - `Xrm.Blazor` — Razor Class Library with all UI components.
   - `Xrm.Server` — Thin host that references Core + Blazor (remains as the
     demo/standalone app).

2. **Define extension points in the framework:**
   - `IDataSeeder` interface — domain projects register seeders via DI.
   - Optional: `IDomainModule` for registering extra services, middleware, etc.
   - Theme/branding configuration (app name, colors, favicon) via options
     pattern.

3. **Set up NuGet packaging:**
   - Add `<IsPackable>true</IsPackable>` to Core and Blazor projects.
   - Publish to GitHub Packages (free for the repo, private if needed).
   - Use `Directory.Build.props` for shared version/metadata.

4. **Create the domain project (`Erp.Server`):**
   - Small ASP.NET host with `PackageReference` to `Xrm.Core` / `Xrm.Blazor`.
   - Contains only: `Program.cs`, domain seeder, and any domain-specific
     extensions.
   - Conditional project reference for local dev:
     ```xml
     <ItemGroup Condition="'$(UseProjectReferences)' == 'true'">
       <ProjectReference Include="../../xrm/src/Xrm.Core/Xrm.Core.csproj" />
       <ProjectReference Include="../../xrm/src/Xrm.Blazor/Xrm.Blazor.csproj" />
     </ItemGroup>
     <ItemGroup Condition="'$(UseProjectReferences)' != 'true'">
       <PackageReference Include="Xrm.Core" Version="1.*" />
       <PackageReference Include="Xrm.Blazor" Version="1.*" />
     </ItemGroup>
     ```

5. **(Future) Create a `dotnet new` template** for bootstrapping new domain
   implementations quickly.

---

## Extension Point Design

The framework should allow domain projects to plug in without modifying
framework code:

| Extension Point | Mechanism | Example |
|----------------|-----------|---------|
| Domain schema seed | `IDataSeeder` registered via DI | Housing corp entities |
| Extra services | Standard DI registration in `Program.cs` | Domain validation |
| UI branding | `XrmOptions` (app name, theme colors) | "ERP Woningcorporatie" |
| Additional Blazor pages | RCL `_Imports.razor` + routing | Domain dashboards |
| Middleware | Standard ASP.NET pipeline in `Program.cs` | Custom auth |

---

## Decision

**→ Proceed with Option C (project references) as the immediate step.**

Restructure `xrm` into composable libraries (`Xrm.Core` + `Xrm.Blazor` RCL)
so domain projects can reference them directly. NuGet packaging (Option A) can
be added later when distribution or versioning becomes necessary — for now,
project references across sibling folders are sufficient.

---

## Implemented Structure

```
xrm/src/
├── Xrm.Core/                          ← Class library (.NET 10)
│   ├── Models/                         ← EntityDefinition, FieldDefinition, Record, etc.
│   ├── Data/
│   │   ├── XrmDbContext.cs             ← EF Core context (SQLite)
│   │   ├── IDataSeeder.cs             ← Extension point for domain seed data
│   │   └── DemoDataSeeder.cs          ← Default CRM demo (implements IDataSeeder)
│   ├── Services/                       ← IEntityService, IRecordService, etc. + implementations
│   └── XrmServiceExtensions.cs        ← AddXrmCore() + AddXrmSeeder<T>() extension methods
│
├── Xrm.Blazor/                        ← Razor Class Library (RCL)
│   ├── Components/
│   │   ├── Layout/                    ← MainLayout, NavMenu, ReconnectModal
│   │   ├── Pages/                     ← Home, RecordList, RecordDetail, Admin/*, Error, NotFound
│   │   ├── App.razor                  ← HTML shell with RCL asset paths
│   │   ├── Routes.razor               ← Parameterized router (AppAssembly + AdditionalAssemblies)
│   │   └── _Imports.razor
│   └── wwwroot/                       ← app.css, bootstrap, favicon
│
└── Xrm.Server/                        ← Thin host (demo/standalone)
    ├── Program.cs                     ← 20 lines: AddXrmCore() + AddXrmSeeder<DemoDataSeeder>()
    ├── Controllers/                   ← REST API wrappers
    └── appsettings.json
```

## How a Domain Project Consumes This

Example: `xrm-for-sales/src/Sales.Server/`

```xml
<!-- Sales.Server.csproj -->
<ProjectReference Include="../../../xrm/src/Xrm.Core/Xrm.Core.csproj" />
<ProjectReference Include="../../../xrm/src/Xrm.Blazor/Xrm.Blazor.csproj" />
```

```csharp
// Program.cs — entire XRM wired in 2 lines
builder.Services.AddXrmCore("Data Source=sales.db");
builder.Services.AddXrmSeeder<SalesDataSeeder>();
```

```csharp
// SalesDataSeeder.cs — implements IDataSeeder
public class SalesDataSeeder : IDataSeeder
{
    public async Task SeedAsync(XrmDbContext db) { /* domain entities */ }
}
```

```razor
<!-- Routes.razor — picks up pages from both Sales + XRM assemblies -->
<Router AppAssembly="typeof(Program).Assembly"
        AdditionalAssemblies="new[] { typeof(Xrm.Blazor.Components.App).Assembly }">
```

The domain project can then add its own pages (e.g., `/dashboard`) that coexist
with all standard XRM pages (record list, detail, admin).

---

## Key Design Decisions in the Split

1. **Routes.razor is parameterized** — the RCL exposes `AppAssembly` and
   `AdditionalAssemblies` parameters so hosts control routing.
2. **Static assets use `_content/Xrm.Blazor/` paths** — standard RCL convention;
   host App.razor references them with this prefix.
3. **IDataSeeder is the primary extension point** — domain projects register
   their seeder via `AddXrmSeeder<T>()`.
4. **AddXrmCore() encapsulates all DI** — one call registers DbContext factory
   + all services. Domain projects don't need to know internals.

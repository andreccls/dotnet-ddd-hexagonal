# dotnet-ddd-hexagonal (English summary)

A **reference / example project** for **Domain-Driven Design + Hexagonal Architecture (Ports & Adapters)** on **.NET 9**.
The full documentation is in Portuguese ([README.md](README.md), [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md), [docs/adr](docs/adr)).

## What you get

- Three CRUDs: **Customers** (e-mail and CPF value objects, activate/deactivate), **Products** (SKU, Money, non-negative stock)
  and **Orders** (aggregate with items, status `Pending → Confirmed → Cancelled`, computed total, stock reservation).
- **Layers as separate projects**: `Domain` (zero dependencies) ← `Application` (use cases + ports) ← `Infrastructure`
  (EF Core + MySQL adapters) and `Api` (Minimal APIs adapter + composition root).
- **Architecture tests** (NetArchTest) that fail the build when the dependency rule is broken.
- **100 % line and branch coverage** in Domain and Application, enforced as a gate by `make coverage`; integration and
  end-to-end tests run against a real MySQL.
- KISS/YAGNI by design: no MediatR, no AutoMapper, no CQRS, no generic repository (see the ADRs).

## Run it

```bash
make up          # MySQL 8.4 + API (Docker only) -> http://localhost:8080/swagger
make test        # build with warnings-as-errors + every test
make coverage    # coverage report in coverage/report, fails if Domain/Application < 100 %
make down
```

`make` uses your local `dotnet` if present, otherwise the official SDK container. Dev passwords in
`docker-compose.yml` / `.env.example` are **development-only**.

## Using it as a template

Follow "Como adicionar um novo CRUD" in [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md): domain aggregate (tests first) →
use cases + ports → EF configuration + repository + migration → endpoints → tests.

License: [MIT](LICENSE) © André Coura

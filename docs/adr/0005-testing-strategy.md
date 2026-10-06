# ADR 0005 — Estratégia de testes e metas de cobertura

- **Status:** aceito
- **Data:** 2026-10-06

## Contexto

O valor do núcleo (Domain + Application) está nas regras de negócio, então é onde a cobertura precisa ser total e
barata. Adaptadores precisam ser provados contra a tecnologia real. A arquitetura precisa ser protegida contra erosão.

## Decisão

1. **Domain.Tests** — xUnit puro, escrito em **TDD** (vermelho → verde). Meta: **100 % de linhas e branches, como gate**.
2. **Application.Tests** — casos de uso com **fakes em memória** escritos à mão (`InMemory*Repository`, `FakeUnitOfWork`,
   `FixedClock`) em vez de framework de mock: testes por estado, legíveis e sem dependência extra. Meta: **100 %, gate**.
3. **Infrastructure.Tests** — integração com **MySQL real** (mapeamentos, migrations, índices únicos, concorrência).
   Um banco descartável por classe de teste, criado pelas migrations reais.
4. **Api.Tests** — `WebApplicationFactory<Program>` + MySQL real, ponta a ponta nos 3 CRUDs (inclusive formato de erro).
5. **Architecture.Tests** — NetArchTest + reflexão: regra de dependência e convenções DDD/hexagonais.

**MySQL nos testes:** `docker compose` (serviço `mysql` com *healthcheck*) + `TEST_MYSQL_CONNECTION` por variável de
ambiente, em vez de Testcontainers. Motivo: funciona igual local, no container do SDK e no GitHub Actions (service
container) sem montar `docker.sock`.

**Cobertura:** Coverlet (`coverlet.msbuild`) com `Threshold=100` para `line` e `branch`, aplicado por camada
(cada camada precisa ser coberta pelos **seus próprios** testes, sem "emprestar" cobertura de outra), e ReportGenerator
para o relatório. Exclusões só com `[ExcludeFromCodeCoverage(Justification = "...")]` e listadas no README
(construtores sem parâmetros do EF, fábrica de design-time, migrations/código gerado).

## Consequências

- (+) Regressão de regra de negócio ou de arquitetura derruba o CI.
- (+) Fakes documentam o contrato das portas; trocar de ORM não toca os testes de Application.
- (−) 100 % de branches exige disciplina (cada `throw` tem um teste), por isso o gate só vale onde compensa:
  Domain e Application. Infrastructure e Api são medidas e reportadas, não travadas.
- (−) Testes de integração exigem MySQL no ar (`make up-db`, automático no `make test`).

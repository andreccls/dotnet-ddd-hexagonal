# ADR 0004 — Minimal APIs organizadas por feature

- **Status:** aceito
- **Data:** 2026-10-06

## Contexto

A camada de entrada só precisa traduzir HTTP em chamadas a casos de uso. Controllers MVC trazem filtros, *model binding*
extenso e uma hierarquia que este projeto não usa.

## Decisão

Usar **Minimal APIs** (.NET 9), uma classe estática por feature (`CustomerEndpoints`, `ProductEndpoints`,
`OrderEndpoints`) com `MapGroup`. Cada rota injeta `IUseCase<,>` e devolve `Results.*`.

- Erros: `IExceptionHandler` (`DomainExceptionHandler`) → `ProblemDetails` (400/404/409), 500 genérico sem detalhes.
- Documentação: OpenAPI nativo do .NET 9 (`AddOpenApi`/`MapOpenApi`) + Swagger UI; `Produces`/`ProducesProblem` em cada rota.
- Health check em `/health` (inclui o MySQL via `AddDbContextCheck`, registrado pela Infrastructure).
- Os comandos da Application são os próprios corpos de requisição (sem DTO duplicado na Api). IDs vindos da rota são
  aplicados com `command with { Id = id }` e o `Id` é `[JsonIgnore]` para não ser aceito pelo corpo.

## Consequências

- (+) Pouco código de cola, rotas legíveis lado a lado, composição em `Program.cs`.
- (+) Os endpoints são `internal` e não conhecem a Infrastructure (verificado por `Architecture.Tests`).
- (−) Sem validação declarativa de entrada: a validação é do domínio (value objects), que é a fonte única de verdade.
  Se uma API pública precisar de mensagens por campo, adicionar um *endpoint filter* na borda.
- (−) Expor os comandos da Application como contrato HTTP acopla o contrato público ao núcleo. Para uma API com
  versionamento/consumidores externos, introduza DTOs próprios na Api.

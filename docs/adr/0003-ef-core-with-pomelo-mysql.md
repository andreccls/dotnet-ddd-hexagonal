# ADR 0003 — EF Core + Pomelo (MySQL), repositório por agregado e UnitOfWork

- **Status:** aceito
- **Data:** 2026-10-06

## Contexto

É preciso persistir os agregados em MySQL. Opções: Dapper/SQL manual, EF Core com o provider **Pomelo**
(`Pomelo.EntityFrameworkCore.MySql` 9.x, compatível com EF Core 9/.NET 9) ou o provider oficial da Oracle.

## Decisão

- **EF Core 9 + Pomelo 9.0.0**, configuração **Fluent** por entidade (`IEntityTypeConfiguration<T>`), mantendo o
  domínio livre de atributos de mapeamento. Value objects mapeados com *value converters* (uma coluna cada).
- **Um repositório por agregado** (`ICustomerRepository`, ...) definido como porta na Application e implementado
  na Infrastructure (`internal`). **Não** há repositório genérico: cada porta expõe só as consultas que os casos de uso
  usam, sem vazar `IQueryable` para o núcleo.
- **`IUnitOfWork`** separado (`SaveChangesAsync`): os repositórios só registram intenção; a transação é uma por caso
  de uso. O `UnitOfWork` traduz erro de chave única do MySQL (1062) e `DbUpdateConcurrencyException` em `ConflictException`.
- **Migrations versionadas** em `Persistence/Migrations`, geradas com `make migration NAME=...`. A versão do servidor é
  explícita (`MySqlServerVersion 8.4`) para não abrir conexão só para detectá-la.
- Estoque é *concurrency token* (concorrência otimista); e-mail e SKU têm índice único como última linha de defesa.

## Consequências

- (+) Mapeamento produtivo, migrations prontas, testes de integração contra o MySQL real.
- (−) EF rastreia mudanças dentro do agregado, então "atualizar" não tem método no repositório (comportamento esperado
  de unit of work, mas surpreende quem vem de Dapper).
- (−) Pomelo é mantido pela comunidade e costuma atrasar alguns meses em relação a novas versões do EF Core.

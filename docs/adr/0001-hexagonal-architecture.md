# ADR 0001 — Arquitetura Hexagonal (Ports & Adapters) com DDD tático

- **Status:** aceito
- **Data:** 2026-10-06

## Contexto

O projeto quer ser uma referência de como manter regras de negócio isoladas de frameworks (HTTP, ORM, banco).
Num CRUD simples, o caminho mais curto é um controller falando direto com o `DbContext`, mas isso acopla regra de
negócio a infraestrutura, dificulta testar e faz o framework "vazar" para o domínio.

## Decisão

Adotar **Arquitetura Hexagonal** com quatro projetos: `Domain`, `Application`, `Infrastructure` e `Api`.

- `Domain` e `Application` formam o núcleo e não referenciam framework algum (Application só conhece Domain).
- `Application` declara as **portas**: de entrada (`IUseCase<,>`) e de saída (`I*Repository`, `IUnitOfWork`).
- `Api` (entrada) e `Infrastructure` (saída) são **adaptadores** que dependem do núcleo, nunca o contrário.
- A regra de dependência é verificada por `Architecture.Tests` (NetArchTest), para que a violação quebre o build.
- Agregados DDD (`Customer`, `Product`, `Order`) com fábricas estáticas, sem setters públicos, referenciando-se por id.

## Consequências

- (+) Núcleo testável sem banco nem HTTP: 100 % de cobertura em Domain/Application com fakes em memória.
- (+) Trocar MySQL por outro banco, ou HTTP por fila/CLI, é escrever outro adaptador.
- (−) Mais projetos e mais arquivos do que um CRUD "direto" exigiria; há mapeamento explícito entre entidades e DTOs.
  Aceitável porque o objetivo do repositório é justamente demonstrar a separação.

# Arquitetura

Este documento explica **como o código está organizado e por quê**, e termina com o passo a passo para usar o
projeto como **template** de um novo CRUD.

## 1. Visão geral

O projeto segue a **Arquitetura Hexagonal** (Ports & Adapters, Alistair Cockburn) com táticas de **DDD**:

- O **núcleo** (Domain + Application) contém as regras de negócio e **define** as interfaces de que precisa (portas).
- Os **adaptadores** (Api, Infrastructure) ficam nas bordas: traduzem o mundo externo (HTTP, MySQL) para o núcleo.
- O núcleo **nunca** depende de um adaptador. Quem depende é o adaptador (inversão de dependência).

| Projeto | Papel | Pode referenciar | Pacotes externos |
|---|---|---|---|
| `Domain` | Modelo de domínio: agregados, value objects, exceções de negócio | **nada** | **nenhum** |
| `Application` | Casos de uso; portas de **entrada** (`IUseCase`) e de **saída** (`IRepository`, `IUnitOfWork`); DTOs | `Domain` | nenhum |
| `Infrastructure` | Adaptador de **saída**: EF Core + MySQL, repositórios, `UnitOfWork`, migrations | `Application` (→ `Domain`) | EF Core, Pomelo |
| `Api` | Adaptador de **entrada**: Minimal APIs, ProblemDetails, OpenAPI; *composition root* | `Application`; `Infrastructure` só em `Program.cs` | ASP.NET Core, Swagger UI |

### Regra de dependência

```
Api ──► Application ──► Domain
Infrastructure ──► Application ──► Domain
```

Ela é **imposta por teste**, não por convenção: [`ArchitectureTests`](../tests/DddHexagonal.Architecture.Tests/ArchitectureTests.cs)
(NetArchTest) verifica, entre outras coisas, que

- `Domain` não referencia nenhum outro projeto/framework (nem no nível de tipos, nem nas referências do assembly);
- `Application` não usa EF Core, ASP.NET, `Infrastructure` nem `Api`;
- `Infrastructure` não conhece `Api`; endpoints não tocam `Infrastructure`;
- cada porta `I*Repository` tem **exatamente uma** implementação, e ela está em `Infrastructure`;
- agregados são `sealed`, sem construtor público e sem setters públicos; value objects são `sealed` e imutáveis;
- todo caso de uso é `sealed` e implementa `IUseCase<,>`.

## 2. Fluxo de uma requisição

Exemplo: `POST /orders` (criar pedido).

```mermaid
sequenceDiagram
    autonumber
    participant C as Cliente HTTP
    participant E as Api · OrderEndpoints
    participant U as Application · CreateOrderUseCase
    participant D as Domain · Customer / Product / Order
    participant R as Infrastructure · repositórios + UnitOfWork
    participant DB as MySQL

    C->>E: POST /orders {customerId, items[]}
    E->>U: ExecuteAsync(CreateOrderCommand)
    U->>R: ICustomerRepository.GetByIdAsync
    R->>DB: SELECT
    U->>D: customer.EnsureActive()
    U->>R: IProductRepository.GetByIdsAsync
    U->>D: Order.Create(...) valida itens (qtd > 0, sem duplicados)
    U->>D: product.AdjustStock(-qtd) (reserva; nunca negativo)
    U->>R: IOrderRepository.Add(order)
    U->>R: IUnitOfWork.SaveChangesAsync()
    R->>DB: INSERT order + items, UPDATE products (1 transação)
    U-->>E: OrderResponse
    E-->>C: 201 Created
    Note over E,C: Se qualquer passo lançar DomainException,<br/>DomainExceptionHandler responde 400/404/409 (ProblemDetails)
```

1. **Api** (`Endpoints/*.cs`) só faz *binding* HTTP → comando e devolve o resultado como HTTP. Zero regra de negócio.
2. **Application** (`Orders/CreateOrder.cs`) orquestra: carrega agregados pelas portas, chama os métodos do domínio,
   coordena vários agregados e confirma tudo com **um** `IUnitOfWork.SaveChangesAsync()`.
3. **Domain** (`Orders/Order.cs`) protege as invariantes. É impossível obter um `Order` inválido pela API pública.
4. **Infrastructure** persiste. O EF Core rastreia as mudanças feitas nos agregados; `Add`/`Remove` dos repositórios
   apenas registram a intenção e o `UnitOfWork` grava (e traduz violação de índice único / concorrência em `409`).

### Tradução de erros

| Origem | Exceção | HTTP |
|---|---|---|
| Valor inválido, regra violada (`Email.Create("x")`, estoque insuficiente, pedido confirmado) | `DomainException` | 400 |
| Agregado inexistente | `NotFoundException` | 404 |
| E-mail/SKU duplicado, alteração concorrente, excluir algo referenciado | `ConflictException` | 409 |
| JSON malformado | `BadHttpRequestException` | 400 |
| Qualquer outra | — | 500 (sem vazar detalhes) |

O mapeamento é feito **uma vez**, em `Api/DomainExceptionHandler.cs`, como `ProblemDetails` (RFC 7807).

## 3. Decisões de modelagem que valem a pena notar

- **Agregados referenciam outros por id** (`Order.CustomerId`, `OrderItem.ProductId`), nunca por objeto: cada agregado é
  carregado e salvo isoladamente e não há grafo gigante carregado "sem querer".
- **Regras entre agregados vivem na Application** (cliente ativo, reserva de estoque): nenhum agregado conhece o outro.
  Como não há FK entre eles no banco, a regra "não excluir cliente/produto com pedidos" também está nos casos de uso.
- **Total do pedido é derivado** (`Order.Total` soma os itens): nunca fica dessincronizado.
- **Preço no item é um *snapshot***: mudar o preço do produto não altera pedidos antigos.
- **Value objects** (`Email`, `Cpf`, `Phone`, `Sku`, `Money`) validam na criação e são persistidos com *value converters*
  (uma coluna cada). Nada de string solta circulando pelo domínio.
- **Concorrência otimista no estoque** (`Product.Stock` é *concurrency token*): dois pedidos simultâneos não "vendem" o
  mesmo estoque duas vezes; o segundo recebe `409` e pode tentar de novo.
- **Eventos de domínio** foram omitidos de propósito (nada os consome hoje). Próximo passo natural: o `AggregateRoot`
  acumular eventos e o `UnitOfWork` publicá-los depois do commit.

## 4. Estratégia de testes (pirâmide)

| Projeto | O que prova | Como | Meta |
|---|---|---|---|
| `Domain.Tests` | Regras de negócio | xUnit puro, escrito em TDD (teste vermelho → verde) | 100 % linhas e branches (gate) |
| `Application.Tests` | Orquestração dos casos de uso | Fakes em memória das portas (sem framework de mock) | 100 % linhas e branches (gate) |
| `Infrastructure.Tests` | Mapeamento EF, migrations, índices únicos, concorrência | MySQL real | relatório |
| `Api.Tests` | Contrato HTTP ponta a ponta dos 3 CRUDs | `WebApplicationFactory` + MySQL real | relatório |
| `Architecture.Tests` | Regra de dependência e convenções | NetArchTest + reflexão | sempre verde |

Os gates de 100 % ficam em `scripts/coverage.sh` (Coverlet com `Threshold`), usado por `make coverage` e pelo CI.

## 5. Como adicionar um novo CRUD (passo a passo)

Exemplo: **Suppliers** (fornecedores: `Name`, `Cnpj`, `Active`). Siga a ordem — ela é a ordem do TDD e das dependências
(de dentro para fora).

### 1) Domain — o modelo e suas regras (TDD)

1. Crie `src/DddHexagonal.Domain/Suppliers/`.
2. **Escreva primeiro** `tests/DddHexagonal.Domain.Tests/Suppliers/SupplierTests.cs` (e `CnpjTests.cs`) descrevendo as
   regras; veja-os falharem (nem compila ainda: é o "vermelho").
3. Implemente `Cnpj : ValueObject` (fábrica estática `Create`, valida e lança `DomainException`) e
   `Supplier : AggregateRoot` (construtor privado + `Create(...)` estático, setters privados, métodos que
   protegem as invariantes, construtor privado sem parâmetros marcado com `[ExcludeFromCodeCoverage]` para o EF).
4. `make test` até ficar verde. Mantenha 100 %.

### 2) Application — casos de uso e portas

1. Porta de saída: `Ports/Out/ISupplierRepository.cs` (`GetByIdAsync`, `ListAsync(PageQuery)`, consultas de unicidade,
   `Add`, `Remove`). Só o que os casos de uso realmente usam (YAGNI).
2. Em `Suppliers/`, **um arquivo por caso de uso**, cada um com seu record de comando/consulta e a classe
   `XxxUseCase : IUseCase<TRequest, TResponse>` (`CreateSupplier.cs`, `GetSupplier.cs`, `ListSuppliers.cs`,
   `UpdateSupplier.cs`, `DeleteSupplier.cs`) + `SupplierResponse.cs` (DTO com `From(Supplier)`).
   Comandos com `Id` vindo da rota usam `[JsonIgnore] public Guid Id { get; init; }`.
3. Teste em `Application.Tests/Suppliers/` usando um `InMemorySupplierRepository` (acrescente em `Fakes/Fakes.cs`).
   O gate de 100 % cobra todos os ramos (inclusive "não encontrado" e "conflito").

Não é preciso registrar nada em DI: `UseCaseRegistration` encontra `IUseCase<,>` por convenção.

### 3) Infrastructure — o adaptador de saída

1. `Persistence/Configurations/SupplierConfiguration.cs` (`IEntityTypeConfiguration<Supplier>`): tabela, chave com
   `ValueGeneratedNever()`, *value converters* dos VOs, índices únicos. É descoberta automaticamente.
2. Adicione `DbSet<Supplier>` em `AppDbContext` e crie `Persistence/Repositories/SupplierRepository.cs`
   (`internal sealed`, implementa `ISupplierRepository`).
3. Registre em `DependencyInjection.AddInfrastructure`: `services.AddScoped<ISupplierRepository, SupplierRepository>();`
4. Gere a migration: `make migration NAME=AddSuppliers` e **revise o SQL** antes de commitar.
5. Teste de integração em `Infrastructure.Tests` (round-trip dos VOs, índice único → `ConflictException`).

### 4) Api — o adaptador de entrada

1. `Endpoints/SupplierEndpoints.cs` copiando `CustomerEndpoints.cs` (um `MapGroup("/suppliers")`, cada rota chama
   `IUseCase<,>` e devolve `Results.*`; use `.Produces<>()`/`.ProducesProblem()` para documentar no OpenAPI).
2. Adicione `app.MapSupplierEndpoints();` em `Program.cs`.
3. Teste ponta a ponta em `Api.Tests` (ciclo de vida completo + erros 400/404/409).

### 5) Confira

```bash
make coverage     # gates 100 % em Domain/Application + testes de arquitetura (o ArchitectureTests já cobre o novo CRUD)
```

O teste `EveryRepositoryPort_HasItsAdapterInInfrastructure_AndNowhereElse` hoje espera **3** portas de repositório:
ao adicionar a quarta, atualize esse número (é de propósito: força você a notar que o hexágono cresceu).

### Checklist rápido

- [ ] Teste de domínio escrito **antes** e visto falhando
- [ ] Sem setters públicos / sem `new` direto: fábrica estática
- [ ] Porta definida na Application, adaptador na Infrastructure
- [ ] Nenhuma regra de negócio no endpoint nem no repositório
- [ ] Migration gerada e revisada
- [ ] `make coverage` verde

# ADR 0002 — Sem MediatR: uma porta de entrada simples por caso de uso

- **Status:** aceito
- **Data:** 2026-10-06

## Contexto

É comum ver CQRS com MediatR em projetos .NET. Aqui há ~20 casos de uso, sem *pipeline behaviors*, sem
notificações e sem necessidade de modelos de leitura separados. MediatR (hoje comercial nas versões novas) e um
mediador em geral adicionam indireção (a chamada deixa de ser navegável com "ir para definição") e uma dependência.

## Decisão

Cada caso de uso é uma classe que implementa a porta de entrada genérica:

```csharp
public interface IUseCase<in TRequest, TResponse>
{
    Task<TResponse> ExecuteAsync(TRequest request, CancellationToken cancellationToken = default);
}
```

- O endpoint injeta exatamente o caso de uso de que precisa (`IUseCase<CreateOrderCommand, OrderResponse>`).
- Registro por convenção em `Api/UseCaseRegistration.cs` (um laço sobre o assembly), sem listar 20 linhas de DI.
- Erros são exceções (`DomainException` e derivadas), traduzidas uma única vez em `ProblemDetails`.
- Sem AutoMapper: `Response.From(entidade)` é um método estático explícito.

## Consequências

- (+) Zero dependência extra, fluxo rastreável, testes triviais (instanciar a classe e chamar `ExecuteAsync`).
- (+) Interface mínima → Segregação de Interfaces; novos casos de uso não alteram código existente (Aberto/Fechado).
- (−) Preocupações transversais (log, transação, validação) não têm *pipeline* pronto. Hoje a transação é o
  `IUnitOfWork` e a validação mora nos value objects. Se surgirem muitas, um decorator sobre `IUseCase<,>` resolve
  sem trazer um mediador.
